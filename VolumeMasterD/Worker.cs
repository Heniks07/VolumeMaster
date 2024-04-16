using VolumeMasterCom;

namespace VolumeMasterD;

public class Worker(ILogger<Worker>? logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var volumeMasterCom = new VolumeMasterCom.VolumeMasterCom(logger);
        var pulseAudioApi = new PulseAudioApi();

        volumeMasterCom.VolumeChanged += VmcOnVolumeChanged;
        volumeMasterCom.RequestVolume();
        await Task.Delay(100, stoppingToken);
        volumeMasterCom.RequestVolume();


        void VmcOnVolumeChanged(object? sender, EventArgs e)
        {
            logger.LogInformation("Volume changed");
            var volumeChangedEventArgs = e as VolumeMasterCom.VolumeMasterCom.VolumeChangedEventArgs;
            var indexesChanged = volumeChangedEventArgs?.SliderIndexesChanged;

            var volume = volumeMasterCom.GetVolume();
            var config = volumeMasterCom.Config;

            ChangeVolume(indexesChanged, volume, config, pulseAudioApi);


            //pulseAudioApi.SetVolume("Chromium", 50);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
        }
    }

    private void ChangeVolume(List<int>? indexesChanged, List<int> volume, Config? config, PulseAudioApi pulseAudioApi)
    {
        if (indexesChanged is { Count: 0 })
            ChangeEveryVolume(volume, config, pulseAudioApi);
        else if (indexesChanged is not null)
            ChangeSliderVolumes(indexesChanged, volume, config, pulseAudioApi);
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
        foreach (var index in indexesChanged)
        foreach (var applicationName in config.SliderApplicationPairs[index])
        {
            //map value from 0-1023 to 0-100
            var newVolume = (int)Math.Round((double)volume[index] / 1023 * 100);
            pulseAudioApi.SetVolume(applicationName, newVolume);
            logger.LogInformation($"Set volume of {applicationName} to {newVolume}");
        }
    }

    /// <summary>
    ///     Change the volume of all applications
    /// </summary>
    /// <param name="volume">a list of the new Volume values</param>
    /// <param name="config">The config from the config file</param>
    /// <param name="pulseAudioApi">An instance of the Api</param>
    private void ChangeEveryVolume(IReadOnlyList<int> volume, Config? config, PulseAudioApi pulseAudioApi)
    {
        for (var i = 0; i < volume.Count; i++)
            foreach (var applicationName in config.SliderApplicationPairs[i])
            {
                //map value from 0-1023 to 0-100
                var newVolume = (int)Math.Round((double)volume[i] / 1023 * 100);
                pulseAudioApi.SetVolume(applicationName, newVolume);
                logger.LogInformation($"Set volume of {applicationName} to {newVolume}");
            }
    }
}