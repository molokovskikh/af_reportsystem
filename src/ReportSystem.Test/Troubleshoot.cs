using System;
using System.Data;
using System.IO;
using System.Reflection;
using Inforoom.ReportSystem;
using Inforoom.ReportSystem.Model;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using Test.Support;

namespace ReportSystem.Test
{
	[TestFixture, Ignore("Что бы подебажить отчет")]
	public class Troubleshoot : IntegrationFixture
	{
		[SetUp]
		public void Setup()
		{
			if (File.Exists("test.xls"))
				File.Delete("test.xls");
		}

		[Test]
		public void Troubleshoot_general_report()
		{
			uint id = 111;
			var dataAdapter = new MySqlDataAdapter("", "server=testsql.analit.net;user=system;password=newpass;default command timeout=0;");
			dataAdapter.SelectCommand.CommandText = @"
select
  *
from
  reports.Reports r,
  reports.reporttypes rt
where
    r.GeneralReportCode = ?reportcode
and rt.ReportTypeCode = r.ReportTypeCode";
			dataAdapter.SelectCommand.Parameters.AddWithValue("?reportcode", id);
			var res = new DataTable();
			dataAdapter.Fill(res);

			using (var connection = new MySqlConnection("server=testsql.analit.net;user=system;password=newpass;database=usersettings;default command timeout=0;")) {
				connection.Open();
				foreach (DataRow drGReport in res.Rows) {
					if (Convert.ToBoolean(drGReport[BaseReportColumns.colEnabled])) {
						//Создаем отчеты и добавляем их в список отчетов
						var reportcode = (ulong)drGReport[BaseReportColumns.colReportCode];
						Console.WriteLine("Отчет {0}", reportcode);
						var prop = GeneralReport.LoadProperties(connection, reportcode);
						var bs = (BaseReport)Activator.CreateInstance(
							GetReportTypeByName(drGReport[BaseReportColumns.colReportClassName].ToString()),
							new object[] {
								reportcode,
								drGReport[BaseReportColumns.colReportCaption].ToString(), connection,
								ReportFormats.Excel,
								prop
							});
						bs.Session = session;
						bs.Write(Path.GetFullPath("test.xls"));
					}
				}
			}
		}

		private Type GetReportTypeByName(string ReportTypeClassName)
		{
			Type t = Assembly.Load("ReportSystem").GetType(ReportTypeClassName);
			if (t == null)
				throw new ReportException(String.Format("Неизвестный тип отчета : {0}", ReportTypeClassName));
			return t;
		}
	}
}