using System.Diagnostics;
using VolumeMasterCom;

namespace VolumeMasterD;

public class Worker(ILogger<Worker>? logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var volumeMasterCom = new VolumeMasterCom.VolumeMasterCom(logger);
        var pulseAudioApi = new PulseAudioApi();


        volumeMasterCom.PlayPause += PlayPause;
        volumeMasterCom.Next += Next;
        volumeMasterCom.Previous += Previous;
        volumeMasterCom.Stop += Stop;


        logger?.LogInformation("Config path is: " + volumeMasterCom.ConfigPath());

        logger?.LogInformation("Starting VolumeMasterD");
        //Update the volume of the applications every 10 seconds
        var lastConfigUpdate = DateTime.MinValue;

        await Task.Delay(10, stoppingToken);

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

                ChangeVolume(indexesChanged, volume, config, pulseAudioApi);
            }
            catch (UnauthorizedAccessException)
            {
                await Task.Delay(1000, stoppingToken);
                logger?.LogWarning("Please reconnect the Arduino");
            }
            catch (Exception exception)
            {
                if (exception is FormatException) continue;
                logger?.LogError(exception, "Error while changing volume");
            }
        }
    }

    private void PlayPause(object? sender, EventArgs e)
    {
        ExecuteCommand("playerctl", "play-pause");
    }

    private void Next(object? sender, EventArgs e)
    {
        ExecuteCommand("playerctl", "next");
    }

    private void Previous(object? sender, EventArgs e)
    {
        ExecuteCommand("playerctl", "previous");
    }

    private void Stop(object? sender, EventArgs e)
    {
        ExecuteCommand("playerctl", "stop");
    }

    private void ChangeVolume(List<int>? indexesChanged, List<int> volume, Config? config, PulseAudioApi pulseAudioApi)
    {
        if (indexesChanged is null)
            ChangeEveryVolume(volume, config, pulseAudioApi);
        else if (indexesChanged.Count > 0)
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
        {
            if (config?.SliderApplicationPairsPresets[config.SelectedPreset].Count <= index) continue;
            foreach (var applicationName in config?.SliderApplicationPairsPresets[config.SelectedPreset][index]!)
            {
                //map value from 0-1023 to 0-100
                var newVolume = (int)Math.Round((double)volume[index] / 1023 * 100);
                pulseAudioApi.SetVolume(applicationName, newVolume);
#if DEBUG
                logger?.LogInformation($"Set volume of {applicationName} to {newVolume}");
#endif
            }
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
        for (var i = 0; i < config?.SliderApplicationPairsPresets[config.SelectedPreset].Count; i++)
            foreach (var applicationName in config?.SliderApplicationPairsPresets[config.SelectedPreset][i]!)
            {
                //map value from 0-1023 to 0-100
                var newVolume = (int)Math.Round((double)volume[i] / 1023 * 100);
                pulseAudioApi.SetVolume(applicationName, newVolume);
#if DEBUG
                logger?.LogInformation($"Set volume of {applicationName} to {newVolume}");
#endif
            }
    }


    /// <summary>
    ///     Execute a command with arguments
    /// </summary>
    /// <param name="command">command to execute</param>
    /// <param name="arguments">arguments to pass</param>
    private void ExecuteCommand(string command, string arguments)
    {
        try
        {
            var process = new Process();
            process.StartInfo.FileName = command;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            process.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error executing command: " + ex.Message);
        }
    }
}