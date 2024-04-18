using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;

namespace VolumeMasterCom;

public partial class VolumeMasterCom
{
    public Config? Config { get; private set; }

    public void ConfigHelper()
    {
        var configPath = ConfigPath();
        LoadConfig(configPath);
    }

    private void LoadConfig(string configPath)
    {
        try
        {
            ReadConfig(configPath);
        }
        catch (Exception e)
        {
            HandleConfigError(e, configPath);
        }
    }

    public string ConfigPath()
    {
        var s = "";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            s = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) +
                "/.config/VolumeMaster/VolumeMasterConfig";
            if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) +
                                  "/.config/VolumeMaster"))
                Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) +
                                          "/.config/VolumeMaster");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            s = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) +
                @"\VolumeMaster\config.yaml";
            if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) +
                                  "\\VolumeMaster"))
                Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) +
                                          "\\VolumeMaster");
        }

        return s;
    }

    private void ReadConfig(string configPath)
    {
        if (!File.Exists(configPath))
        {
            Config = new Config();
            WriteConfig(configPath);
        }
        else
        {
            var yaml = new DeserializerBuilder().Build();
            Config = yaml.Deserialize<Config>(File.ReadAllText(configPath));
        }
    }

    private void WriteConfig(string configPath)
    {
        var yaml = new SerializerBuilder().Build();
        var yamlString = yaml.Serialize(Config);
        File.WriteAllText(configPath, yamlString);
    }

    private void HandleConfigError(Exception e, string configPath)
    {
        if (!_doLog)
            Console.WriteLine(e.Message);
        else
            _logger?.LogError(e.Message);


        Config = new Config();
        var yaml = new SerializerBuilder().Build();
        var yamlString = yaml.Serialize(Config);
        File.WriteAllText(configPath, yamlString);
    }
}

public class Config
{
    public string PortName { get; set; } = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/dev/ttyACM0" : "COM3";
    public int BaudRate { get; set; } = 9600;


    //How much the volume can change per bit
    [YamlMember(Description =
        "Between 1 and, 1024")]
    public ushort Smoothness { get; set; } = 1000;

    public List<List<List<string>>> SliderApplicationPairsPresets { get; set; } =
        [[["Firefox", "Chromium"], ["master"]], [["steam"], ["master"]]];

    public ushort SelectedPreset { get; set; }

    public bool UpdateAfterPresetChange { get; set; } = true;
}