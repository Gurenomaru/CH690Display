# CH690 Display

A lightweight C# utility that runs in the Windows notification area and drives the built-in display on the DeepCool CH690 DIGITAL case.

**[Download for Windows x64](https://github.com/Gurenomaru/CH690Display/releases/latest/download/CH690Display-Windows-x64.zip)** · [Release notes](https://github.com/Gurenomaru/CH690Display/releases/latest) · [Source ZIP](https://github.com/Gurenomaru/CH690Display/releases/latest/download/CH690Display-Source.zip)

## Quick start

1. Extract the Windows ZIP to a folder.
2. Close any other application that is driving the case display.
3. Run `CH690Display.exe` with the bundled DLLs in the same folder. Run as administrator if your hardware sensors require it.
4. Right-click the **CH690 Display** icon to choose CPU, GPU, PSU, PSU 2, or **Turn Off Display**. Use **Update Interval** to select 0.5, 1, or 3 seconds.

The app saves your display mode and interval between launches. On first launch, it opens the GPU page with a 0.5-second interval. Settings and logs are stored in `%LOCALAPPDATA%\CH690Display`.

To launch it when you sign in, create a Task Scheduler task with an **At log on** trigger and point its action at the extracted EXE. Select **Run only when user is logged on** to show the tray icon.

## Compatibility

| Item | Details |
| --- | --- |
| Windows | x64 with .NET Framework 4.8 or newer |
| Tested display | DeepCool CH690 DIGITAL with the standard Windows USB HID driver |
| Other DeepCool displays | Cases may work if their displays use USB VID `3633`, PID `001B`, and the same display protocol |
| Hardware readings | GPU telemetry uses NVIDIA sensors. CPU temperature uses AMD Ryzen Tctl or CCD1 sensors. |

## Build from source

Clone this repository or extract the source ZIP. Extract the Windows release separately and copy its DLLs next to `Build.ps1`. Then run the build script in PowerShell from that directory.

```powershell
.\Build.ps1
```

The script uses the Windows .NET Framework C# compiler and writes `CH690Display.exe` alongside the DLLs. See [third-party notices](THIRD-PARTY-NOTICES.md) for dependency versions, licenses, and source links.
