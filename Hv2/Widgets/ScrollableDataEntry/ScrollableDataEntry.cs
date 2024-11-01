using Cosmo;
using System.Diagnostics;
using System.Text;
using System.Timers;

namespace Hv2UI;

public class ScrollableDataEntry : Widget
{
	public int X { get; set; }
	public int Y { get; set; }
	
	public int Height { get; set; }
	
	// Called whenever the user selects an action
	public Action<int, string> OnSubmit;

	private Task<Maybe<MenuOption>> SubmitTask = null;
	private MenuOption SubmitResult = null;
	
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

	public DataEntryField this[string ID] => Fields.First(f => f.ID == ID);

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
        int DrawCount = Fields.Count > Height ? Height : Fields.Count;

        for (int i = ScrollY; i < ScrollY + DrawCount; i++)
        {
			if (!Fields[i].VisibilityRule())
				continue;

            r.WriteAt(X + 1, Y + CurrentYOff, Fields[i].Text, Fields[i].TextForeground, Fields[i].TextBackground, StyleCode.None);

			Fields[i].defInputField.CursorVisible = i == SelectedFieldIndex;
			
			Fields[i].defInputField.X = X + Fields[i].Text.Length + 1;
            Fields[i].defInputField.Y = Y + CurrentYOff;
            Fields[i].defInputField.Draw(r);

            CurrentYOff++;
        }

        // Fancy ass unicode arrows ↑ ↓
        if (!AtTop)
		{
			//	RenderContext.VTSetCursorPosition(State.X, State.Y);
			//	RenderContext.VTDrawChar('↑');
			
			r.WriteAt(X, Y, "↑");
		}
		
		if (!AtBottom)
		{
			//	RenderContext.VTSetCursorPosition(State.X, State.Y + State.Height - 1);
			//	RenderContext.VTDrawChar('↓');
			
			r.WriteAt(X, Y + Height - 1, "↓");
		}
	}

	private bool CancelGetResult = false;

	//	public async Task<Maybe<MenuOption>> GetResultAsync()
    //	{
	//		if (SubmitTask is not null)
	//			return Maybe<MenuOption>.Fail();
	//	
	//		SubmitTask = Task.Run(delegate
    //	    {
	//			while (SubmitResult is null)
	//			{
	//				if (CancelGetResult)
	//				{
	//					CancelGetResult = false;
	//					SubmitResult = null;
	//					SubmitTask = null;
	//					return Maybe<MenuOption>.Fail();
	//				}
	//	
	//				Thread.Sleep(50);
	//			}
	//	
    //	        var temp = SubmitResult;
	//	
    //	        SubmitResult = null;
    //	        SubmitTask = null;
	//	
	//			return Maybe<MenuOption>.Success(temp);
    //	    });
	//	
	//		return await SubmitTask;
	//	}
	
	public override void OnInput(ConsoleKeyInfo cki)
	{
		switch (cki.Key)
		{
			case ConsoleKey.UpArrow:
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
				
				if (OnSelectionChange is not null)
					OnSelectionChange(Fields[SelectedFieldIndex].ID, Fields[SelectedFieldIndex]);

				Fields[SelectedFieldIndex].defInputField.EnsureCursorVisible();
                break;

			case ConsoleKey.DownArrow:
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
				
				if (OnSelectionChange is not null)
					OnSelectionChange(Fields[SelectedFieldIndex].ID, Fields[SelectedFieldIndex]);

                Fields[SelectedFieldIndex].defInputField.EnsureCursorVisible();
                break;
			
			case ConsoleKey.Enter:
				//	this[SelectedFieldIndex].Action();
				//	
				//	if (OnSubmit is not null) OnSubmit(SelectedFieldIndex, this[SelectedFieldIndex].Text);
				//	if (SubmitTask is not null) SubmitResult = this[SelectedFieldIndex];
				break;

			case ConsoleKey.Escape:
				if (SubmitTask is not null) CancelGetResult = true;
				break;

            default:
				Fields[SelectedFieldIndex].defInputField.OnInput(cki);
				break;
        }
	}

	private bool IsValidIndex(int Index) => Index >= 0 && Index < Fields.Count;
	
	private bool AtTop => ScrollY == 0;
	
	private bool AtBottom => ScrollY == ScrollYMax;

	//	public void AddOption(string Option, Action Action) => AddOption(Option, Action, Color24.White, Color24.Black);
	//	
	//	public void AddOption(string Option, Action Action, Color24 Foreground, Color24 Background)
	//	{
	//		if (Option is null || Action is null)
	//			return;
	//	
	//		var NewOption = new MenuOption()
	//		{
	//			Index = Fields.Count,
	//			
	//			Text = Option,
	//			Action = Action,
	//			
	//			TextForeground = Foreground,
	//			TextBackground = Background
	//		};
	//	
	//		Fields.Add(NewOption);
	//	}

	public void AddField(string ID, DataEntryFieldType Type, string Text) => AddField(ID, Type, Text, Color24.White, Color24.Black, () => true);
	public void AddField(string ID, DataEntryFieldType Type, string Text, Color24 Foreground, Color24 Background) => AddField(ID, Type, Text, Foreground, Background, () => true);

    public void AddField(string ID, DataEntryFieldType Type, string Text, Color24 Foreground, Color24 Background, Func<bool> VisibilityRule)
	{
		if (Fields.Any(f => f.ID == ID))
			return;

		Fields.Add(new()
		{
			ID = ID,
			Type = Type,

			Text = Text,

			TextForeground = Foreground,
			TextBackground = Background,

			defInputField = new()
			{
				X = X,
				Y = Y,
				CursorVisible = false
			},

			VisibilityRule = VisibilityRule
		});
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
