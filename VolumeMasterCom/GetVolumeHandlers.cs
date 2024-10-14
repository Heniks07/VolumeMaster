using System.IO.Ports;
using Microsoft.Extensions.Logging;

namespace VolumeMasterCom;

public partial class VolumeMasterCom
{
    private readonly SerialPort? _port;
    private readonly List<(int index, int volume, bool applied)> _sliderManualOverride = [];
    private List<int>? _sliderIndexesChanged = [];
    private List<int> _volume = [];

    //public event EventHandler<VolumeChangedEventArgs>? VolumeChanged;

    public event EventHandler? PlayPause;
    public event EventHandler? Next;
    public event EventHandler? Previous;
    public event EventHandler? Stop;


    public (List<int>? SliderIndexesChanged, List<int> Volume, List<int> ActualVolume, List<bool> OverrideActive)
        GetVolume(bool changeAll = false)
    {
        //Open the serial port if it is closed
        if (_port is { IsOpen: false })
            _port.Open();

        //try to read from the serial port
        try
        {
            //returns if the data wasn't valid or a button was pressed
            if (HandleReceivedData(changeAll, out var handleCommands))
                return handleCommands;
        }
        catch (InvalidOperationException)
        {
            PrintLog("Error while reading from serial port", LogLevel.Error);
            PrintLog(
                $"Make sure the right serial port ({Config?.PortName}) is configured in the config file ({ConfigPath()}) and no other application is using the port.",
                LogLevel.Info);
        }
        catch (ArgumentOutOfRangeException e)
        {
            _port?.Close();
            PrintLog(e.StackTrace ?? e.Message, LogLevel.Error);
        }
        catch (Exception e)
        {
            PrintLog(e.Message, LogLevel.Error);
        }

        //apply the manual overrides
        var finalVolume = GetVolumeAfterManualOverride();

        var overrideActive = new List<bool>();
        for (var i = 0; i < _volume.Count; i++)
        {
            overrideActive.Add(_sliderManualOverride.Any(x => x.index == i));
        }

        return (_sliderIndexesChanged, finalVolume, _volume, overrideActive);
    }


    private bool HandleReceivedData(bool changeAll,
        out (List<int>? SliderIndexesChanged, List<int> Volume, List<int> ActualVolume, List<bool> OverrideActive)
            handleCommands)
    {
        var receivedData = _port?.ReadExisting().Split('\n');
        var overrideActive = new List<bool>();
        for (var i = 0; i < _volume.Count; i++)
        {
            overrideActive.Add(_sliderManualOverride.Any(x => x.index == i));
        }

        //Check if the received data is valid
        if (receivedData is { Length: < 3 })
        {
            handleCommands = (null, GetVolumeAfterManualOverride(), _volume, overrideActive);
            return true;
        }

        var lastData = receivedData?[^2].Trim();

        if (receivedData != null && receivedData.Any(x => x.Contains("VM")))
        {
            handleCommands = HandleCommands(receivedData.FirstOrDefault(x => x.Contains("VM"))?.Trim() ?? "");
            return true;
        }


        //Split the received data into a list of integers
        var newVolumeStrings = lastData?.Split('|');

        //Check if the received data is valid
        if (newVolumeStrings != null && newVolumeStrings.Any(x => x.Length != 4))
        {
            {
                handleCommands = (null, GetVolumeAfterManualOverride(), _volume, overrideActive);
                return true;
            }
        }

        if (newVolumeStrings != null)
        {
            var newVolume = newVolumeStrings.Select(int.Parse).ToList();

            //_logger?.LogInformation("Received volume: " + string.Join(" | ", newVolume) + " at" + DateTimeOffset.Now.ToString("HH:mm:ss.fff"));

            //if the volume list is empty, set it to the new volume
            //Should only happen once
            //var test = new List<int>(_volume);
            if (_volume.Count == 0)
            {
                _sliderIndexesChanged?.Clear();
                CompareToOldVolume(newVolume, changeAll: true, doSmooth: false);
                handleCommands = (null, GetVolumeAfterManualOverride(), _volume, overrideActive);
                return false;
            }

            //Smooth the volume to prevent sudden changes potentially caused by bad connections or noise
            //Apply the new volume
            _sliderIndexesChanged?.Clear();
            CompareToOldVolume(newVolume, doSmooth: Config?.DoSmooth ?? true, changeAll: changeAll);
        }

        handleCommands = (null, GetVolumeAfterManualOverride(), _volume, overrideActive);
        return false;
    }

