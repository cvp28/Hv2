using System.Text.Json;
using Collections.Pooled;
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

    private DataEntryField[] VisibleFields => Fields.Where(f => {

		try
		{
			return f.VisibilityRule();
		}
		catch (Exception)
		{
			return true;
		}

	}).ToArray();

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
		using var vf = VisibleFields.ToPooledList();

		int CurrentYOff = 0;
		int DrawCount = vf.Count > Height ? Height : vf.Count;

		ScrollY = Math.Clamp(ScrollY, 0, ScrollYMax);

		for (int i = ScrollY; i < ScrollY + DrawCount; i++)
		{
			if (vf[i] == CurrentlySelectedField)
				r.WriteAt(X + 1, Y + CurrentYOff, vf[i].Text, SelectedForeground, SelectedBackground, Style.None);
			else
				r.WriteAt(X + 1, Y + CurrentYOff, vf[i].Text, vf[i].TextForeground, vf[i].TextBackground, Style.None);

			// Field-specific rendering

			switch (vf[i])
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

						Style.None
					);
					break;
				}

				case BooleanOptionField bof:
				{
					int Option1X = PaddingEnabled ? FieldOffsetAfterText : X + VisibleFields[i].Text.Length + 1;
					int Option2X = Option1X + bof.TrueOption.Length + 2;

					r.WriteAt(Option1X, Y + CurrentYOff, bof.TrueOption, Color24.White, Color24.Black, bof.Selected ? bof.SelectedStyle : Style.None);
					r.WriteAt(Option2X, Y + CurrentYOff, bof.FalseOption, Color24.White, Color24.Black, bof.Selected ? Style.None : bof.SelectedStyle);
					break;
				}

				case ListField lf:
				{
					int RenderX = PaddingEnabled ? FieldOffsetAfterText : X + VisibleFields[i].Text.Length + 1;

					// very long very very silly line of code
					string RenderText = lf.PaddingEnabled ? lf.CenteredByPadding(lf.Options[lf.SelectedOptionIndex], lf.Options.MaxBy(o => o.Length).Length + (PaddingAmount * 2)) : lf.Options[lf.SelectedOptionIndex];
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

						ccf.SelectedIndex == 0 && CurrentlySelectedField == ccf ? Style.Underlined : Style.None);

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

                        ccf.SelectedIndex == 1 && CurrentlySelectedField == ccf ? Style.Underlined : Style.None);

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

                        ccf.SelectedIndex == 2 && CurrentlySelectedField == ccf ? Style.Underlined : Style.None);

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

                        ccf.SelectedIndex == 3 && CurrentlySelectedField == ccf ? Style.Underlined : Style.None);

					// Color Display
                    r.WriteAt(
                        RenderX + 16,
                        Y + CurrentYOff,

                        "█",

                        new Color24(ccf.R, ccf.G, ccf.B),
                        Color24.Black,

                        Style.None);
                    break;
				}

				case ActionField af:
				{
					int RenderX = PaddingEnabled ? FieldOffsetAfterText : X + VisibleFields[i].Text.Length + 1;

					r.WriteAt(RenderX, Y + CurrentYOff, $"[{af.ButtonText}]");
					break;
				}

				case SearchableMenuField smf:
				{
					int ThisX = PaddingEnabled ? FieldOffsetAfterText : X + VisibleFields[i].Text.Length + 1;
					int ThisY = Y + CurrentYOff;

					smf.smMenu.X = ThisX;
					smf.smMenu.Y = ThisY;

					if (smf.smMenu.Visible)
					{
						smf.smMenu.Draw(r);

						// If the menu could potentially overflow to other fields, then return early to prevent those fields from occluding this one
						// This is an insult to good design, but it works
						if (smf.smMenu.Height > 1)
							return;
					}
					else
					{
						r.WriteAt(ThisX, ThisY, smf.SelectedOption);
					}

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
			{
				if (CurrentlySelectedField is SearchableMenuField smf && smf.IsSearching)
				{
					smf.smMenu.OnInput(cki);
					break;
				}

				Previous();
				break;
			}

			case ConsoleKey.DownArrow:
			{
				if (CurrentlySelectedField is SearchableMenuField smf && smf.IsSearching)
				{
					smf.smMenu.OnInput(cki);
					break;
				}

				Next();
				break;
			}

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

					case SearchableMenuField smf:
					{
						if (!smf.IsSearching)
						{
							smf.smMenu.Visible = true;
							smf.IsSearching = true;
						}
						else
						{
							smf.SelectedOption = smf.smMenu[smf.smMenu.SelectedOption].Text;
							smf.TryOnUpdate();
							smf.smMenu.Visible = false;
							smf.IsSearching = false;
						}
						goto skip;
					}

					case BooleanCheckboxField bcf:
						if (!AdvanceOnEnter)
						{
							bcf.Checked = !bcf.Checked;
							bcf.TryOnUpdate();
						}
						break;

					case ActionField af:
						if (!AdvanceOnEnter && af.OnEnter is not null)
							try { af.OnEnter(); } catch (Exception) { }
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
                                if (lf.SelectedOptionIndex == 0)
                                    lf.SelectedOptionIndex = lf.Options.Count - 1;
                                else
                                    lf.SelectedOptionIndex--;
                                break;

							case ConsoleKey.RightArrow:
								if (lf.SelectedOptionIndex == lf.Options.Count - 1)
									lf.SelectedOptionIndex = 0;
								else
									lf.SelectedOptionIndex++;
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

					case ActionField af:
					{
						if (cki.Key == ConsoleKey.Spacebar && af.OnEnter is not null)
							try { af.OnEnter(); } catch (Exception) { }

						break;
					}

					case SearchableMenuField smf:
					{
						// This field shouldn't do anything if there are no options to choose from
						if (smf.smMenu.OptionCount == 0)
							break;

						if (smf.IsSearching)
						{
							if (cki.Key == ConsoleKey.Escape)
							{
								smf.IsSearching = false;
								smf.smMenu.Visible = false;
							}
							else
							{
								smf.smMenu.OnInput(cki);
							}
						}
						else
						{
							if (cki.Key == ConsoleKey.Delete)
								smf.SelectedOption = string.Empty;
						}

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