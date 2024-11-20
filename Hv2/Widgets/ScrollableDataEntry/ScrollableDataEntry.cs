using System.Text.Json;
using System.Globalization;

using Cosmo;

namespace Hv2UI;

public class ScrollableDataEntry : Widget
{
    public int X { get; set; }
	public int Y { get; set; }

	public int Height { get; set; }

	public Color24 SelectedForeground = new(255, 238, 140);
	public Color24 SelectedBackground = Color24.Black;

	public bool AdvanceOnEnter { get; set; } = true;
	public bool PaddingEnabled { get; set; } = true;
	public int PaddingAmount { get; set; } = 2;

	private int FieldOffsetAfterText => X + 1 + VisibleFields.MaxBy(f => f.Text.Length).Text.Length + PaddingAmount;

	private int ScrollY = 0; // Index that tracks where in the menu we have scrolled to
	private int ScrollYMax => VisibleFields.Length <= Height ? 0 : VisibleFields.Length - Height;

	public List<DataEntryField> Fields;

    private DataEntryField[] VisibleFields => Fields.Where(f => f.VisibilityRule()).ToArray();

    public DataEntryField this[int Index]
	{
		get
		{
			if (!IsValidIndex(Index))
				return null;

			return Fields[Index];
		}

		set
		{
			if (!IsValidIndex(Index))
				return;

			Fields[Index] = value;
		}
	}

    public DataEntryField this[string ID]
	{
		get => Fields.FirstOrDefault(f => f.ID == ID);

		set
		{
			var test = this[ID];

			if (test is null)
			{
				value.ID = ID;
				AddField(value);
			}
			else
			{
				Fields[Fields.IndexOf(test)] = value;
			}
		}
	}

    public int SelectedFieldIndex { get; internal set; }

    /// <summary>
    /// Called whenever the user selects an option with the arrow keys. Receives current index and option text.
    /// </summary>
    public Action<string, DataEntryField> OnSelectionChange { get; set; }

	public ScrollableDataEntry(int X, int Y, int Height)
	{
		this.X = X;
		this.Y = Y;
		this.Height = Height;

		SelectedFieldIndex = 0;

		Fields = new();
	}

	public void SerializeFieldsToFile(string Path)
	{
		using var f = File.Open(Path, FileMode.Create, FileAccess.Write, FileShare.Read);

		var options = new JsonSerializerOptions();
		options.IncludeFields = true;
		options.WriteIndented = true;

		JsonSerializer.Serialize(f, Fields, options);
	}

	public void LoadFieldsFromFile(string Path)
	{
        using var f = File.Open(Path, FileMode.Open);

        var options = new JsonSerializerOptions();
        options.IncludeFields = true;

        Fields = JsonSerializer.Deserialize<List<DataEntryField>>(f, options);
    }

