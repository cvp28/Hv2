using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;

using Cosmo;

namespace Hv2UI;

public enum InputFilter
{
	None,
	Numerics,
	NumericsWithDots,
	NumericsWithSingleDot,
}

public partial class InputField : Widget
{
	public int X { get; set; } = 0;
	public int Y { get; set; } = 0;

	public string Prompt { get; set; } = string.Empty;

	public string EmptyMessage { get; set; } = string.Empty;
	public Color24 EmptyMessageForeground { get; set; } = new Color24(138, 138, 138); // Equivalent color to "Argent" from Haven Gen. 1
	public Color24 EmptyMessageBackground { get; set; } = Color24.Black;

	public bool ClearOnSubmit { get; set; } = false;

	public int CursorX => (X + Prompt.Length + CurrentBufferIndex) % Hv2.WindowWidth;
	public int CursorY => ((X + Prompt.Length + CurrentBufferIndex) / Hv2.WindowWidth) + Y;

	public bool CursorVisible { get; set; } = true;

	public int CursorBlinkIntervalMs { get; set; } = 500;

	public Color24 CursorForeground { get; set; } = Color24.Black;
	public Color24 CursorBackground { get; set; } = Color24.White;

	public bool HistoryEnabled { get; set; } = true;
	public List<string> History { get; set; } = new();
	public int CurrentHistoryIndex { get; private set; } = 0;

	public bool HighlightingEnabled { get; set; } = false;
	public Action<IEnumerable<Token>> OnHighlight { get; set; }

	public Func<Token, IEnumerable<Token>, IEnumerable<string>> OnRetrieveSuggestions { get; set; }

	public InputFilter Filter { get; set; } = InputFilter.None;

	public int BufferLength => Buffer.Length;

	public Action<string> OnInputReady { get; set; }
	public Func<string, bool> OnCharInput { get; set; }

	#region Non-Public Members
	internal StringBuilder Buffer = new();
	private int CurrentBufferIndex = 0;

	private List<Token> CurrentTokens = new();
	private List<Token> TempTokens = new();

	private List<int> SnapPoints = new();

	private bool InAutoCompleteMode = false;
	private bool InHistoryMode = false;

	private string[] AutoCompleteSuggestions;
	private int CurrentCompletionsIndex = 0;

	private string BufferBackup;

	[GeneratedRegex("\"(.*?)\"|([\\S]*)", RegexOptions.IgnoreCase)]
	private static partial Regex TokenizerRegex();

	private Stopwatch CursorTimer = new();
	private bool InternalDrawCursor = true;
	#endregion

	public InputField()
	{
		CursorTimer.Start();

		// Spin up the cursor draw task
		Task.Run(delegate
		{
			while (this is not null)
			{
				while (CursorTimer.ElapsedMilliseconds < CursorBlinkIntervalMs)
					Thread.Sleep(1);

				InternalDrawCursor = !InternalDrawCursor;
				CursorTimer.Restart();
			}
		});
	}

	public void EnsureCursorVisible()
	{
		InternalDrawCursor = true;
		CursorTimer.Restart();
	}

    public override void OnFocused()
    {
		CursorVisible = true;
    }

    public override void OnDefocused()
    {
		CursorVisible = false;
    }

    public override void Draw(Renderer r)
	{
		r.WriteAt(X, Y, Prompt);

		if (Buffer.Length == 0 && EmptyMessage is not null && EmptyMessage.Length > 0)
			r.WriteAt(X + Prompt.Length, Y, EmptyMessage, EmptyMessageForeground, EmptyMessageBackground, Style.None);

		// Draw buffer using tokens from tokenizer
		foreach (var Token in CurrentTokens)
		{
			var (TokenX, TokenY) = Hv2.GetCoordsFromOffsetEx(X + Prompt.Length, Y, Token.StartIndex);
			r.WriteAt(TokenX, TokenY, Token.RawContent, Token.HighlightForeground, Token.HighlightBackground, Style.None);
		}

		if (CursorVisible && InternalDrawCursor)
		{
			if (Buffer.Length > 0 && CurrentBufferIndex != Buffer.Length)
				r.WriteAt(CursorX, CursorY, $"{Buffer[CurrentBufferIndex]}", CursorForeground, CursorBackground, Style.None);
			else if (Buffer.Length == 0 && EmptyMessage is not null && EmptyMessage.Length > 0)
				r.WriteAt(CursorX, CursorY, EmptyMessage[0], CursorForeground, CursorBackground, Style.None);
			else
				r.WriteAt(CursorX, CursorY, ' ', CursorForeground, CursorBackground, Style.None);
		}
	}

