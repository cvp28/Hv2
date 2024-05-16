
using Cosmo;

namespace Hv2UI;

internal struct TextBoxModifyAction
{
	public TextBoxModifyType Type;

	// Move Type
	public int CursorX;
	public int CursorY;

	// Write Type
	public char Character;

	public Color24 Foreground;
	public Color24 Background;
}

internal enum TextBoxModifyType
{
	MoveCursor,
	WriteCharInPlace,
	Write
}