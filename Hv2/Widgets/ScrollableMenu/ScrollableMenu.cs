using Cosmo;

namespace Hv2UI;

public class ScrollableMenu : Widget
{
	public int X { get; set; }
	public int Y { get; set; }
	
	public int Height { get; set; }
	
	// Called whenever the user selects an action
	public Action<int, string> OnSubmit;
	
	private int ScrollY = 0; // Index that tracks where in the menu we have scrolled to
	private int ScrollYMax => Options.Count <= Height ? 0 : Options.Count - Height;
	
	private List<MenuOption> Options;
	
	public MenuOption this[int Index]
	{
		get
		{
			if (!IsValidIndex(Index))
				return null;

			return Options[Index];
		}

		set
		{
			if (!IsValidIndex(Index))
				return;

			Options[Index] = value;
		}
	}

	public int OptionCount => Options.Count;

	public MenuStyle SelectedOptionStyle { get; set; }
	public Alignment TextAlignment { get; set; }

	/// <summary>
	/// Determines if the options will always be drawn styled regardless of if the menu is focused
	/// </summary>
	public bool DoStyle { get; set; }

	public int SelectedOption { get; internal set; }
	
	/// <summary>
	/// Called whenever the user selects an option with the arrow keys. Receives current index and option text.
	/// </summary>
	public Action<int, string> OnSelectionChange { get; set; }
	
	public ScrollableMenu(int X, int Y, int Height)
	{
		this.X = X;
		this.Y = Y;
		this.Height = Height;
		
		SelectedOption = 0;
		
		DoStyle = true;
		SelectedOptionStyle = MenuStyle.Highlighted;

		Options = new();
	}
	
	public override void Draw(Renderer r)
	{
		switch (TextAlignment)
		{
			case Alignment.Center:
				DrawCenterAligned(r);
				break;

			default:
			case Alignment.Left:
				DrawLeftAligned(r);
				break;
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
	
	void DrawLeftAligned(Renderer r)
		{
			int CurrentYOff = 0;
			int DrawCount = Options.Count > Height ? Height : Options.Count;
			
			for (int i = ScrollY; i < ScrollY + DrawCount; i++)
			{
				if (i == SelectedOption && DoStyle)
					DrawStyledOption(X + 1, Y + CurrentYOff, Options[i], r);
				else
					r.WriteAt(X + 1, Y + CurrentYOff, Options[i].Text, Options[i].TextForeground, Options[i].TextBackground, StyleCode.None);
				
				CurrentYOff++;
			}
		}

		void DrawCenterAligned(Renderer r)
		{
			int CurrentYOff = 0;
			int LongestOptionLength = Options.Max(op => op.Text.Length);
			int DrawCount = Options.Count > Height ? Height : Options.Count;

			for (int i = ScrollY; i < ScrollY + DrawCount; i++)
			{
				int CurrentX = X + (LongestOptionLength / 2) - (Options[i].Text.Length / 2) + 1;

				if (i == SelectedOption && DoStyle)
					DrawStyledOption(CurrentX, Y + CurrentYOff, Options[i], r);
				else
					r.WriteAt(CurrentX, Y + CurrentYOff, Options[i].Text, Options[i].TextForeground, Options[i].TextBackground, StyleCode.None);
				
				CurrentYOff++;
			}
		}

		void DrawStyledOption(int X, int Y, MenuOption Option, Renderer r)
		{
			switch (SelectedOptionStyle)
			{
				case MenuStyle.Arrow:
					{
						//	RenderContext.VTSetCursorPosition(X, Y);
						//	RenderContext.VTEnterColorContext(Option.TextForeground, Option.TextBackground, delegate ()
						//	{
						//		RenderContext.VTDrawText(Option.Text);
						//		RenderContext.VTDrawChar(' ');
						//		RenderContext.VTDrawChar('<');
						//	});
						
						r.WriteAt(X, Y, $"{Option.Text} <", Option.TextForeground, Option.TextBackground, StyleCode.None);
						break;
					}

				case MenuStyle.Highlighted:
					{
						//	RenderContext.VTSetCursorPosition(X, Y);
						//	RenderContext.VTInvert();
						//	RenderContext.VTDrawText(Option.Text);
						//	RenderContext.VTRevert();
						
						r.WriteAt(X, Y, Option.Text, Option.TextForeground, Option.TextBackground, StyleCode.Inverted);
						break;
					}
			}
		}
	
	public override void OnInput(ConsoleKeyInfo cki)
	{
		switch (cki.Key)
		{
			case ConsoleKey.UpArrow:
				if (SelectedOption == 0)
				{
					SelectedOption = Options.Count - 1;
					ScrollY = ScrollYMax;
				}
				else
				{
					SelectedOption--;
					
					if (SelectedOption < ScrollY)
						ScrollY = SelectedOption;
				}
				
				if (OnSelectionChange is not null)
					OnSelectionChange(SelectedOption, this[SelectedOption].Text);
				break;

			case ConsoleKey.DownArrow:
				if (SelectedOption == Options.Count - 1)
				{
					SelectedOption = 0;
					ScrollY = 0;
				}
				else
				{
					SelectedOption++;
					
					if (SelectedOption - ScrollY > Height - 1)
						ScrollY++;
				}
				
				if (OnSelectionChange is not null)
					OnSelectionChange(SelectedOption, this[SelectedOption].Text);
				break;
			
			case ConsoleKey.Enter:
				this[SelectedOption].Action();
				if (OnSubmit is not null) OnSubmit(SelectedOption, this[SelectedOption].Text);
				break;
		}
	}
	
	private bool IsValidIndex(int Index) => Index >= 0 && Index < Options.Count;
	
	private bool AtTop => ScrollY == 0;
	
	private bool AtBottom => ScrollY == ScrollYMax;
	
	public void AddOption(string Option, Action Action) => AddOption(Option, Action, Color24.White, Color24.Black);
	
	public void AddOption(string Option, Action Action, Color24 Foreground, Color24 Background)
	{
		if (Option is null || Action is null)
			return;

		var NewOption = new MenuOption()
		{
			Index = Options.Count,
			
			Text = Option,
			Action = Action,
			
			TextForeground = Foreground,
			TextBackground = Background
		};

		Options.Add(NewOption);
	}

	public void RemoveAllOptions()
	{
		SelectedOption = 0;
		ScrollY = 0;
		Options.Clear();
	}

	public void CenterTo(Dimensions d, int XOff = 0, int YOff = 0)
	{
		if (Options.Count == 0)
			return;

		int LongestOptionLength = Options.Max(op => op.Text.Length);

		X = d.HorizontalCenter - (LongestOptionLength / 2) + XOff;
		Y = d.VerticalCenter - (int) Math.Ceiling( OptionCount / 2.0f ) + YOff;

		if (X < 0)
			X = 0;

		if (Y < 0)
			Y = 0;
	}
	
}
