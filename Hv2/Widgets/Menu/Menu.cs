
using Cosmo;

namespace Hv2UI;

public class Menu : Widget
{
	public int X { get; set; }
	public int Y { get; set; }

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

	public Menu(int X, int Y)
	{
		this.X = X;
		this.Y = Y;
		SelectedOption = 0;

		DoStyle = true;
		SelectedOptionStyle = MenuStyle.Highlighted;

		Options = new();
	}

	public override void OnInput(ConsoleKeyInfo cki)
	{
		if (Options.Count == 0)
			return;

		switch (cki.Key)
		{
			case ConsoleKey.UpArrow:
				if (SelectedOption == 0)
					SelectedOption = Options.Count - 1;
				else
					SelectedOption--;

				if (OnSelectionChange is not null)
					OnSelectionChange(SelectedOption, this[SelectedOption].Text);
				break;

			case ConsoleKey.DownArrow:
				if (SelectedOption == Options.Count - 1)
					SelectedOption = 0;
				else
					SelectedOption++;

				if (OnSelectionChange is not null)
					OnSelectionChange(SelectedOption, this[SelectedOption].Text);
				break;

			case ConsoleKey.Enter:
				this[SelectedOption].Action();
				break;
		}
	}

    public override void OnFocused() => DoStyle = true;
    public override void OnDefocused() => DoStyle = false;

    public override void Draw(Renderer r)
	{
		switch (TextAlignment)
		{
			case Alignment.Center:
				DrawCenterAligned();
				break;

			default:
			case Alignment.Left:
				DrawLeftAligned();
				break;
		}

		void DrawLeftAligned()
		{
			for (int i = 0; i < Options.Count; i++)
			{
				if (i == SelectedOption && DoStyle)
					DrawStyledOption(X, Y + i, Options[i]);
				else
				{
					r.WriteAt(X, Y + i, Options[i].Text, Options[i].TextForeground, Options[i].TextBackground, Style.None);

					//	RenderContext.VTSetCursorPosition(X, Y + i);
					//	RenderContext.VTEnterColorContext(Options[i].TextForeground, Options[i].TextBackground, delegate ()
					//	{
					//		RenderContext.VTDrawText(Options[i].Text);
					//	});
				}
			}
		}

		void DrawCenterAligned()
		{
			int LongestOptionLength = Options.Max(op => op.Text.Length);

			for (int i = 0; i < Options.Count; i++)
			{
				int CurrentX = X + (LongestOptionLength / 2) - (Options[i].Text.Length / 2);

				if (i == SelectedOption && DoStyle)
					DrawStyledOption(CurrentX, Y + i, Options[i]);
				else
				{
					r.WriteAt(CurrentX, Y + i, Options[i].Text, Options[i].TextForeground, Options[i].TextBackground, Style.None);

					//	RenderContext.VTSetCursorPosition(CurrentX, Y + i);
					//	RenderContext.VTEnterColorContext(Options[i].TextForeground, Options[i].TextBackground, delegate ()
					//	{
					//		RenderContext.VTDrawText(Options[i].Text);
					//	});
				}
			}
		}

		void DrawStyledOption(int X, int Y, MenuOption Option)
		{
			switch (SelectedOptionStyle)
			{
				case MenuStyle.Arrow:
					{
						r.WriteAt(X, Y, $"{Option.Text} <", Option.TextForeground, Option.TextBackground, Style.None);

						//	RenderContext.VTSetCursorPosition(X, Y);
						//	RenderContext.VTEnterColorContext(Option.TextForeground, Option.TextBackground, delegate ()
						//	{
						//		RenderContext.VTDrawText(Option.Text);
						//		RenderContext.VTDrawChar(' ');
						//		RenderContext.VTDrawChar('<');
						//	});
						break;
					}

				case MenuStyle.Highlighted:
					{
						r.WriteAt(X, Y, Option.Text, Option.TextForeground, Option.TextBackground, Style.Inverted);

						//	RenderContext.VTSetCursorPosition(X, Y);
						//	RenderContext.VTInvert();
						//	RenderContext.VTDrawText(Option.Text);
						//	RenderContext.VTRevert();
						break;
					}
			}
		}
	}

	private bool IsValidIndex(int Index) => Index >= 0 && Index < Options.Count;

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
		Options.Clear();
	}

	public void CenterTo(Dimensions d, int XOff = 0, int YOff = 0)
	{
		if (Options.Count == 0)
			return;

		int LongestOptionLength = Options.Max(op => op.Text.Length);

		X = d.HorizontalCenter - (LongestOptionLength / 2) + XOff;
		Y = d.VerticalCenter - (int) Math.Ceiling(OptionCount / 2.0f) + YOff;

		if (X < 0)
			X = 0;

		if (Y < 0)
			Y = 0;
	}
}
