using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace FullPowerMode
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    internal sealed class MainForm : Form
    {
        private readonly PowerModeController controller = new PowerModeController();
        private readonly NotifyIcon trayIcon;
        private readonly ToggleSwitch toggle;
        private readonly Label statusLabel;
        private readonly Label titleLabel;
        private readonly Button hideButton;
        private readonly Button exitButton;
        private bool suppressToggle;
        private bool exitRequested;

        public MainForm()
        {
            Text = "Full Power Mode";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = true;
            ClientSize = new Size(440, 292);
            BackColor = Color.FromArgb(248, 249, 252);
            Font = new Font("Segoe UI", 9F);
            Icon = ImageResources.LoadIcon("AppLogo.png");

            PictureBox appLogo = new PictureBox();
            appLogo.Image = ImageResources.LoadBitmap("AppLogo.png");
            appLogo.Location = new Point(24, 19);
            appLogo.Size = new Size(38, 38);
            appLogo.SizeMode = PictureBoxSizeMode.Zoom;

            titleLabel = new Label();
            titleLabel.Text = "Full Power Mode";
            titleLabel.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold);
            titleLabel.ForeColor = Color.FromArgb(24, 28, 35);
            titleLabel.Location = new Point(72, 20);
            titleLabel.Size = new Size(340, 38);

            Label description = new Label();
            description.Text = "Enable Ultimate Performance, CPU max performance, responsiveness tweaks, and memory cache cleanup.";
            description.ForeColor = Color.FromArgb(88, 96, 110);
            description.Location = new Point(26, 64);
            description.Size = new Size(384, 42);

            Label switchLabel = new Label();
            switchLabel.Text = "Power boost";
            switchLabel.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
            switchLabel.ForeColor = Color.FromArgb(32, 38, 48);
            switchLabel.Location = new Point(26, 123);
            switchLabel.Size = new Size(160, 26);

            toggle = new ToggleSwitch();
            toggle.Location = new Point(330, 116);
            toggle.Size = new Size(74, 36);
            toggle.CheckedChanged += ToggleCheckedChanged;

            statusLabel = new Label();
            statusLabel.ForeColor = Color.FromArgb(74, 82, 96);
            statusLabel.Location = new Point(26, 160);
            statusLabel.Size = new Size(380, 28);

            hideButton = new Button();
            hideButton.Text = "Hide to tray";
            hideButton.Location = new Point(220, 198);
            hideButton.Size = new Size(95, 28);
            hideButton.Click += delegate { HideToTray(); };

            exitButton = new Button();
            exitButton.Text = "Exit";
            exitButton.Location = new Point(325, 198);
            exitButton.Size = new Size(80, 28);
            exitButton.Click += delegate
            {
                exitRequested = true;
                Close();
            };

            Panel footer = new Panel();
            footer.Location = new Point(24, 238);
            footer.Size = new Size(392, 38);
            footer.BackColor = Color.White;
            footer.Paint += delegate(object sender, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle border = new Rectangle(0, 0, footer.Width - 1, footer.Height - 1);
                using (GraphicsPath path = ToggleSwitch.RoundedRectangle(border, 10))
                using (Pen pen = new Pen(Color.FromArgb(225, 229, 236)))
                    e.Graphics.DrawPath(pen, path);
            };

            Label creditLabel = new Label();
            creditLabel.Text = "Made With Love By FAISAL QADRI";
            creditLabel.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            creditLabel.ForeColor = Color.FromArgb(38, 44, 55);
            creditLabel.Location = new Point(13, 9);
            creditLabel.Size = new Size(220, 20);

            PictureBox whatsappLogo = new PictureBox();
            whatsappLogo.Image = ImageResources.LoadBitmap("WhatsAppLogo.jpg");
            whatsappLogo.Location = new Point(248, 7);
            whatsappLogo.Size = new Size(24, 24);
            whatsappLogo.SizeMode = PictureBoxSizeMode.Zoom;
            whatsappLogo.Cursor = Cursors.Hand;
            whatsappLogo.Click += delegate { OpenWhatsApp(); };

            LinkLabel whatsappLink = new LinkLabel();
            whatsappLink.Text = "+91 80824 84909";
            whatsappLink.LinkColor = Color.FromArgb(20, 143, 96);
            whatsappLink.ActiveLinkColor = Color.FromArgb(12, 105, 70);
            whatsappLink.VisitedLinkColor = Color.FromArgb(20, 143, 96);
            whatsappLink.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            whatsappLink.Location = new Point(276, 9);
            whatsappLink.Size = new Size(110, 20);
            whatsappLink.LinkClicked += delegate { OpenWhatsApp(); };

            ToolTip tip = new ToolTip();
            tip.SetToolTip(whatsappLogo, "Chat on WhatsApp");
            tip.SetToolTip(whatsappLink, "Open WhatsApp chat");

            footer.Controls.Add(creditLabel);
            footer.Controls.Add(whatsappLogo);
            footer.Controls.Add(whatsappLink);

            Controls.Add(appLogo);
            Controls.Add(titleLabel);
            Controls.Add(description);
            Controls.Add(switchLabel);
            Controls.Add(toggle);
            Controls.Add(statusLabel);
            Controls.Add(hideButton);
            Controls.Add(exitButton);
            Controls.Add(footer);

            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("Open", null, delegate { RestoreFromTray(); });
            menu.Items.Add("Enable Full Power", null, delegate { ApplyPowerMode(true); });
            menu.Items.Add("Restore Previous Mode", null, delegate { ApplyPowerMode(false); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, delegate
            {
                exitRequested = true;
                Close();
            });

            trayIcon = new NotifyIcon();
            trayIcon.Icon = ImageResources.LoadIcon("AppLogo.png");
            trayIcon.Text = "Full Power Mode";
            trayIcon.ContextMenuStrip = menu;
            trayIcon.Visible = true;
            trayIcon.DoubleClick += delegate { RestoreFromTray(); };

            Resize += delegate
            {
                if (WindowState == FormWindowState.Minimized)
                    HideToTray();
            };

            FormClosing += MainFormClosing;
            Load += delegate { RefreshState("Ready."); };
        }

        private void ToggleCheckedChanged(object sender, EventArgs e)
        {
            if (suppressToggle)
                return;

            ApplyPowerMode(toggle.Checked);
        }

        private void ApplyPowerMode(bool enable)
        {
            SetBusy(true, enable ? "Enabling full power mode..." : "Restoring previous mode...");

            ThreadPool.QueueUserWorkItem(delegate
            {
                string message;
                bool success = false;

                try
                {
                    if (enable)
                    {
                        controller.Enable();
                        message = "Full power mode enabled. Restart recommended for best results.";
                    }
                    else
                    {
                        controller.Disable();
                        message = "Previous power settings restored.";
                    }

                    success = true;
                }
                catch (Exception ex)
                {
                    message = ex.Message;
                }

                BeginInvoke(new MethodInvoker(delegate
                {
                    if (!success)
                    {
                        suppressToggle = true;
                        toggle.Checked = controller.IsEnabled();
                        suppressToggle = false;
                    }

                    SetBusy(false, message);
                    RefreshState(message);
                }));
            });
        }

        private void SetBusy(bool busy, string message)
        {
            toggle.Enabled = !busy;
            hideButton.Enabled = !busy;
            exitButton.Enabled = !busy;
            statusLabel.Text = message;
            Cursor = busy ? Cursors.AppStarting : Cursors.Default;
        }

        private void RefreshState(string message)
        {
            bool enabled = controller.IsEnabled();

            suppressToggle = true;
            toggle.Checked = enabled;
            suppressToggle = false;

            statusLabel.Text = message + " Current state: " + (enabled ? "ON" : "OFF");
            trayIcon.Text = enabled ? "Full Power Mode: ON" : "Full Power Mode: OFF";
        }

        private void HideToTray()
        {
            Hide();
            WindowState = FormWindowState.Minimized;
            ShowInTaskbar = false;
        }

        private void RestoreFromTray()
        {
            ShowInTaskbar = true;
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        private void MainFormClosing(object sender, FormClosingEventArgs e)
        {
            if (!exitRequested && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                HideToTray();
                trayIcon.ShowBalloonTip(1500, "Full Power Mode", "Still running in the background.", ToolTipIcon.Info);
                return;
            }

            trayIcon.Visible = false;
            trayIcon.Dispose();
        }

        private static void OpenWhatsApp()
        {
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = "https://wa.me/918082484909";
            start.UseShellExecute = true;
            Process.Start(start);
        }
    }

    internal static class ImageResources
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        public static Bitmap LoadBitmap(string resourceName)
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new InvalidOperationException("Image resource is missing: " + resourceName);

                return new Bitmap(stream);
            }
        }

        public static Icon LoadIcon(string resourceName)
        {
            using (Bitmap bitmap = LoadBitmap(resourceName))
            {
                IntPtr handle = bitmap.GetHicon();
                try
                {
                    using (Icon icon = Icon.FromHandle(handle))
                        return (Icon)icon.Clone();
                }
                finally
                {
                    DestroyIcon(handle);
                }
            }
        }
    }

    internal sealed class ToggleSwitch : CheckBox
    {
        public ToggleSwitch()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle track = new Rectangle(1, 1, Width - 2, Height - 2);
            int knobSize = Height - 10;
            int knobX = Checked ? Width - knobSize - 6 : 6;
            Rectangle knob = new Rectangle(knobX, 5, knobSize, knobSize);

            using (GraphicsPath path = RoundedRectangle(track, Height / 2))
            using (SolidBrush brush = new SolidBrush(Checked ? Color.FromArgb(20, 143, 96) : Color.FromArgb(174, 181, 194)))
            {
                e.Graphics.FillPath(brush, path);
            }

            using (SolidBrush brush = new SolidBrush(Color.White))
            {
                e.Graphics.FillEllipse(brush, knob);
            }
        }

        public static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal sealed class WhatsAppLogo : Control
    {
        public WhatsAppLogo()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle circle = new Rectangle(1, 1, Width - 2, Height - 2);
            using (SolidBrush green = new SolidBrush(Color.FromArgb(37, 211, 102)))
                e.Graphics.FillEllipse(green, circle);

            using (GraphicsPath bubble = new GraphicsPath())
            using (SolidBrush white = new SolidBrush(Color.White))
            {
                bubble.AddEllipse(6, 5, Width - 12, Height - 11);
                bubble.AddPolygon(new[]
                {
                    new Point(8, Height - 8),
                    new Point(5, Height - 3),
                    new Point(12, Height - 6)
                });
                e.Graphics.FillPath(white, bubble);
            }

            using (Pen handset = new Pen(Color.FromArgb(37, 211, 102), 2.1F))
            {
                handset.StartCap = LineCap.Round;
                handset.EndCap = LineCap.Round;
                e.Graphics.DrawBezier(handset, 10, 10, 9, 15, 14, 19, 19, 15);
                e.Graphics.DrawLine(handset, 10, 10, 12, 9);
                e.Graphics.DrawLine(handset, 18, 15, 20, 16);
            }
        }
    }

    internal sealed class PowerModeController
    {
        private const string UltimateScheme = "e9a42b02-d5df-448d-aa00-03f14749eb61";
        private const string BackupPath = @"Software\FullPowerModeApp";

        [DllImport("psapi.dll")]
        private static extern int EmptyWorkingSet(IntPtr hwProc);

        public bool IsEnabled()
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(BackupPath))
            {
                object value = key.GetValue("Enabled");
                return value is int && (int)value == 1;
            }
        }

        public void Enable()
        {
            EnsureAdministrator();
            BackupCurrentSettings();

            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(BackupPath))
            {
                key.SetValue("Enabled", 1, RegistryValueKind.DWord);
                key.SetValue("EnableInProgress", 1, RegistryValueKind.DWord);
            }

            try
            {
                string ultimateGuid = DuplicateOrFindUltimatePlan();
                BackupProcessorValues(ultimateGuid);

                RunPowerCfg("-setactive " + ultimateGuid);
                SetPowerCfgValueIndex(ultimateGuid, "PROCTHROTTLEMIN", 100);
                SetPowerCfgValueIndex(ultimateGuid, "PROCTHROTTLEMAX", 100);
                RunPowerCfg("-setactive " + ultimateGuid);

                SetDword(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power\PowerThrottling", "PowerThrottlingOff", 1);
                SetDword(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", 0);
                SetDword(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation", 26);

                using (Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\DirectX\UserGpuPreferences"))
                {
                }

                TrimWorkingSets();

                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(BackupPath))
                {
                    key.DeleteValue("EnableInProgress", false);
                    key.SetValue("Enabled", 1, RegistryValueKind.DWord);
                }
            }
            catch
            {
                try { RestoreBackedUpSettings(); }
                catch { }
                throw;
            }
        }

        public void Disable()
        {
            EnsureAdministrator();
            RestoreBackedUpSettings();
        }

        private static void RestoreBackedUpSettings()
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(BackupPath))
            {
                object enabled = key.GetValue("Enabled");
                object inProgress = key.GetValue("EnableInProgress");
                if (!(enabled is int) || (int)enabled != 1)
                {
                    if (!(inProgress is int) || (int)inProgress != 1)
                        return;
                }

                RestoreProcessorValues(key);

                string previousPowerScheme = key.GetValue("PreviousPowerScheme") as string;
                if (!string.IsNullOrEmpty(previousPowerScheme))
                    RunPowerCfg("-setactive " + previousPowerScheme);
                else
                    RunPowerCfg("-setactive SCHEME_BALANCED");

                RestoreRegistryDword(key, Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power\PowerThrottling", "PowerThrottlingOff");
                RestoreRegistryDword(key, Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness");
                RestoreRegistryDword(key, Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation");

                ClearProcessorBackup(key);
                key.DeleteValue("EnableInProgress", false);
                key.SetValue("Enabled", 0, RegistryValueKind.DWord);
            }
        }

        private static void BackupCurrentSettings()
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(BackupPath))
            {
                object enabled = key.GetValue("Enabled");
                if (enabled is int && (int)enabled == 1)
                    return;

                key.SetValue("PreviousPowerScheme", GetActivePowerScheme(), RegistryValueKind.String);
                BackupRegistryDword(key, Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power\PowerThrottling", "PowerThrottlingOff");
                BackupRegistryDword(key, Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness");
                BackupRegistryDword(key, Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation");
            }
        }

        private static void BackupProcessorValues(string powerSchemeGuid)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(BackupPath))
            {
                object backedUp = key.GetValue("ProcessorValuesBackedUp");
                if (backedUp is int && (int)backedUp == 1)
                    return;

                key.SetValue("ProcessorPowerScheme", powerSchemeGuid, RegistryValueKind.String);
                key.SetValue("ProcessorMinAcValue", GetPowerCfgValueIndex(powerSchemeGuid, "PROCTHROTTLEMIN"), RegistryValueKind.DWord);
                key.SetValue("ProcessorMaxAcValue", GetPowerCfgValueIndex(powerSchemeGuid, "PROCTHROTTLEMAX"), RegistryValueKind.DWord);
                key.SetValue("ProcessorValuesBackedUp", 1, RegistryValueKind.DWord);
            }
        }

        private static void RestoreProcessorValues(RegistryKey backupKey)
        {
            object backedUp = backupKey.GetValue("ProcessorValuesBackedUp");
            if (!(backedUp is int) || (int)backedUp != 1)
                return;

            string powerSchemeGuid = backupKey.GetValue("ProcessorPowerScheme") as string;
            if (string.IsNullOrEmpty(powerSchemeGuid))
                return;

            object minValue = backupKey.GetValue("ProcessorMinAcValue");
            object maxValue = backupKey.GetValue("ProcessorMaxAcValue");
            if (minValue != null)
                SetPowerCfgValueIndex(powerSchemeGuid, "PROCTHROTTLEMIN", Convert.ToInt32(minValue));
            if (maxValue != null)
                SetPowerCfgValueIndex(powerSchemeGuid, "PROCTHROTTLEMAX", Convert.ToInt32(maxValue));
        }

        private static void ClearProcessorBackup(RegistryKey backupKey)
        {
            backupKey.DeleteValue("ProcessorValuesBackedUp", false);
            backupKey.DeleteValue("ProcessorPowerScheme", false);
            backupKey.DeleteValue("ProcessorMinAcValue", false);
            backupKey.DeleteValue("ProcessorMaxAcValue", false);
        }

        private static void BackupRegistryDword(RegistryKey backupKey, RegistryKey hive, string path, string name)
        {
            using (RegistryKey key = hive.OpenSubKey(path))
            {
                object value = key == null ? null : key.GetValue(name);
                bool exists = value != null;
                backupKey.SetValue(name + "Exists", exists ? 1 : 0, RegistryValueKind.DWord);
                if (exists)
                    backupKey.SetValue(name + "Value", Convert.ToInt32(value), RegistryValueKind.DWord);
            }
        }

        private static void RestoreRegistryDword(RegistryKey backupKey, RegistryKey hive, string path, string name)
        {
            object existsValue = backupKey.GetValue(name + "Exists");
            bool existed = existsValue is int && (int)existsValue == 1;

            if (existed)
            {
                object value = backupKey.GetValue(name + "Value");
                SetDword(hive, path, name, value == null ? 0 : Convert.ToInt32(value));
            }
            else
            {
                using (RegistryKey key = hive.OpenSubKey(path, true))
                {
                    if (key != null)
                    {
                        try { key.DeleteValue(name, false); }
                        catch { }
                    }
                }
            }
        }

        private static string DuplicateOrFindUltimatePlan()
        {
            CommandResult duplicate = RunPowerCfgAllowFail("-duplicatescheme " + UltimateScheme);
            string guid = ParseFirstGuid(duplicate.Output);
            if (!string.IsNullOrEmpty(guid))
                return guid;

            CommandResult list = RunPowerCfgAllowFail("-list");
            foreach (string line in list.Output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.IndexOf("Ultimate", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    guid = ParseFirstGuid(line);
                    if (!string.IsNullOrEmpty(guid))
                        return guid;
                }
            }

            throw new InvalidOperationException("Ultimate Performance power plan could not be created or found.");
        }

        private static string GetActivePowerScheme()
        {
            CommandResult result = RunPowerCfgAllowFail("-getactivescheme");
            string guid = ParseFirstGuid(result.Output);
            return string.IsNullOrEmpty(guid) ? "SCHEME_BALANCED" : guid;
        }

        private static int GetPowerCfgValueIndex(string powerSchemeGuid, string setting)
        {
            CommandResult result = RunPowerCfgAllowFail("-query " + powerSchemeGuid + " sub_processor " + setting);
            if (result.ExitCode != 0)
                throw new InvalidOperationException("powercfg failed: " + result.Error);

            Match match = Regex.Match(result.Output, @"Current AC Power Setting Index:\s*0x([0-9a-fA-F]+)", RegexOptions.IgnoreCase);
            if (!match.Success)
                throw new InvalidOperationException("Could not read processor power setting: " + setting);

            return Convert.ToInt32(match.Groups[1].Value, 16);
        }

        private static void SetPowerCfgValueIndex(string powerSchemeGuid, string setting, int value)
        {
            RunPowerCfg("-setacvalueindex " + powerSchemeGuid + " sub_processor " + setting + " " + value);
        }

        private static string ParseFirstGuid(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            Match match = Regex.Match(text, @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");
            return match.Success ? match.Value : null;
        }

        private static void SetDword(RegistryKey hive, string path, string name, int value)
        {
            using (RegistryKey key = hive.CreateSubKey(path))
            {
                key.SetValue(name, value, RegistryValueKind.DWord);
            }
        }

        private static void TrimWorkingSets()
        {
            foreach (Process process in Process.GetProcesses())
            {
                try
                {
                    EmptyWorkingSet(process.Handle);
                }
                catch
                {
                }
            }
        }

        private static void RunPowerCfg(string arguments)
        {
            CommandResult result = RunPowerCfgAllowFail(arguments);
            if (result.ExitCode != 0)
                throw new InvalidOperationException("powercfg failed: " + result.Error);
        }

        private static CommandResult RunPowerCfgAllowFail(string arguments)
        {
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = "powercfg.exe";
            start.Arguments = arguments;
            start.UseShellExecute = false;
            start.CreateNoWindow = true;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;

            using (Process process = Process.Start(start))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                return new CommandResult(process.ExitCode, output, error);
            }
        }

        private static void EnsureAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
                throw new Win32Exception("This app must be run as administrator. The included manifest should trigger a UAC prompt.");
        }
    }

    internal sealed class CommandResult
    {
        public readonly int ExitCode;
        public readonly string Output;
        public readonly string Error;

        public CommandResult(int exitCode, string output, string error)
        {
            ExitCode = exitCode;
            Output = output;
            Error = error;
        }
    }
}
