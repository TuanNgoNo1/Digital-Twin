# Modbus RTU Gateway for Bai 2

Read-only Windows gateway for the PLC telemetry channel on COM5.

## Defaults

```text
Serial port: COM5
Protocol: Modbus RTU
Baud rate: 38400
Data format: 8-N-1
Slave ID: 3
HTTP: http://127.0.0.1:5002
```

The defaults follow the Factory Automation FX3U example that initializes the PLC
with `MOV H40A1 D8120` and `MOV K3 D8121`.

## Build

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Build-ModbusRtuGateway.ps1
```

## Read D500 once

```powershell
.\bin\ModbusRtuGateway.exe test D500
```

## Start HTTP gateway

```powershell
.\bin\ModbusRtuGateway.exe
```

Endpoints:

```text
GET  /health
GET  /telemetry
GET  /debug
POST /control     always returns HTTP 423 because the gateway is read-only
```

Environment overrides:

```text
MODBUS_SERIAL_PORT
MODBUS_BAUD_RATE
MODBUS_SLAVE_ID
MODBUS_TIMEOUT_MS
MODBUS_HTTP_HOST
MODBUS_HTTP_PORT
```

Only one process may own COM5. Stop the test utility before starting the HTTP gateway.