	public override void OnInput(ConsoleKeyInfo cki)
	{
		if (cki.Key != ConsoleKey.Tab)
			InAutoCompleteMode = false;

		if (cki.Key != ConsoleKey.UpArrow && cki.Key != ConsoleKey.DownArrow)
		{
			InHistoryMode = false;
			CurrentHistoryIndex = 0;
		}

		// If any key is pressed, ensure cursor visibility and restart the cursor timer
		InternalDrawCursor = true;
		CursorTimer.Restart();

		bool RaiseInputEvent = false;

		switch (cki.Key)
		{
			case ConsoleKey.Tab:
				var SelectedToken = CurrentTokens.FirstOrDefault(t => t.Selected);

				// If there is no currently selected token, break
				if (SelectedToken is null)
				{
					CurrentCompletionsIndex = 0;
					InAutoCompleteMode = false;
					break;
				}

				int Index = CurrentTokens.IndexOf(SelectedToken);

				// If token is not found in list (for whatever reason), break
				if (Index == -1)
				{
					CurrentCompletionsIndex = 0;
					InAutoCompleteMode = false;
					break;
				}

				// If suggestions handler is null, break
				if (OnRetrieveSuggestions is null)
				{
					CurrentCompletionsIndex = 0;
					InAutoCompleteMode = false;
					break;
				}

				// This is ugly, I know
				void SetCurrentToken(string NewTokenContent, bool InsertQuotes)
				{
					if (SelectedToken.Quoted)
					{
						Buffer.Remove(SelectedToken.StartIndex, SelectedToken.Content.Length + 2);

						if (InsertQuotes)
						{
							Buffer.Insert(SelectedToken.StartIndex, $"\"{NewTokenContent}\"");
							CurrentBufferIndex = SelectedToken.StartIndex + NewTokenContent.Length + 2;
						}
						else
						{
							Buffer.Insert(SelectedToken.StartIndex, NewTokenContent);
							CurrentBufferIndex = SelectedToken.StartIndex + NewTokenContent.Length;
						}

					}
					else
					{
						Buffer.Remove(SelectedToken.StartIndex, SelectedToken.Content.Length);

						if (InsertQuotes)
						{
							Buffer.Insert(SelectedToken.StartIndex, $"\"{NewTokenContent}\"");
							CurrentBufferIndex = SelectedToken.StartIndex + NewTokenContent.Length + 2;
						}
						else
						{
							Buffer.Insert(SelectedToken.StartIndex, NewTokenContent);
							CurrentBufferIndex = SelectedToken.StartIndex + NewTokenContent.Length;
						}
					}
				}

				if (!InAutoCompleteMode)
				{
					CurrentCompletionsIndex = 0;
					InAutoCompleteMode = true;

					var temp = OnRetrieveSuggestions(SelectedToken, CurrentTokens)?.ToArray();

					// If the caller returned null (for whatever reason) or the returned container did not have any elements
					if (temp is null || !temp.Any())
					{
						CurrentCompletionsIndex = 0;
						InAutoCompleteMode = false;
						break;
					}

					AutoCompleteSuggestions = temp;
				}
				else
				{
					if (CurrentCompletionsIndex == AutoCompleteSuggestions.Length - 1)
						CurrentCompletionsIndex = 0;
					else
						CurrentCompletionsIndex++;
				}

				SetCurrentToken(AutoCompleteSuggestions[CurrentCompletionsIndex], AutoCompleteSuggestions[CurrentCompletionsIndex].Contains(' '));
				break;

			case ConsoleKey.UpArrow:
				if (History.Count == 0)
					break;

				if (!InHistoryMode)
				{
					BufferBackup = Buffer.ToString();
					InHistoryMode = true;

					if (History.Count > 0)
						CurrentHistoryIndex = History.Count - 1;
				}
				else
				{
					if (History.Count > 0)
						CurrentHistoryIndex = DecrementInRange(CurrentHistoryIndex, -1, History.Count - 1);
					else
						break;
				}

				if (CurrentHistoryIndex == -1)
				{
					Buffer.Clear();
					Buffer.Insert(0, BufferBackup);

					if (Buffer.Length == 0)
						CurrentBufferIndex = 0;
					else
						CurrentBufferIndex = Buffer.Length;

					InHistoryMode = false;
				}
				else
				{
					Buffer.Clear();
					Buffer.Insert(0, History[CurrentHistoryIndex]);
					CurrentBufferIndex = Buffer.Length;
				}
				break;

			case ConsoleKey.DownArrow:
				if (History.Count == 0)
					break;

				if (!InHistoryMode)
				{
					BufferBackup = Buffer.ToString();
					InHistoryMode = true;

					CurrentHistoryIndex = 0;
				}
				else
				{
					if (History.Count > 0)
						CurrentHistoryIndex = IncrementInRange(CurrentHistoryIndex, -1, History.Count - 1);
					else
						break;
				}

				if (CurrentHistoryIndex == -1)
				{
					Buffer.Clear();
					Buffer.Insert(0, BufferBackup);

					if (Buffer.Length == 0)
						CurrentBufferIndex = 0;
					else
						CurrentBufferIndex = Buffer.Length;

					InHistoryMode = false;
				}
				else
				{
					Buffer.Clear();
					Buffer.Insert(0, History[CurrentHistoryIndex]);
					CurrentBufferIndex = Buffer.Length;
				}
				break;

			case ConsoleKey.LeftArrow:
				if ((cki.Modifiers & ConsoleModifiers.Control) != 0)
				{
					var ValidSnapPoints = SnapPoints.Where(p => p < CurrentBufferIndex);

					if (ValidSnapPoints.Any())
					{
						CurrentBufferIndex = ValidSnapPoints.Last();
						break;
					}
					else
						break;
				}

				CursorLeft();
				break;

			case ConsoleKey.RightArrow:
				if ((cki.Modifiers & ConsoleModifiers.Control) != 0)
				{
					var ValidSnapPoints = SnapPoints.Where(p => p > CurrentBufferIndex);

					if (ValidSnapPoints.Any())
					{
						CurrentBufferIndex = ValidSnapPoints.First();
						break;
					}
					else
						break;
				}

				CursorRight();
				break;

			case ConsoleKey.Delete:
				if ((cki.Modifiers & ConsoleModifiers.Control) != 0)
				{
					Clear();
					break;
				}

				InverseBackspace();
				break;

			case ConsoleKey.Backspace:
				Backspace();
				break;

			case ConsoleKey.Home:
				CursorToStart();
				break;

			case ConsoleKey.End:
				CursorToEnd();
				break;

			case ConsoleKey.Escape:
                if (ReadLineTask is not null) CancelGetResult = true;
                break;

			case ConsoleKey.Enter:
				RaiseInputEvent = true;
				break;

			default:
				char c = cki.KeyChar;
				bool Valid = false;

				switch (Filter)
				{
					case InputFilter.None:
						Valid = char.IsLetterOrDigit(c) || char.IsPunctuation(c) || char.IsWhiteSpace(c) || char.IsSymbol(c);
						break;

					case InputFilter.Numerics:
						Valid = char.IsDigit(c);
						break;

					case InputFilter.NumericsWithDots:
						Valid = char.IsDigit(c) || c == '.';
						break;

					case InputFilter.NumericsWithSingleDot:
						if (Buffer.ToString().Contains('.'))
							Valid = char.IsDigit(c);
						else
							Valid = char.IsDigit(c) || c == '.';

						break;
				}

				if (!Valid)
					break;

				Buffer.Insert(CurrentBufferIndex, c);
				CurrentBufferIndex++;
				break;
		}

		if (OnCharInput is not null && OnCharInput(Buffer.ToString()))
			RaiseInputEvent = true;

		if (RaiseInputEvent)
		{
			string Result = Buffer.ToString();

			OnInputReady?.Invoke(Result);

			if (ReadLineTask is not null)
				ReadLineResult = Result;

			if (Result.Length > 0 && HistoryEnabled)
				History.Add(Result);

			if (ClearOnSubmit)
			{
				Buffer.Clear();
				CurrentBufferIndex = 0;
			}
		}

		// Clear current tokens list
		CurrentTokens.Clear();

		// Clear cursor snap point list (Makes CTRL+LeftArrow/CTRL+RightArrow behavior work)
		SnapPoints.Clear();

		// Tokenize buffer
		var tokens = Tokenize(Buffer.ToString());

		// Determine currently selected token
		for (int i = 0; i < tokens.Length; i++)
		{
			Token CurrentToken = tokens[i];

			int Offset = 0;

			if (CurrentToken.Quoted)
			{
				bool IsTokenImmediatelyAfter = tokens.FirstOrDefault(t => t.StartIndex == CurrentToken.StartIndex + CurrentToken.Content.Length + 2) is not null;

				Offset = IsTokenImmediatelyAfter ? 2 : 3;
			}
			else
			{
				Offset = 1;
			}

			if (CurrentBufferIndex >= CurrentToken.StartIndex && CurrentBufferIndex < CurrentToken.StartIndex + CurrentToken.Content.Length + Offset)
				CurrentToken.Selected = true;

			CurrentTokens.Add(CurrentToken);

			// Add a cursor snap point at the start and end of this token
			SnapPoints.Add(CurrentToken.StartIndex);
			SnapPoints.Add(CurrentToken.StartIndex + CurrentToken.FullLength - 1);
		}

		// Add a cursor snap point at the end of the buffer
		SnapPoints.Add(Buffer.Length);

		// Perform syntax highlighting via user-set rules if enabled
		if (HighlightingEnabled && OnHighlight is not null)
			OnHighlight(CurrentTokens);
	}

