using Cosmo;

namespace Hv2;

public class Label : Widget
{
	public int X { get; set; }
	public int Y { get; set; }
	
	public string Text { get; set; }
	
	public Color24 Foreground { get; set;}
	public Color24 Background { get; set; }
	
	public StyleCode Style { get; set; }
	
	#region Constructors
	public Label() => Make(0, 0, "", Color24.White, Color24.Black, StyleCode.None);
	public Label(int X, int Y, string Text) => Make(X, Y, Text, Color24.White, Color24.Black, StyleCode.None);
	public Label(int X, int Y, string Text, Color24 Foreground, Color24 Background) => Make(X, Y, Text, Foreground, Background, StyleCode.None);
	public Label(int X, int Y, string Text, Color24 Foreground, Color24 Background, StyleCode Style) => Make(X, Y, Text, Foreground, Background, Style);
	
	private Label Make(int X, int Y, string Text, Color24 Foreground, Color24 Background, StyleCode Style) => new()
	{
		X = X,
		Y = Y,
		Text = Text,
		Foreground = Foreground,
		Background = Background,
		Style = Style
	};
	#endregion
	
	// This just computes a hash accounting for every property that affects visual appearance (which happens to be all of them)
	public override int ComputeVisHash() => HashCode.Combine(X, Y, Text, Foreground, Background, Style);

	public override void Draw(Renderer r)
	{
		r.WriteAt(X, Y, Text, Foreground, Background, Style);
	}
}