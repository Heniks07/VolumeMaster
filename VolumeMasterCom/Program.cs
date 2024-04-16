using System.IO.Ports;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;

namespace VolumeMasterCom;

internal static class Program
{
    public static void Main(string[] args)
    {
    }
}

public class VolumeMasterCom
{
    private readonly bool _doLog;
    private readonly ILogger<object>? _logger;
    private readonly SerialPort? _port;

    private readonly List<int>? _sliderIndexesChanged = [];
    private List<int> _volume = [];


    public VolumeMasterCom(ILogger<object>? logger)
    {
        _logger = logger;
        _doLog = true;
        try
        {
#if DEBUG
            if (_logger != null) _logger.LogInformation("Starting VolumeMasterCom");
#endif
            ConfigHelper();
            if (Config is null) return;
            _port = new SerialPort(Config?.PortName, Config!.BaudRate);
            _port.Open();
            _port.DtrEnable = true;
            _port.RtsEnable = true;


            _port.DataReceived += portOnDataReceived;
        }
        catch (Exception e)
        {
            if (_logger != null) _logger.LogError(e.Message);
        }
    }

    /// <summary>
    ///     This constructor is used for Windows since I didn't manage to get the other way to work properly
    /// </summary>
    /// <param name="logger">The logger from the service</param>
    /// <param name="windows">Isn't used; Just there to make another constructor just for Windows</param>
    public VolumeMasterCom(ILogger<object>? logger, bool windows)
    {
        _logger = logger;
        _doLog = true;

        try
        {
#if DEBUG
            if (_logger != null) _logger.LogInformation("Starting VolumeMasterCom");
#endif
            ConfigHelper();
            if (Config is null) return;
            _port = new SerialPort(Config?.PortName, Config!.BaudRate);
            _port?.Open();
            if (_port != null)
            {
                _port.DtrEnable = true;
                _port.RtsEnable = true;
            }
        }
        catch (Exception e)
        {
            if (_logger != null) _logger.LogError(e.Message);
        }
    }

    public VolumeMasterCom()
    {
        _doLog = false;
        try
        {
            ConfigHelper();
            if (Config is null) return;
            _port = new SerialPort(Config?.PortName, Config!.BaudRate);
            _port.Open();
            _port.DtrEnable = true;
            _port.RtsEnable = true;


            _port.DataReceived += portOnDataReceived;
        }
        catch (Exception e)
        {
            _logger?.LogError(e.Message);
        }
    }

    public Config? Config { get; private set; }

    public (List<int>? SliderIndexesChanged, List<int> Volume) GetVolumeFromWidows()
    {
        var receivedData = _port?.ReadLine();
        if (receivedData is null)
            return (_sliderIndexesChanged, _volume);

        //Split the received data into a list of integers
        var newVolume = receivedData.Split('|').Select(int.Parse).ToList();

        //_logger?.LogInformation("Received volume: " + string.Join(" | ", newVolume) + " at" + DateTimeOffset.Now.ToString("HH:mm:ss.fff"));

        //if the volume list is empty, set it to the new volume
        //Should only happen once
        if (_volume.Count == 0)
        {
            _volume = newVolume;
            return (null, newVolume);
        }

        //Smooth the volume to prevent sudden changes potentially caused by bad connections or noise
        //Apply the new volume
        _sliderIndexesChanged?.Clear();
        CompareToOldVolume(newVolume, false);


        return (_sliderIndexesChanged, _volume);
    }

    public List<int> GetVolume()
    {
        return _volume;
    }

    public void RequestVolume()
    {
        _port?.WriteLine("getVolume");
    }

    private void portOnDataReceived(object sender, EventArgs e)
    {
        var receivedData = ((SerialPort)sender).ReadLine();
        //Split the received data into a list of integers
        var newVolume = receivedData.Split('|').Select(int.Parse).ToList();

        //if the volume list is empty, set it to the new volume
        //Should only happen once
        if (_volume.Count == 0)
        {
            _volume = newVolume;
            VolumeChanged?.Invoke(this, new VolumeChangedEventArgs { SliderIndexesChanged = [] });
            return;
        }

        //Smooth the volume to prevent sudden changes potentially caused by bad connections or noise
        //Apply the new volume
        _sliderIndexesChanged?.Clear();
        CompareToOldVolume(newVolume);
        VolumeChanged?.Invoke(this, new VolumeChangedEventArgs { SliderIndexesChanged = _sliderIndexesChanged });

#if DEBUG
        //Print the volume for debugging
        Console.WriteLine("new volume: " + string.Join(" | ", _volume));
#endif
    }

    public List<int> GetSliderIndexesChanged()
    {
        return _sliderIndexesChanged ?? new List<int>();
    }

    private void CompareToOldVolume(IReadOnlyList<int> volume, bool doSmooth = true)
    {
        //If the volume is different from the last volume, change it not more than the smoothness
        for (var i = 0; i < volume.Count; i++)
        {
            if (Math.Abs(_volume[i] - volume[i]) < 3)
                continue;

            if (Config is not null && Math.Abs(volume[i] - _volume[i]) > Config.Smoothness && doSmooth)
            {
#if DEBUG
                _logger?.LogInformation(
                    $"Unsmoothed volume change would be {Math.Abs(volume[i] - _volume[i])} bits, smoothing to {Config.Smoothness} bits");
#endif
                if (volume[i] > _volume[i])
                {
                    _volume[i] += Config.Smoothness;
                    continue;
                }

                _volume[i] -= Config.Smoothness;
                continue;
            }

            _volume[i] = volume[i];
            _sliderIndexesChanged?.Add(i);
        }
    }

    public event EventHandler<VolumeChangedEventArgs>? VolumeChanged;


    private void ConfigHelper()
    {
        var configPath = ConfigPath();
        try
        {
            ReadConfig(configPath);
        }
        catch (Exception e)
        {
            HandleError(e, configPath);
        }
    }

    public string ConfigPath()
    {
        var s = "";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            s = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) +
                "/.config/VolumeMasterConfig";
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
            var yaml = new SerializerBuilder().Build();
            var yamlString = yaml.Serialize(Config);
            File.WriteAllText(configPath, yamlString);
        }
        else
        {
            var yaml = new DeserializerBuilder().Build();
            Config = yaml.Deserialize<Config>(File.ReadAllText(configPath));
        }
    }

    private void HandleError(Exception e, string configPath)
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


    public class VolumeChangedEventArgs : EventArgs
    {
        public List<int>? SliderIndexesChanged { get; init; }
    }
}

public class Config
{
    public string PortName { get; set; } = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/dev/ttyACM0" : "COM3";
    public int BaudRate { get; set; } = 9600;


    //How much the volume can change per bit
    [YamlMember(Description =
        "Between 1 and, 1024\n")]
    public ushort Smoothness { get; set; } = 1000;

    public List<List<string>> SliderApplicationPairs { get; set; } =
        [["Firefox", "Chromium"], ["master"]];
}