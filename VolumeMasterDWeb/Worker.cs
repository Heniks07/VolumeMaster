using VolumeMasterCom;
using VolumeMasterDWeb;

namespace VolumeMasterD;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker>? _logger;
    private readonly int _timeout;
    public readonly PulseAudioApi AudioApi = new();
    public readonly List<int> LastActualVolume = new();
    public readonly List<bool> LastOverrideActive = new();
    public readonly List<int> LastVolume = new();
    public readonly VolumeMasterCom.VolumeMasterCom VolumeMasterCom;

    private DateTime _lastConfigUpdate = DateTime.MinValue;

    public Worker(ILogger<Worker>? logger)
    {
        _logger = logger;
        VolumeMasterCom = new VolumeMasterCom.VolumeMasterCom(logger);
        _timeout = (int)Math.Round(1000.0d / VolumeMasterCom.Config!.BaudRate *
                                   (VolumeMasterCom.Config.SliderCount * 4 + VolumeMasterCom.Config.SliderCount - 1)) *
                   2;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        VolumeMasterCom.PlayPause += PlayPause;
        VolumeMasterCom.Next += Next;
        VolumeMasterCom.Previous += Previous;
        VolumeMasterCom.Stop += Stop;


        _logger?.LogInformation("Config path is: " + VolumeMasterCom.ConfigPath());

        _logger?.LogInformation("Starting VolumeMasterD");
        //Update the volume of the applications every 10 seconds

        await Task.Delay(10, stoppingToken);


        while (!stoppingToken.IsCancellationRequested) await Update(stoppingToken, VolumeMasterCom);
    }

    private async Task Update(CancellationToken stoppingToken, VolumeMasterCom.VolumeMasterCom volumeMasterCom)
    {
        if ((DateTime.Now - _lastConfigUpdate).TotalMilliseconds > 10000)
        {
            _lastConfigUpdate = DateTime.Now;
            volumeMasterCom.ConfigHelper();
        }

        try
        {
            if (AudioApi.CheckForChanges().Result)
            {
                ChangeEveryVolume(volumeMasterCom.GetVolume().Volume, volumeMasterCom.Config);
                return;
            }

            var changes = VolumeMasterCom.GetVolume();
            var indexesChanged = changes.SliderIndexesChanged;
            var volume = changes.Volume;
            var actualVolume = changes.ActualVolume;
            var overrideActive = changes.OverrideActive;

            LastVolume.Clear();
            LastVolume.AddRange(volume);
            LastActualVolume.Clear();
            LastActualVolume.AddRange(actualVolume);
            LastOverrideActive.Clear();
            LastOverrideActive.AddRange(overrideActive);

            var config = volumeMasterCom.Config;

            ChangeVolume(indexesChanged, volume, config, AudioApi, actualVolume, overrideActive);
        }
        catch (UnauthorizedAccessException)
        {
            await Task.Delay(1000, stoppingToken);
            _logger?.LogWarning("Please reconnect the Arduino");
        }
        catch (Exception exception)
        {
            if (exception is FormatException) return;
            _logger?.LogError(exception, "Error while changing volume");
        }
    }

    private void PlayPause(object? sender, EventArgs e)
    {
        MultiMediaApi.Pause();
    }

    private void Next(object? sender, EventArgs e)
    {
        MultiMediaApi.Next();
    }

    private void Previous(object? sender, EventArgs e)
    {
        MultiMediaApi.Previous();
    }

    private void Stop(object? sender, EventArgs e)
    {
        MultiMediaApi.Stop();
    }

    private void ChangeVolume(List<int>? indexesChanged, List<int> volume, Config? config, PulseAudioApi audioApi,
        List<int>? actualVolume, List<bool>? overrideActive)
    {
        if (indexesChanged is null)
            return;
        if (indexesChanged.Count == 0 || indexesChanged.Count(x => x == 0) == indexesChanged.Count) return;

        OnVolumeChanged(new VolumeChangedEventArgs
        {
            SliderIndexesChanged = indexesChanged,
            Volume = volume,
            ActualVolume = actualVolume,
            OverrideActive = overrideActive
        });

        ChangeSliderVolumes(indexesChanged, volume, config, audioApi);
    }

    /// <summary>
    ///     Change the volume of the applications that are associated with the sliders that have changed
    /// </summary>
    /// <param name="indexesChanged">The indexes of all the sliders that changed</param>
    /// <param name="volume">The new volume values of all the sliders that changed</param>
    /// <param name="config">The config from the config file</param>
    /// <param name="pulseAudioApi">An instance of the Api</param>
    private void ChangeSliderVolumes(List<int> indexesChanged, List<int> volume, Config? config,
        PulseAudioApi pulseAudioApi)
    {
        for (var i = 0; i < indexesChanged.Count; i++)
        {
            if (indexesChanged[i] == 0)
                continue;
            ApplyVolumeChange(volume, config, i);
        }
    }

    private void ApplyVolumeChange(List<int> volume, Config? config, int i)
    {
        try
        {
            if (config?.SliderApplicationPairsPresets[config.SelectedPreset].Count <= i) return;
            foreach (var applicationName in config?.SliderApplicationPairsPresets[config.SelectedPreset][i]!)
            {
                //map value from 0-1023 to 0-100
                var newVolume = (int)Math.Round((double)volume[i] / 1023 * 100, 2);
                AudioApi.SetVolume(applicationName, newVolume);
#if DEBUG
                _logger?.LogInformation($"Set volume of {applicationName} to {newVolume}");
#endif
            }
        }
        catch (Exception? e)
        {
            _logger?.LogError(e, "Error while setting volume");
        }
    }

    /// <summary>
    ///     Change the volume of all applications
    /// </summary>
    /// <param name="volume">a list of the new Volume values</param>
    /// <param name="config">The config from the config file</param>
    private void ChangeEveryVolume(IReadOnlyList<int> volume, Config? config)
    {
        for (var i = 0; i < config?.SliderApplicationPairsPresets[config.SelectedPreset].Count; i++)
            foreach (var applicationName in config.SliderApplicationPairsPresets[config.SelectedPreset][i])
            {
                //map value from 0-1023 to 0-100
                var newVolume = (int)Math.Round((double)volume[i] / 1023 * 100);
                AudioApi.SetVolume(applicationName, newVolume);
#if DEBUG
                _logger?.LogInformation($"Set volume of {applicationName} to {newVolume}");
#endif
            }
    }

    public void SetVolume(int sliderIndex, int volume)
    {
        Task.Run(() => VolumeMasterCom.AddManualOverride(sliderIndex, volume));
    }

    public event EventHandler<VolumeChangedEventArgs>? VolumeChanged;

    protected virtual void OnVolumeChanged(VolumeChangedEventArgs e)
    {
        VolumeChanged?.Invoke(this, e);
    }
}

public class VolumeChangedEventArgs
{
    public List<int>? Volume { get; set; }
    public List<int>? ActualVolume { get; set; }
    public List<bool>? OverrideActive { get; set; }


    public List<int>? SliderIndexesChanged { get; set; }
}