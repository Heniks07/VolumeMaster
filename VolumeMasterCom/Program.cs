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

    private ushort _skip;


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
            if (!File.Exists(Config?.PortName))
                return;
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

    private void CompareToOldVolume(List<int> volume, bool doSmooth = true, bool changeAll = false)
    {
        if (_skip > 0)
            _skip--;

        //Create a new list that has the same size as the volume list
        _sliderIndexesChanged = new int[volume.Count()].ToList();

        //ensure that the volume list has the same size as the _volume list
        if (_volume.Count != volume.Count)
        {
            _volume = new int[volume.Count].ToList();
        }

        //If the volume is different from the last volume, change it not more than the smoothness
        for (var i = 0; i < volume.Count; i++)
        {
            _sliderIndexesChanged[i] = 0;
            if (Math.Abs(_volume[i] - volume[i]) < 3 && !changeAll)
                continue;


            if (doSmooth && !changeAll && Smooth(volume, i))
                continue;

            SetIndexChanges(volume[i], _volume[i], changeAll, i);
        }

        List<(int index, int volume, bool applied)> overrides = [.._sliderManualOverride];
        //Remove the manual override if any slider is changed
        if (_sliderIndexesChanged.Count(x => x == 0) != _sliderIndexesChanged.Count && _sliderManualOverride.Count != 0)
        {
            //_sliderIndexesChanged = [-1];
            RemoveManualOverride();
        }

        DecreaseBeforeIncrease(volume, changeAll, overrides);
    }

    private void DecreaseBeforeIncrease(List<int> newVolume, bool changeAll,
        List<(int index, int volume, bool applied)> overrides)
    {
        if (_sliderIndexesChanged is null)
        {
            _volume = newVolume;
            return;
        }

        if (!_sliderIndexesChanged.Contains(1))
        {
            _volume = newVolume;
            return;
        }

        if (!_sliderIndexesChanged.Contains(-1))
        {
            _volume = newVolume;
            return;
        }

        for (var i = 0; i < _sliderIndexesChanged.Count; i++)
        {
            if (_sliderIndexesChanged[i] == 1)
            {
                _sliderIndexesChanged[i] = 0;
                if (overrides.All(x => x.index != i))
                    continue;
                _volume[i] = overrides.FirstOrDefault(x => x.index == i).volume;
                continue;
            }

            _volume[i] = newVolume[i];
        }
    }

    private void RemoveManualOverride()
    {
        foreach (var manualOverride in _sliderManualOverride)
        {
            SetIndexChanges(_volume[manualOverride.index], manualOverride.volume, false, manualOverride.index);
        }

        _sliderManualOverride.Clear();
    }

    private void SetIndexChanges(int newVolume, int oldVolume, bool changeAll, int i)
    {
        if (_sliderIndexesChanged is null)
            return;
        if (oldVolume > newVolume)
        {
            _sliderIndexesChanged[i] = -1;
        }
        else if (oldVolume < newVolume)
        {
            _sliderIndexesChanged[i] = 1;
        }
        else if (changeAll)
        {
            _sliderIndexesChanged[i] = 1;
        }
    }

    private bool Smooth(IReadOnlyList<int> volume, int i)
    {
        if (_skip > 0)
            return false;

        if (volume[i] < 5 || _volume[i] < 5)
        {
            //When the lever is switched the arduino sometimes doesn't immediately take on the new value but something in between. This 'skip' var is used to counteract this by skipping the next five cycles
            _skip = 5;
            return false;
        }

        if (Config is null || Math.Abs(volume[i] - _volume[i]) <= Config.Smoothness)
            return false;

        SetIndexChanges(volume[i], _volume[i], false, i);
        if (volume[i] > _volume[i])
        {
            _volume[i] += Config.Smoothness;

            return true;
        }

        _volume[i] -= Config.Smoothness;
        return true;
    }
}