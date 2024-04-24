using VolumeMasterCom;

namespace VolumeMasterService;

public class Worker(ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var volumeMasterCom = new VolumeMasterCom.VolumeMasterCom(logger);
        var audioApi = new AudioApi();

        //volumeMasterCom.VolumeChanged += VmcOnVolumeChanged;
        // volumeMasterCom.RequestVolume();
        // await Task.Delay(100, stoppingToken);
        // volumeMasterCom.RequestVolume();


        // void VmcOnVolumeChanged(object? sender, EventArgs e)
        // {
        //     try
        //     {
        //         var volumeChangedEventArgs = e as VolumeMasterCom.VolumeMasterCom.VolumeChangedEventArgs;
        //         var indexesChanged = volumeChangedEventArgs?.SliderIndexesChanged;
        //         // ReSharper disable once AccessToModifiedClosure
        //         var volume = volumeMasterCom.GetVolume();
        //         // ReSharper disable once AccessToModifiedClosure
        //         var config = volumeMasterCom.Config;
        //
        //         ChangeVolume(indexesChanged, volume, config, audioApi);
        //     }
        //     catch (Exception exception)
        //     {
        //         _logger.LogError(exception, "Error while changing volume");
        //     }
        // }
        var lastConfigUpdate = DateTime.MinValue;
        while (!stoppingToken.IsCancellationRequested)
        {
            if ((DateTime.Now - lastConfigUpdate).TotalMilliseconds > 10000)
            {
                lastConfigUpdate = DateTime.Now;
                volumeMasterCom.ConfigHelper();
            }

            try
            {
                var changes = volumeMasterCom.GetVolume();
                var indexesChanged = changes.SliderIndexesChanged;
                var volume = changes.Volume;


                var config = volumeMasterCom.Config;

                ChangeVolume(indexesChanged, volume, config, audioApi);
            }
            catch (UnauthorizedAccessException)
            {
                await Task.Delay(1000, stoppingToken);
                logger?.LogWarning("Please reconnect the Arduino");
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error while changing volume");
            }
        }
    }

    private void ChangeVolume(List<int>? indexesChanged, List<int> volume, Config? config, AudioApi audioApi)
    {
        if (indexesChanged is null)
            ChangeEveryVolume(volume, config, audioApi);
        else if (indexesChanged.Count > 0)
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
        foreach (var index in indexesChanged)
            try
            {
                foreach (var applicationName in config?.SliderApplicationPairsPresets[config.SelectedPreset][index]!)
                {
                    //map value from 0-1023 to 0-100
                    var newVolume = (float)Math.Round((double)volume[index] / 1023, 2);
                    audioApi.SetVolume(applicationName, newVolume);
#if DEBUG
                    logger.LogInformation($"Set volume of {applicationName} to {newVolume}");
#endif
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while setting volume");
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
        for (var i = 0; i < volume.Count; i++)
            try
            {
                foreach (var applicationName in config?.SliderApplicationPairsPresets[config.SelectedPreset][i]!)
                {
                    //map value from 0-1023 to 0-100
                    var newVolume = (float)Math.Round((double)volume[i] / 1023, 2);
                    audioApi.SetVolume(applicationName, newVolume);
#if DEBUG
                    logger.LogInformation($"Set volume of {applicationName} to {newVolume}");
#endif
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while setting volume");
            }
    }
}