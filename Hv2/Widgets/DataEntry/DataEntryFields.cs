
using System.Globalization;
using System.Text.Json.Serialization;

using Cosmo;

namespace Hv2UI;

[JsonDerivedType(typeof(TextField), "tf")]
[JsonDerivedType(typeof(BooleanCheckboxField), "bcf")]
[JsonDerivedType(typeof(BooleanOptionField), "bof")]
[JsonDerivedType(typeof(ListField), "lf")]
[JsonDerivedType(typeof(ColorCodeField), "ccf")]
[JsonDerivedType(typeof(ActionField), "af")]
[JsonDerivedType(typeof(SearchableMenuField), "smf")]
public class DataEntryField
{
    public string ID;

    public string Text;

    public Color24 TextForeground;
    public Color24 TextBackground;

    public DataEntryField()
    {
        TextForeground = Color24.White;
        TextBackground = Color24.Black;
        VisibilityRule = () => true;

	}

    /// <summary>
    /// Determines if the field should be displayed
    /// </summary>
    public Func<bool> VisibilityRule;

    public T As<T>() where T : DataEntryField => this as T; // A C programmer somewhere just died because of this
}

public class TextField : DataEntryField
{
    public TextField() : this(string.Empty)
    { }

    public TextField(string Text)
    {
        this.Text = Text;

        defInputField = new()
        {
            X = 0,
            Y = 0,

            CursorVisible = false,

            HistoryEnabled = false,
            HighlightingEnabled = false
        };
    }

    [JsonIgnore]
    internal InputField defInputField;

    public string Value
    {
        get => defInputField.Buffer.ToString();

        set
        {
            if (value is null)
                return;

            defInputField.Buffer.Clear();
            defInputField.Buffer.Append(value);

            // Hacky way of getting the InputField to register the fact that we just filled it with text
            // Needed to render properly
            defInputField.OnInput(new(' ', ConsoleKey.End, false, false, false));
        }
    }

    public Action<string> OnUpdate;

    public void TryOnUpdate()
    {
        try
        {
            if (OnUpdate is not null) OnUpdate(Value);
        }
        catch (Exception)
        { }
	}
}

public class BooleanCheckboxField : DataEntryField
{
    public bool Checked;

    public BooleanCheckboxField()
    { }

    public BooleanCheckboxField(string Text, bool Checked = true)
    {
        base.Text = Text;
        this.Checked = Checked;
    }

    public Action<bool> OnUpdate;

    public void TryOnUpdate()
    {
		try
		{
			if (OnUpdate is not null) OnUpdate(Checked);
		}
		catch (Exception)
		{ }
	}
}

public class BooleanOptionField : DataEntryField
{
    public string TrueOption;

    public string FalseOption;

    /// <summary>
    /// true for Option1, false for Option2
    /// </summary>
    public bool Selected;

    public Style SelectedStyle;

    public BooleanOptionField()
    { }

    public BooleanOptionField(string Text, string TrueOption, string FalseOption, Style SelectedStyle = Style.Underlined, bool Selected = true)
    {
        base.Text = Text;
        this.TrueOption = TrueOption;
        this.FalseOption = FalseOption;
        this.SelectedStyle = SelectedStyle;
        this.Selected = Selected;
    }

    public Action<bool> OnUpdate;

    public void TryOnUpdate()
    {
		try
		{
			if (OnUpdate is not null) OnUpdate(Selected);
		}
		catch (Exception)
		{ }
	}
}

public class ListField : DataEntryField
{
    public List<string> Options { get; set; }

    public int SelectedOptionIndex { get; set; } = 0;
    public string SelectedOption => Options[SelectedOptionIndex];

    public bool PaddingEnabled { get; set; } = true;
    public int PaddingAmount { get; set; } = 2;

    public ListField()
    {
        Options = [];
    }

    public ListField(string Text, params string[] Options)
    {
        base.Text = Text;

        if (Options.Any())
        {
            this.Options = Options.ToList();
            SelectedOptionIndex = 0;
        }
    }

    public Action<int, string> OnUpdate;

    public void TryOnUpdate()
    {
		try
		{
            if (OnUpdate is not null) OnUpdate(SelectedOptionIndex, SelectedOption);
		}
		catch (Exception)
		{ }
	}

    internal string CenteredByPadding(string StringToCenter, int TotalLength)
	{
		return StringToCenter.PadLeft( ((TotalLength - StringToCenter.Length) / 2) + StringToCenter.Length).PadRight(TotalLength);
	}
}

