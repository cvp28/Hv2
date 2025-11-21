using System.Text;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using Cosmo;

using Collections.Pooled;

namespace Hv2UI;

public static partial class Hv2
{
	public static int WindowWidth { get; private set; }
	public static int WindowHeight { get; private set; }

	public static Renderer CosmoRenderer;
	public static readonly PooledDictionary<string, Layer> Layers = [];

	private static Widget _FocusedWidget;

	public static Widget FocusedWidget
	{
		get => _FocusedWidget;

		set
		{
			if (_FocusedWidget is not null)
				_FocusedWidget.OnDefocused();

			_FocusedWidget = value;

			if (value is not null)
				_FocusedWidget.OnFocused();
		}
	}

	public static PooledDictionary<ConsoleKey, Action> GlobalKeyActions;

	private static bool Running = false;

	private static System.Timers.Timer FPSIntervalTimer;

	private static int CurrentFPS;
	public static int LastFPS { get; private set; }

    /// <summary>
    /// <para>Sleeps for the minimum timing resolution of the host OS after each mainloop iteration</para>
    /// <para>This should equate to 64 FPS on Windows and 100 FPS on Linux (assuming default CONFIG_HZ of 100)</para>
    /// </summary>
    public static bool FrameRateLimiterEnabled { get; set; }

	private static TimeSpan MainLoopElapsed;			// Measures MainLoop execution time
	private static TimeSpan FrameRateLimiterElapsed;	// Measures time spent sleeping (if FrameRateLimiter is enabled)

	// Public facing API for profiling and instrumentation purposes
	public static double MainLoopElapsedSeconds => MainLoopElapsed.TotalSeconds;
	public static double FrameRateLimiterElapsedSeconds => FrameRateLimiterElapsed.TotalSeconds;

	private static bool Initialized = false;

	public static void Initialize(bool LimitFrameRate = false)
	{
		if (Initialized)
			throw new Exception("Hv2 is already initialized.");

		CosmoRenderer = new();
		GlobalKeyActions = new();

		WindowWidth = Console.WindowWidth;
		WindowHeight = Console.WindowHeight;

		InputBuffer = new();

		Console.OutputEncoding = Encoding.UTF8;

		Hv2Worker = new(Hv2WorkerProc);

		FPSIntervalTimer = new() { Interval = 1000 };
		FPSIntervalTimer.Elapsed += (obj, args) =>
		{
			LastFPS = CurrentFPS;
			CurrentFPS = 0;
			//Console.Title = $"LastFPS: {LastFPS}";
		};

		LastFPS = 0;
		CurrentFPS = 0;

		MainLoopElapsed = TimeSpan.Zero;
		FrameRateLimiterEnabled = LimitFrameRate;

		// Enable VT processing manually on Windows (just in case we are running on an old version)
        if (OperatingSystem.IsWindows())
        {
            IntPtr hOut = k32GetStdHandle(-11);

            k32GetConsoleMode(hOut, out uint mode);
            mode |= 4;
            k32SetConsoleMode(hOut, mode);
        }

        Initialized = true;
	}

	public static (int X, int Y) GetCoordsFromOffsetEx(int X, int Y, int Offset)
	{
		int NewX = (X + Offset) % WindowWidth;
		int OffY = (X + Offset) / WindowWidth;

		return (NewX, Y + OffY);
	}

	private static void ThrowIfNotInitialized()
	{
		if (!Initialized)
			throw new Exception("Hv2 is not initialized. Please call Hv2.Initialize() first.");
	}

	private static Dimensions LastDimensions = new();
	private static Dimensions CurrentDimensions = new();

	private static bool DimensionsHaveChanged = false;

    private static void ResizeRoutine()
    {
        Stopwatch ResizeTimer = new();

        Dimensions LastDimensions = Dimensions.Current;

        ResizeTimer.Restart();

        while (true)
        {
            Console.CursorVisible = false;

            var CurrentDimensions = Dimensions.Current;

            if (DimensionsAreDifferent(CurrentDimensions, LastDimensions))
                ResizeTimer.Restart();

            LastDimensions = CurrentDimensions;

            if (ResizeTimer.ElapsedMilliseconds >= 100)
            {
                ResizeTimer.Reset();
                return;
            }

            Thread.Sleep(10);
        }
    }

