using System.IO.Ports;
using Microsoft.Extensions.Logging;

namespace VolumeMasterCom;

public partial class VolumeMasterCom
{
    private readonly SerialPort? _port;
    private List<int> _volume = [];

    //public event EventHandler<VolumeChangedEventArgs>? VolumeChanged;

    public event EventHandler? PlayPause;
    public event EventHandler? Next;
    public event EventHandler? Previous;
    public event EventHandler? Stop;


    public (List<int>? SliderIndexesChanged, List<int> Volume) GetVolume()
    {
        try
        {
            var receivedData = _port?.ReadLine();
            if (receivedData is null)
                return (_sliderIndexesChanged, _volume);

            if (receivedData.Contains("VM")) return HandleCommands(receivedData);

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
        }
        catch (InvalidOperationException)
        {
            PrintLog("Error while reading from serial port", LogLevel.Error);
            PrintLog(
                $"Make sure the right serial port ({Config?.PortName}) is configured in the config file ({ConfigPath()}) and no other application is using the port.",
                LogLevel.Info);
        }

        return (_sliderIndexesChanged, _volume);
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

    private (List<int>? SliderIndexesChanged, List<int> Volume) HandleCommands(string receivedData)
    {
        switch (receivedData.Trim())
        {
            case "VM.changePreset":
            {
                if (Config == null) return (_sliderIndexesChanged, _volume);

                Config.SelectedPreset++;
                Config.SelectedPreset %= (ushort)Config.SliderApplicationPairsPresets.Count;

                WriteConfig(ConfigPath());

                return (Config.UpdateAfterPresetChange ? null : _sliderIndexesChanged, _volume);
            }
            case "VM.playPause":
            {
                PlayPause?.Invoke(this, EventArgs.Empty);
                return (_sliderIndexesChanged, _volume);
            }
            case "VM.next":
            {
                Next?.Invoke(this, EventArgs.Empty);
                return (_sliderIndexesChanged, _volume);
            }
            case "VM.previous":
            {
                Previous?.Invoke(this, EventArgs.Empty);
                return (_sliderIndexesChanged, _volume);
            }
            case "VM.stop":
            {
                Stop?.Invoke(this, EventArgs.Empty);
                return (_sliderIndexesChanged, _volume);
            }
            default:
            {
                return (_sliderIndexesChanged, _volume);
            }
        }
    }


    public void RequestVolume()
    {
        _port?.WriteLine("getVolume");
    }

    private enum LogLevel
    {
        Info,
        Warning,
        Error
    }

    /*private void portOnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        var receivedData = ((SerialPort)sender).ReadLine();
        //Split the received data into a list of integers
        var newVolume = receivedData.Split('|').Select(int.Parse).ToList();

        //if the volume list is empty, set it to the new volume
        //Should only happen once
        if (_volume.Count == 0)
        {
            _volume = newVolume;
            VolumeChanged?.Invoke(this, new VolumeChangedEventArgs { });
            return;
        }

        //Smooth the volume to prevent sudden changes potentially caused by bad connections or noise
        //Apply the new volume
        _sliderIndexesChanged?.Clear();
        CompareToOldVolume(newVolume);
        VolumeChanged?.Invoke(this, new VolumeChangedEventArgs { });

#if DEBUG
        //Print the volume for debugging
        Console.WriteLine("new volume: " + string.Join(" | ", _volume));
#endif
    }
*/
}