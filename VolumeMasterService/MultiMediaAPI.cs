using WindowsInput;

namespace VolumeMasterService;

public class MultiMediaApi
{
    public void Pause()
    {
       var inputSimulator = new InputSimulator();
        inputSimulator.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.MEDIA_PLAY_PAUSE);
    }
    public void Next()
    {
        var inputSimulator = new InputSimulator();
        inputSimulator.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.MEDIA_NEXT_TRACK);
    }
    public void Previous()
    {
        var inputSimulator = new InputSimulator();
        inputSimulator.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.MEDIA_PREV_TRACK);
    }
    public void Stop()
    {
        var inputSimulator = new InputSimulator();
        inputSimulator.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.MEDIA_STOP);
    }
}