	private Task<string> ReadLineTask = null;
	private string ReadLineResult = string.Empty;

	private bool CancelGetResult = false;

	public async Task<string> ReadLineAsync()
	{
		// Only allows one thread to wait at a time for a result
		// All other threads will immediately get an empty string if another thread is waiting
		if (ReadLineTask is not null)
			return string.Empty;

		ReadLineTask = Task.Run(delegate
		{
			// Simply check 20 times a second for a result to come in
			while (ReadLineResult == string.Empty)
			{
                if (CancelGetResult)
                {
                    CancelGetResult = false;
					ReadLineResult = string.Empty;
                    ReadLineTask = null;
                    return null;
                }

                Thread.Sleep(50);
			}

			var temp = ReadLineResult;

			// Reset state
			ReadLineResult = string.Empty;
			ReadLineTask = null;

            return temp;
		});

		return await ReadLineTask;
	}

	public void Clear()
	{
		Buffer.Clear();
		CurrentBufferIndex = 0;
		CurrentTokens.Clear();
		SnapPoints.Clear();
	}

	private Token[] Tokenize(string Buffer)
	{
		TempTokens.Clear();

		foreach (Match m in TokenizerRegex().Matches(Buffer).Cast<Match>())
		{
			if (m.Value.Length == 0)
				continue;

			TempTokens.Add(new()
			{
				StartIndex = m.Index,
				Content = m.Value.Trim('\"'),
				RawContent = m.Value,

				Quoted = m.Value.StartsWith('\"') && m.Value.EndsWith('\"'),

				HighlightForeground = Color24.White,
				HighlightBackground = Color24.Black
			});
		}

		return TempTokens.ToArray();
	}

	private void CursorToStart()
	{
		CurrentBufferIndex = 0;
	}

	private void CursorToEnd()
	{
		CurrentBufferIndex = Buffer.Length;
	}

	private void CursorLeft()
	{
		if (CurrentBufferIndex == 0)
			return;

		CurrentBufferIndex--;
	}

	private void CursorRight()
	{
		if (CurrentBufferIndex == Buffer.Length)
			return;

		CurrentBufferIndex++;
	}

	private void InverseBackspace()
	{
		if (CurrentBufferIndex == Buffer.Length)
			return;

		Buffer.Remove(CurrentBufferIndex, 1);
	}

	private void Backspace(bool Force = false)
	{
		if (!Force)
			if (CurrentBufferIndex == 0)
				return;

		// Remove 1 character starting at the current buffer index
		Buffer.Remove(CurrentBufferIndex - 1, 1);
		CursorLeft();
	}

	private int IncrementInRange(int Value, int LowerBound, int UpperBound)
	{
		if (Value < UpperBound)
			return Value + 1;
		else
			return LowerBound;
	}

	private int DecrementInRange(int Value, int LowerBound, int UpperBound)
	{
		if (Value > LowerBound)
			return Value - 1;
		else
			return UpperBound;
	}
}
