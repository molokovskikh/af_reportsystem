using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;
using ExecuteTemplate;

namespace Inforoom.ReportSystem
{
	//Дефектурный отчет
	public class DefReport : BaseReport
	{
		public DefReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn)
			: base(ReportCode, ReportCaption, Conn)
		{
		}

		public override void GenerateReport(ExecuteArgs e)
		{
		}

		public override void ReportToFile(string FileName)
		{ }
	}
}