	public override void Draw(Renderer r)
	{
		int CurrentYOff = 0;
		int DrawCount = VisibleFields.Length > Height ? Height : VisibleFields.Length;

		ScrollY = Math.Clamp(ScrollY, 0, ScrollYMax);

		for (int i = ScrollY; i < ScrollY + DrawCount; i++)
		{
			if (VisibleFields[i] == CurrentlySelectedField)
				r.WriteAt(X + 1, Y + CurrentYOff, VisibleFields[i].Text, SelectedForeground, SelectedBackground, StyleCode.None);
			else
				r.WriteAt(X + 1, Y + CurrentYOff, VisibleFields[i].Text, VisibleFields[i].TextForeground, VisibleFields[i].TextBackground, StyleCode.None);

			// Field-specific rendering

			switch (VisibleFields[i])
			{
				case TextField tf:
				{
					tf.defInputField.CursorVisible = VisibleFields[i].ID == Fields[SelectedFieldIndex].ID;

					tf.defInputField.X = PaddingEnabled ? FieldOffsetAfterText : X + VisibleFields[i].Text.Length + 1;
					tf.defInputField.Y = Y + CurrentYOff;
					tf.defInputField.Draw(r);
					break;
				}

				case BooleanCheckboxField bcf:
				{
					var Green = new Color24(0, 200, 0);
					var Red = new Color24(200, 0, 0);

					r.WriteAt(
						PaddingEnabled ? FieldOffsetAfterText : X + VisibleFields[i].Text.Length + 1,
						Y + CurrentYOff,

						bcf.Checked ? "✓" : "X",

						bcf.Checked ? Green : Red,
						Color24.Black,

						StyleCode.None
					);
					break;
				}

				case BooleanOptionField bof:
				{
					int Option1X = PaddingEnabled ? FieldOffsetAfterText : X + VisibleFields[i].Text.Length + 1;
					int Option2X = Option1X + bof.TrueOption.Length + 2;

					r.WriteAt(Option1X, Y + CurrentYOff, bof.TrueOption, Color24.White, Color24.Black, bof.Selected ? bof.SelectedStyle : StyleCode.None);
					r.WriteAt(Option2X, Y + CurrentYOff, bof.FalseOption, Color24.White, Color24.Black, bof.Selected ? StyleCode.None : bof.SelectedStyle);
					break;
				}

				case ListField lf:
				{
					int RenderX = PaddingEnabled ? FieldOffsetAfterText : X + VisibleFields[i].Text.Length + 1;

					// very long very very silly line of code
					string RenderText = lf.PaddingEnabled ? lf.CenteredByPadding(lf.Options[lf.SelectedOption], lf.Options.MaxBy(o => o.Length).Length + (PaddingAmount * 2)) : lf.Options[lf.SelectedOption];
					// this could easily be an if statement that's kind on the eyes
					// but that would be too easy

					r.WriteAt(RenderX, Y + CurrentYOff, $"<{RenderText}>");
					break;
				}

				case ColorCodeField ccf:
				{
					int RenderX = PaddingEnabled ? FieldOffsetAfterText : X + VisibleFields[i].Text.Length + 1;

					// Index 0 (RGB/HEX Switch)
					r.WriteAt(
						RenderX,
						Y + CurrentYOff,

						ccf.Type switch
						{
							ColorCodeType.RgbHex => "HEX",
							ColorCodeType.RgbDec => "DEC"
						},

                        new Color24(ccf.R, ccf.G, ccf.B),
                        Color24.Black,

						ccf.SelectedIndex == 0 && CurrentlySelectedField == ccf ? StyleCode.Underlined : StyleCode.None);

                    // Index 1 (Byte 1 - Red)
                    r.WriteAt(
                        RenderX + 4,
                        Y + CurrentYOff,

                        ccf.Type switch
                        {
                            ColorCodeType.RgbHex => ccf.R.ToString("X"),
                            ColorCodeType.RgbDec => ccf.R.ToString()
                        },

                        Color24.White,
                        Color24.Black,

                        ccf.SelectedIndex == 1 && CurrentlySelectedField == ccf ? StyleCode.Underlined : StyleCode.None);

                    // Index 2 (Byte 2 - Green)
                    r.WriteAt(
                        RenderX + 8,
                        Y + CurrentYOff,

                        ccf.Type switch
                        {
                            ColorCodeType.RgbHex => ccf.G.ToString("X"),
                            ColorCodeType.RgbDec => ccf.G.ToString()
                        },

                        Color24.White,
                        Color24.Black,

                        ccf.SelectedIndex == 2 && CurrentlySelectedField == ccf ? StyleCode.Underlined : StyleCode.None);

                    // Index 3 (Byte 3 - Blue)
                    r.WriteAt(
                        RenderX + 12,
                        Y + CurrentYOff,

                        ccf.Type switch
                        {
                            ColorCodeType.RgbHex => ccf.B.ToString("X"),
                            ColorCodeType.RgbDec => ccf.B.ToString()
                        },

                        Color24.White,
                        Color24.Black,

                        ccf.SelectedIndex == 3 && CurrentlySelectedField == ccf ? StyleCode.Underlined : StyleCode.None);

					// Color Display
                    r.WriteAt(
                        RenderX + 16,
                        Y + CurrentYOff,

                        "█",

                        new Color24(ccf.R, ccf.G, ccf.B),
                        Color24.Black,

                        StyleCode.None);
                    break;
				}
			}

			CurrentYOff++;
		}

		// Fancy ass unicode arrows ↑ ↓
		if (!AtTop)
			r.WriteAt(X, Y, "↑");

		if (!AtBottom)
			r.WriteAt(X, Y + Height - 1, "↓");
	}