public class ColorCodeField : DataEntryField
{
    public byte R;
    public byte G;
    public byte B;

    public ColorCodeType Type;

    /// <summary>
    /// Range: 0 - 3 (0 = RGB/DEC Switch, 1 = Red Field, 2 = Green Field, 3 = Blue Field)
    /// </summary>
    public int SelectedIndex = 0;

    public ColorCodeField()
    { }

    public ColorCodeField(string Text, byte R = 255, byte G = 255, byte B = 255, ColorCodeType Type = ColorCodeType.RgbDec)
    {
        this.Text = Text;

        this.R = R;
        this.G = G;
        this.B = B;

        this.Type = Type;
    }

    public Action<byte, byte, byte> OnUpdate;

    public void TryOnUpdate()
    {
		try
		{
			if (OnUpdate is not null) OnUpdate(R, G, B);
		}
		catch (Exception)
		{ }
	}

    internal void Next() => SelectedIndex = SelectedIndex == 3 ? 0 : ++SelectedIndex;

    internal void Previous() => SelectedIndex = SelectedIndex == 0 ? 3 : --SelectedIndex;

    internal void DoColorInput(char Character)
    {
    begin:
        string Str = string.Empty;

        if (Type == ColorCodeType.RgbHex)
            Str = SelectedIndex switch { 1 => R.ToString("X"), 2 => G.ToString("X"), 3 => B.ToString("X") };
        else
            Str = SelectedIndex switch { 1 => R.ToString(), 2 => G.ToString(), 3 => B.ToString() };

        if (!byte.TryParse(Str += Character, Type == ColorCodeType.RgbHex ? NumberStyles.HexNumber : NumberStyles.Number, null, out byte NewValue))
        {
            switch (SelectedIndex)
            {
                case 1: R = 0; break;
                case 2: G = 0; break;
                case 3: B = 0; break;
            }

            // If we can parse the character by itself, then loop back through
            if (byte.TryParse(Character.ToString(), Type == ColorCodeType.RgbHex ? NumberStyles.HexNumber : NumberStyles.Number, null, out _))
                goto begin;
        }

        switch (SelectedIndex)
        {
            case 1: R = NewValue; break;
            case 2: G = NewValue; break;
            case 3: B = NewValue; break;
        }
    }
}

public enum ColorCodeType
{
    RgbHex,
    RgbDec
}

public class ActionField : DataEntryField
{
    public string ButtonText;
    public Action OnEnter;

    public Color24 ButtonForeground;
    public Color24 ButtonBackground;

    public ActionField()
    { }

    public ActionField(string FieldText, string ButtonText) : this(FieldText, ButtonText, null, Color24.White, Color24.Black)
    { }

	public ActionField(string FieldText, string ButtonText, Action OnEnter) : this(FieldText, ButtonText, OnEnter, Color24.White, Color24.Black)
	{ }

	public ActionField(string FieldText, string ButtonText, Action OnEnter, Color24 ButtonForeground, Color24 ButtonBackground)
	{
		Text = FieldText;
		this.ButtonText = ButtonText;
		this.OnEnter = OnEnter;
		this.ButtonForeground = ButtonForeground;
		this.ButtonBackground = ButtonBackground;
	}
}

public class SearchableMenuField : DataEntryField
{
    public string SelectedOption;
    internal bool IsSearching = false;

    public SearchableMenuField()
    { }

    public SearchableMenuField(string Text, int MenuHeight = 3) : this(Text, MenuHeight, [], Color24.White, Color24.Black)
    { }

    public SearchableMenuField(string Text, int MenuHeight, IList<string> Options) : this(Text, MenuHeight, Options, Color24.White, Color24.Black)
    { }

    public SearchableMenuField(string Text, int MenuHeight, IList<string> Options, Color24 TextForeground, Color24 TextBackground)
    {
        this.Text = Text;
        this.TextForeground = TextForeground;
        this.TextBackground = TextBackground;

        SelectedOption = string.Empty;

        smMenu = new(0, 0, MenuHeight);
        smMenu.Visible = false;
        smMenu.EnableSearching = true;

        foreach (var Option in Options)
            smMenu.AddOption(Option, delegate { });
    }

    internal ScrollableMenu smMenu;

    public Action<string> OnUpdate;

    public void TryOnUpdate()
    {
		try
		{
			if (OnUpdate is not null) OnUpdate(SelectedOption);
		}
		catch (Exception)
		{ }
	}
}