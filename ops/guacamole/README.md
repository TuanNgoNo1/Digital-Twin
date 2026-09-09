# PLC Guacamole Gateway

This folder runs Apache Guacamole for the PLC/GX Works2 remote lab.

Target URLs:

```text
Local test:
http://127.0.0.1:8081/gxworks2/

Public through existing Caddy:
http://103.238.69.131:8080/gxworks2/
```

The student app remains on the existing route:

```text
http://103.238.69.131:8080/
```

## Runtime Layout

```text
Caddy :8080
  /             -> existing student app at 127.0.0.1:8080
  /gxworks2/    -> Guacamole at 127.0.0.1:8081/gxworks2/

Guacamole
  guacamole web -> guacd -> RDP -> Windows user plc_student -> GX Works2
```

## First Run

Run from PowerShell:

```powershell
cd D:\MIGRATION_2026-06-29\Windows_Readable\Digital-Twin-main\ops\guacamole
.\Initialize-Guacamole.ps1
```

If WSL/Docker was just installed and Windows requested a restart, do not run the stack until after that restart. After the restart, run:

```powershell
cd D:\MIGRATION_2026-06-29\Windows_Readable\Digital-Twin-main\ops\guacamole
.\Post-Reboot-Start.ps1
```

Then open:

```text
http://127.0.0.1:8081/gxworks2/
```

Guacamole administrator login:

```text
username: guacadmin
password: changed during installation on 2026-07-12
```

Do not restore the public default password `guacadmin`.

## RDP Connection Settings

The following Guacamole RDP RemoteApp connection was created during installation:

```text
Name: GX Works2 - PLC Server
Protocol: RDP
Hostname: host.docker.internal
Port: 3389
Username: plc_student
Security mode: Any / NLA
Ignore server certificate: enabled
RemoteApp: ||GXWorks2
RemoteApp directory: C:\Program Files (x86)\MELSOFT\GPPW2
```

The Windows password is not stored. Enter the `plc_student` password when
testing, or store it in the connection only if the deployment owner accepts
that risk.

RemoteApp Tool 6.1.0.0 is installed in:

```text
C:\Program Files (x86)\RemoteApp Tool
```

The Windows RemoteApp alias `GXWorks2` points to:

```text
C:\Program Files (x86)\MELSOFT\GPPW2\GD2.EXE
```

When remote GX Works2 mode is active, `/plc/health` returns 502 because the
normal Python gateway has released `COM3`. This is expected; GX Works2 and the
HTTP PLC gateway cannot own the same serial cable simultaneously.

Before GX Works2 needs the real PLC, release `COM3` from the normal HTTP gateway:

```powershell
cd D:\MIGRATION_2026-06-29\Windows_Readable\Digital-Twin-main\ops\server-control
.\Prepare-GXWorksRemoteMode.ps1
```

For the complete student/admin lifecycle, the separate `PLCLogoff` RemoteApp,
and the planned web-triggered mode automation, see
[`../../HUONG_DAN_GUACAMOLE_REMOTEAPP_VA_CHUYEN_MODE.md`](../../HUONG_DAN_GUACAMOLE_REMOTEAPP_VA_CHUYEN_MODE.md).
