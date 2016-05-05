using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Common.Models;
using Common.Models.Helpers;
using Common.Models.Repositories;
using Common.MySql;
using Common.NHibernate;
using Common.Tools;
using Inforoom.ReportSystem.Helpers;
using MySql.Data.MySqlClient;
using NHibernate.Linq;

namespace Inforoom.ReportSystem.Models.Reports
{
	[Description("Экспорт предложений")]
	public class OffersExport : BaseReport
	{
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

		[Description("Разбивать предложения поставщиков по файлам, работает при экспорте в формате dbf и ИнфоАптека," +
			" для dbf файлы будут называться <код поставщика>.dbf, для ИнфоАптека <код прайса>_<код региона>.xml")]
		public bool SplitByPrice { get; set; }

		private void Init()
		{
			DbfSupported = true;
		}

		protected override void GenerateReport()
		{
			if (Format == ReportFormats.InfoDrugstore)
				return;
			Connection.Execute(@"
drop temporary table if exists activeprices;
call Customers.GetOffers(?userId);", new { userId = UserId });
			var data = Connection.Fill(@"
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
	c0.Junk,
	sup.Id as RlSpplrId,
	c0.Note,
	c0.Period,
	c0.Doc,
	c0.VitallyImportant as VitalImp,
	c0.RegistryCost as RegCost,
	c0.OrderCost as MinOrdSum,
	c0.ProducerCost as ProdCost,
	c0.Nds,
	c0.CodeOKP,
	c0.Series
from UserSettings.Core c
	join Usersettings.ActivePrices ap on ap.PriceCode = c.PriceCode and ap.RegionCode = c.RegionCode
	join Farm.Core0 c0 on c.Id = c0.Id
	join Farm.Synonym S on s.SynonymCode = c0.SynonymCode
	left join Farm.SynonymFirmCr scr on scr.SynonymFirmCrCode = c0.SynonymFirmCrCode
	join Usersettings.PricesData pd on pd.PriceCode=c0.PriceCode
	join Customers.Suppliers sup on sup.Id = pd.FirmCode;

drop temporary table if exists activeprices;");
			data.TableName = "Results";
			_dsReport.Tables.Add(data);
		}

		protected override void DataTableToDbf(DataTable dtExport, string fileName)
		{
			if (SplitByPrice) {
				var groups = GetReportTable().AsEnumerable().GroupBy(r => r["RlSpplrId"]);
				foreach (var price in groups) {
					var table = price.CopyToDataTable();
					var filename = Path.Combine(Path.GetDirectoryName(fileName), price.Key + ".dbf");
					using (var writer = new StreamWriter(filename, false, Encoding.GetEncoding(866)))
						Dbf2.SaveAsDbf4(table, writer);
				}
			}
			else {
				using (var writer = new StreamWriter(fileName, false, Encoding.GetEncoding(866)))
					Dbf2.SaveAsDbf4(dtExport, writer);
			}
		}

		public override void WriteInfoDrugstore(string filename)
		{
			var offers = QueryOffers();
			var settings = new XmlWriterSettings { Encoding = Encoding.GetEncoding(1251) };
			if (SplitByPrice) {
				foreach (var group in offers.GroupBy(o => o.PriceList)) {
						var activePrice = @group.Key;
						var file = Path.Combine(Path.GetDirectoryName(filename),
							$"{activePrice.Id.Price.PriceCode}_{activePrice.Id.RegionCode}.xml");
						using (var writer = XmlWriter.Create(file, settings)) {
							writer.WriteStartDocument(true);
							ExportPrice(writer, activePrice, group);
						}
				}
			} else {
				using (var writer = XmlWriter.Create(filename, settings)) {
					writer.WriteStartDocument(true);
					offers.GroupBy(o => o.PriceList).Each(x => ExportPrice(writer, x.Key, x));
				}
			}
		}

		private IList<NamedOffer> QueryOffers()
		{
			var query = new OfferQuery();
			query.SelectSynonyms();

			using(StorageProcedures.GetActivePrices((MySqlConnection)Session.Connection, UserId)) {
				var sql = query.ToSql()
					.Replace(" as {Offer.Id.CoreId}", " as CoreId")
					.Replace(" as {Offer.Id.RegionCode}", " as RegionId")
					.Replace("{Offer.", "")
					.Replace("}", "");
				var offers = Session.CreateSQLQuery(sql)
					.SetResultTransformer(new AliasToPropertyTransformer(typeof(NamedOffer)))
					.List<NamedOffer>();
				var activePrices = Session.Query<ActivePrice>().Where(p => p.Id.Price.PriceCode > 0).ToList();
				offers.Each(offer => offer.PriceList = activePrices.First(price => price.Id.Price.PriceCode == offer.PriceCode && price.Id.RegionCode == offer.Id.RegionCode));
				return offers;
			}
		}

		public static void ExportPrice(XmlWriter writer, ActivePrice activePrice, IEnumerable<NamedOffer> offers)
		{
			var supplier = activePrice.Id.Price.Supplier;
			writer.WriteStartElement("PACKET");
			writer.WriteAttributeString("NAME", "Прайс-лист");
			writer.WriteAttributeString("FROM", supplier.Name);
			writer.WriteAttributeString("TYPE", "10");

			writer.WriteStartElement("PRICELIST");
			writer.WriteAttributeString("DATE", activePrice.PriceDate.ToString("dd.MM.yyyy HH:mm"));
			writer.WriteAttributeString("NAME", $"{supplier.Name} {activePrice.Id.Price.PriceName}");

			foreach (var offer in offers)
				ExportOffer(writer, offer);

			writer.WriteEndElement();

			writer.WriteEndElement();
		}

		private static void ExportOffer(XmlWriter writer, NamedOffer offer)
		{
			writer.WriteStartElement("ITEM");

			writer.Element("ACODE", offer.ProductId);
			writer.Element("ACODECR", offer.CodeFirmCr);
			writer.Element("CODE", String.Concat(offer.Code, offer.CodeCr));
			writer.Element("NAME", offer.ProductSynonym);
			writer.Element("VENDOR", offer.ProducerSynonym);
			writer.Element("VENDORBARCODE", offer.EAN13.Slice(12));
			writer.Element("QTTY", offer.Quantity);
			writer.Element("VALID_DATE", offer.NormalizedPeriod);
			writer.Element("ISBAD", offer.Junk ? 1 : 0);
			writer.Element("COMMENT", offer.Note);
			writer.Element("XCODE", offer.Id.CoreId);
			writer.Element("MINQTTY", offer.MinOrderCount);
			writer.Element("MINSUM", offer.OrderCost);
			writer.Element("PACKQTTY", offer.RequestRatio);

			writer.WriteStartElement("PRICES");
			writer.Element("Базовая", offer.Cost);
			writer.WriteEndElement();

			writer.WriteEndElement();
		}
	}

	public static class XmlWriterExtentions
	{
		public static void Element(this XmlWriter writer, string name, object value)
		{
			value = NullableHelper.GetNullableValue(value);
			if (value == null
				|| value.Equals(String.Empty))
				return;

			if (value is bool) {
				value = (bool)value ? 1 : 0;
			}

			writer.WriteElementString(name, value.ToString());
		}
	}

	public class NamedOffer : Offer
	{
		public NamedOffer()
		{
			Id = new OfferKey();
		}

		public string ProductSynonym { get; set; }
		public string ProducerSynonym { get; set; }

		public string NormalizedPeriod
		{
			get
			{
				DateTime date;
				if (DateTime.TryParse(Period, out date))
					return date.ToShortDateString();
				return null;
			}
		}

		public ulong CoreId
		{
			get { return Id.CoreId; }
			set { Id.CoreId = value; }
		}

		public ulong RegionId
		{
			get { return Id.RegionCode; }
			set { Id.RegionCode = value; }
		}
	}
}