    private List<int> GetVolumeAfterManualOverride()
    {
        var finalVolume = _volume.ToList();
        foreach (var (index, volume, applied) in _sliderManualOverride.ToList())
        {
            if (!applied)
            {
                if (volume > _volume[index])
                    _sliderIndexesChanged![index] = 1;
                else
                    _sliderIndexesChanged![index] = -1;
            }

            //set applied to true for the current manual override
            while (_sliderManualOverride.Count(x => x.index == index) > 1)
                _sliderManualOverride.RemoveAt(_sliderManualOverride.FindIndex(x => x.index == index));
            _sliderManualOverride[_sliderManualOverride.FindIndex(x => x.index == index)] = (index, volume, true);


            finalVolume[index] = volume;
        }

        return finalVolume;
    }

    private void PrintLog(string message, LogLevel logLevel)
    {
        if (_doLog)
        {
            Log();
            return;
        }

        Console();

        return;

        void Log()
        {
            switch (logLevel)
            {
                case LogLevel.Info:
                    _logger?.LogInformation(message);
                    break;
                case LogLevel.Warning:
                    _logger?.LogWarning(message);
                    break;
                case LogLevel.Error:
                    _logger?.LogError(message);
                    break;
            }
        }

        void Console()
        {
            switch (logLevel)
            {
                case LogLevel.Info:
                    System.Console.WriteLine(message);
                    break;
                case LogLevel.Warning:
                    System.Console.ForegroundColor = ConsoleColor.Yellow;
                    System.Console.WriteLine(message);
                    System.Console.ResetColor();
                    break;
                case LogLevel.Error:
                    System.Console.ForegroundColor = ConsoleColor.Red;
                    System.Console.WriteLine(message);
                    System.Console.ResetColor();
                    break;
            }
        }
    }

    private (List<int>? SliderIndexesChanged, List<int> Volume, List<int> ActualVolume, List<bool> OverrideActive)
        HandleCommands(string receivedData)
    {
        var overrideActive = new List<bool>();
        for (var i = 0; i < _volume.Count; i++)
        {
            overrideActive.Add(_sliderManualOverride.Any(x => x.index == i));
        }

        switch (receivedData.Trim())
        {
            case "VM.changePreset":
            {
                if (Config == null)
                    return (_sliderIndexesChanged, GetVolumeAfterManualOverride(), _volume, overrideActive);

                Config.SelectedPreset++;
                Config.SelectedPreset %= (ushort)Config.SliderApplicationPairsPresets.Count;

                WriteConfig(ConfigPath());
                if (!Config.UpdateAfterPresetChange)
                    return (null, GetVolumeAfterManualOverride(), _volume, overrideActive);

                Thread.Sleep(100);
                return GetVolume(true);
            }
            case "VM.playPause":
            {
                PlayPause?.Invoke(this, EventArgs.Empty);
                return (_sliderIndexesChanged, GetVolumeAfterManualOverride(), _volume, overrideActive);
            }
            case "VM.next":
            {
                Next?.Invoke(this, EventArgs.Empty);
                return (_sliderIndexesChanged, GetVolumeAfterManualOverride(), _volume, overrideActive);
            }
            case "VM.previous":
            {
                Previous?.Invoke(this, EventArgs.Empty);
                return (_sliderIndexesChanged, GetVolumeAfterManualOverride(), _volume, overrideActive);
            }
            case "VM.stop":
            {
                Stop?.Invoke(this, EventArgs.Empty);
                return (_sliderIndexesChanged, GetVolumeAfterManualOverride(), _volume, overrideActive);
            }
            default:
            {
                return (_sliderIndexesChanged, GetVolumeAfterManualOverride(), _volume, overrideActive);
            }
        }
    }


    public void RequestVolume()
    {
        _port?.WriteLine("getVolume");
    }

    public void AddManualOverride(int sliderIndex, int volume)
    {
        _sliderManualOverride.Add((sliderIndex, volume, false));
    }

    private enum LogLevel
    {
        Info,
        Warning,
        Error
    }
}