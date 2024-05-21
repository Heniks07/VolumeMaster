namespace VolumeMasterRemote;

public class Config
{
    public int ConfigVersionNumber { get; set; } = -1;
    public string PortName { get; set; }
    public int BaudRate { get; set; }

    public int SliderCount { get; set; }

    public ushort Smoothness { get; set; }

    public bool DoSmooth { get; set; }

    public ushort DecreaseBeforeIncreaseTimeout { get; set; }


    public List<List<List<string>>> SliderApplicationPairsPresets { get; set; }

    public ushort SelectedPreset { get; set; }

    public bool UpdateAfterPresetChange { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj is not Config config) return false;
        return ConfigVersionNumber == config.ConfigVersionNumber && PortName == config.PortName &&
               BaudRate == config.BaudRate && SliderCount == config.SliderCount &&
               Smoothness == config.Smoothness && DoSmooth == config.DoSmooth &&
               DecreaseBeforeIncreaseTimeout == config.DecreaseBeforeIncreaseTimeout &&
               SliderApplicationPairsPresets.SequenceEqual(config.SliderApplicationPairsPresets) &&
               SelectedPreset == config.SelectedPreset && UpdateAfterPresetChange == config.UpdateAfterPresetChange;
    }
}