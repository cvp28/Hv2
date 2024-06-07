using Cosmo;

namespace Hv2UI;

public class SearchField<T> : Widget
{
	public int X { get; set; }
	public int Y { get; set; }
	
	public int MenuHeight { get => Menu.Height; set => Menu.Height = value; }
	
	public Func<string, Task<IEnumerable<(string, T)>>> SearchAction;
	
	private InputField Input;
	private ScrollableMenu Menu;
	
	private IEnumerable<(string Text, T Result)> CurrentResults;
	
	private string CurrentMessage = string.Empty;
	
	// If true, focus input field
	// Else, focus menu
	private bool InputHasFocus = true;
	
	private Task SearchTask;
	
	private bool Searching => SearchTask is not null && !SearchTask.IsCompleted;
	private bool HasResults => Menu.OptionCount > 0;
	private bool ResultsReady => !Searching && HasResults;
	
	public SearchField(int X, int Y, string Prompt, int Height, Func<string, Task<IEnumerable<(string, T)>>> SearchAction)
	{
		this.X = X;
		this.Y = Y;
		
		this.SearchAction = SearchAction;
		
		Input = new()
		{
			X = X,
			Y = Y,
			Prompt = Prompt,
			CursorVisible = true,
			ClearOnSubmit = false
		};
		
		Menu = new(X + Input.Prompt.Length - 1, Y + 1, Height)
		{
			DoStyle = false
		};
		
		Input.OnInputReady = OnSubmit;
	}
	
	private void OnSubmit(string Input)
	{
		if (Searching)
			return;
			
		CurrentMessage = "Searching...";
		Menu.RemoveAllOptions();
		
		SearchTask = Task.Run(async delegate
		{
			CurrentResults = await SearchAction(Input);		// Run the client-defined search action and retrieve the results
			
			if (CurrentResults is null || !CurrentResults.Any())
			{
				ResetState(false);
				return;
			}
			
			// Populate menu
			foreach (var Result in CurrentResults)
				Menu.AddOption(Result.Text, delegate {});
			
			CurrentMessage = string.Empty;
		});
	}
	
	private bool OnChar(string Input)
	{
		if (Searching)
			return false;
		
		CurrentMessage = "Searching...";
		Menu.RemoveAllOptions();
		
		SearchTask = Task.Run(async delegate
		{
			CurrentResults = await SearchAction(Input);		// Run the client-defined search action and retrieve the results
			
			if (CurrentResults is null || !CurrentResults.Any())
			{
				ResetState(false);
				return;
			}
			
			// Populate menu
			foreach (var Result in CurrentResults)
				Menu.AddOption(Result.Text, delegate {});
			
			CurrentMessage = string.Empty;
		});
		
		return false;
	}
	
	public void EnableImmediateRefresh()
	{
		Input.OnCharInput = OnChar;
		Input.OnInputReady = null;
	}
	
	public void DisableImmediateRefresh()
	{
		Input.OnCharInput = null;
		Input.OnInputReady = OnSubmit;
	}
	
	// Resets the state (no shit)
	// Only hard resets clear the buffer
	private void ResetState(bool Hard)
	{
		if (Hard) Input.Clear();
		Menu.RemoveAllOptions();
		
		// If the task is already running, then undo its work when it finishes
		if (Searching) SearchTask.ContinueWith((t) => Menu.RemoveAllOptions());
		
		InputHasFocus = true;
		
		Input.CursorVisible = true;
		Menu.DoStyle = false;
		
		CurrentMessage = string.Empty;
	}
	
	public override void OnInput(ConsoleKeyInfo cki)
	{
		if (cki.Key == ConsoleKey.Tab)
		{
			// If we're currently searching or there are no results to select, ignore this key
			if (Searching || !HasResults) return;
			
			InputHasFocus = !InputHasFocus;
		}
		
		if (cki.Key == ConsoleKey.Escape)
		{
			if (InputHasFocus) ResetState(true);
		}
		
		
	}

	public override void Draw(Renderer r)
	{
		Input.CursorVisible = InputHasFocus;
		Menu.DoStyle = !InputHasFocus;
		
		if (InputHasFocus)
			Hv2.FocusedWidget = Input;
		else
			Hv2.FocusedWidget = Menu;
		
		Input.Draw(r);
		Menu.Draw(r);
	}
	
	
}
