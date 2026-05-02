using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Layout;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace PsobbLauncher
{
    public partial class EventsWindow : Window
    {
        public EventsWindow()
        {
            InitializeComponent();
            LoadEventsAsync();
        }

        private async void LoadEventsAsync()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync("https://psobb.io/api/events.php");
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var events = JsonSerializer.Deserialize<JsonElement>(json);
                        
                        EventsListPanel.Children.Clear();
                        
                        if (events.ValueKind == JsonValueKind.Array && events.GetArrayLength() > 0)
                        {
                            foreach (var ev in events.EnumerateArray())
                            {
                                var title = ev.TryGetProperty("title", out var t) ? t.GetString() : "Unknown Event";
                                var desc = ev.TryGetProperty("description", out var d) ? d.GetString() : "";
                                var start = ev.TryGetProperty("startDate", out var s) ? s.GetString() : "";
                                var end = ev.TryGetProperty("endDate", out var en) ? en.GetString() : "";
                                var type = ev.TryGetProperty("type", out var ty) ? ty.GetString() : "";

                                var card = new Border
                                {
                                    Background = new SolidColorBrush(Color.Parse("#2A2A2A")),
                                    BorderBrush = new SolidColorBrush(Color.Parse("#ffca28")),
                                    BorderThickness = new Thickness(1, 1, 1, 1),
                                    CornerRadius = new CornerRadius(5),
                                    Padding = new Thickness(15),
                                    Child = new StackPanel
                                    {
                                        Children =
                                        {
                                            new TextBlock { Text = title, FontWeight = FontWeight.Bold, FontSize = 18, Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 5) },
                                            new TextBlock { Text = $"Type: {type} | {start} to {end}", Foreground = new SolidColorBrush(Color.Parse("#ffca28")), FontSize = 12, Margin = new Thickness(0, 0, 0, 10) },
                                            new TextBlock { Text = desc, Foreground = new SolidColorBrush(Color.Parse("#CCC")), TextWrapping = TextWrapping.Wrap }
                                        }
                                    }
                                };
                                EventsListPanel.Children.Add(card);
                            }
                        }
                        else
                        {
                            EventsListPanel.Children.Add(new TextBlock { Text = "No upcoming events found.", Foreground = Brushes.Gray, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 50, 0, 0) });
                        }
                    }
                    else
                    {
                        LoadingText.Text = "Failed to load events. Server returned an error.";
                    }
                }
            }
            catch (Exception ex)
            {
                if (EventsListPanel.Children.Contains(LoadingText))
                {
                    LoadingText.Text = "Failed to load events. Could not connect to server.";
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
            this.Close();
        }
    }
}
