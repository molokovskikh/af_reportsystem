using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.ReportSystem;
using System.Data;
using System.IO;
using ReportSystem.Test.Properties;
using ReportSystem.Profiling;

namespace ReportSystem.Test
{
	public enum ReportsTypes
	{
		MixedProductName,
		MixedName,
		MixedFullName,
		MixedFull,
		SuppliersRating,
		SpecialProducer,
		SpecialCountProducer,
		SpecialCount,
		Special,
		RatingNotJunkOnly,
		RatingJunkOnly,
		Rating,
		RatingFull,
		MinCost,
		MinCostProducer,
		MinCostCountAndProducer,
		MinCostCount,
		MinCostManyClients,
		MinCostByPriceProducer,
		MinCostByPriceCountProducer,
		MinCostByPriceCount,
		MinCostByPrice,
		DefectureProductsWithProducer,
		DefectureProductsOnly,
		DefectureNameOnly,
		DefectureNameAndFormWithProducer,
		DefectureNameAndForm,
		CombineProducer,
		CombineCountAndProducer,
		CombineCount,
		Combine,
		Individual
	}

	public static class TestHelper
	{
		public static System.Data.DataSet LoadProperties(ReportsTypes type)
		{
			DataSet result = new DataSet();
			result.ReadXml("TestData\\" + type.ToString() + ".xml");
			return result;
		}

		public static string EnsureDeletion(ReportsTypes type)
		{
			string fileName = Path.Combine(Settings.Default.ExcelDir, type.ToString() + ".xls");
			if (File.Exists(fileName))
				File.Delete(fileName);
			return fileName;
		}

		public static void ProcessReport(BaseReport report, ReportsTypes type)
		{
			ProfileHelper.Start();
			report.ProcessReport();
			report.ReportToFile(TestHelper.EnsureDeletion(type));
			ProfileHelper.Stop();
		}
	}	
}
