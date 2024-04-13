namespace VolumeMasterD;

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
        var pulseAudioApi = new PulseAudioApi();

        volumeMasterCom.VolumeChanged += VmcOnVolumeChanged;
        volumeMasterCom.RequestVolume();
        await Task.Delay(100, stoppingToken);
        volumeMasterCom.RequestVolume();


        void VmcOnVolumeChanged(object? sender, EventArgs e)
        {
            _logger.LogInformation("Volume changed");
            var volumeChangedEventArgs = e as VolumeMasterCom.VolumeMasterCom.VolumeChangedEventArgs;
            var IndexesChanged = volumeChangedEventArgs?.SliderIndexsCahnged;

            var volume = volumeMasterCom.GetVolume();
            var config = volumeMasterCom.Config;

            if (IndexesChanged.Count == 0)

                for (var i = 0; i < volume.Count; i++)
                    foreach (var applicationName in config.SliderApplicationPairs[i])
                    {
                        //map value from 0-1023 to 0-100
                        var newVolume = (int)Math.Round((double)volume[i] / 1023 * 100);
                        pulseAudioApi.SetVolume(applicationName, newVolume);
                        _logger.LogInformation($"Set volume of {applicationName} to {newVolume}");
                    }
            else
                foreach (var index in IndexesChanged)
                foreach (var applicationName in config.SliderApplicationPairs[index])
                {
                    //map value from 0-1023 to 0-100
                    var newVolume = (int)Math.Round((double)volume[index] / 1023 * 100);
                    pulseAudioApi.SetVolume(applicationName, newVolume);
                    _logger.LogInformation($"Set volume of {applicationName} to {newVolume}");
                }


            //pulseAudioApi.SetVolume("Chromium", 50);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
        }
    }
}