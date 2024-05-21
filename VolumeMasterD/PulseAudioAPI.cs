using System.Diagnostics;
using Newtonsoft.Json;

namespace VolumeMasterD;

public class PulseAudioApi
{
    private List<SinkInput>? _inputs = new();

    private async Task<List<SinkInput>?> RequestInputs()
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
        return inputs;
    }

    public async Task<bool> CheckForChanges()
    {
        var newInputs = await RequestInputs();
        if (newInputs == null)
            return false;
        if (newInputs.Count == 0)
            return false;
        if (newInputs.All(_inputs!.Contains))
            return false;
        _inputs = newInputs;
        return true;
    }

    public async void SetVolume(string applicationName, int volumePercent)
    {
        if (applicationName == "master")
            await SetSinkVolume(volumePercent);
        else
            await SetSinkInputVolume(applicationName, volumePercent);
    }

    private async Task SetSinkInputVolume(string applicationName, int volumePercent)
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

public class SinkInput : IEquatable<SinkInput>
{
    [JsonProperty("index")] public int Index { get; set; }

    [JsonProperty("sink")] public int Sink { get; set; }

    [JsonProperty("volume")] public Volume? Volume { get; set; }

    [JsonProperty("properties")] public Properties? Properties { get; set; }

    public bool Equals(SinkInput? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Index == other.Index && Sink == other.Sink && Equals(Volume, other.Volume) &&
               Equals(Properties, other.Properties);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((SinkInput)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Index, Sink, Volume, Properties);
    }
}

public class Volume : IEquatable<Volume>
{
    [JsonProperty("mono")] public VolumePercent? Mono { get; set; }

    [JsonProperty("front-right")] public VolumePercent? FrontRight { get; set; }

    public bool Equals(Volume? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Equals(Mono, other.Mono) && Equals(FrontRight, other.FrontRight);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((Volume)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Mono, FrontRight);
    }
}

public class VolumePercent : IEquatable<VolumePercent>
{
    [JsonProperty("value_percent")] public string? ValuePercent { get; set; }

    public bool Equals(VolumePercent? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return ValuePercent == other.ValuePercent;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((VolumePercent)obj);
    }

    public override int GetHashCode()
    {
        return ValuePercent != null ? ValuePercent.GetHashCode() : 0;
    }
}

public class Properties : IEquatable<Properties>
{
    [JsonProperty("application.name")] public string? ApplicationName { get; set; }

    [JsonProperty("application.process.binary")]
    public string? ApplicationProcessBinary { get; set; }

    public bool Equals(Properties? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return ApplicationName == other.ApplicationName && ApplicationProcessBinary == other.ApplicationProcessBinary;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((Properties)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ApplicationName, ApplicationProcessBinary);
    }
}