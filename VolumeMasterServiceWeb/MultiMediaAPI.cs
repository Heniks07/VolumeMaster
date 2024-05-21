using WindowsInput;

namespace VolumeMasterServiceWeb;

public static class MultiMediaApi
{
    public static void Pause()
    {
        var inputSimulator = new InputSimulator();
        inputSimulator.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.MEDIA_PLAY_PAUSE);
    }

    public static void Next()
    {
        var inputSimulator = new InputSimulator();
        inputSimulator.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.MEDIA_NEXT_TRACK);
    }

    public static void Previous()
    {
        var inputSimulator = new InputSimulator();
        inputSimulator.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.MEDIA_PREV_TRACK);
    }

    public static void Stop()
    {
        var inputSimulator = new InputSimulator();
        inputSimulator.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.MEDIA_STOP);
    }
}