
using Cosmo;

namespace Hv2UI;

internal class StatusMessage
{
    internal string Message;
    internal TimeSpan Time;
    internal Color24 Foreground;
    internal Color24 Background;

    internal StatusMessage(string Message, TimeSpan Time)
    {
        this.Message = Message;
        this.Time = Time;
        Foreground = new(252, 228, 156); // "Sign Of The Crown" - https://icolorpalette.com/color/fce49c
        Background = Color24.Black;
    }
}
