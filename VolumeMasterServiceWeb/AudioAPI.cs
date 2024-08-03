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
    /// <param name="configSliderApplicationPairsPreset">In the case of VM.other used to determine if a application is already assigned another slider</param>
    public void SetVolume(string applicationName, float volumePercent,
        List<List<string>>? configSliderApplicationPairsPreset)
    {
        if (Device.AudioEndpointVolume is null || Device.AudioSessionManager2?.Sessions is null ||
            Device.AudioSessionManager2.Sessions.Count == 0)
            return;

        if (applicationName == "master")
        {
            Device.AudioEndpointVolume.MasterVolumeLevelScalar = volumePercent;
            return;
        }

        if (applicationName == "VM.other")
        {
            //Set the volume of all applications that are not in the config file
            var sessions = Device.AudioSessionManager2?.Sessions;
            if (sessions is null)
                return;
            if (configSliderApplicationPairsPreset == null)
                return;
            foreach (var session1 in sessions)
            {
                var process = Process.GetProcessById((int)session1.ProcessID);
                if (configSliderApplicationPairsPreset.Any(x => x.Contains(process.ProcessName)))
                    continue;


                if (session1?.SimpleAudioVolume == null) continue;
                session1.SimpleAudioVolume.MasterVolume = volumePercent;
            }
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