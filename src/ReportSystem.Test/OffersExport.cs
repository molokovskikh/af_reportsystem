using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using Common.Tools;
using Inforoom.ReportSystem;
using Inforoom.ReportSystem.ByOffers;
using Inforoom.ReportSystem.Models.Reports;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace ReportSystem.Test
{
	[TestFixture]
	public class OffersExportFixture : BaseProfileFixture2
	{
		private TestSupplier supplier;

		[SetUp]
		public void Setup()
		{
			FileHelper.InitDir("tmp");
			supplier = TestSupplier.CreateNaked(session);
			supplier.CreateSampleCore(session);
			var client = TestClient.CreateNaked(session);
			Property("UserId", client.Users[0].Id);
		}

		[Test]
		public void Build()
		{
			InitReport<OffersExport>("test", ReportFormats.DBF);
			BuildReport("tmp/test.dbf");
			Assert.IsTrue(File.Exists("tmp/test.dbf"));
			var data = Dbf.Load("tmp/test.dbf");
			Assert.IsTrue(data.Columns.Contains("Code"));
			Assert.IsTrue(data.Columns.Contains("CodeCr"));
			Assert.IsTrue(data.Columns.Contains("PriceDate"));
			Assert.IsTrue(data.Columns.Contains("RlSpplrId"));
			Assert.IsTrue(data.Columns.Contains("EAN13"));
		}

		[Test]
		public void Split_by_price()
		{
			Property("SplitByPrice", true);
			InitReport<OffersExport>("test", ReportFormats.DBF);
			BuildReport("tmp/test.dbf");
			Assert.IsFalse(File.Exists("tmp/test.dbf"));
			var resultFile = $"tmp/{supplier.Id}.dbf";
			Assert.IsTrue(File.Exists(resultFile), "должен быть {0} есть {1}", resultFile, Directory.GetFiles("tmp").Implode());
			var data = Dbf.Load(resultFile);
			Assert.IsTrue(data.Columns.Contains("Code"));
			Assert.IsTrue(data.Columns.Contains("CodeCr"));
			Assert.IsTrue(data.Columns.Contains("PriceDate"));
			Assert.IsTrue(data.Columns.Contains("RlSpplrId"));
		}

		[Test]
		public void Export_info_drugstore()
		{
			Property("SplitByPrice", true);
			InitReport<OffersExport>("test", ReportFormats.InfoDrugstore);
			BuildReport("tmp/test.dbf");
			var filename = $"tmp\\{supplier.Prices[0].Id}_1.xml";
			Assert.IsTrue(File.Exists(filename), $"должен быть файл {filename} есть {Directory.GetFiles("tmp").Implode()}");
			var doc = XDocument.Load(filename);
			Assert.AreEqual(1, doc.XPathSelectElements("PACKET").Count());
		}
	}
}