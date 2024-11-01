
using Cosmo;
using System.Text;

namespace Hv2UI;

public class DataEntryField
{
    public string ID;

    public DataEntryFieldType Type;

    public string Text;
    public Color24 TextForeground;
    public Color24 TextBackground;

    internal InputField defInputField;

    /// <summary>
    /// Determines if the field should be displayed
    /// </summary>
    public Func<bool> VisibilityRule = () => true;
}

public enum DataEntryFieldType
{
    String,
    Int,
    Float
}