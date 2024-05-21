using System.Runtime.InteropServices;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace VolumeMasterCom;

public class Config
{
    [YamlMember(Description =
        "The version of the config file\n" +
        "This is used to determine if the config file needs to be updated\n" +
        "!!!Do not change this value!!!")]
    public int ConfigVersionNumber { get; set; } = -1;

    [YamlMember(Description = "---Communication---")]
    public string PortName { get; set; } = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/dev/ttyACM0" : "COM3";

    public int BaudRate { get; set; } = 9600;

    public int SliderCount { get; set; } = 4;


    //How much the volume can change per bit
    [YamlMember(Description = "---Smoothness---\n" +
                              "Between 1 and, 1024")]
    public ushort Smoothness { get; set; } = 1000;

    public bool DoSmooth { get; set; } = true;

    [YamlMember(Description =
        "How many milliseconds to wait after decreasing the application volumes of sliders before increasing those of other sliders\n" +
        "Helpful to prevent a short spike in volume when moving a slider after a manual override has been applied")]
    public ushort DecreaseBeforeIncreaseTimeout { get; set; } = 20;


    [YamlMember(Description = "---Presets---")]
    public List<List<List<string>>> SliderApplicationPairsPresets { get; set; } =
        [[["Firefox", "Chromium"], ["master"]], [["steam"], ["master"]]];

    public ushort SelectedPreset { get; set; }

    public bool UpdateAfterPresetChange { get; set; } = true;

    public override bool Equals(object? obj)
    {
        if (obj is not Config config) return false;
        return ConfigVersionNumber == config.ConfigVersionNumber && PortName == config.PortName &&
               BaudRate == config.BaudRate && SliderCount == config.SliderCount &&
               Smoothness == config.Smoothness && DoSmooth == config.DoSmooth &&
               DecreaseBeforeIncreaseTimeout == config.DecreaseBeforeIncreaseTimeout &&
               PresetsAreEqual(SliderApplicationPairsPresets, config.SliderApplicationPairsPresets) &&
               SelectedPreset == config.SelectedPreset && UpdateAfterPresetChange == config.UpdateAfterPresetChange;
    }

    private static bool PresetsAreEqual(List<List<List<string>>> preset1, List<List<List<string>>> preset2)
    {
        var preset1String = JsonConvert.SerializeObject(preset1);
        var preset2String = JsonConvert.SerializeObject(preset2);

        return preset1String.Equals(preset2String);
    }
}