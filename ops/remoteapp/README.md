# GX Works2 RemoteApp

RemoteApp Tool 6.1.0.0 is installed without requiring a reboot.

Registered Windows RemoteApps:

```text
Alias: GXWorks2
Name: GX Works2
Program: C:\Program Files (x86)\MELSOFT\GPPW2\GD2.EXE
Guacamole parameter: ||GXWorks2

Alias: PLCLogoff
Name: Ket thuc phien PLC
Program: C:\ProgramData\PDTwin\RemoteApp\EndPlcSession.exe
Guacamole parameter: ||PLCLogoff
```

The allow-list is enabled and client-provided command-line arguments are
disabled. Windows 11 Pro permits only one active RDP session, which matches the
single-PLC lab design.

`PLCLogoff` displays a confirmation dialog and then signs out the current
Windows session. It does not grant the student a desktop, shell, PowerShell,
or administrative rights. Register a second Guacamole RDP connection using
`||PLCLogoff` and grant the student Guacamole account READ permission to that
connection.

Installation and configuration helpers in this folder require UAC elevation
but do not change the machine-wide PowerShell execution policy.

Detailed student/admin procedures are in
[`../../HUONG_DAN_GUACAMOLE_REMOTEAPP_VA_CHUYEN_MODE.md`](../../HUONG_DAN_GUACAMOLE_REMOTEAPP_VA_CHUYEN_MODE.md).

## Student sample project

Run `PREPARE-PLC-STUDENT-PROJECT.bat` as an administrator to copy the known
sample project into:

```text
C:\PLC\Bai2.gxw
```

The `plc_student` account can modify files in `C:\PLC`. A clean admin-only copy
is retained under `C:\ProgramData\PDTwin\PLC-Templates`. In the GX Works2
RemoteApp, use `Project > Open` and open `C:\PLC\Bai2.gxw`. Do not publish
`explorer.exe` as a RemoteApp because it would expose a general Windows shell.
