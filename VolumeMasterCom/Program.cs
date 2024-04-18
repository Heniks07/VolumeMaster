using System.IO.Ports;
using Microsoft.Extensions.Logging;

namespace VolumeMasterCom;

internal static class Program
{
    public static void Main(string[] args)
    {
    }
}

public partial class VolumeMasterCom
{
    private readonly bool _doLog;
    private readonly ILogger<object>? _logger;

    private readonly List<int>? _sliderIndexesChanged = [];

    /*
    /// <summary>
    /// Starts the VolumeMasterCom with logging
    /// </summary>
    /// <param name="logger"></param>
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
    }*/

    /// <summary>
    /// This constructor is used for Services with logging
    /// </summary>
    /// <param name="logger">The logger from the service</param>
    public VolumeMasterCom(ILogger<object>? logger)
    {
        _logger = logger;
        _doLog = true;


#if DEBUG
            if (_logger != null) _logger.LogInformation("Starting VolumeMasterCom");
#endif

        ConfigHelper();
        if (Config is null) return;
        try
        {
            _port = new SerialPort(Config?.PortName, Config!.BaudRate);
            _port?.Open();
            if (_port == null) return;
            _port.DtrEnable = true;
            _port.RtsEnable = true;
        }
        catch (InvalidOperationException)
        {
            _logger?.LogError("Error while reading from serial port");
            _logger?.LogInformation(
                $"Make sure the right serial port ({Config.PortName}) is configured in the config file ({ConfigPath()}) and no other application is using the port.");
        }
    }

    /// <summary>
    /// Start the VolumeMasterCom without logging
    /// </summary>
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
        }
        catch (Exception e)
        {
            _logger?.LogError(e.Message);
        }
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
}