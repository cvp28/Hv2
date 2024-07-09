using Cosmo;

namespace Hv2UI;

public class Label : Widget
{
	public int X { get; set; }
	public int Y { get; set; }
	
	public string Text { get; set; }
	
	public Color24 Foreground { get; set;}
	public Color24 Background { get; set; }
	
	public StyleCode Style { get; set; }

	#region Constructors
	public Label() : this(0, 0, "", Color24.White, Color24.Black, StyleCode.None) { }
	public Label(int X, int Y, string Text) : this(X, Y, Text, Color24.White, Color24.Black, StyleCode.None) { }
	public Label(int X, int Y, string Text, Color24 Foreground, Color24 Background) : this(X, Y, Text, Foreground, Background, StyleCode.None) { }

	public Label(int X, int Y, string Text, Color24 Foreground, Color24 Background, StyleCode Style)
	{
		this.X = X;
		this.Y = Y;
		this.Text = Text;
		this.Foreground = Foreground;
		this.Background = Background;
		this.Style = Style;
	}
	#endregion
	
	public override void Draw(Renderer r)
	{
		r.WriteAt(X, Y, Text, Foreground, Background, Style);
	}
}