using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ReportTuner.Models
{
	public class ReportTunerException : Exception
	{
		public ReportTunerException(string message) : base(message) { }
		public ReportTunerException(string message, Exception innerException) : base(message, innerException) { }
	}

	public class ExceptionHandling
	{
	}
}
