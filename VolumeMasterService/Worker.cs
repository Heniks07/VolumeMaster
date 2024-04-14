using VolumeMasterCom;

namespace VolumeMasterService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var volumeMasterCom = new VolumeMasterCom.VolumeMasterCom(_logger);
        var audioApi = new AudioApi();

        volumeMasterCom.VolumeChanged += VmcOnVolumeChanged;
        volumeMasterCom.RequestVolume();
        await Task.Delay(100, stoppingToken);
        volumeMasterCom.RequestVolume();


        void VmcOnVolumeChanged(object? sender, EventArgs e)
        {
            _logger.LogInformation("Volume changed");
            var volumeChangedEventArgs = e as VolumeMasterCom.VolumeMasterCom.VolumeChangedEventArgs;
            var indexesChanged = volumeChangedEventArgs?.SliderIndexesChanged;

            var volume = volumeMasterCom.GetVolume();
            var config = volumeMasterCom.Config;

            ChangeVolume(indexesChanged, volume, config, audioApi);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
        }
    }

    private void ChangeVolume(List<int>? indexesChanged, List<int> volume, Config? config, AudioApi audioApi)
    {
        if (indexesChanged is { Count: 0 })
            ChangeEveryVolume(volume, config, audioApi);
        else if (indexesChanged is not null)
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
                foreach (var applicationName in config?.SliderApplicationPairs[index]!)
                {
                    //map value from 0-1023 to 0-100
                    var newVolume = (float)Math.Round((double)volume[index] / 1023, 2);
                    audioApi.SetVolume(applicationName, newVolume);
                    _logger.LogInformation($"Set volume of {applicationName} to {newVolume}");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while setting volume");
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
                foreach (var applicationName in config?.SliderApplicationPairs[i]!)
                {
                    //map value from 0-1023 to 0-100
                    var newVolume = (float)Math.Round((double)volume[i] / 1023, 2);
                    audioApi.SetVolume(applicationName, newVolume);
                    _logger.LogInformation($"Set volume of {applicationName} to {newVolume}");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while setting volume");
            }
    }
}