using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Inforoom.ReportSystem
{
	public class ReportException : Exception
	{
		public ulong SubreportCode;
		public string ReportCaption;
		public string Payer;

		public ReportException(string message) : base(message)
		{
		}

		public ReportException(string message, Exception ex, ulong subreportCode, string reportCaption, string payer) : base(message, ex)
		{
			SubreportCode = subreportCode;
			ReportCaption = reportCaption;
			Payer = payer;
		}

		public ReportException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}