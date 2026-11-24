using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace CH690Display
{
    internal enum DisplayPage : byte { Off = 0, Cpu = 2, Gpu = 4, Psu = 5, Psu2 = 6 }

    internal sealed class DisplaySettings
    {
        private static readonly string SettingsPath = Path.Combine(AppData.DirectoryPath, "settings.txt");
        public readonly DisplayPage Page;
        public readonly int IntervalMs;

        public DisplaySettings(DisplayPage page, int intervalMs)
        {
            Page = page;
            IntervalMs = intervalMs;
        }

        public static DisplaySettings Load()
        {
            DisplayPage page = DisplayPage.Gpu;
            int interval = 500;
            try
            {
                foreach (string line in File.ReadLines(SettingsPath))
                {
                    string[] entry = line.Split('=');
                    int value;
                    if (entry.Length != 2 || !int.TryParse(entry[1], out value))
                        continue;

                    if (entry[0] == "page" && value >= 0 && value <= byte.MaxValue
                        && Enum.IsDefined(typeof(DisplayPage), (byte)value))
                        page = (DisplayPage)value;
                    if (entry[0] == "interval_ms" && (value == 500 || value == 1000 || value == 3000))
                        interval = value;
                }
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
            catch (IOException ex) { AppData.Log(ex.Message); }
            catch (UnauthorizedAccessException ex) { AppData.Log(ex.Message); }
            return new DisplaySettings(page, interval);
        }

        public void Save()
        {
            Directory.CreateDirectory(AppData.DirectoryPath);
            string temporary = SettingsPath + ".tmp";
            try
            {
                File.WriteAllText(temporary, string.Format(CultureInfo.InvariantCulture,
                    "page={0}\r\ninterval_ms={1}\r\n", (byte)Page, IntervalMs));
                if (File.Exists(SettingsPath))
                    File.Replace(temporary, SettingsPath, null);
                else
                    File.Move(temporary, SettingsPath);
            }
            finally
            {
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }
        }
    }

    internal static class AppData
    {
        public static readonly string DirectoryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CH690Display");

        public static void Log(string message)
        {
            try
            {
                Directory.CreateDirectory(DirectoryPath);
                File.AppendAllText(Path.Combine(DirectoryPath, "ch690.log"),
                    DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss] ") + message + Environment.NewLine);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    internal sealed class TrayApplication : ApplicationContext
    {
        private readonly NotifyIcon tray;
        private readonly ContextMenuStrip menu = new ContextMenuStrip();
        private readonly Icon icon = CreateIcon();
        private readonly DisplayWorker worker;

        public TrayApplication(DisplaySettings settings)
        {
            worker = new DisplayWorker(settings);
            AddPage("CPU", DisplayPage.Cpu);
            AddPage("GPU", DisplayPage.Gpu);
            AddPage("PSU", DisplayPage.Psu);
            AddPage("PSU 2", DisplayPage.Psu2);
            AddPage("Turn Off Display", DisplayPage.Off);
            menu.Items.Add(new ToolStripSeparator());
            var updateRate = new ToolStripMenuItem("Update Interval");
            AddInterval(updateRate, "0.5 s", 500);
            AddInterval(updateRate, "1 s", 1000);
            AddInterval(updateRate, "3 s", 3000);
            menu.Items.Add(updateRate);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, (sender, args) => ExitThread());

            tray = new NotifyIcon { Text = "CH690 Display", Icon = icon, ContextMenuStrip = menu, Visible = true };
            AppData.Log(string.Format("Tray started. Page: {0}, interval: {1} ms.", settings.Page, settings.IntervalMs));
            worker.Start();
        }

        private void AddPage(string label, DisplayPage page)
        {
            var item = new ToolStripMenuItem(label);
            menu.Opening += (sender, args) => item.Checked = worker.Settings.Page == page;
            item.Click += (sender, args) => Set(new DisplaySettings(page, worker.Settings.IntervalMs));
            menu.Items.Add(item);
        }

        private void AddInterval(ToolStripMenuItem parent, string label, int interval)
        {
            var item = new ToolStripMenuItem(label);
            parent.DropDownOpening += (sender, args) => item.Checked = worker.Settings.IntervalMs == interval;
            item.Click += (sender, args) => Set(new DisplaySettings(worker.Settings.Page, interval));
            parent.DropDownItems.Add(item);
        }

        private void Set(DisplaySettings settings)
        {
            try
            {
                settings.Save();
                worker.ChangeSettings(settings);
            }
            catch (IOException ex)
            {
                ShowSaveError(ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                ShowSaveError(ex);
            }
        }

        private void ShowSaveError(Exception error)
        {
            AppData.Log("Could not save settings: " + error.Message);
            tray.ShowBalloonTip(4000, "CH690 Display", "Unable to save settings.", ToolTipIcon.Warning);
        }

        private static Icon CreateIcon()
        {
            using (var bitmap = new Bitmap(32, 32))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var background = new SolidBrush(Color.FromArgb(28, 34, 48)))
            using (var font = new Font("Segoe UI", 9, FontStyle.Bold, GraphicsUnit.Point))
            {
                graphics.FillRectangle(background, 3, 3, 26, 26);
                graphics.DrawRectangle(Pens.White, 3, 3, 26, 26);
                graphics.DrawString("DC", font, Brushes.White, 5, 7);
                IntPtr handle = bitmap.GetHicon();
                try
                {
                    using (Icon fromHandle = Icon.FromHandle(handle))
                        return (Icon)fromHandle.Clone();
                }
                finally { DestroyIcon(handle); }
            }
        }

        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern bool DestroyIcon(IntPtr handle);

        protected override void ExitThreadCore()
        {
            tray.Visible = false;
            worker.Stop();
            base.ExitThreadCore();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                tray.Dispose();
                worker.Dispose();
                menu.Dispose();
                icon.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    internal static class Program
    {
        [STAThread]
        private static int Main()
        {
            bool createdNew;
            using (var singleInstance = new Mutex(false, @"Local\CH690Display", out createdNew))
            {
                if (!createdNew)
                    return 0;

                try
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    using (var tray = new TrayApplication(DisplaySettings.Load()))
                        Application.Run(tray);
                    return 0;
                }
                catch (Exception ex)
                {
                    AppData.Log(ex.ToString());
                    return 1;
                }
            }
        }
    }
}
