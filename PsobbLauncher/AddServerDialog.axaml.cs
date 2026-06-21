using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace PsobbLauncher
{
    public partial class AddServerDialog : Window
    {
        private readonly ServerProfile? _editing;

        public AddServerDialog() : this(null) { }

        public AddServerDialog(ServerProfile? existing)
        {
            _editing = existing;
            InitializeComponent();
            if (_editing != null)
                Populate(_editing);
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        private void Populate(ServerProfile p)
        {
            Title = "Edit Server";
            var nameBox = this.FindControl<TextBox>("NameBox");
            var hostBox = this.FindControl<TextBox>("HostBox");
            var loginPortBox = this.FindControl<TextBox>("LoginPortBox");
            var patchPortBox = this.FindControl<TextBox>("PatchPortBox");

            if (nameBox != null) nameBox.Text = p.Name;
            if (hostBox != null) hostBox.Text = p.LoginHost;
            if (loginPortBox != null) loginPortBox.Text = p.LoginPort.ToString();
            if (patchPortBox != null) patchPortBox.Text = p.PatchPort.ToString();

            // Hangame fields. Password is DPAPI-protected at rest; we don't
            // round-trip the plaintext into the box on edit — leave it blank
            // and only overwrite the stored blob if the user types a new one.
            var hangameCheck = this.FindControl<CheckBox>("HangameModeCheck");
            var hangameUser = this.FindControl<TextBox>("HangameUserBox");
            if (hangameCheck != null) hangameCheck.IsChecked = p.AuthMode == AuthMode.Hangame;
            if (hangameUser != null) hangameUser.Text = p.HangameUsername;

            var installPathBox = this.FindControl<TextBox>("InstallPathBox");
            if (installPathBox != null) installPathBox.Text = p.InstallPath ?? "";
        }

        private void HangameMode_Changed(object? sender, RoutedEventArgs e)
        {
            var panel = this.FindControl<StackPanel>("HangamePanel");
            var check = this.FindControl<CheckBox>("HangameModeCheck");
            if (panel != null && check != null)
                panel.IsVisible = check.IsChecked == true;
        }
        private async void BrowseInstallPath_Click(object? sender, RoutedEventArgs e)
        {
            var sp = StorageProvider;
            if (sp is null)
                return;

            var folders = await sp.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select the server's install folder (containing psobb.exe)",
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                string? path = folders[0].TryGetLocalPath();
                var box = this.FindControl<TextBox>("InstallPathBox");
                if (box != null && !string.IsNullOrEmpty(path))
                    box.Text = path;
            }
        }
        private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);

        private void Save_Click(object? sender, RoutedEventArgs e)
        {
            ClearErrors();

            var nameBox = this.FindControl<TextBox>("NameBox");
            var hostBox = this.FindControl<TextBox>("HostBox");
            var loginPortBox = this.FindControl<TextBox>("LoginPortBox");
            var patchPortBox = this.FindControl<TextBox>("PatchPortBox");
            var hangameCheck = this.FindControl<CheckBox>("HangameModeCheck");
            var hangameUser = this.FindControl<TextBox>("HangameUserBox");
            var hangamePass = this.FindControl<TextBox>("HangamePassBox");
            var installPathBox = this.FindControl<TextBox>("InstallPathBox");

            string host = hostBox?.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(host))
                return;

            bool hangameOn = hangameCheck?.IsChecked == true;

            // Validate Hangame fields only when that mode is active.
            if (hangameOn)
            {
                string u = hangameUser?.Text?.Trim() ?? "";
                if (!ValidateHangameUser(u, out string ue))
                {
                    ShowError("HangameUserError", ue);
                    return;
                }

                // Password is required on add; on edit it may be left blank to
                // keep the existing stored blob.
                string pw = hangamePass?.Text ?? "";
                bool editingWithExisting =
                    _editing?.HangameProtectedPassword is { Length: > 0 };
                if (!(pw.Length == 0 && editingWithExisting))
                {
                    if (!ValidateHangamePass(pw, out string pe))
                    {
                        ShowError("HangamePassError", pe);
                        return;
                    }
                }
            }
            // Validate install path: blank is fine (means "use launcher dir").
            // If set, the folder must actually contain psobb.exe — otherwise
            // launch would fail (ResolveGameExe treats a custom path as
            // authoritative with no fallback).
            string installPath = installPathBox?.Text?.Trim() ?? "";
            if (installPath.Length > 0)
            {
                if (!Directory.Exists(installPath))
                {
                    ShowError("InstallPathError", "That folder doesn't exist.");
                    return;
                }
                if (!File.Exists(Path.Combine(installPath, "psobb.exe")))
                {
                    ShowError("InstallPathError", "No psobb.exe found in that folder.");
                    return;
                }
            }

            int.TryParse(loginPortBox?.Text, out int loginPort);
            int.TryParse(patchPortBox?.Text, out int patchPort);

            var profile = _editing ?? new ServerProfile();

            profile.Name = string.IsNullOrWhiteSpace(nameBox?.Text) ? host : nameBox!.Text!.Trim();
            profile.LoginHost = host;
            profile.PatchHost = host;
            profile.LoginPort = loginPort == 0 ? 12000 : loginPort;
            profile.PatchPort = patchPort == 0 ? 11000 : patchPort;

            profile.AuthMode = hangameOn ? AuthMode.Hangame : AuthMode.Standard;
            // Empty -> null so "blank" genuinely means "use default location".
            profile.InstallPath = installPath.Length > 0 ? installPath : null;
            if (hangameOn)
            {
                profile.HangameUsername = hangameUser?.Text?.Trim() ?? "";

                string pw = hangamePass?.Text ?? "";
                if (pw.Length > 0 && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Protect like the captured blob: UTF-8 bytes -> DPAPI.
                    var bytes = System.Text.Encoding.UTF8.GetBytes(pw);
                    profile.HangameProtectedPassword = CredentialProtector.Protect(bytes);
                }
                // else: keep existing HangameProtectedPassword untouched (edit case)
            }
            // Standard creds (Account / ProtectedPassword) are left untouched here —
            // they're set by the capture flow, and persist independently.

            Close(profile);
        }

        private void ShowError(string controlName, string msg)
        {
            var tb = this.FindControl<TextBlock>(controlName);
            if (tb != null) tb.Text = msg;
        }

        private void ClearErrors()
        {
            var u = this.FindControl<TextBlock>("HangameUserError");
            var p = this.FindControl<TextBlock>("HangamePassError");
            var i = this.FindControl<TextBlock>("InstallPathError");
            if (u != null) u.Text = "";
            if (p != null) p.Text = "";
            if (i != null) i.Text = "";
        }
        // --- Hangame credential format rules (from newserv issue #401) ---
        private static bool ValidateHangameUser(string u, out string error)
        {
            error = "";
            if (string.IsNullOrEmpty(u)) { error = "Username required."; return false; }
            if (!u.EndsWith("@HG", StringComparison.Ordinal))
            { error = "Username must end in '@HG'."; return false; }
            if (u.Length > 11)
            { error = "Username must be 11 characters or fewer (including @HG)."; return false; }
            return true;
        }

        private static bool ValidateHangamePass(string p, out string error)
        {
            error = "";
            if (p.Length is < 1 or > 8) { error = "Password must be 1-8 digits."; return false; }
            if (!p.All(char.IsDigit)) { error = "Password must be numeric."; return false; }
            return true;
        }
    }
}