# CH690 Display

A lightweight Windows tray utility for the built-in display on the DeepCool CH690 DIGITAL PC case. It uses LibreHardwareMonitor to read hardware sensors and libusb to send updates to the display.

- Switch between CPU, GPU, PSU, PSU 2, and display off.
- Choose an update interval of 0.5, 1, or 3 seconds. The utility saves the selected page and interval between launches.
- The default settings are the GPU page and a 0.5-second update interval.

## Run on Windows

1. Download `CH690Display-Windows-x64.zip` from [Releases](https://github.com/Gurenomaru/CH690Display/releases) and extract it to a folder.
2. Close any other utility that is driving the case display.
3. Run `CH690Display.exe` from the extracted folder with its DLLs alongside it. Some hardware sensors require running as administrator.
4. Right-click the CH690 Display icon in the notification area. Choose **Update Interval** to set how often the display refreshes.

Use 64-bit Windows with .NET Framework 4.8 or newer. The release was tested with a DeepCool CH690 DIGITAL display (USB VID `3633`, PID `001B`) using the standard Windows USB HID driver. GPU telemetry uses NVIDIA sensors, and CPU temperature uses AMD Ryzen Tctl or CCD1 sensors. Settings and logs are stored in `%LOCALAPPDATA%\CH690Display`.

To start the utility automatically, create a Task Scheduler task with an **At log on** trigger for your account. Set the action to run `CH690Display.exe` from the extracted folder. Choose **Run only when user is logged on** to show the tray icon and enable **Run with highest privileges** if your sensors require it.

## Build from source

Extract `CH690Display-Source.zip` and copy the DLLs from the Windows package to the directory containing `Build.ps1`. Run `.\Build.ps1` in PowerShell to compile `CH690Display.exe` with the Windows .NET Framework C# compiler.

See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) for dependency versions, licenses, and corresponding source links.
