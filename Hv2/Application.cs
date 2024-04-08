using System.Diagnostics;

using Cosmo;

namespace Hv2;

public class Application
{
	private Renderer CosmoRenderer;
	
	public Action<Renderer> RenderAction;
	
	private bool Running = false;
	
	private System.Timers.Timer FrameTimer;
	private int LastFPS;
	public int CurrentFPS { get; private set; }
	
	
	public Application()
	{
		CosmoRenderer = new();

		FrameTimer = new()
		{
			Interval = 1000
		};
		
		FrameTimer.Elapsed += (obj, args) => 
		{
			LastFPS = CurrentFPS;
			CurrentFPS = 0;
		};

		LastFPS = 0;
		CurrentFPS = 0;
	}
	
	/// <summary>
	/// Blocking call. Executes the current application.
	/// </summary>
	public void Run()
	{
		Running = true;
		FrameTimer.Start();
		
		while (Running)
		{
			RenderAction(CosmoRenderer);
			CosmoRenderer.Flush();
			CurrentFPS++;
		}
	}
	
	public void SignalExit()
	{
		Running = false;
		FrameTimer.Stop();
	}
}
