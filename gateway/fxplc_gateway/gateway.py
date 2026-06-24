#!/usr/bin/env python3
"""HTTP gateway compatible with PiGatewayHsl, backed by the MIT fxplc driver."""

import asyncio
import json
import os
import threading
import time
from datetime import datetime, timezone
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from typing import Any, Coroutine

from fxplc.client.FXPLCClient import FXPLCClient
from fxplc.transports.TransportSerial import TransportSerial


SERIAL_PORT = os.getenv(
    "FXPLC_SERIAL_PORT", "/dev/serial/by-id/usb-1a86_USB_Serial-if00-port0"
)
HTTP_HOST = os.getenv("FXPLC_HTTP_HOST", "127.0.0.1")
HTTP_PORT = int(os.getenv("FXPLC_HTTP_PORT", "5001"))
ALLOW_WRITES = os.getenv("FXPLC_ALLOW_WRITES", "0") == "1"
PULSE_SECONDS = float(os.getenv("FXPLC_PULSE_SECONDS", "0.1"))

BIT_START = "M1"
BIT_FWD = "M2"
BIT_RUN_ROT = "M4"
BIT_RUN_ANGLE = "M5"
BIT_REV = "M8"
BIT_RESET_COUNTER = "M12"
BIT_RESET_ALL = "M13"
BIT_ERR_RESET = "M14"
BIT_SPEED_UP = "M15"
BIT_SPEED_DOWN = "M16"
BIT_STOP = "M17"

REG_PULSES = "D104"
REG_ROT = "D112"
REG_ANGLE = "D114"
REG_SPEED = "D128"


def utc_now() -> str:
    return datetime.now(timezone.utc).isoformat()


class PlcRuntime:
    def __init__(self) -> None:
        self.loop = asyncio.new_event_loop()
        self.thread = threading.Thread(target=self._run_loop, daemon=True)
        self.ready = threading.Event()
        self.client: FXPLCClient | None = None
        self.move_end_monotonic: float | None = None
        self.state: dict[str, Any] = {
            "runId": "",
            "lessonId": "TH1",
            "userId": "demo-user",
            "timestamp": utc_now(),
            "action": "",
            "running": False,
            "speedRpm": 0,
            "count": 0,
            "rotations": 0,
            "angle": 0,
            "direction": "forward",
            "backendSynced": False,
            "backendStatus": "STARTING",
        }

    def _run_loop(self) -> None:
        asyncio.set_event_loop(self.loop)
        self.loop.run_until_complete(self._connect())
        self.ready.set()
        self.loop.run_forever()

    async def _connect(self) -> None:
        transport = TransportSerial(SERIAL_PORT, baudrate=9600, timeout=1)
        self.client = FXPLCClient(transport)

    def start(self) -> None:
        self.thread.start()
        if not self.ready.wait(timeout=5):
            raise RuntimeError("PLC runtime did not initialize")

    def run(self, operation: Coroutine[Any, Any, Any], timeout: float = 15) -> Any:
        future = asyncio.run_coroutine_threadsafe(operation, self.loop)
        return future.result(timeout=timeout)

    def require_client(self) -> FXPLCClient:
        if self.client is None:
            raise RuntimeError("PLC client is not connected")
        return self.client

    def require_writes(self) -> None:
        if not ALLOW_WRITES:
            raise PermissionError("fxplc gateway is in read-only mode")

    async def read_int(self, register: str) -> int:
        return await self.require_client().read_int(register)

    async def read_bit(self, register: str) -> bool:
        return await self.require_client().read_bit(register)

    async def write_int(self, register: str, value: int) -> None:
        self.require_writes()
        await self.require_client().write_int(register, value)

    async def write_bit(self, register: str, value: bool) -> None:
        self.require_writes()
        await self.require_client().write_bit(register, value)

    async def pulse(self, register: str) -> None:
        await self.write_bit(register, True)
        await asyncio.sleep(PULSE_SECONDS)
        await self.write_bit(register, False)

    async def pulse_direction(self, direction: str) -> None:
        forward = direction.lower() != "reverse"
        self.state["direction"] = "forward" if forward else "reverse"
        await self.pulse(BIT_FWD if forward else BIT_REV)

    async def telemetry(self) -> dict[str, Any]:
        errors: list[str] = []
        try:
            self.state["speedRpm"] = await self.read_int(REG_SPEED)
        except Exception as exc:
            errors.append(f"{REG_SPEED}: {exc}")
        try:
            self.state["count"] = await self.read_int(REG_PULSES)
        except Exception as exc:
            errors.append(f"{REG_PULSES}: {exc}")

        if (
            self.state["running"]
            and self.move_end_monotonic is not None
            and time.monotonic() >= self.move_end_monotonic
        ):
            self.state["running"] = False
            self.move_end_monotonic = None

        self.state["timestamp"] = utc_now()
        self.state["backendSynced"] = not errors
        self.state["backendStatus"] = "SYNCED" if not errors else "; ".join(errors)
        return self.state

    async def debug(self) -> dict[str, Any]:
        result: dict[str, Any] = {}
        int_registers = {
            "D128_speedSet": "D128",
            "D104_pulses": "D104",
            "D112_rotationsSet": "D112",
            "D114_angleSet": "D114",
            "D146_angleSet": "D146",
            "D164_encPer100ms": "D164",
        }
        bit_registers = {
            "M1_run": "M1",
            "M2_fwd": "M2",
            "M8_rev": "M8",
            "M12_rstCnt": "M12",
            "M13_rstAll": "M13",
            "M14_errRst": "M14",
            "M17_stop": "M17",
            "M8029_done": "M8029",
        }
        for key, register in int_registers.items():
            result[key] = await self._debug_value(self.read_int(register))
        for key, register in bit_registers.items():
            result[key] = await self._debug_value(self.read_bit(register))
        return result

    async def _debug_value(self, operation: Coroutine[Any, Any, Any]) -> dict[str, Any]:
        try:
            return {"ok": True, "val": await operation, "msg": "Success"}
        except Exception as exc:
            return {"ok": False, "val": None, "msg": str(exc)}

    async def control(self, command: dict[str, Any]) -> dict[str, Any]:
        self.require_writes()
        action = str(command.get("action", "")).upper()
        self.state["action"] = command.get("action", "")
        self.state["timestamp"] = utc_now()
        for key in ("runId", "lessonId", "userId"):
            if command.get(key):
                self.state[key] = command[key]

        if action == "ON":
            await self.pulse_direction(str(command.get("direction", "forward")))
            rotations = float(command.get("rotations", 0))
            angle = float(command.get("angle", 0))
            if rotations > 0:
                self.state["rotations"] = rotations
                await self.write_int(REG_ROT, int(rotations))
                await self.pulse(BIT_RUN_ROT)
            elif angle > 0:
                self.state["angle"] = angle
                await self.write_int(REG_ANGLE, int(angle))
                await self.pulse(BIT_RUN_ANGLE)
            await self.pulse(BIT_START)
            self.state["running"] = True
            frequency = await self.read_int(REG_SPEED)
            pulses = rotations * 5000 + angle * 5000 / 360
            self.move_end_monotonic = (
                time.monotonic() + pulses / frequency + 0.5
                if frequency > 0 and pulses > 0
                else None
            )
        elif action == "OFF":
            await self.write_bit(BIT_START, False)
            await self.pulse(BIT_STOP)
            self.state["running"] = False
            self.move_end_monotonic = None
        elif action in ("SPEED_UP", "SPEED_DOWN"):
            count = max(1, int(float(command.get("speed", 1))))
            register = BIT_SPEED_UP if action == "SPEED_UP" else BIT_SPEED_DOWN
            for _ in range(count):
                await self.pulse(register)
        elif action == "SET_ROTATIONS":
            value = float(command.get("rotations", 0))
            self.state["rotations"] = value
            await self.write_int(REG_ROT, int(value))
        elif action == "SET_ANGLE":
            value = float(command.get("angle", 0))
            self.state["angle"] = value
            await self.write_int(REG_ANGLE, int(value))
        elif action == "SET_DIRECTION":
            await self.pulse_direction(str(command.get("direction", "forward")))
        elif action == "RESET_COUNTER":
            await self.pulse(BIT_RESET_COUNTER)
        elif action == "RESET":
            await self.pulse(BIT_RESET_ALL)
        elif action == "ERR_RESET":
            await self.pulse(BIT_ERR_RESET)
        else:
            raise ValueError(f"Unsupported action: {action}")
        return self.state


