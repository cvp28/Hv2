using Cosmo;

namespace Hv2;

public abstract class Widget
{
	/// <summary>
	/// <para>Hv2 uses this method to determine if a widget needs to be redrawn.</para>
	/// <para>If this method returns a hash that does not match any stored from the previous mainloop iteration, then this widget's draw method is called.</para>
	/// </summary>
	/// <returns>A hash representing the current visual state of this widget</returns>
	public virtual int ComputeVisHash() => 0;
	
	public virtual void Draw(Renderer r) { }

	public virtual void OnInput(ConsoleKeyInfo cki) { }
}
