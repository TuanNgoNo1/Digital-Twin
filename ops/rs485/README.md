# Bai 2 - COM5 Modbus RTU

Luong 2 uses Modbus RTU only. The former Computer Link Format 1 test has been removed.

## PLC configuration in GX Works2

Make a backup copy of the GX Works2 project before changing the communication mode.

1. Open `PLC Parameter -> PLC System (2) -> CH1`.
2. Clear `Operate Communication Setting` so the old Computer Link parameter does not overwrite `D8120`.
3. Add these first-scan initialization instructions to the PLC program:

```text
M8002 ----[ MOV H40A1 D8120 ]
      ----[ MOV K3    D8121 ]
```

The settings used by the Factory Automation example are:

```text
Protocol: Modbus RTU slave
Baud rate: 38400
Data bits: 8
Parity: None
Stop bits: 1
Slave ID: 3
```

`H40A1` corresponds to the 38400/8-N-1 configuration used by the Python example.
The source article has one nearby label that says 9600, but its Python client uses 38400.

If `D8120` and `D8121` monitor correctly but the PLC never replies, verify the exact
CPU/board firmware. Mitsubishi's official FX3U-485-BD manual does not list native
Modbus RTU; this `H40A1` slave mode depends on compatible/clone firmware or matching
Modbus-capable hardware.

4. Compile the PLC project.
5. Put the PLC in STOP.
6. Write both `Parameter` and `Program` to the PLC.
7. Power-cycle the PLC itself.
8. Put the PLC in RUN.
9. Monitor `D8120` in hexadecimal and `D8121` in decimal.

Expected runtime values:

```text
D8120 = H40A1
D8121 = 3
```

Do not enable Computer Link and do not write `H6086` while using this mode.

## End-to-end read test

1. Keep GX Works2 connected through COM3.
2. In Device/Buffer Memory Batch Monitor, set `D500` to a recognizable value such as `1234`.
3. Run `TEST-RS485-COM5-READONLY.bat` from the repository root.

The batch file sends up to ten requests with a short pause so TXD/RXD activity is
visible. It stops immediately when a valid response is received.

Expected output:

```text
TX: 03 03 01 F4 00 01 C5 E6
D500 unsigned=1234, signed=1234
MODBUS_RTU_READ=PASS
```

The request is Modbus slave 3, function 03, holding register 500, quantity 1.
It does not modify PLC memory.

## Isolated DTech loopback test

Use this before changing the PLC program when the adapter's electrical transmit/receive
path is uncertain.

1. Power off the PLC.
2. Photograph and label the existing wires.
3. Disconnect the DTech terminal board completely from the PLC board.
4. On the DTech terminal board only, bridge `T/R+` to `RXD+` and `T/R-` to `RXD-`.
5. Leave `GND` disconnected for this isolated test.
6. Run `TEST-DTECH-COM5-LOOPBACK.bat`.

Expected result:

```text
DTECH_LOOPBACK=PASS
```

After the test, remove both loopback bridges before reconnecting the PLC wiring.

## HTTP gateway

Start `START-RS485-MODBUS-GATEWAY.bat` and keep its console window open.

```text
GET  http://127.0.0.1:5002/health
GET  http://127.0.0.1:5002/telemetry
GET  http://127.0.0.1:5002/debug
POST http://127.0.0.1:5002/control  -> HTTP 423
```

Only the gateway may own COM5. Close the one-shot test before starting the gateway.

## Address mapping

The Factory Automation mapping is direct:

```text
Holding register 500 = PLC D500
Holding register 100 = PLC D100
Coil 11 = PLC M11
```

The gateway reads D100 through D165 in one FC03 request and M0 through M17 in one
FC01 request. It exposes the existing Unity telemetry JSON shape without allowing writes.
