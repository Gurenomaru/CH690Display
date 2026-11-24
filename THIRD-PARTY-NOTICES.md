# Third-party notices

The Windows distribution includes the following unmodified libraries.

| Component | Version | License and corresponding source |
| --- | --- | --- |
| LibreHardwareMonitorLib | 0.9.6 | MPL 2.0 and the [upstream third-party terms](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/blob/3d331e3370efb858411f19511373eff65a218701/THIRD-PARTY-NOTICES.txt). See the [corresponding source revision](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/tree/3d331e3370efb858411f19511373eff65a218701) and [NuGet package](https://www.nuget.org/packages/LibreHardwareMonitorLib/0.9.6). |
| libusb | 1.0.28 | GNU LGPL 2.1 or later. The Windows package includes the [upstream source archive](https://github.com/libusb/libusb/releases/tag/v1.0.28) under `third-party-source/`. |
| System.Buffers | 4.6.1 | MIT. [NuGet package](https://www.nuget.org/packages/System.Buffers/4.6.1). |
| System.Memory | 4.6.3 | MIT. [NuGet package](https://www.nuget.org/packages/System.Memory/4.6.3). |
| System.Numerics.Vectors | 4.6.1 | MIT. [NuGet package](https://www.nuget.org/packages/System.Numerics.Vectors/4.6.1). |
| System.Runtime.CompilerServices.Unsafe | 6.1.2 | MIT. [NuGet package](https://www.nuget.org/packages/System.Runtime.CompilerServices.Unsafe/6.1.2). |

The Windows archive includes the license texts and notices in `licenses/`. CH690 Display loads libusb as a separate DLL. It can be replaced with a compatible build of the library.
