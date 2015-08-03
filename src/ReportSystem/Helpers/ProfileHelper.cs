using System;
using System.Diagnostics;
using System.Linq;
using Common.Tools;
using log4net;
using MySql.Data.MySqlClient;

namespace Inforoom.ReportSystem.Helpers
{
	internal class Operation
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
		private static ILog log = LogManager.GetLogger(typeof(ProfileHelper));

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
			End();
			if (IsProfiling) {
				currentOperation = new Operation(operation);
				Console.WriteLine("Started " + operation);
			}
			if (log.IsDebugEnabled) {
				currentOperation = new Operation(operation);
				log.Debug("Started " + operation);
			}
		}

		public static void SpendedTime(string operation)
		{
			if (IsProfiling) {
				var duration = DateTime.Now.Subtract(currentOperation.startedOn);
				Console.WriteLine(operation + duration.TotalMilliseconds + " milliseconds.");
				currentOperation = null;
			}
			if (log.IsDebugEnabled) {
				var duration = DateTime.Now.Subtract(currentOperation.startedOn);
				log.Debug(operation + duration.TotalMilliseconds + " milliseconds.");
				currentOperation = null;
			}
		}

		public static void End()
		{
			if (currentOperation == null)
				return;
			if (IsProfiling) {
				var duration = DateTime.Now.Subtract(currentOperation.startedOn);
				Console.WriteLine(currentOperation.OperationName + " ended after " + duration.TotalMilliseconds + " milliseconds.");
				currentOperation = null;
			}
			if (log.IsDebugEnabled) {
				var duration = DateTime.Now.Subtract(currentOperation.startedOn);
				log.Debug(currentOperation.OperationName + " ended after " + duration.TotalMilliseconds + " milliseconds.");
				currentOperation = null;
			}
		}

		public static void Stop()
		{
			End();
			if (IsProfiling) {
				var duration = DateTime.Now.Subtract(firstStartedOn);
				Console.WriteLine("End!!! After " + duration.TotalMilliseconds + " milliseconds.");
			}
			if (log.IsDebugEnabled) {
				var duration = DateTime.Now.Subtract(firstStartedOn);
				log.Debug("End!!! After " + duration.TotalMilliseconds + " milliseconds.");
			}
		}

		public static void WriteLine(MySqlCommand command)
		{
			if (IsProfiling) {
				Console.WriteLine(command.CommandText + ";");
				Console.WriteLine(command.Parameters.Cast<MySqlParameter>().Implode(p => Tuple.Create(p.ParameterName, p.Value)) + ";");
			}
			if (log.IsDebugEnabled) {
				log.Debug(command.CommandText + ";");
				log.Debug(command.Parameters.Cast<MySqlParameter>().Implode(p => Tuple.Create(p.ParameterName, p.Value)) + ";");
			}
		}

		public static void WriteLine(string text)
		{
			if (IsProfiling)
				Console.WriteLine(text + ";");
			if (log.IsDebugEnabled) {
				log.Debug(text);
			}
		}
	}
}