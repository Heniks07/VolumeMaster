using System.Diagnostics;
using Newtonsoft.Json;

namespace VolumeMasterD;

public class PulseAudioApi
{
    private List<SinkInput>? _inputs = new();

    private async void RequestInputs()
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "pactl",
                Arguments = "-fjson list sink-inputs",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        await process.WaitForExitAsync();
        var inputs = JsonConvert.DeserializeObject<List<SinkInput>>(output);
        _inputs = inputs;
    }

    public async void SetVolume(string applicationName, int volumePercent)
    {
        RequestInputs();

        if (applicationName == "master")
            await SetSinkVolume(volumePercent);
        else
            await SerSinkInputVolume(applicationName, volumePercent);
    }

    private async Task SerSinkInputVolume(string applicationName, int volumePercent)
    {
        var input = _inputs?.FindAll(i => i.Properties?.ApplicationName == applicationName);
        if (input == null)
            return;

        foreach (var process in input.Select(sinkInput => new Process
                 {
                     StartInfo = new ProcessStartInfo
                     {
                         FileName = "pactl",
                         Arguments = $"set-sink-input-volume {sinkInput.Index} {volumePercent}%",
                         RedirectStandardOutput = true,
                         UseShellExecute = false,
                         CreateNoWindow = true
                     }
                 }))
        {
            process.Start();
            await process.WaitForExitAsync();
        }
    }

    private async Task SetSinkVolume(int volumePercent)
    {
        if (_inputs?.Count == 0) return;
        var sink = _inputs?[0].Sink;

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "pactl",
                Arguments = $"set-sink-volume {sink} {volumePercent}%",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        await process.WaitForExitAsync();
    }
}

public class SinkInput
{
    [JsonProperty("index")] public int Index { get; set; }

    [JsonProperty("sink")] public int Sink { get; set; }

    [JsonProperty("volume")] public Volume? Volume { get; set; }

    [JsonProperty("properties")] public Properties? Properties { get; set; }
}

public class Volume
{
    [JsonProperty("mono")] public VolumePercent? Mono { get; set; }

    [JsonProperty("front-right")] public VolumePercent? FrontRight { get; set; }
}

public class VolumePercent
{
    [JsonProperty("value_percent")] public string? ValuePercent { get; set; }
}

public class Properties
{
    [JsonProperty("application.name")] public string? ApplicationName { get; set; }

    [JsonProperty("application.process.binary")]
    public string? ApplicationProcessBinary { get; set; }
}