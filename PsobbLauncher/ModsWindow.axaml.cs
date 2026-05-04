using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace PsobbLauncher
{
    public class ModItem : System.ComponentModel.INotifyPropertyChanged
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Author { get; set; } = "";
        public string Version { get; set; } = "";
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public long FileSize { get; set; } = 0;
        public double AverageRating { get; set; } = 0;
        public int RatingCount { get; set; } = 0;

        private Bitmap? _thumbnail;
        [System.Text.Json.Serialization.JsonIgnore]
        public Bitmap? Thumbnail
        {
            get => _thumbnail;
            set
            {
                if (_thumbnail != value)
                {
                    _thumbnail = value;
                    PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Thumbnail)));
                }
            }
        }

        private bool _isInstalled;
        public bool IsInstalled
        {
            get => _isInstalled;
            set
            {
                if (_isInstalled != value)
                {
                    _isInstalled = value;
                    PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsInstalled)));
                    PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(StatusText)));
                    PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(StatusColor)));
                }
            }
        }

        public string StatusText => IsInstalled ? "ENABLED" : "";
        public IBrush StatusColor => IsInstalled ? new SolidColorBrush(Color.Parse("#00ffcc")) : Brushes.Transparent;

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    }

    public class InstalledModState
    {
        public string Id { get; set; } = "";
        public string Version { get; set; } = "";
        public List<string> Files { get; set; } = new List<string>();
        public List<string> BackedUpFiles { get; set; } = new List<string>();
    }

    public partial class ModsWindow : Window
    {
        private ObservableCollection<ModItem> _mods = new ObservableCollection<ModItem>();
        private Dictionary<string, InstalledModState> _installedState = new Dictionary<string, InstalledModState>();
        private string _gameRoot;
        private string _stateFile;
        private HttpClient _httpClient = new HttpClient();

        public ModsWindow()
        {
            InitializeComponent();
            ModsListBox.ItemsSource = _mods;

            _gameRoot = AppDomain.CurrentDomain.BaseDirectory;
            if (!File.Exists(Path.Combine(_gameRoot, "psobb.exe")))
            {
                string parentExe = Path.GetFullPath(Path.Combine(_gameRoot, "..", "psobb.exe"));
                if (File.Exists(parentExe))
                {
                    _gameRoot = Path.GetDirectoryName(parentExe) ?? _gameRoot;
                }
            }

            _stateFile = Path.Combine(_gameRoot, "installed_mods.json");
            LoadState();
            LoadModsAsync();
        }

        private void LoadState()
        {
            try
            {
                if (File.Exists(_stateFile))
                {
                    string json = File.ReadAllText(_stateFile);
                    var list = JsonSerializer.Deserialize<List<InstalledModState>>(json);
                    if (list != null)
                    {
                        foreach (var state in list)
                        {
                            _installedState[state.Id] = state;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to load mod state: " + ex.Message);
            }
        }

        private void SaveState()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_installedState.Values.ToList(), options);
                File.WriteAllText(_stateFile, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to save mod state: " + ex.Message);
            }
        }

        private async void LoadModsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("https://psobb.io/api/mods.php");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var fetchedMods = JsonSerializer.Deserialize<List<ModItem>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (fetchedMods != null)
                    {
                        foreach (var m in fetchedMods)
                        {
                            m.IsInstalled = _installedState.ContainsKey(m.Id);
                            _mods.Add(m);
                        }
                        
                        _ = LoadThumbnailsAsync(fetchedMods);
                    }
                }
                else
                {
                    StatusMessageText.Text = "Failed to load mods from server.";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to load mods: " + ex.Message);
                StatusMessageText.Text = "Could not connect to mod server.";
            }
        }

        private async Task LoadThumbnailsAsync(List<ModItem> mods)
        {
            string cacheDir = Path.Combine(_gameRoot, "mod_cache");
            if (!Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
            }

            foreach (var mod in mods)
            {
                if (string.IsNullOrEmpty(mod.ImageUrl)) continue;
                
                try
                {
                    Uri uri = new Uri(mod.ImageUrl);
                    string ext = Path.GetExtension(uri.AbsolutePath).ToLower();
                    if (ext != ".jpg" && ext != ".jpeg" && ext != ".png")
                    {
                        // Skip video thumbnails or unknown formats
                        continue;
                    }
                    
                    string fileName = $"{mod.Id}{ext}";
                    string localPath = Path.Combine(cacheDir, fileName);

                    if (File.Exists(localPath))
                    {
                        // Load from cache
                        await Task.Run(() =>
                        {
                            using var fs = File.OpenRead(localPath);
                            var bitmap = new Bitmap(fs);
                            Dispatcher.UIThread.Post(() => mod.Thumbnail = bitmap);
                        });
                    }
                    else
                    {
                        // Download and cache
                        var imgBytes = await _httpClient.GetByteArrayAsync(mod.ImageUrl);
                        await File.WriteAllBytesAsync(localPath, imgBytes);
                        
                        await Task.Run(() =>
                        {
                            using var ms = new MemoryStream(imgBytes);
                            var bitmap = new Bitmap(ms);
                            Dispatcher.UIThread.Post(() => mod.Thumbnail = bitmap);
                        });
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to load thumbnail for {mod.Id}: {ex.Message}");
                }
            }
        }

        private void ModsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ModsListBox.SelectedItem is ModItem selected)
            {
                SelectPromptText.IsVisible = false;
                ModInfoPanel.IsVisible = true;
                
                ModDetailsImage.Source = selected.Thumbnail;
                ModTitleText.Text = selected.Name;
                ModAuthorText.Text = $"By {selected.Author}";
                ModVersionText.Text = $"Version: {selected.Version} | Category: {selected.Category}";
                ModSizeText.Text = $"Size: {(selected.FileSize / 1024.0 / 1024.0):F2} MB";
                ModRatingText.Text = $"Rating: {selected.AverageRating:F1}/5.0 ({selected.RatingCount} votes)";
                ModDescText.Text = selected.Description;

                ActionBtn.IsEnabled = true;
                UpdateActionButton(selected);
            }
            else
            {
                SelectPromptText.IsVisible = true;
                ModInfoPanel.IsVisible = false;
                ActionBtn.IsEnabled = false;
            }
        }

        private void UpdateActionButton(ModItem mod)
        {
            if (mod.IsInstalled)
            {
                ActionBtn.Content = "Disable Mod";
                ActionBtn.Background = new SolidColorBrush(Color.Parse("#ff4444"));
            }
            else
            {
                ActionBtn.Content = "Enable Mod";
                ActionBtn.Background = new SolidColorBrush(Color.Parse("#00ffcc"));
            }
        }

        private async void ActionBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ModsListBox.SelectedItem is not ModItem mod) return;

            ActionBtn.IsEnabled = false;
            
            if (mod.IsInstalled)
            {
                await UninstallModAsync(mod);
            }
            else
            {
                await InstallModAsync(mod);
            }
            
            ActionBtn.IsEnabled = true;
            UpdateActionButton(mod);
        }

        private async Task InstallModAsync(ModItem mod)
        {
            string stagingDir = Path.Combine(_gameRoot, "mod-staging", mod.Id);
            bool requiresDownload = !Directory.Exists(stagingDir);

            if (requiresDownload)
            {
                StatusMessageText.Text = "Downloading...";
                DownloadProgressBar.IsVisible = true;
                DownloadProgressBar.Value = 0;

                string tempZip = Path.Combine(Path.GetTempPath(), $"{mod.Id}.zip");

                try
                {
                    // Download
                    using (var response = await _httpClient.GetAsync(mod.DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();
                        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                        var canReportProgress = totalBytes != -1;

                        using (var stream = await response.Content.ReadAsStreamAsync())
                        using (var fileStream = new FileStream(tempZip, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            var buffer = new byte[8192];
                            long totalRead = 0;
                            int bytesRead;
                            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, bytesRead);
                                totalRead += bytesRead;
                                
                                if (canReportProgress)
                                {
                                    var percent = (double)totalRead / totalBytes * 100;
                                    Dispatcher.UIThread.Post(() => 
                                    {
                                        DownloadProgressBar.Value = percent;
                                        DownloadProgressText.Text = $"{Math.Round(percent)}%";
                                    });
                                }
                            }
                        }
                    }

                    StatusMessageText.Text = "Extracting...";
                    DownloadProgressBar.IsVisible = false;

                    await Task.Run(() =>
                    {
                        Directory.CreateDirectory(stagingDir);
                        ZipFile.ExtractToDirectory(tempZip, stagingDir, true);
                    });
                }
                catch (Exception ex)
                {
                    StatusMessageText.Text = "Download failed: " + ex.Message;
                    if (File.Exists(tempZip)) File.Delete(tempZip);
                    DownloadProgressBar.IsVisible = false;
                    return;
                }
                finally
                {
                    if (File.Exists(tempZip)) File.Delete(tempZip);
                    DownloadProgressBar.IsVisible = false;
                }
            }

            try
            {
                StatusMessageText.Text = "Enabling...";
                
                // Track files
                var state = new InstalledModState { Id = mod.Id, Version = mod.Version };
                
                await Task.Run(() =>
                {
                    string backupDir = Path.Combine(_gameRoot, "mods-filebackups", mod.Id);

                    // Process files
                    string[] stagedFiles = Directory.GetFiles(stagingDir, "*.*", SearchOption.AllDirectories);
                    
                    foreach (string stagedFile in stagedFiles)
                    {
                        // Get relative path
                        string relPath = Path.GetRelativePath(stagingDir, stagedFile);
                        string targetPath = Path.GetFullPath(Path.Combine(_gameRoot, relPath));
                        
                        // Prevent Zip Slip
                        if (!targetPath.StartsWith(_gameRoot, StringComparison.OrdinalIgnoreCase))
                            continue;

                        // Backup original if it exists
                        if (File.Exists(targetPath))
                        {
                            string backupFilePath = Path.Combine(backupDir, relPath);
                            if (!File.Exists(backupFilePath))
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(backupFilePath)!);
                                // Move the original file to backup dir
                                File.Move(targetPath, backupFilePath, true);
                                state.BackedUpFiles.Add(relPath);
                            }
                        }

                        // Install the modded file
                        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                        File.Copy(stagedFile, targetPath, true);
                        state.Files.Add(relPath);
                    }
                });

                _installedState[mod.Id] = state;
                SaveState();
                
                mod.IsInstalled = true;
                StatusMessageText.Text = "Enabled successfully!";
            }
            catch (Exception ex)
            {
                StatusMessageText.Text = "Enable failed: " + ex.Message;
            }
        }

        private async Task UninstallModAsync(ModItem mod)
        {
            StatusMessageText.Text = "Disabling...";
            
            try
            {
                if (_installedState.TryGetValue(mod.Id, out var state))
                {
                    await Task.Run(() =>
                    {
                        string backupDir = Path.Combine(_gameRoot, "mods-filebackups", mod.Id);

                        // 1. Delete installed files
                        foreach (var relPath in state.Files)
                        {
                            string absPath = Path.Combine(_gameRoot, relPath);
                            if (File.Exists(absPath))
                            {
                                File.Delete(absPath);
                            }
                        }

                        // 2. Restore backed up files
                        if (state.BackedUpFiles != null)
                        {
                            foreach (var relPath in state.BackedUpFiles)
                            {
                                string backupFilePath = Path.Combine(backupDir, relPath);
                                string targetPath = Path.Combine(_gameRoot, relPath);

                                if (File.Exists(backupFilePath))
                                {
                                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                                    File.Move(backupFilePath, targetPath, true);
                                }
                            }
                        }

                        // 3. Clean up backup directory
                        if (Directory.Exists(backupDir))
                        {
                            try { Directory.Delete(backupDir, true); } catch { /* Ignore */ }
                        }
                    });
                    
                    _installedState.Remove(mod.Id);
                    SaveState();
                }
                
                mod.IsInstalled = false;
                StatusMessageText.Text = "Disabled successfully!";
            }
            catch (Exception ex)
            {
                StatusMessageText.Text = "Disable failed: " + ex.Message;
            }
        }

        private void TitleBar_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                BeginMoveDrag(e);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SearchBox_KeyUp(object sender, KeyEventArgs e)
        {
            string query = SearchBox.Text?.ToLower() ?? "";
            
            if (string.IsNullOrWhiteSpace(query))
            {
                ModsListBox.ItemsSource = _mods;
            }
            else
            {
                var filtered = _mods.Where(m => 
                    m.Name.ToLower().Contains(query) || 
                    m.Author.ToLower().Contains(query) || 
                    m.Category.ToLower().Contains(query)
                ).ToList();
                ModsListBox.ItemsSource = filtered;
            }
        }
    }
}
