using System;
using System.Data;
using System.Diagnostics;
using Inforoom.ReportSystem.Model;
using NUnit.Framework;
using ReportSystem.Test.Helpers;

namespace ReportSystem.Test.ProviderReport
{
	[TestFixture]
	public class OfferFixture
	{
		private DataTable _dataTable;
		private Random _random;
		private DataRow _row;
		private DataRowAdapter _rowAdapter;

		[TestFixtureSetUp]
		public void FixtureSetUp()
		{
			_random = new Random();

			_dataTable = new DataTable();
			_dataTable.Columns.Add("CatalogId", typeof(uint));
			_dataTable.Columns.Add("ProductId", typeof(uint));
			_dataTable.Columns.Add("ProducerId", typeof(uint));
			_dataTable.Columns.Add("ProductName", typeof(string));
			_dataTable.Columns.Add("ProducerName", typeof(string));

			_dataTable.Columns.Add("CoreId", typeof(ulong));
			_dataTable.Columns.Add("Code", typeof(string));
			_dataTable.Columns.Add("SupplierId", typeof(uint));
			_dataTable.Columns.Add("PriceId", typeof(uint));
			_dataTable.Columns.Add("RegionId", typeof(ulong));
			_dataTable.Columns.Add("Quantity", typeof(string));
			_dataTable.Columns.Add("Cost", typeof(float));

			_dataTable.Columns.Add("AssortmentCoreId", typeof(ulong));
			_dataTable.Columns.Add("AssortmentCode", typeof(string));
			_dataTable.Columns.Add("AssortmentCodeCr", typeof(string));
			_dataTable.Columns.Add("AssortmentSupplierId", typeof(uint));
			_dataTable.Columns.Add("AssortmentPriceId", typeof(uint));
			_dataTable.Columns.Add("AssortmentRegionId", typeof(ulong));
			_dataTable.Columns.Add("AssortmentQuantity", typeof(string));
			_dataTable.Columns.Add("AssortmentCost", typeof(float));
		}

		[SetUp]
		public void TestSetUp()
		{
			_row = _dataTable.NewRow();
			_rowAdapter = new DataRowAdapter(_row);

			_row["CatalogId"] = 3u;

			_row["ProductId"] = 4u;
			_row["ProducerId"] = 5u;
			_row["ProductName"] = "test product";
			_row["ProducerName"] = "test producer";

			_row["CoreId"] = 6ul;
			_row["Code"] = "test code";
			_row["SupplierId"] = 7u;
			_row["PriceId"] = 8u;
			_row["RegionId"] = 9ul;
			_row["Quantity"] = "4";
			_row["Cost"] = Convert.ToSingle(_random.NextDouble() * _random.Next(30));

			_row["AssortmentCoreId"] = 10ul;
			_row["AssortmentCode"] = "test code";
			_row["AssortmentSupplierId"] = 11u;
			_row["AssortmentPriceId"] = 12u;
			_row["AssortmentRegionId"] = 14ul;
			_row["AssortmentQuantity"] = "7";
			_row["AssortmentCost"] = Convert.ToSingle(_random.NextDouble() * _random.Next(30));
		}

		[Test]
		public void ArgumentNullExceptionOnRow()
		{
			Assert.That(
				() => new Offer(null, null, null),
				Throws.InstanceOf<ArgumentNullException>()
					.And.Property("ParamName").EqualTo("row"));
		}

		[Test]
		public void ArgumentNullExceptionOnRandom()
		{
			Assert.That(
				() => new Offer(new DataRowAdapter(_dataTable.NewRow()), 1, null),
				Throws.InstanceOf<ArgumentNullException>()
					.And.Property("ParamName").EqualTo("random")
					.And.Message.StartsWith("При установленном параметре noiseSupplierId не установлен параметр random: генератор случайных чисел"));
		}

