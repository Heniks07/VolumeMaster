using System.IO.Ports;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;

namespace VolumeMasterCom;

internal class Program
{
    public static void Main(string[] Args)
    {
    }
}

public class VolumeMasterCom
{
    private readonly bool _doLog;
    private readonly ILogger<object> _logger;
    private readonly SerialPort _port;
    private List<int> _volume = new();

    private List<int> SliderIndexesChanged;


    public VolumeMasterCom(ILogger<object> logger)
    {
        _logger = logger;
        _doLog = true;
        try
        {
            _logger.LogInformation("Starting VolumeMasterCom");
            ConfigHelper();
            _port = new SerialPort(Config.PortName, Config.BaudRate);
            _port.Open();
            _port.DtrEnable = true;
            _port.RtsEnable = true;


            _port.DataReceived += portOnDataReceived;
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
        }
    }

    public Config Config { get; private set; }

    public List<int> GetVolume()
    {
        return _volume;
    }

    public void RequestVolume()
    {
        _port.WriteLine("getVolume");
    }

    private void portOnDataReceived(object sender, EventArgs e)
    {
        var receivedData = ((SerialPort)sender).ReadLine();
        //Split the received data into a list of integers
        var newVolume = receivedData.Split(' ').Select(receivedDataPart => int.Parse(receivedDataPart)).ToList();

        //if the volume list is empty, set it to the new volume
        //Should only happen once
        if (_volume.Count == 0)
        {
            _volume = newVolume;
            VolumeChanged?.Invoke(this, new VolumeChangedEventArgs { SliderIndexsCahnged = [] });
            return;
        }

        //Smooth the volume to prevent sudden changes potentially caused by bad connections or noise
        //Apply the new volume
        SliderIndexesChanged = new List<int>();
        SmoothVolume(newVolume);
        VolumeChanged?.Invoke(this, new VolumeChangedEventArgs { SliderIndexsCahnged = SliderIndexesChanged });

#if DEBUG
            //Print the volume for debugging
            Console.WriteLine("new volume: " + string.Join(" | ", _volume));
#endif
    }

    private void SmoothVolume(IReadOnlyList<int> volume)
    {
        //If the volume is different from the last volume, change it not more than the smoothness
        for (var i = 0; i < volume.Count; i++)
        {
            if (_volume[i] == volume[i]) continue;
            if (Math.Abs(volume[i] - _volume[i]) > Config.Smoothness)
            {
                _logger.LogInformation(
                    $"Unsmoothed volume change would be {Math.Abs(volume[i] - _volume[i])} bits, smoothing to {Config.Smoothness} bits");
                if (volume[i] > _volume[i])
                {
                    _volume[i] += Config.Smoothness;
                    continue;
                }

                _volume[i] -= Config.Smoothness;
                continue;
            }

            _volume[i] = volume[i];
            SliderIndexesChanged.Add(i);
        }
    }

    public event EventHandler<VolumeChangedEventArgs> VolumeChanged;


    private void ConfigHelper()
    {
        var configPath = "";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            configPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) +
                         "/.config/VolumeMasterConfig";
        try
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
        catch (Exception e)
        {
            if (!_doLog)
                Console.WriteLine(e.Message);
            else
                _logger.LogError(e.Message);

            Config = new Config();
            var yaml = new SerializerBuilder().Build();
            var yamlString = yaml.Serialize(Config);
            File.WriteAllText(configPath, yamlString);
        }
    }


    public class VolumeChangedEventArgs : EventArgs
    {
        public List<int> SliderIndexsCahnged { get; set; }
    }
}

public class Config
{
    public string PortName { get; set; } = "/dev/ttyACM0";
    public int BaudRate { get; set; } = 9600;


    //How much the volume can change per bit
    [YamlMember(Description =
        "How much the volume can change per bit (if unchanged: approximately 5 ms)\nBetween 1 and, 1024\nDefault: 10 (lets you change the volume by approximately 200% per second)")]
    public ushort Smoothness { get; set; } = 10;

    public List<List<string>> SliderApplicationPairs { get; set; } = new()
        { new List<string> { "Firefox", "Chromium" }, new List<string> { "speech-dispatcher-dummy" } };
}