    private static void MainLoop()
	{
		while (Running)
		{
			CosmoRenderer.FrameRateLimiterEnabled = FrameRateLimiterEnabled;

			long MainLoopStartTicks = Stopwatch.GetTimestamp();

            if (DimensionsHaveChanged)
            {
				CosmoRenderer.Resize();

                Console.CursorVisible = false;

                // Blocks until dimensions are stable for at least a second
                ResizeRoutine();
            }

            // Do input
            HandleInput();

			// Do rendering
			RenderLayers(Layers);

			// Render current status message if set (label being visible means the message should be set)
			if (lblCurrentStatusMessage.Visible)
				lblCurrentStatusMessage.Draw(CosmoRenderer);

			CurrentFPS++;
			MainLoopElapsed = Stopwatch.GetElapsedTime(MainLoopStartTicks);


			// Frame rate limiter
			var FrameRateLimiterStartTicks = Stopwatch.GetTimestamp();

			if (FrameRateLimiterEnabled) // TODO (Carson): Somehow make this better
            {
				// Hardcoded 60 FPS limit for now
				var sleep_time = TimeSpan.FromSeconds(1.0 / 60) - MainLoopElapsed;

				if (sleep_time.Milliseconds > 0)
				{
					PlatformEnableHighResolutionTiming();
					Thread.Sleep((int)Math.Floor(sleep_time.TotalMilliseconds));
					PlatformDisableHighResolutionTiming();
				}
			}

			FrameRateLimiterElapsed = Stopwatch.GetElapsedTime(FrameRateLimiterStartTicks);
		}
	}

	private static void HandleInput()
	{
		if (!InputBuffer.Any())
			return;

		InputBuffer.TryDequeue(out var cki);

		foreach (var l in Layers)
			if (l.Value.Active && l.Value.KeyActions.TryGetValue(cki.Key, out var LayerKeyAction))
			{
				LayerKeyAction(cki);
				break;
			}

		// Global KeyActions take precedence before focused widgets
		if (GlobalKeyActions.TryGetValue(cki.Key, out var GlobalKeyAction))
		{
			GlobalKeyAction();
		}
		else if (FocusedWidget is not null)
		{
			FocusedWidget.OnInput(cki);
		}
	}

	private static void RenderLayers(IDictionary<string, Layer> Layers)
	{
		// I wish I had a way to go down
		// The humble staircase:

		foreach (var l in Layers)
			if (l.Value.Active)
				foreach (var w in l.Value.Widgets)
					if (w.Visible) w.Draw(CosmoRenderer);

		CosmoRenderer.Flush();
	}

	/// <summary>
	/// Initiates a center-screen status message to display for the specified time
	/// </summary>
	/// <param name="Message">The message content</param>
	/// <param name="Time">The time that this message will be on the screen</param>
	public static void DoStatusMessage(string Message, TimeSpan Time, bool Flashing = true) => 
		PendingStatusMessages.Enqueue(new(Message, Time, Flashing));

	/// <summary>
	/// Initiates a center-screen status message to display for the specified time
	/// </summary>
	/// <param name="Message">The message content</param>
	/// <param name="Time">The time that this message will be on the screen</param>
	/// <param name="Foreground">The message foreground color</param>
	/// <param name="Background">The message background color</param>
	public static void DoStatusMessage(string Message, TimeSpan Time, Color24 Foreground, Color24 Background, bool Flashing = true) =>
		PendingStatusMessages.Enqueue(new(Message, Time, Foreground, Background, Flashing));

	public static void EndCurrentStatusMessage()
	{
		lblCurrentStatusMessage.Visible = false;
		CurrentStatusMessage = null;
	}

	public static void ClearAllStatusMessages(bool IncludingCurrent)
	{
		if (IncludingCurrent) EndCurrentStatusMessage();

		PendingStatusMessages.Clear();
	}

	private static Action PlatformEnableHighResolutionTiming = OperatingSystem.IsWindows() ?
		delegate	// Windows Impl
		{
			winmmTimeBeginPeriod(1);
		}
		:
		delegate	// Unix Impl
		{
            // empty for now :P
        };

	private static Action PlatformDisableHighResolutionTiming = OperatingSystem.IsWindows() ?
        delegate    // Windows Impl
        {
            winmmTimeEndPeriod(1);
        }
		:
        delegate    // Unix Impl
        {
			// empty for now :P
        };

	/// <summary>
	/// Blocking call. Executes the current application.
	/// </summary>
	public static void Run()
	{
		ThrowIfNotInitialized();
		
		Running = true;
		FPSIntervalTimer.Start();

		// Start background worker thread
		Hv2Worker.Start();

		// Start mainloop
        MainLoop();
	}
	
	public static void SignalExit()
	{
		ThrowIfNotInitialized();
		
		Running = false;
		FPSIntervalTimer.Stop();
	}
}