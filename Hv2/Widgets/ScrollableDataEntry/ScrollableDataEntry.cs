using Cosmo;
using System.ComponentModel.Design;

namespace Hv2UI;

public class ScrollableDataEntry : Widget
{
	public int X { get; set; }
	public int Y { get; set; }
	
	public int Height { get; set; }

	public Color24 SelectedForeground = new(255, 238, 140);
	public Color24 SelectedBackground = Color24.Black;

	private int ScrollY = 0; // Index that tracks where in the menu we have scrolled to
	private int ScrollYMax => Fields.Count <= Height ? 0 : Fields.Count - Height;
	
	private List<DataEntryField> Fields;
	
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
				Fields.Add(value);
			}
			else
				Fields[Fields.IndexOf(test)] = value;
		}
	}

	public int FieldCount => Fields.Count;

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

		var VisibleFields = Fields.Where(f => f.VisibilityRule()).ToArray();
        int DrawCount = VisibleFields.Length > Height ? Height : VisibleFields.Length;

        for (int i = ScrollY; i < ScrollY + DrawCount; i++)
        {
			//	if (!Fields[i].VisibilityRule())
			//		continue;

			if (VisibleFields[i] == CurrentlySelectedField)
				r.WriteAt(X + 1, Y + CurrentYOff, VisibleFields[i].Text, SelectedForeground, SelectedBackground, StyleCode.None);
			else
				r.WriteAt(X + 1, Y + CurrentYOff, VisibleFields[i].Text, VisibleFields[i].TextForeground, VisibleFields[i].TextBackground, StyleCode.None);

			// Field-specific rendering

			switch (VisibleFields[i])
			{
				case TextField tf:
                    tf.defInputField.CursorVisible = VisibleFields[i].ID == Fields[SelectedFieldIndex].ID;
					
                    tf.defInputField.X = X + VisibleFields[i].Text.Length + 1;
                    tf.defInputField.Y = Y + CurrentYOff;
                    tf.defInputField.Draw(r);
                    break;

				case BooleanCheckboxField bcf:
					r.WriteAt(
						X + VisibleFields[i].Text.Length + 1,
						Y + CurrentYOff,

						bcf.Checked ? "✓" : "X"
					);
					break;

				case BooleanOptionField bof:
					//	r.WriteAt()
					//	
					//	r.WriteAt(
					//		X + VisibleFields[i].Text.Length + 1,
					//		Y + CurrentYOff,
					//	
					//		$"{bof.Option1}  {bof.Option2}",
					//	
					//		Color24.White,
					//		Color24.Black,
					//	
					//		bof == CurrentlySelectedField && !
					//		)
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
				do
				{
					if (SelectedFieldIndex == 0)
					{
						SelectedFieldIndex = Fields.Count - 1;
						ScrollY = ScrollYMax;
					}
					else
					{
						SelectedFieldIndex--;

						if (SelectedFieldIndex < ScrollY)
							ScrollY = SelectedFieldIndex;
					}
				}
				while (!CurrentlySelectedField.VisibilityRule());

				if (OnSelectionChange is not null)
					OnSelectionChange(CurrentlySelectedField.ID, CurrentlySelectedField);

                // Field-specific actions

                switch (CurrentlySelectedField)
                {
                    case TextField tf:
                        tf.defInputField.EnsureCursorVisible();
                        break;
                }
                break;

			case ConsoleKey.DownArrow:
				do
				{
					if (SelectedFieldIndex == Fields.Count - 1)
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
				}
				while (!CurrentlySelectedField.VisibilityRule());

                if (OnSelectionChange is not null)
					OnSelectionChange(CurrentlySelectedField.ID, CurrentlySelectedField);

				// Field-specific actions

				switch (CurrentlySelectedField)
				{
					case TextField tf:
						tf.defInputField.EnsureCursorVisible();
						break;
				}

                break;

            default:
				// Field-specific input

				switch (CurrentlySelectedField)
				{
					case TextField tf:
						tf.defInputField.OnInput(cki);
						break;

					case BooleanCheckboxField bcf:
						if (cki.Key == ConsoleKey.Enter || cki.Key == ConsoleKey.Spacebar)
							bcf.Checked = !bcf.Checked;
						break;

					case BooleanOptionField bof:
						
						break;
				}

				break;
        }
	}

	private bool IsValidIndex(int Index) => Index >= 0 && Index < Fields.Count;

	public DataEntryField CurrentlySelectedField => Fields[SelectedFieldIndex];

	private bool AtTop => ScrollY == 0;
	
	private bool AtBottom => ScrollY == ScrollYMax;

    public void AddField(string Text, string ID, DataEntryField Field)
	{
		if (Fields.Any(f => f.ID == Field.ID))
			return;

		Field.Text = Text;
		Field.ID = ID;

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
