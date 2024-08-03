using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;

namespace VolumeMasterCom;

public partial class VolumeMasterCom
{
    private const int ConfigVersionNumber = 1;
    public Config? Config { get; private set; }


    public void ConfigHelper()
    {
        var configPath = ConfigPath();
        LoadConfig(configPath);

        if (Config?.ConfigVersionNumber == ConfigVersionNumber) return;
        Config ??= new Config();
        Config.ConfigVersionNumber = ConfigVersionNumber;
        WriteConfig(configPath);
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

    /// <summary>
    /// Determines the path of the config file based on the OS
    /// </summary>
    /// <returns>Returns the path to the configuration file</returns>
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
            Config.ConfigVersionNumber = ConfigVersionNumber;
            WriteConfig(configPath);
        }
        else
        {
            var yaml = new DeserializerBuilder().IgnoreUnmatchedProperties().Build();
            var config = yaml.Deserialize<Config>(File.ReadAllText(configPath));
            if (Config is not null && Config.Equals(config)) return;
            Config = config;
            PrintLog("Config updated sucesfully", LogLevel.Info);
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