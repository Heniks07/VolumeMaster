using System.Diagnostics;
using CoreAudio;

namespace VolumeMasterServiceWeb;

public class AudioApi
{
    public MMDevice Device =
        new MMDeviceEnumerator(Guid.NewGuid()).GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);


    /// <summary>
    /// Set the volume of an application
    /// </summary>
    /// <param name="applicationName">The application name (Can be found in the Windows volume mixes)</param>
    /// <param name="volumePercent">The new volume as a float between 0 and 1</param>
    public void SetVolume(string applicationName, float volumePercent)
    {
        if (Device.AudioEndpointVolume is null || Device.AudioSessionManager2?.Sessions is null ||
            Device.AudioSessionManager2.Sessions.Count == 0)
            return;

        if (applicationName == "master")
        {
            Device.AudioEndpointVolume.MasterVolumeLevelScalar = volumePercent;
            return;
        }

        Device = new MMDeviceEnumerator(Guid.NewGuid()).GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

        var session =
            (from s in Device.AudioSessionManager2?.Sessions
                let process = Process.GetProcessById((int)s.ProcessID)
                where process.ProcessName == applicationName
                select s).FirstOrDefault();


        if (session?.SimpleAudioVolume != null)
            session.SimpleAudioVolume.MasterVolume = volumePercent;
    }


    /// <summary>
    ///     Get a list of all applications that are currently playing audio
    /// </summary>
    public List<string> GetApplications()
    {
        return (from s in Device.AudioSessionManager2?.Sessions
            let process = Process.GetProcessById((int)s.ProcessID)
            select process.ProcessName).ToList();
    }
}