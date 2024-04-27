using System.Collections.Concurrent;

namespace Hv2;

public static partial class Hv2
{
	private static Thread Hv2Worker;
	
	private static ConcurrentQueue<ConsoleKeyInfo> InputBuffer;
	
	private static void Hv2WorkerProc()
	{
		while (Running)
		{
			if (Console.KeyAvailable)
				InputBuffer.Enqueue(Console.ReadKey(true));

		}
	}
}
