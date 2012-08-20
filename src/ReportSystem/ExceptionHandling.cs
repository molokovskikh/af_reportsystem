using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Inforoom.ReportSystem
{
	public class ReportException : Exception
	{
		public ReportException(string message) : base(message)
		{
		}

		public ReportException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}

	public class ExceptionHandling
	{
	}
}