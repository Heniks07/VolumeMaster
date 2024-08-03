using VolumeMasterCom;

namespace VolumeMasterServiceWeb;

public partial class Worker : BackgroundService
{
    private readonly ILogger<Worker>? _logger;
    private readonly int _timeout;
    public readonly AudioApi AudioApi = new();
    public readonly List<int> LastActualVolume = new();
    public readonly List<bool> LastOverrideActive = new();
    public readonly List<int> LastVolume = new();
    public readonly VolumeMasterCom.VolumeMasterCom VolumeMasterCom;

    public Worker(ILogger<Worker>? logger)
    {
        _logger = logger;
        VolumeMasterCom = new(logger);
        _timeout = (int)Math.Round(1000.0d / VolumeMasterCom.Config!.BaudRate *
                                   (VolumeMasterCom.Config.SliderCount * 4 + VolumeMasterCom.Config.SliderCount - 1)) *
                   2;
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        AudioApi.Device.AudioSessionManager2!.OnSessionCreated += (_, _) =>
        {
            ChangeEveryVolume(VolumeMasterCom.GetVolume().Volume, VolumeMasterCom.Config, AudioApi);
        };
        VolumeMasterCom.PlayPause += (_, _) => MultiMediaApi.Pause();
        VolumeMasterCom.Next += (_, _) => MultiMediaApi.Next();
        VolumeMasterCom.Previous += (_, _) => MultiMediaApi.Previous();
        VolumeMasterCom.Stop += (_, _) => MultiMediaApi.Stop();

        var lastConfigUpdate = DateTime.MinValue;

        //loop that runs as long as the service is running
        await Loop(stoppingToken, lastConfigUpdate);
    }


    private void ChangeVolume(List<int>? indexesChanged, List<int> volume, Config? config, AudioApi audioApi,
        List<int>? actualVolume, List<bool>? overrideActive)
    {
        if (indexesChanged is null)
            return;
        if (indexesChanged.Count <= 0 || indexesChanged.Count(x => x == 0) == indexesChanged.Count) return;

        OnVolumeChanged(new VolumeChangedEventArgs()
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
    /// <param name="audioApi">An instance of the Api</param>
    private void ChangeSliderVolumes(List<int> indexesChanged, List<int> volume, Config? config, AudioApi audioApi)
    {
        for (var i = 0; i < indexesChanged.Count; i++)
        {
            if (indexesChanged[i] == 0)
                continue;
            ApplyVolumeChange(volume, config, i);
        }
        // for (var i = 0; i < indexesChanged.Count; i++)
        // {
        //     if (indexesChanged[i] != -1)
        //         continue;
        //     ApplyVolumeChange(volume, config, audioApi, i);
        // }
        // if(indexesChanged.Any(x=>x == 1) && indexesChanged.Any(x=>x == -1))
        //     Thread.Sleep((int)config?.DecreaseBeforeIncreaseTimeout!);
        // for (var i = 0; i < indexesChanged.Count; i++)
        // {
        //     if(indexesChanged[i] == 0)
        //         continue;
        //     ApplyVolumeChange(volume, config, audioApi, i);
        // }
    }

    private void ApplyVolumeChange(List<int> volume, Config? config, int i)
    {
        try
        {
            if (config?.SliderApplicationPairsPresets[config.SelectedPreset].Count <= i) return;
            foreach (var applicationName in config?.SliderApplicationPairsPresets[config.SelectedPreset][i]!)
            {
                //map value from 0-1023 to 0-100
                var newVolume = (float)Math.Round((double)volume[i] / 1023, 2);
                AudioApi.SetVolume(applicationName, newVolume,
                    config?.SliderApplicationPairsPresets[config.SelectedPreset]);
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
    /// <param name="audioApi">An instance of the Api</param>
    private void ChangeEveryVolume(List<int> volume, Config? config, AudioApi audioApi)
    {
        for (var i = 0; i < config?.SliderApplicationPairsPresets[config.SelectedPreset].Count; i++)
            try
            {
                foreach (var applicationName in config.SliderApplicationPairsPresets[config.SelectedPreset][i])
                {
                    //map value from 0-1023 to 0-100
                    var newVolume = (float)Math.Round((double)volume[i] / 1023, 2);
                    audioApi.SetVolume(applicationName, newVolume,
                        config.SliderApplicationPairsPresets[config.SelectedPreset]);
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

    public void SetVolume(int sliderIndex, int volume)
    {
        Task.Run(() => VolumeMasterCom.AddManualOverride(sliderIndex, volume));
    }

    // an event that is triggered when the volume changes
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