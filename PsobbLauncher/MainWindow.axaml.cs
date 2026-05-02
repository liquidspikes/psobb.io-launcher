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
                        using (var scaledBitmap = sourceBitmap.CreateScaledBitmap(new Avalonia.PixelSize(32, 32), Avalonia.Visuals.Media.Imaging.BitmapInterpolationMode.HighQuality))
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
                            
                            // Copy Pixel Data
                            // Create a temporary WriteableBitmap to access pixels
                            var format = Avalonia.Platform.PixelFormat.Bgra8888;
                            var alphaFormat = Avalonia.Platform.AlphaFormat.Unpremul;
                            using (var renderTarget = new Avalonia.Media.Imaging.RenderTargetBitmap(new Avalonia.PixelSize(width, height), new Avalonia.Vector(96, 96)))
                            {
                                using (var ctx = renderTarget.CreateDrawingContext(null))
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
                    Console.WriteLine("Error processing team flag: " + ex.Message);
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
