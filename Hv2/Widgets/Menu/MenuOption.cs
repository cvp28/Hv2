using Cosmo;

namespace Hv2UI;

public class MenuOption
{
	public int Index { get; internal set; }
	public string Text { get; set; }

	public Color24 TextForeground { get; set; }
	public Color24 TextBackground { get; set; }

	public Action Action { get; set; }
}
