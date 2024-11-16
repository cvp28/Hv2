
using System.Reflection;
using System.Xml.Serialization;

using Cosmo;

namespace Hv2UI;

[XmlInclude(typeof(TextField))]
[XmlInclude(typeof(BooleanCheckboxField))]
[XmlInclude(typeof(BooleanOptionField))]
[XmlInclude(typeof(ListField))]
public class DataEntryField
{
    [XmlAttribute("ID")]            public string ID;

    [XmlElement("Text")]            public string Text;

    [XmlElement("TextForeground")]  public Color24 TextForeground;
    [XmlElement("TextBackground")]  public Color24 TextBackground;

    public DataEntryField()
    {
        TextForeground = Color24.White;
        TextBackground = Color24.Black;
        VisibilityRule = () => true;
    }

    /// <summary>
    /// Determines if the field should be displayed
    /// </summary>
    [XmlIgnore]
    public Func<bool> VisibilityRule;

    public T As<T>() where T : class => this as T; // A C programmer somewhere just died because of this
}

public class TextField : DataEntryField
{
    public TextField() : this("")
    { }

    public TextField(string Text)
    {
        base.Text = Text;

        defInputField = new()
        {
            X = 0,
            Y = 0,

            CursorVisible = false,

            HistoryEnabled = false,
            HighlightingEnabled = false
        };
    }

    [XmlIgnore]
    internal InputField defInputField;

    [XmlElement("Buffer")]
    public string Value
    {
        get => defInputField.Buffer.ToString();

        set
        {
            if (value is null)
                return;

            defInputField.Buffer.Clear();
            defInputField.Buffer.Append(value);
        }
    }

    
    [XmlIgnore] public Action<string> OnUpdate;

    public void TryOnUpdate()
    {
        if (OnUpdate is not null) OnUpdate(Value);
    }
}

public class BooleanCheckboxField : DataEntryField
{
    [XmlElement("Checked")]
    public bool Checked;

    public BooleanCheckboxField() : this("")
    { }

    public BooleanCheckboxField(string Text, bool Checked = true)
    {
        base.Text = Text;
        this.Checked = Checked;
    }

    [XmlIgnore]
    public Action<bool> OnUpdate;

    public void TryOnUpdate()
    {
        if (OnUpdate is not null) OnUpdate(Checked);
    }
}

public class BooleanOptionField : DataEntryField
{
    [XmlElement("Option1")]
    public string Option1;

    [XmlElement("Option2")]
    public string Option2;

    /// <summary>
    /// true for Option1, false for Option2
    /// </summary>
    [XmlElement("Selected")]
    public bool Selected;

    [XmlElement("StyleCode")]
    public StyleCode SelectedStyle;

    public BooleanOptionField() : this("", "", "")
    { }

    public BooleanOptionField(string Text, string Option1, string Option2, StyleCode SelectedStyle = StyleCode.Underlined, bool Selected = true)
    {
        base.Text = Text;
        this.Option1 = Option1;
        this.Option2 = Option2;
        this.SelectedStyle = SelectedStyle;
        this.Selected = Selected;
    }

    [XmlIgnore]
    public Action<bool> OnUpdate;

    public void TryOnUpdate()
    {
        if (OnUpdate is not null) OnUpdate(Selected);
    }
}

public class ListField : DataEntryField
{
    [XmlArray("Options")]
    public List<string> Options;

    [XmlElement("SelectedOption")]
    public int SelectedOption;

    public bool PaddingEnabled { get; set; } = true;
    public int PaddingAmount { get; set; } = 2;

    public ListField() : this("", "")
    { }

    public ListField(string Text, params string[] Options)
    {
        base.Text = Text;

        if (Options.Any())
        {
            this.Options = Options.ToList();
            SelectedOption = 0;
        }
    }

    [XmlIgnore]
    public Action<int, string> OnUpdate;

    public void TryOnUpdate()
    {
        if (OnUpdate is not null) OnUpdate(SelectedOption, Options[SelectedOption]);
    }

    internal string CenteredByPadding(string StringToCenter, int TotalLength)
	{
		return StringToCenter.PadLeft( ((TotalLength - StringToCenter.Length) / 2) + StringToCenter.Length).PadRight(TotalLength);
	}
}