using System.Xml.Serialization;

using Collections.Pooled;
using Cosmo;

namespace Hv2UI;

public abstract partial class Widget
{
    [XmlIgnore] public bool Visible = true;

    /// <summary>
    /// Toggles whether OnFocused or OnDefocused will be called when the widget gains or looses focus, respectively. (default true)
    /// </summary>
    [XmlIgnore] public bool EnableFocusChangeEvents = true;


    [XmlIgnore] public PooledDictionary<ConsoleKey, Action<ConsoleKeyInfo>> KeyActions = [];

	public virtual partial void OnFocused();
	public virtual partial void OnDefocused();

	public virtual partial void OnFocused()
	{
		if (!EnableFocusChangeEvents) return;
	}

	public virtual partial void OnDefocused()
	{
        if (!EnableFocusChangeEvents) return;
    }

	public virtual void Draw(Renderer r) { }

	public virtual partial void OnInput(ConsoleKeyInfo cki);

	public virtual partial void OnInput(ConsoleKeyInfo cki)
	{
		if (KeyActions.ContainsKey(cki.Key)) KeyActions[cki.Key](cki);
	}
}
