using Collections.Pooled;
using Cosmo;

namespace Hv2UI;

public abstract partial class Widget
{
	public bool Visible = true;

	protected PooledDictionary<ConsoleKeyInfo, Action<ConsoleKeyInfo>> KeyActions = [];

	public virtual void Draw(Renderer r) { }

	public virtual partial void OnInput(ConsoleKeyInfo cki);

	public virtual partial void OnInput(ConsoleKeyInfo cki)
	{
		if (KeyActions.ContainsKey(cki)) KeyActions[cki](cki);
	}
}
