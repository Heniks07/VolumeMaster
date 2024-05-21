using System.Text;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Newtonsoft.Json;

namespace VolumeMasterRemote;

public partial class MainPage
{
    private const ushort MaxValue = 1023;
    private readonly HttpClient _httpClient = new();

    private readonly View _originalContent;
    private List<ushort> _actualVolumeValues = new();

    private CancellationTokenSource _cancellationTokenSource = new();

    private Config? _config;
    private List<bool> _overrideActive = new();
    private List<Slider> _sliders = new();
    private string _url = "http://192.168.178.10:5195";
    private List<ushort> _volumeValues = new();

    public MainPage()
    {
        InitializeComponent();
        _httpClient.Timeout = TimeSpan.FromMilliseconds(10000);
        Content.FindByName<Entry>("UrlEntry").Text = _url;
        _originalContent = Content;
        HandleNetworkAndBuildUi();
    }

    private void CreateSliders(StackLayout stackLayout)
    {
        for (var index = 0; index < _config?.SliderCount; index++)
        {
            var value = 0;
            if (_volumeValues.Count > index)
                value = _volumeValues[index];

            var nameText = "Unasigned";
            if (_config?.SliderApplicationPairsPresets[_config.SelectedPreset].Count > index &&
                _config.SliderApplicationPairsPresets[_config.SelectedPreset][index].Count > 0)
                nameText = _config?.SliderApplicationPairsPresets[_config.SelectedPreset][index][0] ?? "loading...";

            var name = new Label
            {
                Text = nameText,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(5, 5, 5, 0)
            };

            var slider = new Slider
            {
                Maximum = 100,
                Minimum = 0,
                Value = value,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                Margin = new Thickness(15, 2, 15, 20),
            };

            var sliderIndex = index;
            slider.ValueChanged += async (sender, _) => { await OnValueChanged(sender, sliderIndex); };
            stackLayout.Children.Add(name);
            stackLayout.Children.Add(slider);
            _sliders.Add(slider);
        }
    }

    private async Task OnValueChanged(object? sender, int sliderIndex)
    {
        try
        {
            var sliderValue = (int)((Slider)sender!).Value;
            if (Math.Abs(sliderValue - _volumeValues[sliderIndex]) < 2) return;


            var response = await _httpClient.PostAsync(
                $"{_url}/SetVolume?sliderIndex={sliderIndex}&volume={(int)Math.Round(sliderValue / 100.0 * MaxValue)}",
                new StringContent("", Encoding.UTF8, "application/json"));

            response.EnsureSuccessStatusCode();
        }
        catch (Exception e)
        {
            Console.WriteLine($"An error occurred: {e.Message}");
        }
    }

    private async void HandleNetworkAndBuildUi()
    {
        StackLayout stackLayout = new StackLayout
        {
            VerticalOptions = LayoutOptions.Fill,
            HorizontalOptions = LayoutOptions.Fill
        };
        // Perform the network operations on a background thread
        await Task.Run(async () => await LoadConfig());
        CreateUrlInput(stackLayout);
        CreateMultiMediaButtons(stackLayout);
        CreateSliders(stackLayout);
        var scrollView = new ScrollView
        {
            Content = stackLayout
        };
        Content = scrollView;
        await Task.Run(async () => await StartSse(), _cancellationTokenSource.Token);
    }