runtime = PlcRuntime()


class GatewayHandler(BaseHTTPRequestHandler):
    server_version = "FxPlcGateway/1.0"

    def do_OPTIONS(self) -> None:
        self.respond(204, None)

    def do_GET(self) -> None:
        path = self.path.rstrip("/")
        try:
            if path == "/health":
                self.respond(
                    200,
                    {
                        "gateway": "fxplc",
                        "allowWrites": ALLOW_WRITES,
                        "serialPort": SERIAL_PORT,
                    },
                )
            elif path == "/telemetry":
                self.respond(200, runtime.run(runtime.telemetry()))
            elif path == "/debug":
                self.respond(200, runtime.run(runtime.debug(), timeout=30))
            else:
                self.respond(404, {"error": "not found"})
        except Exception as exc:
            self.respond(503, {"error": str(exc), "gateway": "fxplc"})

    def do_POST(self) -> None:
        path = self.path.rstrip("/")
        try:
            length = int(self.headers.get("Content-Length", "0"))
            body = json.loads(self.rfile.read(length) or b"{}")
            if path == "/control":
                self.respond(200, runtime.run(runtime.control(body), timeout=60))
            else:
                self.respond(404, {"error": "not found"})
        except PermissionError as exc:
            self.respond(423, {"error": str(exc), "gateway": "fxplc"})
        except Exception as exc:
            self.respond(503, {"error": str(exc), "gateway": "fxplc"})

    def log_message(self, format_string: str, *args: Any) -> None:
        print(f"{self.address_string()} - {format_string % args}", flush=True)

    def respond(self, status: int, payload: Any) -> None:
        body = b"" if payload is None else json.dumps(payload).encode("utf-8")
        self.send_response(status)
        self.send_header("Access-Control-Allow-Origin", "*")
        self.send_header("Access-Control-Allow-Methods", "GET, POST, OPTIONS")
        self.send_header("Access-Control-Allow-Headers", "Content-Type")
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        if body:
            self.wfile.write(body)


def main() -> None:
    runtime.start()
    print(
        f"fxplc gateway on http://{HTTP_HOST}:{HTTP_PORT}; "
        f"serial={SERIAL_PORT}; allow_writes={ALLOW_WRITES}",
        flush=True,
    )
    ThreadingHTTPServer((HTTP_HOST, HTTP_PORT), GatewayHandler).serve_forever()


if __name__ == "__main__":
    main()
