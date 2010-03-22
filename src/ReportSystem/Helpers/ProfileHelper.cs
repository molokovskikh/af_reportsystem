using System;
using System.Diagnostics;

namespace Inforoom.ReportSystem.Helpers
{
	class Operation
	{
		public Operation(string operation)
		{
			startedOn = DateTime.Now;
			OperationName = operation;
		}

		public DateTime startedOn;
		public string OperationName;
	}

	public static class ProfileHelper
	{
		private static Operation currentOperation;
		private static DateTime firstStartedOn;
		public static bool IsProfiling
		{
			get
			{
#if TESTING
				return true;
#else
				return false;
#endif
			}
		}
		public static void Start()
		{
			currentOperation = null;
			firstStartedOn = DateTime.Now;
		}

		public static void Next(string operation)
		{
			if (IsProfiling)
			{
				End();
				currentOperation = new Operation(operation);
				Debug.WriteLine("Started " + operation);
			}
		}

		public static void End()
		{
			if (IsProfiling && currentOperation != null)
			{
				TimeSpan duration = DateTime.Now.Subtract(currentOperation.startedOn);
				Debug.WriteLine(currentOperation.OperationName + " ended after " + duration.TotalMilliseconds + " milliseconds.");
				Debug.WriteLine(String.Empty);
				currentOperation = null;
			}
		}

		public static void Stop()
		{
			if (IsProfiling)
			{
				End();
				TimeSpan duration = DateTime.Now.Subtract(firstStartedOn);
				Debug.WriteLine("End!!! After " + duration.TotalMilliseconds + " milliseconds.");
				Debug.WriteLine(String.Empty);
			}
		}

		public static void WriteLine(string text)
		{
			if (IsProfiling)
				Debug.WriteLine(text);
		}
	}
}