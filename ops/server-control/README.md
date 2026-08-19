# Server Control Notes

This machine currently has two lab stacks plus remote-access services.

The complete student/admin operating procedure is documented in
[`../../HUONG_DAN_GUACAMOLE_REMOTEAPP_VA_CHUYEN_MODE.md`](../../HUONG_DAN_GUACAMOLE_REMOTEAPP_VA_CHUYEN_MODE.md).

## Current Groups

```text
Web stack
  Caddy             -> ports 80 and 10.170.43.240:8080
  Spring Boot Java  -> 127.0.0.1:8080
  PLC HTTP gateway  -> 127.0.0.1:5000
  Camera snapshot   -> ffmpeg writes camera_www/snapshot.jpg

Pixel Streaming stack
  TURN              -> 10.170.43.240:19303
  Node/Wilbur       -> 8090, 8888, 8889
  PlcBridge         -> 9090 (only when it owns the PLC serial port)
  Unreal PLC.exe    -> pixel-streamed digital twin

GX Works stack
  GX Works2         -> GD2.exe
  MELSOFT services  -> Windows services from Mitsubishi tooling

Remote access
  Tailscale
  RustDesk
  StarDesk
  UltraViewer
  Remote Desktop
```

## Status

```powershell
cd D:\MIGRATION_2026-06-29\Windows_Readable\Digital-Twin-main\ops\server-control
.\Status-ServerStack.ps1
```

## Start After Reboot

Fast path: after logging in, double-click:

```text
D:\MIGRATION_2026-06-29\Windows_Readable\Digital-Twin-main\POST-REBOOT-RUN-THIS.bat
```

This starts:

```text
1. Current web/student stack
2. Current Pixel Streaming stack
3. Bai 2 Guacamole/GX Works2 remote stack
4. Status check
```

Manual path: run these from PowerShell after logging in:

```powershell
cd D:\MIGRATION_2026-06-29\Windows_Readable\Digital-Twin-main\ops\server-control
.\Start-WebStack.ps1
.\Start-PixelStack.ps1
cd D:\MIGRATION_2026-06-29\Windows_Readable\Digital-Twin-main\ops\guacamole
.\Post-Reboot-Start.ps1
cd D:\MIGRATION_2026-06-29\Windows_Readable\Digital-Twin-main\ops\server-control
.\Status-ServerStack.ps1
```

## Stop Without Reboot

```powershell
cd D:\MIGRATION_2026-06-29\Windows_Readable\Digital-Twin-main\ops\server-control
.\Stop-PixelStack.ps1
.\Stop-WebStack.ps1
```

These scripts do not stop Tailscale, RustDesk, StarDesk, UltraViewer, RDP, MELSOFT services, or Mosquitto.

## PLC Serial Modes

The CH340 PLC cable is currently `COM3`. Only one process can own it at a time.
`Start-PixelStack.ps1` is pinned to CH340/COM3 and intentionally ignores the
FTDI/COM5 adapter reserved for Bai 2 telemetry.

Normal student-system mode uses the Python PLC gateway on port `5000`. In this
mode, `PlcBridge` port `9090` is expected to remain offline.

Before a remote GX Works2 session, run PowerShell as an administrator:

```powershell
cd D:\MIGRATION_2026-06-29\Windows_Readable\Digital-Twin-main\ops\server-control
.\Prepare-GXWorksRemoteMode.ps1
```

This releases `COM3` while leaving Caddy, the student web app, camera, Pixel
Streaming, RDP, and Guacamole online. Start the PLC gateway again after the
GX Works2 session with `Start-PlcGateway.ps1` from the repository root.

## Loopback Lab Session Controller

The optional controller in this folder exposes a token-protected API only on
`127.0.0.1:5010`. It is intended to be called by the authenticated Spring
backend, never directly by a browser or public Caddy route.

Install it from an elevated prompt with:

```powershell
.\Run-InstallLabSessionController.ps1
```

It also starts a watchdog that returns COM3 to Gateway mode after a real
`plc_student` logoff. A disconnected session does not trigger that transition.
