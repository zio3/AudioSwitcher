using System;

namespace AudioSwitcher.AudioApi.CoreAudio
{
    [Flags]
    public enum ERole : uint
    {
        Console = 0,
        Multimedia = 1,
        Communications = 2
    }
}