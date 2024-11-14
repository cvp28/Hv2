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

	private List<DataEntryField> Fields;
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
					tf.defInputField.CursorVisible = VisibleFields[i].ID == Fields[SelectedFieldIndex].ID;

					tf.defInputField.X = PaddingEnabled ? FieldOffsetAfterText : X + VisibleFields[i].Text.Length + 1;
					tf.defInputField.Y = Y + CurrentYOff;
					tf.defInputField.Draw(r);
					break;

				case BooleanCheckboxField bcf:
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

				case BooleanOptionField bof:
					int Option1X = PaddingEnabled ? FieldOffsetAfterText : X + VisibleFields[i].Text.Length + 1;
					int Option2X = Option1X + bof.Option1.Length + 2;

					r.WriteAt(Option1X, Y + CurrentYOff, bof.Option1, Color24.White, Color24.Black, bof.Selected ? bof.SelectedStyle : StyleCode.None);
					r.WriteAt(Option2X, Y + CurrentYOff, bof.Option2, Color24.White, Color24.Black, bof.Selected ? StyleCode.None : bof.SelectedStyle);
					break;

				case ListField lf:
					int RenderX = PaddingEnabled ? FieldOffsetAfterText : X + VisibleFields[i].Text.Length + 1;

					// very long very very silly line of code
					string RenderText = PaddingEnabled ? lf.CenteredByPadding(lf.Options[lf.SelectedOption], lf.Options.MaxBy(o => o.Length).Length + (PaddingAmount * 2)) : lf.Options[lf.SelectedOption];
					// this could easily be an if statement that's easy to read
					// but that would be too easy

					r.WriteAt(RenderX, Y + CurrentYOff, $"<{RenderText}>");
					break;
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
				if (AdvanceOnEnter)
				{
					if ((ConsoleModifiers.Shift & cki.Modifiers) != 0)
						Previous();
					else
						Next();
				}
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
