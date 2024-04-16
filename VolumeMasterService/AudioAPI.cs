using System.Diagnostics;
using CoreAudio;

namespace VolumeMasterService;

public class AudioApi
{
    private readonly MMDevice _device;

    public AudioApi()
    {
        var devEnum = new MMDeviceEnumerator(Guid.NewGuid());
        _device = devEnum.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
    }


    /// <summary>
    /// Set the volume of an application
    /// </summary>
    /// <param name="applicationName">The application name (Can be found in the Windows volume mixes)</param>
    /// <param name="volumePercent">The new volume as a float between 0 and 1</param>
    public void SetVolume(string applicationName, float volumePercent)
    {
        if (_device.AudioEndpointVolume is null || _device.AudioSessionManager2?.Sessions is null ||
            _device.AudioSessionManager2.Sessions.Count == 0)
            return;

        if (applicationName == "master")
        {
            _device.AudioEndpointVolume.MasterVolumeLevelScalar = volumePercent;
            return;
        }

        var session =
            (from s in _device.AudioSessionManager2.Sessions
                let process = Process.GetProcessById((int)s.ProcessID)
                where process.ProcessName == applicationName
                select s).FirstOrDefault();


        if (session?.SimpleAudioVolume != null) session.SimpleAudioVolume.MasterVolume = volumePercent;
    }


    /// <summary>
    ///     Get a list of all applications that are currently playing audio
    /// </summary>
    public List<string> GetApplications()
    {
        return (from s in _device.AudioSessionManager2?.Sessions
            let process = Process.GetProcessById((int)s.ProcessID)
            select process.ProcessName).ToList();
    }
}