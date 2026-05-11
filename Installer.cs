using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("Full Power Mode Setup")]
[assembly: AssemblyProduct("Full Power Mode")]
[assembly: AssemblyVersion("1.0.3.0")]
[assembly: AssemblyFileVersion("1.0.3.0")]
[assembly: AssemblyInformationalVersion("1.0.3")]

namespace FullPowerModeSetup
{
    internal static class InstallerProgram
    {
        private const string AppName = "Full Power Mode";
        private const string ResourceName = "FullPowerMode.exe";
        internal const string AppVersion = "1.0.3";

        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new InstallerForm());
        }

        internal static string InstallDirectory
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "FullPowerMode"); }
        }

        internal static string InstalledExePath
        {
            get { return Path.Combine(InstallDirectory, "FullPowerMode.exe"); }
        }

        internal static bool IsInstalled
        {
            get { return File.Exists(InstalledExePath); }
        }

        internal static void Install(Action<string> report)
        {
            report("Preparing installation folder...");
            Directory.CreateDirectory(InstallDirectory);

            report("Closing old app copy if it is running...");
            CloseRunningInstalledApp();

            report("Installing application files...");
            ExtractEmbeddedApp(InstalledExePath);

            report("Creating Desktop shortcut...");
            CreateDesktopShortcut();

            report("Creating Start Menu shortcut...");
            CreateStartMenuShortcut();

            report("Installation complete.");
        }

        internal static void Uninstall(Action<string> report)
        {
            report("Closing installed app if it is running...");
            CloseRunningInstalledApp();

            report("Restoring power settings...");
            PowerModeRestore.RestoreIfEnabled();

            report("Removing Desktop shortcut...");
            DeleteShortcut(GetCommonDesktopShortcutPath());
            DeleteShortcut(GetUserDesktopShortcutPath());

            report("Removing Start Menu shortcut...");
            DeleteShortcut(GetStartMenuShortcutPath());
            DeleteFolderIfEmpty(Path.GetDirectoryName(GetStartMenuShortcutPath()));

            report("Removing installed app files...");
            if (File.Exists(InstalledExePath))
                File.Delete(InstalledExePath);

            DeleteFolderIfEmpty(InstallDirectory);
            report("Uninstall complete.");
        }

        internal static void LaunchInstalledApp()
        {
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = InstalledExePath;
            start.UseShellExecute = true;
            Process.Start(start);
        }

        private static void ExtractEmbeddedApp(string outputPath)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream input = assembly.GetManifestResourceStream(ResourceName))
            {
                if (input == null)
                    throw new InvalidOperationException("Installer resource is missing: " + ResourceName);

                string tempPath = outputPath + ".installing";
                using (FileStream output = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    input.CopyTo(output);

                if (File.Exists(outputPath))
                    File.Delete(outputPath);

                File.Move(tempPath, outputPath);
            }
        }

        private static void CloseRunningInstalledApp()
        {
            foreach (Process process in Process.GetProcessesByName("FullPowerMode"))
            {
                try
                {
                    string path = null;
                    try { path = process.MainModule.FileName; }
                    catch { }

                    if (!string.Equals(path, InstalledExePath, StringComparison.OrdinalIgnoreCase))
                        continue;

                    process.CloseMainWindow();
                    if (!process.WaitForExit(3000))
                        process.Kill();
                }
                catch
                {
                }
            }
        }

        private static void CreateDesktopShortcut()
        {
            string shortcutPath = GetCommonDesktopShortcutPath();
            if (string.IsNullOrEmpty(Path.GetDirectoryName(shortcutPath)))
                shortcutPath = GetUserDesktopShortcutPath();

            CreateShortcut(shortcutPath);
        }

        private static void CreateStartMenuShortcut()
        {
            string folder = Path.GetDirectoryName(GetStartMenuShortcutPath());
            Directory.CreateDirectory(folder);
            CreateShortcut(GetStartMenuShortcutPath());
        }

        private static string GetCommonDesktopShortcutPath()
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
            return string.IsNullOrEmpty(desktop) ? string.Empty : Path.Combine(desktop, AppName + ".lnk");
        }

        private static string GetUserDesktopShortcutPath()
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            return string.IsNullOrEmpty(desktop) ? string.Empty : Path.Combine(desktop, AppName + ".lnk");
        }

        private static string GetStartMenuShortcutPath()
        {
            string programs = Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms);
            return Path.Combine(programs, AppName, AppName + ".lnk");
        }

        private static void CreateShortcut(string shortcutPath)
        {
            IShellLinkW link = (IShellLinkW)new CShellLink();
            link.SetPath(InstalledExePath);
            link.SetWorkingDirectory(InstallDirectory);
            link.SetDescription("Open Full Power Mode");
            link.SetIconLocation(InstalledExePath, 0);

            IPersistFile file = (IPersistFile)link;
            file.Save(shortcutPath, true);
        }

        private static void DeleteShortcut(string shortcutPath)
        {
            if (!string.IsNullOrEmpty(shortcutPath) && File.Exists(shortcutPath))
                File.Delete(shortcutPath);
        }

        private static void DeleteFolderIfEmpty(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                return;

            if (Directory.GetFileSystemEntries(folderPath).Length == 0)
                Directory.Delete(folderPath);
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

    internal static class PowerModeRestore
    {
        private const string BackupPath = @"Software\FullPowerModeApp";

        internal static void RestoreIfEnabled()
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(BackupPath))
            {
                object enabled = key.GetValue("Enabled");
                if (!(enabled is int) || (int)enabled != 1)
                    return;

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

        private static void SetDword(RegistryKey hive, string path, string name, int value)
        {
            using (RegistryKey key = hive.CreateSubKey(path))
            {
                key.SetValue(name, value, RegistryValueKind.DWord);
            }
        }

        private static void SetPowerCfgValueIndex(string powerSchemeGuid, string setting, int value)
        {
            RunPowerCfg("-setacvalueindex " + powerSchemeGuid + " sub_processor " + setting + " " + value);
        }

        private static void RunPowerCfg(string arguments)
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
                if (process.ExitCode != 0)
                    throw new InvalidOperationException("powercfg failed: " + (string.IsNullOrEmpty(error) ? output : error));
            }
        }
    }

    internal sealed class InstallerForm : Form
    {
        private readonly Label titleLabel;
        private readonly Label statusLabel;
        private readonly Button installButton;
        private readonly Button uninstallButton;
        private readonly Button launchButton;
        private readonly Button closeButton;
        private bool operationRunning;

        public InstallerForm()
        {
            Text = "Full Power Mode Setup " + InstallerProgram.AppVersion;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(460, 270);
            BackColor = Color.FromArgb(248, 249, 252);
            Font = new Font("Segoe UI", 9F);
            Icon = ImageResources.LoadIcon("AppLogo.png");

            PictureBox appLogo = new PictureBox();
            appLogo.Image = ImageResources.LoadBitmap("AppLogo.png");
            appLogo.Location = new Point(26, 21);
            appLogo.Size = new Size(38, 38);
            appLogo.SizeMode = PictureBoxSizeMode.Zoom;

            titleLabel = new Label();
            titleLabel.Text = "Install Full Power Mode " + InstallerProgram.AppVersion;
            titleLabel.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold);
            titleLabel.ForeColor = Color.FromArgb(24, 28, 35);
            titleLabel.Location = new Point(74, 22);
            titleLabel.Size = new Size(352, 38);

            Label detailLabel = new Label();
            detailLabel.Text = "This setup will install the app, add a Desktop shortcut, and add a Start Menu shortcut.";
            detailLabel.ForeColor = Color.FromArgb(88, 96, 110);
            detailLabel.Location = new Point(28, 66);
            detailLabel.Size = new Size(390, 42);

            Panel card = new Panel();
            card.Location = new Point(28, 122);
            card.Size = new Size(404, 62);
            card.BackColor = Color.White;
            card.Paint += delegate(object sender, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle border = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                using (GraphicsPath path = RoundedRectangle(border, 10))
                using (Pen pen = new Pen(Color.FromArgb(225, 229, 236)))
                    e.Graphics.DrawPath(pen, path);
            };

            statusLabel = new Label();
            statusLabel.Text = "Ready to install.";
            statusLabel.ForeColor = Color.FromArgb(50, 58, 72);
            statusLabel.Location = new Point(16, 20);
            statusLabel.Size = new Size(365, 22);
            card.Controls.Add(statusLabel);

            Label creditLabel = new Label();
            creditLabel.Text = "Made With Love By FAISAL QADRI";
            creditLabel.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            creditLabel.ForeColor = Color.FromArgb(38, 44, 55);
            creditLabel.Location = new Point(30, 195);
            creditLabel.Size = new Size(230, 20);

            PictureBox whatsappLogo = new PictureBox();
            whatsappLogo.Image = ImageResources.LoadBitmap("WhatsAppLogo.jpg");
            whatsappLogo.Location = new Point(262, 194);
            whatsappLogo.Size = new Size(22, 22);
            whatsappLogo.SizeMode = PictureBoxSizeMode.Zoom;
            whatsappLogo.Cursor = Cursors.Hand;
            whatsappLogo.Click += delegate { OpenWhatsApp(); };

            LinkLabel whatsappLink = new LinkLabel();
            whatsappLink.Text = "+91 80824 84909";
            whatsappLink.LinkColor = Color.FromArgb(20, 143, 96);
            whatsappLink.ActiveLinkColor = Color.FromArgb(12, 105, 70);
            whatsappLink.VisitedLinkColor = Color.FromArgb(20, 143, 96);
            whatsappLink.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            whatsappLink.Location = new Point(288, 195);
            whatsappLink.Size = new Size(138, 20);
            whatsappLink.LinkClicked += delegate { OpenWhatsApp(); };

            installButton = new Button();
            installButton.Text = "Install";
            installButton.Location = new Point(102, 228);
            installButton.Size = new Size(78, 28);
            installButton.Click += delegate { BeginInstall(); };

            uninstallButton = new Button();
            uninstallButton.Text = "Uninstall";
            uninstallButton.Location = new Point(186, 228);
            uninstallButton.Size = new Size(86, 28);
            uninstallButton.Click += delegate { BeginUninstall(); };

            launchButton = new Button();
            launchButton.Text = "Open App";
            launchButton.Location = new Point(278, 228);
            launchButton.Size = new Size(82, 28);
            launchButton.Enabled = InstallerProgram.IsInstalled;
            launchButton.Click += delegate { InstallerProgram.LaunchInstalledApp(); };

            closeButton = new Button();
            closeButton.Text = "Close";
            closeButton.Location = new Point(366, 228);
            closeButton.Size = new Size(66, 28);
            closeButton.Click += delegate { Close(); };

            Controls.Add(appLogo);
            Controls.Add(titleLabel);
            Controls.Add(detailLabel);
            Controls.Add(card);
            Controls.Add(creditLabel);
            Controls.Add(whatsappLogo);
            Controls.Add(whatsappLink);
            Controls.Add(installButton);
            Controls.Add(uninstallButton);
            Controls.Add(launchButton);
            Controls.Add(closeButton);

            Shown += delegate
            {
                if (!InstallerProgram.IsInstalled)
                    BeginInstall();
                else
                    statusLabel.Text = "Already installed. You can open or uninstall it.";
            };
            FormClosing += InstallerFormClosing;
        }

        private void BeginInstall()
        {
            if (operationRunning)
                return;

            BeginOperation(
                delegate(Action<string> report) { InstallerProgram.Install(report); },
                "Installed. Desktop shortcut is ready.",
                "Install failed: ",
                true);
        }

        private void BeginUninstall()
        {
            if (operationRunning)
                return;

            BeginOperation(
                delegate(Action<string> report) { InstallerProgram.Uninstall(report); },
                "Uninstalled. Shortcuts and installed files were removed.",
                "Uninstall failed: ",
                false);
        }

        private void BeginOperation(Action<Action<string>> operation, string successMessage, string failurePrefix, bool installedAfterSuccess)
        {
            operationRunning = true;
            installButton.Enabled = false;
            uninstallButton.Enabled = false;
            launchButton.Enabled = false;
            closeButton.Enabled = false;
            Cursor = Cursors.AppStarting;

            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    operation(delegate(string message)
                    {
                        PostToUi(delegate { statusLabel.Text = message; });
                        Thread.Sleep(200);
                    });

                    PostToUi(delegate
                    {
                        operationRunning = false;
                        statusLabel.Text = successMessage;
                        installButton.Enabled = true;
                        uninstallButton.Enabled = true;
                        launchButton.Enabled = installedAfterSuccess;
                        closeButton.Enabled = true;
                        closeButton.Text = "Finish";
                        Cursor = Cursors.Default;
                    });
                }
                catch (Exception ex)
                {
                    PostToUi(delegate
                    {
                        operationRunning = false;
                        installButton.Enabled = true;
                        uninstallButton.Enabled = true;
                        launchButton.Enabled = InstallerProgram.IsInstalled;
                        closeButton.Enabled = true;
                        statusLabel.Text = failurePrefix + ex.Message;
                        Cursor = Cursors.Default;
                    });
                }
            });
        }

        private void InstallerFormClosing(object sender, FormClosingEventArgs e)
        {
            if (!operationRunning)
                return;

            e.Cancel = true;
            statusLabel.Text = "Please wait for the current operation to finish.";
        }

        private void PostToUi(MethodInvoker action)
        {
            if (IsDisposed || !IsHandleCreated)
                return;

            try
            {
                BeginInvoke(action);
            }
            catch (InvalidOperationException)
            {
            }
        }

        private static void OpenWhatsApp()
        {
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = "https://wa.me/918082484909";
            start.UseShellExecute = true;
            Process.Start(start);
        }

        private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
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

    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    internal class CShellLink
    {
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    internal interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszFile, int cchMaxPath, IntPtr pfd, uint fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszName, int cchMaxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszDir, int cchMaxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszArgs, int cchMaxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszIconPath, int cchIconPath, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
        void Resolve(IntPtr hwnd, uint fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("0000010b-0000-0000-C000-000000000046")]
    internal interface IPersistFile
    {
        void GetClassID(out Guid pClassID);
        void IsDirty();
        void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
        void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, bool fRemember);
        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
        void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
    }
}
