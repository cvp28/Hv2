
using Cosmo;

namespace Hv2UI;

public class Token
{
	public string Content;
	public string RawContent;

	public int FullLength => Quoted ? Content.Length + 2 : Content.Length;

	public int StartIndex;

	public bool Selected = false;
	public bool Quoted = false;

	public Color24 HighlightForeground;
	public Color24 HighlightBackground;
}
