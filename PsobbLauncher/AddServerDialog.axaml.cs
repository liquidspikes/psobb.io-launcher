using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

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

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

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
        }

        private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);

        private void Save_Click(object? sender, RoutedEventArgs e)
        {
            var nameBox = this.FindControl<TextBox>("NameBox");
            var hostBox = this.FindControl<TextBox>("HostBox");
            var loginPortBox = this.FindControl<TextBox>("LoginPortBox");
            var patchPortBox = this.FindControl<TextBox>("PatchPortBox");

            string host = hostBox?.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(host))
                return;

            int.TryParse(loginPortBox?.Text, out int loginPort);
            int.TryParse(patchPortBox?.Text, out int patchPort);

            // Edit in place to preserve Id and captured credentials;
            // or create a new profile when adding.
            var profile = _editing ?? new ServerProfile();

            profile.Name = string.IsNullOrWhiteSpace(nameBox?.Text) ? host : nameBox!.Text!.Trim();
            profile.LoginHost = host;
            profile.PatchHost = host;
            profile.LoginPort = loginPort == 0 ? 12000 : loginPort;
            profile.PatchPort = patchPort == 0 ? 11000 : patchPort;

            Close(profile);
        }
    }
}