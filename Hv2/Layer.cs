using System.Reflection;

using Collections.Pooled;

namespace Hv2UI;

public abstract partial class Layer
{
	internal PooledList<Widget> Widgets = [];
	
	/// <summary>
	/// A convenience dictionary provided so the client can easily store map-based state
	/// </summary>
	protected PooledDictionary<string, dynamic> State = [];
	
	public Layer()
	{ }
	
	// Called when the layer gets added to Hv2
	// We wait until then because the widgets are all almost certainly going to be initialized by then
	internal void OnAttachInternal()
	{
		var fields = GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Where(p => p.IsDefined(typeof(WidgetAttribute))).ToArray();
		
		foreach (var field in fields)
			Widgets.Add(field.GetValue(this) as Widget);
		
		OnAttach();
	}

	public virtual void OnAttach()
	{ }
	
	public virtual void OnShow()
	{ }
	
	public virtual void OnHide()
	{ }
}