    private void CreateUrlInput(StackLayout stackLayout)
    {
        var label = new Label
        {
            Text = "Enter the Url of the VolumeMasterService",
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Margin = new Thickness(5)
        };
        stackLayout.Children.Add(label);
        var entryStackLayout = new StackLayout
        {
            Orientation = StackOrientation.Horizontal,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        var entry = new Entry
        {
            Text = _url,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            Margin = new Thickness(5)
        };
        var button = new Button
        {
            Text = "Set Url",
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            Margin = new Thickness(5)
        };
        button.Clicked += (sender, args) => { OnSetUrl(entry); };
        entryStackLayout.Children.Add(entry);
        entryStackLayout.Children.Add(button);
        stackLayout.Children.Add(entryStackLayout);
    }

    private void OnSetUrl(Entry entry)
    {
        _url = entry.Text;
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();
        Content = _originalContent;

        Content.FindByName<ActivityIndicator>("Indicator").IsRunning = true;

        _sliders = new();
        _volumeValues = new();
        _actualVolumeValues = new();
        _overrideActive = new();
        _config = null;

        HandleNetworkAndBuildUi();
    }

    private void CreateMultiMediaButtons(StackLayout stackLayout)
    {
        var buttonStackLayout = new StackLayout
        {
            Orientation = StackOrientation.Horizontal,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 0, 0, 20)
        };

        var previousButton = new Button
        {
            Text = "Previous",
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            Margin = new Thickness(5)
        };
        previousButton.Clicked += async (sender, args) =>
        {
            try
            {
                var response = await _httpClient.PostAsync($"{_url}/Previous",
                    new StringContent("", Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred: {e.Message}");
            }
        };

        var playButton = new Button
        {
            Text = "Play/Pause",
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            Margin = new Thickness(5)
        };
        playButton.Clicked += async (sender, args) =>
        {
            try
            {
                var response = await _httpClient.PostAsync($"{_url}/PlayPause",
                    new StringContent("", Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred: {e.Message}");
            }
        };

        var nextButton = new Button
        {
            Text = "Next",
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            Margin = new Thickness(5)
        };
        nextButton.Clicked += async (sender, args) =>
        {
            try
            {
                var response = await _httpClient.PostAsync($"{_url}/Next",
                    new StringContent("", Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred: {e.Message}");
            }
        };

        buttonStackLayout.Children.Add(previousButton);
        buttonStackLayout.Children.Add(playButton);
        buttonStackLayout.Children.Add(nextButton);
        stackLayout.Children.Add(buttonStackLayout);
    }

    private async Task LoadConfig()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_url}/GetConfig");
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            _config = JsonConvert.DeserializeObject<Config>(responseString);
        }
        catch (TaskCanceledException ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            //show a toast notification
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Content.FindByName<ActivityIndicator>("Indicator").IsRunning = false;
                var toast = Toast.Make("Connection timed out! Is the service running?", ToastDuration.Long);
                toast.Show();
            });
        }
        catch (Exception ex)
        {
            // Handle the error
            Console.WriteLine($"An error occurred: {ex.Message}");
            //show a toast notification
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Content.FindByName<ActivityIndicator>("Indicator").IsRunning = false;
                var toast = Toast.Make(ex.Message, ToastDuration.Long);
                toast.Show();
            });
        }
    }

    private async Task StartSse()
    {
        while (true)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_url}/GetAllVolumes");
                response.EnsureSuccessStatusCode();
                var responseString = await response.Content.ReadAsStringAsync();
                UpdateVolumeValue(responseString);


                var stream = await _httpClient.GetStreamAsync($"{_url}/volumeUpdate");
                using var streamReader = new StreamReader(stream);
                while (!streamReader.EndOfStream)
                {
                    var message = await streamReader.ReadLineAsync();
                    UpdateVolumeValue(message);
                }
            }
            catch (Exception ex)
            {
                // Handle the error
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
        // ReSharper disable once FunctionNeverReturns
    }

    private void UpdateVolumeValue(string? message)
    {
        if (string.IsNullOrEmpty(message)) return;

        var sliderValues = JsonConvert.DeserializeObject<SseData>(message);

        List<ushort> volumeValues = new();


        if (sliderValues is null) return;
        if (sliderValues.Volume is not null)
            volumeValues.AddRange(sliderValues.Volume.Select(sliderValue =>
                (ushort)Math.Round(sliderValue / (double)MaxValue * 100)));

        if (sliderValues.ActualVolume is not null)
            _actualVolumeValues = sliderValues.ActualVolume.Select(sliderValue =>
                (ushort)Math.Round(sliderValue / (double)MaxValue * 100)).ToList();

        if (sliderValues.OverrideActive is not null)
            _overrideActive = sliderValues.OverrideActive;

        _volumeValues = volumeValues;

        for (var i = 0; i < _sliders.Count; i++)
        {
            _sliders[i].Value = _volumeValues[i];
            _sliders[i].BackgroundColor = _overrideActive[i] ? Colors.Red : Colors.Transparent;
        }
    }

    private void Button_OnClicked(object? sender, EventArgs e)
    {
        OnSetUrl(Content.FindByName<Entry>("UrlEntry"));
    }


    class SseData
    {
        public List<int>? Volume { get; set; }
        public List<int>? ActualVolume { get; set; }
        public List<bool>? OverrideActive { get; set; }
    }
}