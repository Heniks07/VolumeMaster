using VolumeMasterCom;

namespace VolumeMasterServiceWeb;

public class Worker : BackgroundService
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

        var lastConfigUpdate = DateTime.MinValue;
        while (!stoppingToken.IsCancellationRequested)
        {
            if ((DateTime.Now - lastConfigUpdate).TotalMilliseconds > 10000)
            {
                lastConfigUpdate = DateTime.Now;
                VolumeMasterCom.ConfigHelper();
            }

            try
            {
                Thread.Sleep(_timeout);
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


                var config = VolumeMasterCom.Config;

                ChangeVolume(indexesChanged, volume, config, AudioApi, actualVolume, overrideActive);
            }
            catch (UnauthorizedAccessException)
            {
                await Task.Delay(1000, stoppingToken);
                _logger?.LogWarning("Please reconnect the Arduino");
            }
            catch (Exception? exception)
            {
                _logger?.LogError(exception, "Error while changing volume");
            }
        }
    }

    private void ChangeVolume(List<int>? indexesChanged, List<int> volume, Config? config, AudioApi audioApi,
        List<int>? actualVolume, List<bool>? overrideActive)
    {
        if (indexesChanged is null)
            return;

        // if (indexesChanged.Contains(-1))
        // {
        //     ChangeEveryVolume(volume, config, audioApi);
        // }
        /*else*/
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
            ApplyVolumeChange(volume, config, audioApi, i);
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

    private void ApplyVolumeChange(List<int> volume, Config? config, AudioApi audioApi, int i)
    {
        try
        {
            if (config?.SliderApplicationPairsPresets[config.SelectedPreset].Count <= i) return;
            foreach (var applicationName in config?.SliderApplicationPairsPresets[config.SelectedPreset][i]!)
            {
                //map value from 0-1023 to 0-100
                var newVolume = (float)Math.Round((double)volume[i] / 1023, 2);
                audioApi.SetVolume(applicationName, newVolume);
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
                    audioApi.SetVolume(applicationName, newVolume);
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