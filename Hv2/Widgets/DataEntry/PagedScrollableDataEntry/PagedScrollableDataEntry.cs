using System.Text.Json;

using Cosmo;

using Collections.Pooled;

namespace Hv2UI;

public class PagedScrollableDataEntry : Widget
{
    public int X { get; set; }
	public int Y { get; set; }

	public int Height { get; set; }

	public Color24 SelectedForeground = new(255, 238, 140);
	public Color24 SelectedBackground = Color24.Black;

	public bool AdvanceOnEnter { get; set; } = true;
	public bool PaddingEnabled { get; set; } = true;
	public int PaddingAmount { get; set; } = 2;

	private int FieldOffsetAfterText => X + 1 + CurrentPage.VisibleFields.MaxBy(f => f.Text.Length).Text.Length + PaddingAmount;

	private List<PageContainer> Pages;
	public PageContainer this[int Index] => Pages[Index];
    public PageContainer this[string ID] => Pages.FirstOrDefault(f => f.PageID == ID);

	public int CurrentPageIndex { get; private set; }
	public PageContainer CurrentPage => this[CurrentPageIndex];

	public DataEntryField CurrentField => CurrentPage.CurrentField;

    /// <summary>
    /// Called whenever the user selects an option with the arrow keys. Receives current index and option text.
    /// </summary>
    public Action<string, DataEntryField> OnSelectionChange { get; set; }

	public PagedScrollableDataEntry(int X, int Y, int Height)
	{
		this.X = X;
		this.Y = Y;
		this.Height = Height;

		Pages = [ new PageContainer() { PageID = "Main", PageName = "Main", AttachedInstance = this } ];
		CurrentPageIndex = 0;
	}

	private static JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
	{
		IncludeFields = true,
		WriteIndented = true
	};

	public void SerializeToFile(string Path)
	{
		using var f = File.Open(Path, FileMode.Create, FileAccess.Write, FileShare.Read);

		JsonSerializer.Serialize(f, Pages, SerializerOptions);
	}

	public void LoadFromFile(string Path)
	{
        using var f = File.Open(Path, FileMode.Open);

		Pages = JsonSerializer.Deserialize<List<PageContainer>>(f, SerializerOptions);

		foreach (var page in Pages) page.AttachedInstance = this;
    }

	public override void Draw(Renderer r)
	{
		r.WriteAt(X + 1, Y, $"{CurrentPage.PageName} ← {CurrentPageIndex + 1}/{Pages.Count} →");

		using var vf = CurrentPage.VisibleFields.ToPooledList();

		int CurrentYOff = 1;
		int DrawCount = vf.Count > Height ? Height : vf.Count;

		CurrentPage.ScrollY = Math.Clamp(CurrentPage.ScrollY, 0, CurrentPage.ScrollYMax);

		for (int i = CurrentPage.ScrollY; i < CurrentPage.ScrollY + DrawCount; i++)
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
					tf.defInputField.CursorVisible = vf[i].ID == CurrentField.ID;
					
					tf.defInputField.X = PaddingEnabled ? FieldOffsetAfterText : X + vf[i].Text.Length + 1;
					tf.defInputField.Y = Y + CurrentYOff;
					tf.defInputField.Draw(r);
					break;
				}

				case BooleanCheckboxField bcf:
				{
					var Green = new Color24(0, 200, 0);
					var Red = new Color24(200, 0, 0);

					r.WriteAt(
						PaddingEnabled ? FieldOffsetAfterText : X + vf[i].Text.Length + 1,
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
					int Option1X = PaddingEnabled ? FieldOffsetAfterText : X + vf[i].Text.Length + 1;
					int Option2X = Option1X + bof.TrueOption.Length + 2;

					r.WriteAt(Option1X, Y + CurrentYOff, bof.TrueOption, Color24.White, Color24.Black, bof.Selected ? bof.SelectedStyle : Style.None);
					r.WriteAt(Option2X, Y + CurrentYOff, bof.FalseOption, Color24.White, Color24.Black, bof.Selected ? Style.None : bof.SelectedStyle);
					break;
				}

				case ListField lf:
				{
					int RenderX = PaddingEnabled ? FieldOffsetAfterText : X + vf[i].Text.Length + 1;

					// very long very very silly line of code
					string RenderText = lf.PaddingEnabled ? lf.CenteredByPadding(lf.Options[lf.SelectedOptionIndex], lf.Options.MaxBy(o => o.Length).Length + (PaddingAmount * 2)) : lf.Options[lf.SelectedOptionIndex];
					// this could easily be an if statement that's kind on the eyes
					// but that would be too easy

					r.WriteAt(RenderX, Y + CurrentYOff, $"<{RenderText}>");
					break;
				}

				case ColorCodeField ccf:
				{
					int RenderX = PaddingEnabled ? FieldOffsetAfterText : X + vf[i].Text.Length + 1;

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
					int RenderX = PaddingEnabled ? FieldOffsetAfterText : X + vf[i].Text.Length + 1;

					r.WriteAt(RenderX, Y + CurrentYOff, $"[{af.ButtonText}]");
					break;
				}
			}

			CurrentYOff++;
		}

		// Fancy ass unicode arrows ↑ ↓
		if (!AtTop)
			r.WriteAt(X, Y + 1, "↑");

		if (!AtBottom)
			r.WriteAt(X, Y + 1 + Height - 1, "↓");
	}

	public override void OnInput(ConsoleKeyInfo cki)
	{
		switch (cki.Key)
		{
			case ConsoleKey.UpArrow:
				CurrentPage.Previous();
				break;

			case ConsoleKey.DownArrow:
				CurrentPage.Next();
				break;

			case ConsoleKey.RightArrow:
				
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
						CurrentPage.Previous();
					else
						CurrentPage.Next();
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
				}
				break;
		}
	}

	private bool IsValidIndex(int Index) => Index >= 0 && Index < Pages.Count;

    public DataEntryField CurrentlySelectedField => CurrentPage.VisibleFields[CurrentPage.SelectedFieldIndex];

	private bool AtTop => CurrentPage.ScrollY == 0;
	
	private bool AtBottom => CurrentPage.ScrollY == CurrentPage.ScrollYMax;

	public void AddPage(string ID, string Name = "")
	{
        if (Pages.Any(p => p.PageID == ID))
			return;

		Pages.Add(new() { AttachedInstance = this });
    }

	public void RemovePage(string ID)
	{
		var test = Pages.FirstOrDefault(p => p.PageID == ID);

		if (test is null)
			return;

		Pages.Remove(test);
    }

	public void RemoveAllOptions()
	{
		CurrentPage.SelectedFieldIndex = 0;
		CurrentPage.ScrollY = 0;
		Pages.Clear();
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