﻿using System;
using System.IO;
using Common.Tools;
using Inforoom.ReportSystem;
using Inforoom.ReportSystem.ByOffers;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace ReportSystem.Test
{
	[TestFixture]
	public class OffersExportFixture : BaseProfileFixture2
	{
		[Test]
		public void Build()
		{
			FileHelper.InitDir("tmp");
			var supplier = TestSupplier.CreateNaked(session);
			supplier.CreateSampleCore(session);
			var client = TestClient.CreateNaked(session);

			Property("UserId", client.Users[0].Id);
			InitReport<OffersExport>("test", ReportFormats.DBF);
			BuildReport("tmp/test.dbf");
			Assert.IsTrue(File.Exists("tmp/test.dbf"));
		}
	}
}