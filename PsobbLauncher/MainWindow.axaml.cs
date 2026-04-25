using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;

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
                string exePath = Path.Combine(root, "psobb.exe");
                
                if (!File.Exists(exePath)) {
                    string parentExe = Path.GetFullPath(Path.Combine(root, "..", "psobb.exe"));
                    if (File.Exists(parentExe)) {
                        exePath = parentExe;
                        root = Path.GetDirectoryName(exePath);
                    }
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Force WINDOW_MODE=1 in registry so the game launches in windowed mode
                    using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\SonicTeam\PSOBB")) {
                        if (key != null) key.SetValue("WINDOW_MODE", 1, Microsoft.Win32.RegistryValueKind.DWord);
                    }

                    ProcessStartInfo psi = new ProcessStartInfo(exePath);
                    psi.WorkingDirectory = root;
                    Process.Start(psi);
                }
                else
                {
                    // Launch with Wine on Mac/Linux
                    ProcessStartInfo psi = new ProcessStartInfo("wine");
                    psi.Arguments = $"\"{exePath}\"";
                    psi.WorkingDirectory = root;
                    Process.Start(psi);
                }
                
                if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.Shutdown();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to launch psobb.exe: " + ex.Message);
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settings = new SettingsWindow();
            settings.ShowDialog(this);
        }

        private void WebsiteButton_Click(object sender, RoutedEventArgs e)
        {
            try { 
                var url = "https://psobb.io";
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
            } catch { }
        }

        private void TitleBar_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                BeginMoveDrag(e);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }
    }
}
