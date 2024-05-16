using System.Diagnostics.CodeAnalysis;
using System.Text;

using Collections.Pooled;

using Cosmo;

namespace Hv2UI;

public static partial class Hv2
{
	public static int WindowWidth { get; private set; }
	public static int WindowHeight { get; private set; }

	private static Renderer CosmoRenderer;
	private static PooledList<Layer> LayerStack;

	public static Widget FocusedWidget;

	public static PooledDictionary<ConsoleKey, Action> GlobalKeyActions;

	private static bool Running = false;
	
	private static System.Timers.Timer FPSIntervalTimer;
	public  static int LastFPS { get; private set; }
	private static int CurrentFPS;
	
	private static bool Initialized = false;
	
	public static void Initialize()
	{
		if (Initialized)
			throw new Exception("Hv2 is already initialized.");
		
		CosmoRenderer = new();
		LayerStack = new();
		GlobalKeyActions = new();

		InputBuffer = new();

		Console.OutputEncoding = Encoding.UTF8;

		Hv2Worker = new(Hv2WorkerProc);

		FPSIntervalTimer = new() { Interval = 1000 };
		FPSIntervalTimer.Elapsed += (obj, args) => 
		{
			LastFPS = CurrentFPS;
			CurrentFPS = 0;
			Console.Title = $"LastFPS: {LastFPS}";
		};
		
		LastFPS = 0;
		CurrentFPS = 0;

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
	
	private static void MainLoop()
	{
		while (Running)
		{
			// Do input
			HandleInput();
			
			// Do rendering
			RenderLayers(LayerStack);

			CurrentFPS++;
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
				w.Draw(CosmoRenderer);

		CosmoRenderer.Flush();
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
	public static void AddLayerFront<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>() where T : Layer
	{
		var l = Activator.CreateInstance<T>();
		l.OnAttachInternal();
		l.OnShow();
		
		LayerStack.Add(l);
	}
	
	public static void AddLayerBack<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>() where T : Layer
	{
		var l = Activator.CreateInstance<T>();
		l.OnAttachInternal();
		l.OnShow();
		
		LayerStack.Insert(0, l);
	}
	
	public static void RemoveLayerFront()
	{
		if (!LayerStack.Any()) return;

		LayerStack[^1].OnHide();
		LayerStack.RemoveAt(LayerStack.Count - 1);
	}
	
	public static void RemoveLayerBack()
	{
		if (!LayerStack.Any()) return;

		LayerStack[0].OnHide();
		LayerStack.RemoveAt(0);
	}
	
	public static Maybe<T> GetLayerByType<T>() where T : Layer
	{
		var temp = LayerStack.FirstOrDefault(l => l.GetType() == typeof(T));
		
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
