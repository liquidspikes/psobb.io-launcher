using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;

namespace PsobbLauncher
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            CheckForGameUpdate();
            InitServers();
        }
        private void CheckForGameUpdate()
        {
            try
            {
                string root = AppDomain.CurrentDomain.BaseDirectory;
                string patPath = Path.Combine(root, "psobb.pat");
                string exePath = Path.Combine(root, "psobb.exe");
                string bakPath = Path.Combine(root, "psobb.exe.bak");

                // If pat/exe are not in root, check one folder up
                if (!File.Exists(patPath) && !File.Exists(exePath))
                {
                    string parentDir = Path.GetFullPath(Path.Combine(root, ".."));
                    patPath = Path.Combine(parentDir, "psobb.pat");
                    exePath = Path.Combine(parentDir, "psobb.exe");
                    bakPath = Path.Combine(parentDir, "psobb.exe.bak");
                }

                if (File.Exists(patPath))
                {
                    if (File.Exists(bakPath))
                    {
                        File.Delete(bakPath);
                    }

                    if (File.Exists(exePath))
                    {
                        File.Move(exePath, bakPath);
                    }

                    File.Move(patPath, exePath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to apply psobb.pat update: " + ex.Message);
            }
        }
        private readonly ServerStore _store = new();
        private CaptureService? _capture;
        private ServerProfile? _selectedServer;

        private void InitServers()
        {
            _store.Load();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                _capture = new CaptureService(_store);

            RefreshServerCombo();
        }

        private void RefreshServerCombo()
        {
            ServerCombo.ItemsSource = _store.Servers.Select(s => s.Name).ToList();

            if (_store.Servers.Count > 0)
                ServerCombo.SelectedIndex = 0;
        }

        private void ServerCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            int idx = ServerCombo.SelectedIndex;
            _selectedServer = (idx >= 0 && idx < _store.Servers.Count)
                ? _store.Servers[idx]
                : null;
        }

        private async void AddServerButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var dlg = new AddServerDialog();
            var profile = await dlg.ShowDialog<ServerProfile?>(this);
            if (profile is null)
                return;

            _store.AddOrUpdate(profile);
            RefreshServerCombo();
            ServerCombo.SelectedIndex = _store.Servers.FindIndex(s => s.Id == profile.Id);
            StatusText.Text = $"Added server: {profile.Name}";
        }

        private void CaptureButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                StatusText.Text = "Credential capture is Windows-only.";
                return;
            }
            if (_selectedServer is null)
            {
                StatusText.Text = "Select a server first.";
                return;
            }

            bool ok = _capture!.CaptureCurrentLogin(_selectedServer);
            StatusText.Text = ok
                ? $"Captured login for {_selectedServer.Name}."
                : "No login found. Launch this server and log in once first.";
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
                        root = Path.GetDirectoryName(exePath) ?? root;
                    }
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Force WINDOW_MODE=1 in registry so the game launches in windowed mode
                    using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\SonicTeam\PSOBB"))
                    {
                        if (key != null) key.SetValue("WINDOW_MODE", 1, Microsoft.Win32.RegistryValueKind.DWord);
                    }

                    // Write the selected server's psobb.cfg and stamp its saved credentials
                    if (_selectedServer != null)
                    {
                        PsobbConfig.WriteForProfile(root, _selectedServer);
                        _capture?.ApplyCredentials(_selectedServer);
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
                Debug.WriteLine("Failed to launch psobb.exe: " + ex.Message);
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
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to open website: " + ex.Message);
            }
        }

        private void ModsButton_Click(object sender, RoutedEventArgs e)
        {
            ModsWindow modsWindow = new ModsWindow();
            modsWindow.ShowDialog(this);
        }

        private void EventsButton_Click(object sender, RoutedEventArgs e)
        {
            EventsWindow eventsWindow = new EventsWindow();
            eventsWindow.ShowDialog(this);
        }

        private async void TeamFlagButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "Select Team Flag Image";
            dialog.Filters.Add(new FileDialogFilter { Name = "Images", Extensions = { "png", "jpg", "jpeg", "bmp" } });
            dialog.AllowMultiple = false;

            var result = await dialog.ShowAsync(this);
            if (result != null && result.Length > 0)
            {
                string sourcePath = result[0];
                try
                {
                    string root = AppDomain.CurrentDomain.BaseDirectory;
                    string teamFlagDir = Path.Combine(root, "teamflag");
                    
                    if (!Directory.Exists(teamFlagDir))
                    {
                        // Fallback to checking parent dir in case we are in bin/Debug/
                        string parentDir = Path.GetFullPath(Path.Combine(root, "..", "teamflag"));
                        if (Directory.Exists(Path.GetFullPath(Path.Combine(root, "..", "data"))))
                        {
                            teamFlagDir = parentDir;
                        }
                    }

                    if (!Directory.Exists(teamFlagDir))
                        Directory.CreateDirectory(teamFlagDir);

                    string targetPath = Path.Combine(teamFlagDir, "flag.bmp");

                    // Load the image with Avalonia
                    using (var sourceBitmap = new Avalonia.Media.Imaging.Bitmap(sourcePath))
                    {
                        // Create a 32x32 scaled version (High Quality)
                        using (var scaledBitmap = sourceBitmap.CreateScaledBitmap(new Avalonia.PixelSize(32, 32), Avalonia.Media.Imaging.BitmapInterpolationMode.HighQuality))
                        {
                            // We need to write a simple 32-bit BMP file
                            int width = 32;
                            int height = 32;
                            int bpp = 32;
                            int dataSize = width * height * (bpp / 8);
                            int fileSize = 54 + dataSize;

                            byte[] bmpFile = new byte[fileSize];
                            
                            // BITMAPFILEHEADER
                            bmpFile[0] = (byte)'B'; bmpFile[1] = (byte)'M';
                            BitConverter.GetBytes(fileSize).CopyTo(bmpFile, 2);
                            BitConverter.GetBytes(54).CopyTo(bmpFile, 10); // DataOffset
                            
                            // BITMAPINFOHEADER
                            BitConverter.GetBytes(40).CopyTo(bmpFile, 14); // InfoHeaderSize
                            BitConverter.GetBytes(width).CopyTo(bmpFile, 18);
                            BitConverter.GetBytes(-height).CopyTo(bmpFile, 22); // Negative height for top-down
                            BitConverter.GetBytes((short)1).CopyTo(bmpFile, 26); // Planes
                            BitConverter.GetBytes((short)bpp).CopyTo(bmpFile, 28); // BPP
                            BitConverter.GetBytes(dataSize).CopyTo(bmpFile, 34); // ImageSize
                            
                            // Copy Pixel Data via RenderTargetBitmap
                            using (var renderTarget = new Avalonia.Media.Imaging.RenderTargetBitmap(new Avalonia.PixelSize(width, height), new Avalonia.Vector(96, 96)))
                            {
                                using (var ctx = renderTarget.CreateDrawingContext())
                                {
                                    ctx.DrawImage(scaledBitmap, new Avalonia.Rect(0, 0, width, height));
                                }
                                
                                // Copy the raw bytes directly
                                unsafe
                                {
                                    fixed (byte* ptr = &bmpFile[54])
                                    {
                                        renderTarget.CopyPixels(new Avalonia.PixelRect(0, 0, width, height), (IntPtr)ptr, dataSize, width * 4);
                                    }
                                }
                            }
                            
                            File.WriteAllBytes(targetPath, bmpFile);
                        }
                    }
                    
                    // Show a quick success dialog (or we can just be silent/use a label)
                    var msg = new Window { Title = "Success", Width = 300, Height = 100, WindowStartupLocation = WindowStartupLocation.CenterOwner };
                    msg.Content = new TextBlock { Text = "Team Flag has been updated successfully!", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
                    await msg.ShowDialog(this);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error processing team flag: " + ex.Message);
                }
            }
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
