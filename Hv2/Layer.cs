using System.Reflection;

using Collections.Pooled;

namespace Hv2UI;

public abstract partial class Layer
{
	public bool Active
	{
		get => field;
		set
		{
			field = value;

			if (value)
			{
				OnShow();
			}
			else
			{
				OnHide();
			}
		}
	}

	internal PooledList<Widget> Widgets = [];
	
	/// <summary>
	/// A convenience dictionary provided so the client can easily store map-based state
	/// </summary>
	protected PooledDictionary<string, dynamic> State = [];

	public PooledDictionary<Keybind, Action<ConsoleKeyInfo>> KeyActions = [];
	
	public Layer(bool Active = false)
	{ 
		this.Active = Active;
	}
	
	public void AddWidgetsInternal()
	{
		var fields = GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Where(p => p.IsDefined(typeof(WidgetAttribute))).ToArray();
		
		foreach (var field in fields)
			Widgets.Add(field.GetValue(this) as Widget);
	}
	
	public virtual void OnShow()
	{ }
	
	public virtual void OnHide()
	{ }
}