	public override void OnInput(ConsoleKeyInfo cki)
	{
		switch (cki.Key)
		{
			case ConsoleKey.UpArrow:
				Previous();
				break;

			case ConsoleKey.DownArrow:
				Next();
				break;

			case ConsoleKey.Enter:
				// Field-specific override
				switch (CurrentlySelectedField)
				{
					case ColorCodeField ccf:
						if (ccf.SelectedIndex != 3)
						{
							ccf.Next();
							goto skip;
						}
						else
						{
							ccf.SelectedIndex = 1;
						}

						break;

				}

				if (AdvanceOnEnter)
				{
					if ((ConsoleModifiers.Shift & cki.Modifiers) != 0)
						Previous();
					else
						Next();
				}

				skip:
				break;

			default:
				// Field-specific input

				switch (CurrentlySelectedField)
				{
					case TextField tf:
						tf.defInputField.OnInput(cki);
						tf.TryOnUpdate();
						break;

					case BooleanCheckboxField bcf:
                        bcf.Checked = cki.Key switch
                        {
                            ConsoleKey.LeftArrow or ConsoleKey.RightArrow or ConsoleKey.Spacebar => !bcf.Checked
                        };
                        bcf.TryOnUpdate();
						break;

					case BooleanOptionField bof:
						bof.Selected = cki.Key switch
						{
							ConsoleKey.LeftArrow or ConsoleKey.RightArrow or ConsoleKey.Spacebar => !bof.Selected
						};
						bof.TryOnUpdate();
						break;

					case ListField lf:
						switch (cki.Key)
						{
							case ConsoleKey.LeftArrow:
                                if (lf.SelectedOption == 0)
                                    lf.SelectedOption = lf.Options.Count - 1;
                                else
                                    lf.SelectedOption--;
                                break;

							case ConsoleKey.RightArrow:
								if (lf.SelectedOption == lf.Options.Count - 1)
									lf.SelectedOption = 0;
								else
									lf.SelectedOption++;
								break;
						}

						lf.TryOnUpdate();
						break;

					case ColorCodeField ccf:
					{
						switch (ccf.SelectedIndex)
						{
							case 0:
								if (cki.Key == ConsoleKey.Spacebar || cki.Key == ConsoleKey.Enter)
									ccf.Type = ccf.Type == ColorCodeType.RgbHex ? ColorCodeType.RgbDec : ColorCodeType.RgbHex;
								break;

							case 1:
								switch (cki.Key)
								{
									case ConsoleKey.OemPlus:
									case ConsoleKey.Add:
										ccf.R = ccf.R >= 255 ? (byte)0 : ++ccf.R;
										break;

									case ConsoleKey.OemMinus:
									case ConsoleKey.Subtract:
										ccf.R = ccf.R <= 0 ? (byte)255 : --ccf.R;
										break;

									case ConsoleKey.Delete:
										ccf.R = 0;
										break;

									default:
                                        ccf.DoColorInput(cki.KeyChar);
                                        break;
								}
								break;

							case 2:
								switch (cki.Key)
								{
									case ConsoleKey.OemPlus:
									case ConsoleKey.Add:
										ccf.G = ccf.G >= 255 ? (byte)0 : ++ccf.G;
										break;

									case ConsoleKey.OemMinus:
									case ConsoleKey.Subtract:
										ccf.G = ccf.G <= 0 ? (byte)255 : --ccf.G;
										break;

									case ConsoleKey.Delete:
										ccf.G = 0;
										break;

                                    default:
                                        ccf.DoColorInput(cki.KeyChar);
                                        break;
                                }
								break;

							case 3:
								switch (cki.Key)
								{
									case ConsoleKey.OemPlus:
									case ConsoleKey.Add:
										ccf.B = ccf.B >= 255 ? (byte)0 : ++ccf.B;
										break;

									case ConsoleKey.OemMinus:
									case ConsoleKey.Subtract:
										ccf.B = ccf.B <= 0 ? (byte)255 : --ccf.B;
										break;

									case ConsoleKey.Delete:
										ccf.B = 0;
										break;

                                    default:
                                        ccf.DoColorInput(cki.KeyChar);
                                        break;
                                }
								break;
						}

						if (cki.Key == ConsoleKey.RightArrow)
							ccf.Next();
						else if (cki.Key == ConsoleKey.LeftArrow)
							ccf.Previous();

						break;
					}
				}
				break;
		}
	}

	private void Previous()
	{
        if (SelectedFieldIndex == 0)
        {
            SelectedFieldIndex = VisibleFields.Length - 1;
            ScrollY = ScrollYMax;
        }
        else
        {
            SelectedFieldIndex--;

            if (SelectedFieldIndex < ScrollY)
                ScrollY = SelectedFieldIndex;
        }

        if (OnSelectionChange is not null)
            OnSelectionChange(CurrentlySelectedField.ID, CurrentlySelectedField);

        // Field-specific actions

        switch (CurrentlySelectedField)
        {
            case TextField tf:
                tf.defInputField.EnsureCursorVisible();
                break;
        }
    }

	private void Next()
	{
        if (SelectedFieldIndex == VisibleFields.Length - 1)
        {
            SelectedFieldIndex = 0;
            ScrollY = 0;
        }
        else
        {
            SelectedFieldIndex++;

            if (SelectedFieldIndex - ScrollY > Height - 1)
                ScrollY++;
        }

        if (OnSelectionChange is not null)
            OnSelectionChange(CurrentlySelectedField.ID, CurrentlySelectedField);

        // Field-specific actions

        switch (CurrentlySelectedField)
        {
            case TextField tf:
                tf.defInputField.EnsureCursorVisible();
                break;
        }
    }

	private bool IsValidIndex(int Index) => Index >= 0 && Index < Fields.Count;

    public DataEntryField CurrentlySelectedField => VisibleFields[SelectedFieldIndex];

	private bool AtTop => ScrollY == 0;
	
	private bool AtBottom => ScrollY == ScrollYMax;

    public void AddField(DataEntryField Field)
	{
		if (Fields.Any(f => f.ID == Field.ID))
			return;

		Fields.Add(Field);
	}

	public void RemoveAllOptions()
	{
		SelectedFieldIndex = 0;
		ScrollY = 0;
		Fields.Clear();
	}

	//	public void CenterTo(Dimensions d, int XOff = 0, int YOff = 0)
	//	{
	//		if (Fields.Count == 0)
	//			return;
	//	
	//		int LongestOptionLength = Fields.Max(op => op.Text.Length);
	//	
	//		X = d.HorizontalCenter - (LongestOptionLength / 2) + XOff;
	//		Y = d.VerticalCenter - (int) Math.Ceiling( FieldCount / 2.0f ) + YOff;
	//	
	//		if (X < 0)
	//			X = 0;
	//	
	//		if (Y < 0)
	//			Y = 0;
	//	}
	
}