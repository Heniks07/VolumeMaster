using System.IO.Ports;

namespace VolumeMasterCom;

public partial class VolumeMasterCom
{
    private readonly SerialPort? _port;
    private List<int> _volume = [];

    public event EventHandler<VolumeChangedEventArgs>? VolumeChanged;


    public (List<int>? SliderIndexesChanged, List<int> Volume) GetVolumeWindows()
    {
        var receivedData = _port?.ReadLine();
        if (receivedData is null)
            return (_sliderIndexesChanged, _volume);

        if (receivedData.Contains("VM"))
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
                default:
                {
                    return (_sliderIndexesChanged, _volume);
                }
            }
        }

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
        catch (InvalidOperationException invalidOperationException)
        {
            var errorMessage = "Error while reading from serial port: " + invalidOperationException.Message;
            var infoMessage =
                $"Make sure the right serial port ({Config.PortName}) is configured in the config file ({ConfigPath()}) and no other application is using the port.";
            printLog(errorMessage, LogLevel.Error);
            printLog(infoMessage, LogLevel.Info);
        }

        return (_sliderIndexesChanged, _volume);
    }

    private void printLog(string message, LogLevel logLevel)
    {
        if (_doLog)
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

            return;
        }

        switch (logLevel)
        {
            case LogLevel.Info:
                Console.WriteLine(message);
                break;
            case LogLevel.Warning:
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(message);
                Console.ResetColor();
                break;
            case LogLevel.Error:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(message);
                Console.ResetColor();
                break;
        }
    }


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

    public class VolumeChangedEventArgs : EventArgs
    {
        public List<int>? SliderIndexesChanged { get; init; }
    }
}