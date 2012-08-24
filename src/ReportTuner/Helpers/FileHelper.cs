using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace ReportTuner.Helpers
{
	public class FileHelper
	{
		public static void CopyStream(Stream input, Stream output)
		{
			byte[] buffer = new byte[8 * 1024];
			int len;
			while ((len = input.Read(buffer, 0, buffer.Length)) > 0) {
				output.Write(buffer, 0, len);
			}
		}
	}
}