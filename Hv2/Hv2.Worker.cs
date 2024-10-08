﻿using Cosmo;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace Hv2UI;

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

            HandleStatusMessages();

			Console.CursorVisible = false;

			WindowWidth = Console.WindowWidth;
			WindowHeight = Console.WindowHeight;

            UpdateDimensions(ref CurrentDimensions);
            DimensionsHaveChanged = DimensionsAreDifferent(CurrentDimensions, LastDimensions);
            LastDimensions = CurrentDimensions;

            Thread.Sleep(10);
		}
	}

    private static bool DimensionsAreDifferent(Dimensions d, Dimensions d2)
    {
        bool WindowWidthChanged = d.WindowWidth != d2.WindowWidth;
        bool WindowHeightChanged = d.WindowHeight != d2.WindowHeight;
        bool BufferWidthChanged = d.BufferWidth != d2.BufferWidth;
        bool BufferHeightChanged = d.BufferHeight != d2.BufferHeight;

        return WindowWidthChanged || WindowHeightChanged || BufferWidthChanged || BufferHeightChanged;
    }

    private static void UpdateDimensions(ref Dimensions d)
    {
        d.WindowWidth = Console.WindowWidth;
        d.WindowHeight = Console.WindowHeight;
        d.BufferWidth = Console.BufferWidth;
        d.BufferHeight = Console.BufferHeight;
    }

    private static ConcurrentQueue<StatusMessage> PendingStatusMessages = [];

    private static StatusMessage CurrentStatusMessage;
    private static long CurrentStatusMessageStartingTimestamp;
    private static Label lblCurrentStatusMessage = new(0, 0, string.Empty) { Visible = false };     // We just use a label internally to display status messages

    public static TimeSpan StatusMessageFlashInterval = TimeSpan.FromSeconds(0.5);

    private static long StatusMessageLastFlash;
    private static bool FlashToggle = false;

    private static void HandleStatusMessages()
    {
        if (CurrentStatusMessage is null)
        {
            if (!PendingStatusMessages.Any()) return;

            PendingStatusMessages.TryDequeue(out CurrentStatusMessage);

            lblCurrentStatusMessage.Text = CurrentStatusMessage.Message;
            lblCurrentStatusMessage.Foreground = CurrentStatusMessage.Foreground;
            lblCurrentStatusMessage.Background = CurrentStatusMessage.Background;

            lblCurrentStatusMessage.X = (WindowWidth / 2) - (CurrentStatusMessage.Message.Length / 2);
            lblCurrentStatusMessage.Y = WindowHeight / 2;

            lblCurrentStatusMessage.Visible = true;

            CurrentStatusMessageStartingTimestamp = Stopwatch.GetTimestamp();
            StatusMessageLastFlash = CurrentStatusMessageStartingTimestamp;
        }
        else
        {
            var StatusMessageElapsed = Stopwatch.GetElapsedTime(CurrentStatusMessageStartingTimestamp);

            if (CurrentStatusMessage.Flashing)
            {
                var LastFlashElapsed = Stopwatch.GetElapsedTime(StatusMessageLastFlash);

                if (LastFlashElapsed >= StatusMessageFlashInterval)
                {
                    FlashToggle = !FlashToggle;
                    StatusMessageLastFlash = Stopwatch.GetTimestamp();
                }

                lblCurrentStatusMessage.Foreground = FlashToggle ? Color24.White : CurrentStatusMessage.Foreground;
            }

            if (StatusMessageElapsed >= CurrentStatusMessage.Time) EndCurrentStatusMessage();
        }
    }
}
