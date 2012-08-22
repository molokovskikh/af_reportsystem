using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Common.MySql;
using ExecuteTemplate;
using Inforoom.ReportSystem;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using MySqlHelper = MySql.Data.MySqlClient.MySqlHelper;

namespace ReportSystem.Test
{
	[TestFixture]
	public class ArchFileFixture
	{
		[Test]
		public void TestArchBase()
		{
			var gr = new GeneralReport(true);
			gr.Reports.Add(new FakeReport());
			var file = gr.BuildResultFile();
			Assert.That(Path.GetExtension(file), Is.EqualTo(".xls"));
			gr = new GeneralReport();
			gr.Reports.Add(new FakeReport());
			file = gr.BuildResultFile();
			Assert.That(Path.GetExtension(file), Is.EqualTo(".zip"));
		}

		[Test, Description("Нет асертов, проверяем, что все просто выподняется - без эксепшонов конвертации параметров")]
		public void ReadParam()
		{
			var sqlCommand = @"
insert into Customers.Suppliers (Name) values ('123');
INSERT INTO
	reports.general_reports
(PayerId, Allow, Comment, FirmCode)
select
 s.Payer , false, '123',  Id
from Customers.Suppliers s
;
set @LastReportPropertyId = last_insert_id();
SELECT
 cr.*
FROM reports.general_reports cr
WHERE
cr.generalreportcode = @LastReportPropertyId;";
			DataTable dtGeneralReports;
			var connection = new MySqlConnection(ConnectionHelper.GetConnectionString());
			connection.Open();
			try {
				var transaction = connection.BeginTransaction();
				dtGeneralReports = MySqlHelper.ExecuteDataset(connection, sqlCommand).Tables[0];
				transaction.Commit();
			}
			finally {
				connection.Close();
			}

			foreach (DataRow drReport in dtGeneralReports.Rows) {
				var GeneralReportCode = (ulong)drReport[GeneralReportColumns.GeneralReportCode];
				var FirmCode = Convert.ToUInt32(drReport[GeneralReportColumns.FirmCode]);
				var ContactGroupId = (Convert.IsDBNull(drReport[GeneralReportColumns.ContactGroupId])) ? null : (uint?)Convert.ToUInt32(drReport[GeneralReportColumns.ContactGroupId]);
				var EMailSubject = drReport[GeneralReportColumns.EMailSubject].ToString();
				var ReportFileName = drReport[GeneralReportColumns.ReportFileName].ToString();
				var ReportArchName = drReport[GeneralReportColumns.ReportArchName].ToString();
				var Temporary = Convert.ToBoolean(drReport[GeneralReportColumns.Temporary]);
				var Format = (ReportFormats)Enum.Parse(typeof(ReportFormats), drReport[GeneralReportColumns.Format].ToString());
				var NoArchive = Convert.ToBoolean(drReport[GeneralReportColumns.NoArchive]);
			}
		}
	}
}