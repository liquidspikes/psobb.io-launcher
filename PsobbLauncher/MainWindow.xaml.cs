using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace PsobbLauncher
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void LaunchButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string root = AppDomain.CurrentDomain.BaseDirectory;
                string exePath = System.IO.Path.Combine(root, "psobb.exe");
                
                if (!System.IO.File.Exists(exePath)) {
                    string parentExe = System.IO.Path.GetFullPath(System.IO.Path.Combine(root, "..", "psobb.exe"));
                    if (System.IO.File.Exists(parentExe)) {
                        exePath = parentExe;
                        root = System.IO.Path.GetDirectoryName(exePath);
                    }
                }

                // Force WINDOW_MODE=1 in registry so the game launches in windowed mode (which patch converts to borderless/windowed)
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\SonicTeam\PSOBB")) {
                    if (key != null) key.SetValue("WINDOW_MODE", 1, Microsoft.Win32.RegistryValueKind.DWord);
                }

                // Launch game directly - Frame Generation runs within bbio.dll
                ProcessStartInfo psi = new ProcessStartInfo(exePath);
                psi.WorkingDirectory = root;
                Process.Start(psi);
                
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to launch psobb.exe: " + ex.Message);
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settings = new SettingsWindow();
            settings.Owner = this;
            settings.ShowDialog();
        }

        private void WebsiteButton_Click(object sender, RoutedEventArgs e)
        {
            try { Process.Start("https://psobb.io"); } catch { }
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
