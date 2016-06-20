using System;
using System.IO;
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
	public class OffersExportFixture : ReportFixture
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
			TryInitReport<OffersExport>("test", ReportFormats.DBF);
			report.ReportCaption = "test";
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
			TryInitReport<OffersExport>("test", ReportFormats.DBF);
			report.ReportCaption = "test";
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
	}
}