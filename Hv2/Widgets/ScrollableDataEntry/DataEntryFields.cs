
using Cosmo;

namespace Hv2UI;

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

    public T As<T>() where T : class => this as T; // A C programmer somewhere just died because of this
}

public class TextField : DataEntryField
{
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
        }
    }

    public Action<string> OnUpdate;

    public void TryOnUpdate()
    {
        if (OnUpdate is not null) OnUpdate(Value);
    }
}

public class BooleanCheckboxField : DataEntryField
{
    public bool Checked;

    public BooleanCheckboxField(string Text, bool Checked = true)
    {
        base.Text = Text;
        this.Checked = Checked;
    }

    public Action<bool> OnUpdate;

    public void TryOnUpdate()
    {
        if (OnUpdate is not null) OnUpdate(Checked);
    }
}

public class BooleanOptionField : DataEntryField
{
    public string Option1;
    public string Option2;

    /// <summary>
    /// true for Option1, false for Option2
    /// </summary>
    public bool Selected;

    public StyleCode SelectedStyle;

    public BooleanOptionField(string Text, string Option1, string Option2, StyleCode SelectedStyle = StyleCode.Underlined, bool Selected = true)
    {
        base.Text = Text;
        this.Option1 = Option1;
        this.Option2 = Option2;
        this.SelectedStyle = SelectedStyle;
        this.Selected = Selected;
    }

    public Action<bool> OnUpdate;

    public void TryOnUpdate()
    {
        if (OnUpdate is not null) OnUpdate(Selected);
    }
}

public class ListField : DataEntryField
{
    public List<string> Options;
    public int SelectedOption;

    public ListField(string Text, params string[] Options)
    {
        base.Text = Text;

        if (Options.Any())
        {
            this.Options = Options.ToList();
            SelectedOption = 0;
        }
    }

    public Action<int, string> OnUpdate;

    public void TryOnUpdate()
    {
        if (OnUpdate is not null) OnUpdate(SelectedOption, Options[SelectedOption]);
    }
}