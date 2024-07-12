using Collections.Pooled;
using Cosmo;

namespace Hv2UI;

public abstract partial class Widget
{
	public bool Visible = true;

	/// <summary>
	/// Toggles whether OnFocused or OnDefocused will be called when the widget gains or looses focus, respectively. (default true)
	/// </summary>
	public bool EnableFocusChangeEvents = true;

	protected PooledDictionary<ConsoleKeyInfo, Action<ConsoleKeyInfo>> KeyActions = [];

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
		if (KeyActions.ContainsKey(cki)) KeyActions[cki](cki);
	}
}
