using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace PsobbLauncher
{
    public partial class AddServerDialog : Window
    {
        public AddServerDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
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

            var profile = new ServerProfile
            {
                Name = string.IsNullOrWhiteSpace(nameBox?.Text) ? host : nameBox!.Text!.Trim(),
                LoginHost = host,
                PatchHost = host,
                LoginPort = loginPort == 0 ? 12000 : loginPort,
                PatchPort = patchPort == 0 ? 11000 : patchPort,
            };

            Close(profile);
        }
    }
}