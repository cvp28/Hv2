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
	private static PooledList<Layer> LayerStack;

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

	private static SleepState MainLoopSleepState;

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
		LayerStack = new();
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

		MainLoopSleepState = new();
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
                //ResizeRoutine();
            }

            // Do input
            HandleInput();

			// Do rendering
			RenderLayers(LayerStack);

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

		// Global KeyActions take precedence before focused widgets

		if (GlobalKeyActions.TryGetValue(cki.Key, out var Action))
		{
			Action();
		}
		else if (FocusedWidget is not null)
		{
			FocusedWidget.OnInput(cki);
		}
	}

	private static void RenderLayers(IEnumerable<Layer> Layers)
	{
		foreach (var l in Layers)
			foreach (var w in l.Widgets)
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

	private static void Sleep(TimeSpan Time, ref SleepState s)
	{
		double RemainingSeconds = Time.TotalSeconds;
        TimeSpan observed;
        int powersaving_count = 0;

        // Power saving section
        var powersaving_time_start = Stopwatch.GetTimestamp();
        PlatformEnableHighResolutionTiming();

        do
        {
            var start = Stopwatch.GetTimestamp();
            Thread.Sleep(1);
            observed = Stopwatch.GetElapsedTime(start);

            RemainingSeconds -= observed.TotalSeconds;

            s.estimate = UpdateEstimate(observed, ref s);

            powersaving_count++;
        }
        while (RemainingSeconds > s.estimate);

        PlatformDisableHighResolutionTiming();
        var powersaving_time = Stopwatch.GetElapsedTime(powersaving_time_start);
		PowerSavingSleep = powersaving_time;

        // Spin lock section
        var spinlock_time_start = Stopwatch.GetTimestamp();

        int spinlock_count = 0;
        var spin_lock_start = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(spin_lock_start).TotalSeconds < RemainingSeconds) spinlock_count++;

        var spinlock_time = Stopwatch.GetElapsedTime(spinlock_time_start);
		SpinLockSleep = spinlock_time;

        var total_sleep_time = powersaving_time.TotalSeconds + spinlock_time.TotalSeconds;
    }

	public static TimeSpan PowerSavingSleep;
	public static TimeSpan SpinLockSleep;

    // local helper function (thank you https://blog.bearcats.nl/accurate-sleep-function/)
    private static double UpdateEstimate(TimeSpan observed, ref SleepState s)
    {
        double delta = observed.TotalSeconds - s.mean;
        s.count++;
        s.mean += delta / s.count;
        s.m2 += delta * (observed.TotalSeconds - s.mean);
        double stddev = Math.Sqrt(s.m2 / (s.count - 1));
        return s.mean + stddev;
    }
	
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
	
	#region Layer Controls
	public static void AddLayerFront<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(params object[] Args) where T : Layer
    {
        ThrowIfNotInitialized();

        Layer l = Args.Length == 0 ?
            Activator.CreateInstance<T>()
            :
            Activator.CreateInstance(typeof(T), Args) as T;
		
        l.OnAttachInternal();
		l.OnShow();
		
		LayerStack.Add(l);
	}
	
	public static void AddLayerBack<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(params object[] Args) where T : Layer
    {
        ThrowIfNotInitialized();

		Layer l = Args.Length == 0 ? 
			Activator.CreateInstance<T>()
			: 
			Activator.CreateInstance(typeof(T), Args) as T;

		l.OnAttachInternal();
		l.OnShow();
		
		LayerStack.Insert(0, l);
	}
	
	public static void RemoveLayerFront()
    {
        ThrowIfNotInitialized();

        if (!LayerStack.Any()) return;

		LayerStack[^1].OnHide();
		LayerStack.RemoveAt(LayerStack.Count - 1);
	}
	
	public static void RemoveLayerBack()
    {
        ThrowIfNotInitialized();

        if (!LayerStack.Any()) return;

		LayerStack[0].OnHide();
		LayerStack.RemoveAt(0);
	}

	public static bool RemoveLayerByType<T>() where T : Layer
	{
		ThrowIfNotInitialized();

		var layer = GetLayerByType<T>();

		return LayerStack.Remove(layer);
	}

	public static bool ReplaceLayerByType<T, U>(params object[] Args) where T : Layer where U : Layer
    {
        ThrowIfNotInitialized();

		var layer = GetLayerByType<T>();

		if (!layer) return false;

		var index = LayerStack.IndexOf(layer);

        Layer new_layer = Args.Length == 0 ?
            Activator.CreateInstance<U>()
            :
            Activator.CreateInstance(typeof(U), Args) as U;

        new_layer.OnAttachInternal();
        new_layer.OnShow();

		LayerStack.Insert(index, new_layer);		// Insert new layer
		LayerStack.RemoveAt(index + 1);				// Remove old layer (which will have been bumped up by one index at this point)

		return true;
	}

	public static Maybe<T> GetLayerByType<T>() where T : Layer
    {
        ThrowIfNotInitialized();

        var temp = LayerStack.FirstOrDefault(l => l.GetType().IsAssignableTo(typeof(T)));
		
		if (temp is null)
			return Maybe<T>.Fail();
		
		return Maybe<T>.Success(temp as T);
	}
	
	#endregion
	
	public static void SignalExit()
	{
		ThrowIfNotInitialized();
		
		Running = false;
		FPSIntervalTimer.Stop();
	}
}

struct SleepState
{
    public double estimate = 0.001;

    public double mean = 0.001;

    public double m2 = 0;

    public long count = 1;

    public SleepState()
    { }

    public static SleepState Default = new();
}