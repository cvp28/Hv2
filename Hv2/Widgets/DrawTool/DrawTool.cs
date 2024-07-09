using Cosmo;

namespace Hv2UI;

// Literally just a passthrough for direct access to the underlying renderer
public class DrawTool : Widget
{
    public Action<Renderer> DrawAction { get; set; }

    public DrawTool(Action<Renderer> DrawAction)
    {
        this.DrawAction = DrawAction;
    }

    public override void Draw(Renderer r) => DrawAction(r);
}