		public void CheckRequiredFields(DataRow row, Offer offer)
		{
			Assert.That(offer.CatalogId, Is.EqualTo(row["CatalogId"]));
			Assert.That(offer.ProductId, Is.EqualTo(row["ProductId"]));
			Assert.That(offer.ProducerId, Is.EqualTo(row["ProducerId"]));
			Assert.That(offer.ProductName, Is.EqualTo(row["ProductName"]));
			Assert.That(offer.ProducerName, Is.EqualTo(row["ProducerName"]));

			Assert.That(offer.CoreId, Is.EqualTo(row["CoreId"]));
			Assert.That(offer.Code, Is.EqualTo(row["Code"]));
			Assert.That(offer.SupplierId, Is.EqualTo(row["SupplierId"]));
			Assert.That(offer.PriceId, Is.EqualTo(row["PriceId"]));
			Assert.That(offer.RegionId, Is.EqualTo(row["RegionId"]));
			Assert.That(offer.Quantity, Is.EqualTo(row["Quantity"]));
			Assert.That(offer.RealCost, Is.EqualTo(row["Cost"]));
		}

		public void CheckOptionalFields(DataRow row, Offer offer)
		{
			Assert.That(offer.AssortmentCoreId, Is.EqualTo(row["AssortmentCoreId"]));
			Assert.That(offer.AssortmentCode, Is.EqualTo(row["AssortmentCode"]));
			Assert.That(offer.AssortmentSupplierId, Is.EqualTo(row["AssortmentSupplierId"]));
			Assert.That(offer.AssortmentPriceId, Is.EqualTo(row["AssortmentPriceId"]));
			Assert.That(offer.AssortmentRegionId, Is.EqualTo(row["AssortmentRegionId"]));
			Assert.That(offer.AssortmentQuantity, Is.EqualTo(row["AssortmentQuantity"]));
			Assert.That(offer.AssortmentRealCost, Is.EqualTo(row["AssortmentCost"]));
		}

		[Test]
		public void LoadFull()
		{
			var offer = new Offer(_rowAdapter, null, null);

			CheckRequiredFields(_row, offer);

			Assert.That(offer.Cost, Is.EqualTo(offer.RealCost));

			CheckOptionalFields(_row, offer);

			Assert.That(offer.AssortmentCost, Is.EqualTo(offer.AssortmentRealCost));
		}

		[Test]
		public void LoadWithoutAssortment()
		{
			_row["AssortmentCoreId"] = DBNull.Value;

			var offer = new Offer(_rowAdapter, null, null);

			CheckRequiredFields(_row, offer);

			Assert.That(offer.Cost, Is.EqualTo(offer.RealCost));

			Assert.That(offer.AssortmentCoreId, Is.Null);
			Assert.That(offer.AssortmentSupplierId, Is.Null);
			Assert.That(offer.AssortmentPriceId, Is.Null);
			Assert.That(offer.AssortmentRegionId, Is.Null);
			Assert.That(offer.AssortmentRealCost, Is.Null);
			Assert.That(offer.AssortmentCost, Is.Null);
		}

		[Test]
		public void LoadWithoutAssortmentCost()
		{
			_row["AssortmentCost"] = DBNull.Value;

			var offer = new Offer(_rowAdapter, null, null);

			CheckRequiredFields(_row, offer);

			Assert.That(offer.Cost, Is.EqualTo(offer.RealCost));

			Assert.That(offer.AssortmentCoreId, Is.Not.Null);
			Assert.That(offer.AssortmentSupplierId, Is.Not.Null);
			Assert.That(offer.AssortmentPriceId, Is.Not.Null);
			Assert.That(offer.AssortmentRegionId, Is.Not.Null);
			Assert.That(offer.AssortmentRealCost, Is.Null);
			Assert.That(offer.AssortmentCost, Is.Null);
		}

		[Test]
		public void LoadWithNoise()
		{
			var offer = new Offer(_rowAdapter, Convert.ToUInt32(_row["SupplierId"]), _random);

			CheckRequiredFields(_row, offer);

			Assert.That(offer.Cost, Is.EqualTo(offer.RealCost));

			CheckOptionalFields(_row, offer);

			Assert.That(offer.AssortmentCost, Is.Not.EqualTo(offer.AssortmentRealCost));
		}

		[Test]
		public void LoadWithNoiseAssortment()
		{
			var offer = new Offer(_rowAdapter, Convert.ToUInt32(_row["AssortmentSupplierId"]), _random);

			CheckRequiredFields(_row, offer);

			Assert.That(offer.Cost, Is.Not.EqualTo(offer.RealCost));

			CheckOptionalFields(_row, offer);

			Assert.That(offer.AssortmentCost, Is.EqualTo(offer.AssortmentRealCost));
		}
	}
}