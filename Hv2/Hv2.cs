using System.Diagnostics.CodeAnalysis;
using Collections.Pooled;

using Cosmo;

namespace Hv2;

public static partial class Hv2
{
	private static Renderer CosmoRenderer;
	private static PooledList<Layer> LayerStack;

	private static PooledSet<int> LastFrameHashes;
	private static PooledSet<int> NextFrameHashes;

	public static Widget FocusedWidget;

	public static PooledDictionary<ConsoleKeyInfo, Action> GlobalKeyActions;

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

		LastFrameHashes = new(ClearMode.Never);
		NextFrameHashes = new(ClearMode.Never);

		Hv2Worker = new(Hv2WorkerProc);

		FPSIntervalTimer = new() { Interval = 1000 };
		FPSIntervalTimer.Elapsed += (obj, args) => 
		{
			LastFPS = CurrentFPS;
			CurrentFPS = 0;
		};
		
		LastFPS = 0;
		CurrentFPS = 0;
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
		}
	}
	
	private static void HandleInput()
	{
		if (!InputBuffer.Any())
			return;

		InputBuffer.TryDequeue(out var cki);

		FocusedWidget.OnInput(cki);
	}
	
	private static void RenderLayers(IEnumerable<Layer> Layers)
	{
		foreach (var l in Layers)
		{
			foreach (var w in l.Widgets)
			{
				var VisHash = w.ComputeVisHash();

				if (VisHash == 0)
				{
					w.Draw(CosmoRenderer);
					continue;
				}

				if (!LastFrameHashes.Contains(VisHash))
					w.Draw(CosmoRenderer);

				NextFrameHashes.Add(VisHash);
			}
		}

		// Shift hashes back
		LastFrameHashes.Clear();
		foreach (var h in NextFrameHashes) LastFrameHashes.Add(h);

		NextFrameHashes.Clear();
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
		
		LayerStack.Add(l);
	}
	
	public static void AddLayerBack<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>() where T : Layer
	{
		var l = Activator.CreateInstance<T>();
		l.OnAttachInternal();
		
		LayerStack.Insert(0, l);
	}
	
	public static void RemoveLayerFront()
	{
		if (!LayerStack.Any()) return;
		LayerStack.RemoveAt(LayerStack.Count - 1);
	}
	
	public static void RemoveLayerBack()
	{
		if (!LayerStack.Any()) return;
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
