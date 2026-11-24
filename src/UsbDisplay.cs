using System;
using System.Runtime.InteropServices;

namespace CH690Display
{
    internal sealed class DisplayPacket
    {
        // The protocol uses 64-byte interrupt reports with big-endian sensor values.
        public readonly byte[] Buffer = new byte[64];

        public DisplayPacket()
        {
            Buffer[0] = 0x10;
            Buffer[1] = 0x68;
            Buffer[2] = 0x01;
            Buffer[3] = 0x06;
            Buffer[4] = 0x23;
            Buffer[5] = 0x01;
            Buffer[41] = 0x16;
        }

        public void Write(DisplayPage page, Telemetry data)
        {
            Buffer[6] = (byte)page;
            if (page == DisplayPage.Off)
            {
                Array.Clear(Buffer, 7, 21);
            }
            else
            {
                WriteUInt16(7, data.CpuPower);
                WriteFloat(10, data.CpuTemperature);
                Buffer[14] = (byte)Rounded(data.CpuLoad, byte.MaxValue);
                WriteUInt16(15, data.CpuClock);
                WriteUInt16(19, data.GpuPower);
                WriteFloat(21, data.GpuTemperature);
                Buffer[25] = (byte)Rounded(data.GpuLoad, byte.MaxValue);
                WriteUInt16(26, data.GpuClock);
            }

            int checksum = 0;
            for (int index = 1; index < 40; index++)
                checksum += Buffer[index];
            Buffer[40] = (byte)(checksum & 0xff);
        }

        private void WriteUInt16(int offset, float value)
        {
            int rounded = Rounded(value, ushort.MaxValue);
            Buffer[offset] = (byte)(rounded >> 8);
            Buffer[offset + 1] = (byte)rounded;
        }

        private void WriteFloat(int offset, float value)
        {
            var bits = new FloatBits { Value = value };
            Buffer[offset] = (byte)(bits.Bits >> 24);
            Buffer[offset + 1] = (byte)(bits.Bits >> 16);
            Buffer[offset + 2] = (byte)(bits.Bits >> 8);
            Buffer[offset + 3] = (byte)bits.Bits;
        }

        private static int Rounded(float value, int maximum)
        {
            return float.IsNaN(value) ? 0 : (int)Math.Max(0, Math.Min(maximum, Math.Round(value)));
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct FloatBits
        {
            [FieldOffset(0)] public float Value;
            [FieldOffset(0)] public uint Bits;
        }
    }

    internal sealed class UsbDisplay : IDisposable
    {
        private const ushort VendorId = 0x3633;
        private const ushort ProductId = 0x001b;
        private const byte OutputEndpoint = 0x01;
        private const int InterfaceNumber = 0;
        private const int Configuration = 1;

        private IntPtr context;
        private IntPtr handle;
        private bool claimed;

        public UsbDisplay()
        {
            int result = Native.libusb_init(out context);
            if (result < 0)
                throw new InvalidOperationException("libusb initialization failed (" + result + ").");
        }

        public bool Connect()
        {
            handle = Native.libusb_open_device_with_vid_pid(context, VendorId, ProductId);
            if (handle == IntPtr.Zero)
                return false;

            // WinUSB may reject a configuration request when it already has this configuration.
            int activeConfiguration;
            if (Native.libusb_get_configuration(handle, out activeConfiguration) < 0 || activeConfiguration != Configuration)
                Native.libusb_set_configuration(handle, Configuration);
            return true;
        }

        public int Send(byte[] packet, uint timeoutMs)
        {
            if (!claimed)
            {
                int claimResult = Native.libusb_claim_interface(handle, InterfaceNumber);
                if (claimResult < 0)
                    return claimResult;
                claimed = true;
            }

            int transferred;
            int result = Native.libusb_interrupt_transfer(handle, OutputEndpoint,
                packet, packet.Length, out transferred, timeoutMs);
            if (result == 0 && transferred != packet.Length)
                return -1;
            return result;
        }

        public void Disconnect()
        {
            if (handle == IntPtr.Zero)
                return;
            if (claimed)
                Native.libusb_release_interface(handle, InterfaceNumber);
            Native.libusb_close(handle);
            claimed = false;
            handle = IntPtr.Zero;
        }

        public void Dispose()
        {
            Disconnect();
            Native.libusb_exit(context);
        }

        private static class Native
        {
            private const string Library = "libusb-1.0.dll";

            [DllImport(Library, ExactSpelling = true)]
            [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
            internal static extern int libusb_init(out IntPtr context);

            [DllImport(Library, ExactSpelling = true)]
            internal static extern void libusb_exit(IntPtr context);

            [DllImport(Library, ExactSpelling = true)]
            internal static extern IntPtr libusb_open_device_with_vid_pid(IntPtr context, ushort vendor, ushort product);

            [DllImport(Library, ExactSpelling = true)]
            internal static extern void libusb_close(IntPtr handle);

            [DllImport(Library, ExactSpelling = true)]
            internal static extern int libusb_get_configuration(IntPtr handle, out int configuration);

            [DllImport(Library, ExactSpelling = true)]
            internal static extern int libusb_set_configuration(IntPtr handle, int configuration);

            [DllImport(Library, ExactSpelling = true)]
            internal static extern int libusb_claim_interface(IntPtr handle, int interfaceNumber);

            [DllImport(Library, ExactSpelling = true)]
            internal static extern int libusb_release_interface(IntPtr handle, int interfaceNumber);

            [DllImport(Library, ExactSpelling = true)]
            internal static extern int libusb_interrupt_transfer(IntPtr handle, byte endpoint, [In] byte[] data,
                int length, out int transferred, uint timeout);
        }
    }
}
