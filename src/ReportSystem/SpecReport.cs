using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;
using ExecuteTemplate;

namespace Inforoom.ReportSystem
{
	//����������� ����� �����-������
	public class SpecReport : ProviderReport
	{
		public SpecReport(ulong ReportCode, string ReportCaption, MySqlConnection Conn)
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
