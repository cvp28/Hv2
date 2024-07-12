
using Cosmo;

namespace Hv2UI;

internal class StatusMessage
{
    internal string Message;
    internal TimeSpan Time;

    internal Color24 Foreground;
    internal Color24 Background;

    internal bool Flashing;

    internal StatusMessage(string Message, TimeSpan Time, bool Flashing = true)
    {
        this.Message = Message;
        this.Time = Time;

        Foreground = new(255, 255, 0); // "Sign Of The Crown" - https://icolorpalette.com/color/fce49c
        Background = Color24.Black;

        this.Flashing = Flashing;
    }

    internal StatusMessage(string Message, TimeSpan Time, Color24 Foreground, Color24 Background, bool Flashing = true)
    {
        this.Message = Message;
        this.Time = Time;

        this.Foreground = Foreground;
        this.Background = Background;

        this.Flashing = Flashing;
    }
}
