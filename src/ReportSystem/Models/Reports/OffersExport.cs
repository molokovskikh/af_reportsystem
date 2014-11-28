using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Common.Models;
using Common.Models.Helpers;
using Common.MySql;
using Common.Tools;
using MySql.Data.MySqlClient;

namespace Inforoom.ReportSystem.Models.Reports
{
	[Description("Экспорт предложений")]
	public class OffersExport : BaseReport
	{
		private DataTable data;

		public OffersExport(ulong reportCode, string reportCaption, MySqlConnection connection, ReportFormats format, DataSet dsProperties)
			: base(reportCode, reportCaption, connection, format, dsProperties)
		{
			Init();
		}

		public OffersExport()
		{
			Init();
		}

		[Description("Пользователь")]
		public uint UserId { get; set; }

		private void Init()
		{
			DbfSupported = true;
		}

		protected override void GenerateReport(ExecuteArgs e)
		{
			Connection.Execute("call Customers.GetOffers(?userId);", new { userId = UserId });
			data = Connection.Fill(@"
select
	c0.Id,
	c0.PriceCode SupplierId,
	sup.Name Supplier,
	DATE_FORMAT(ap.PriceDate, '%m/%d/%Y %H:%i:%s') as PriceDate,
	s.Synonym Name,
	scr.Synonym Producer,
	c.Cost,
	c0.Code,
	c0.CodeCr,
	c0.Exp,
	c0.EAN13,
	c0.Quantity,
	c0.Volume,
	c0.MinOrderCount as MinOrdCnt,
	c0.RequestRatio as RequestRt,
	c0.Junk
from UserSettings.Core c
	join Usersettings.ActivePrices ap on ap.PriceCode = c.PriceCode and ap.RegionCode = c.RegionCode
	join Farm.Core0 c0 on c.Id = c0.Id
	join Farm.Synonym S on s.SynonymCode = c0.SynonymCode
	left join Farm.SynonymFirmCr scr on scr.SynonymFirmCrCode = c0.SynonymFirmCrCode
	join Usersettings.PricesData pd on pd.PriceCode=c0.PriceCode
	join Customers.Suppliers sup on sup.Id = pd.FirmCode;");
		}

		protected override void DataTableToDbf(DataTable dtExport, string fileName)
		{
			using (var writer = new StreamWriter(fileName, false, Encoding.GetEncoding(866)))
				Dbf2.SaveAsDbf4(dtExport, writer);
		}

		public override DataTable GetReportTable()
		{
			return data;
		}
	}
}