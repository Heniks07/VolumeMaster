using System.Diagnostics;

namespace VolumeMasterDWeb;

public static class MultiMediaApi
{
    public static void Pause()
    {
        ExecuteCommand("playerctl", "play-pause");
    }

    public static void Next()
    {
        ExecuteCommand("playerctl", "next");
    }

    public static void Previous()
    {
        ExecuteCommand("playerctl", "previous");
    }

    public static void Stop()
    {
        ExecuteCommand("playerctl", "stop");
    }

    private static void ExecuteCommand(string command, string arguments)
    {
        try
        {
            var process = new Process();
            process.StartInfo.FileName = command;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            process.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error executing command: " + ex.Message);
        }
    }
}