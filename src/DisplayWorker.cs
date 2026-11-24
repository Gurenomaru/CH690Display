using System;
using System.Diagnostics;
using System.Threading;
using LibreHardwareMonitor.Hardware;

namespace CH690Display
{
    internal sealed class DisplayWorker : IDisposable
    {
        private readonly ManualResetEvent stop = new ManualResetEvent(false);
        private readonly AutoResetEvent changed = new AutoResetEvent(false);
        private readonly Thread thread;
        private volatile DisplaySettings settings;

        public DisplaySettings Settings { get { return settings; } }

        public DisplayWorker(DisplaySettings settings)
        {
            this.settings = settings;
            thread = new Thread(Run) { IsBackground = true, Name = "CH690 display polling" };
        }

        public void Start() { thread.Start(); }
        public void Stop() { stop.Set(); }

        public void ChangeSettings(DisplaySettings value)
        {
            settings = value;
            changed.Set();
        }

        private void Run()
        {
            try
            {
                using (var display = new UsbDisplay())
                using (var sensors = new Sensors())
                {
                    var offPacket = new DisplayPacket();
                    var dataPacket = new DisplayPacket();
                    offPacket.Write(DisplayPage.Off, default(Telemetry));
                    AppData.Log("Sensors initialized.");
                    bool connected = display.Connect();
                    AppData.Log(connected ? "Display opened." : "Display not found.");

                    for (int attempt = 0; connected && attempt < 4 && !stop.WaitOne(0); attempt++)
                    {
                        display.Send(offPacket.Buffer, 500);
                        if (stop.WaitOne(600))
                            return;
                    }

                    var timer = new Stopwatch();
                    var signals = new WaitHandle[] { stop, changed };
                    DisplayPage? lastSentPage = null;
                    while (!stop.WaitOne(0))
                    {
                        timer.Restart();
                        DisplayPage page = settings.Page;
                        try
                        {
                            if (!connected)
                            {
                                connected = display.Connect();
                                if (connected)
                                    AppData.Log("Display opened.");
                            }

                            if (connected)
                            {
                                byte[] packet = offPacket.Buffer;
                                if (page != DisplayPage.Off)
                                {
                                    dataPacket.Write(page, sensors.Read());
                                    packet = dataPacket.Buffer;
                                }
                                int result = display.Send(packet, 1000);
                                if (result < 0)
                                {
                                    AppData.Log("USB write failed (" + result + "). Reopening display.");
                                    display.Disconnect();
                                    connected = false;
                                    lastSentPage = null;
                                }
                                else if (lastSentPage != page)
                                {
                                    AppData.Log("Display updated. Page: " + page + ".");
                                    lastSentPage = page;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            AppData.Log(ex.ToString());
                        }
                        int delay = (int)Math.Max(50, settings.IntervalMs - timer.ElapsedMilliseconds);
                        if (WaitHandle.WaitAny(signals, delay) == 0)
                            return;
                    }
                }
            }
            catch (Exception ex)
            {
                AppData.Log(ex.ToString());
            }
        }

        public void Dispose()
        {
            Stop();
            thread.Join();
            stop.Dispose();
            changed.Dispose();
        }
    }

    internal struct Telemetry
    {
        public float CpuPower, CpuTemperature, CpuLoad, CpuClock;
        public float GpuPower, GpuTemperature, GpuLoad, GpuClock;
    }

    internal sealed class Sensors : IDisposable
    {
        private readonly Computer computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMotherboardEnabled = true
        };

        private struct Reading
        {
            public float Value;
            private HardwareType hardware;
            private string name;

            public void KeepFirst(HardwareType type, string sensorName, float value)
            {
                // The Python dictionary replaces repeated keys while preserving their order.
                if (name == null || (hardware == type && name == sensorName))
                {
                    name = sensorName;
                    hardware = type;
                    Value = value;
                }
            }
        }

        private struct Readings
        {
            public Reading CpuTctl, CpuCcd, CpuPower, CpuLoad;
            public Reading GpuPower, GpuTemperature, GpuLoad, GpuClock;
            public float CpuClock;
        }

        public Sensors() { computer.Open(); }

        public Telemetry Read()
        {
            var readings = new Readings();
            foreach (IHardware hardware in computer.Hardware)
                Update(hardware, ref readings);
            return new Telemetry
            {
                CpuPower = readings.CpuPower.Value,
                CpuTemperature = readings.CpuTctl.Value != 0 ? readings.CpuTctl.Value : readings.CpuCcd.Value,
                CpuLoad = readings.CpuLoad.Value,
                CpuClock = readings.CpuClock,
                GpuPower = readings.GpuPower.Value,
                GpuTemperature = readings.GpuTemperature.Value,
                GpuLoad = readings.GpuLoad.Value,
                GpuClock = readings.GpuClock.Value
            };
        }

        private static void Update(IHardware hardware, ref Readings readings)
        {
            hardware.Update();
            HardwareType type = hardware.HardwareType;
            foreach (ISensor sensor in hardware.Sensors)
            {
                float? value = sensor.Value;
                if (!value.HasValue)
                    continue;

                string name = sensor.Name;
                bool nvidia = type == HardwareType.GpuNvidia;
                switch (sensor.SensorType)
                {
                    case SensorType.Temperature:
                        if (Contains(name, "Tctl")) readings.CpuTctl.KeepFirst(type, name, value.Value);
                        if (Contains(name, "CCD1")) readings.CpuCcd.KeepFirst(type, name, value.Value);
                        if (nvidia && Contains(name, "GPU Core")) readings.GpuTemperature.KeepFirst(type, name, value.Value);
                        break;
                    case SensorType.Load:
                        if (Contains(name, "CPU Total")) readings.CpuLoad.KeepFirst(type, name, value.Value);
                        if (nvidia && Contains(name, "GPU Core")) readings.GpuLoad.KeepFirst(type, name, value.Value);
                        break;
                    case SensorType.Clock:
                        if (Contains(name, "Core #")) readings.CpuClock = Math.Max(readings.CpuClock, value.Value);
                        if (nvidia && Contains(name, "GPU Core")) readings.GpuClock.KeepFirst(type, name, value.Value);
                        break;
                    case SensorType.Power:
                        if (type == HardwareType.Cpu && Contains(name, "Package")) readings.CpuPower.KeepFirst(type, name, value.Value);
                        if (nvidia && Contains(name, "GPU Package")) readings.GpuPower.KeepFirst(type, name, value.Value);
                        break;
                }
            }
            foreach (IHardware subHardware in hardware.SubHardware)
                Update(subHardware, ref readings);
        }

        private static bool Contains(string name, string fragment)
        {
            return name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public void Dispose() { computer.Close(); }
    }
}
