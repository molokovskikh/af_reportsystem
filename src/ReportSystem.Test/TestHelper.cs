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
		Contacts,
		MixedProductName,
		MixedName,
		MixedFullName,
		MixedFull,
		MixedNew,
		MixedNewDifficult,
		SuppliersRating,
		SuppliersRatingNew,
		SuppliersRatingNewDifficult,
		SpecialProducer,
		SpecialCountProducer,
		SpecialCount,
		Special,
		SpecialNew,
		SpecialNewDifficult,
		RatingNotJunkOnly,
		RatingJunkOnly,
		Rating,
		RatingFull,
		RatingNew,
		RatingWithPayersList,
		MinCost,
		MinCostProducer,
		MinCostCountAndProducer,
		MinCostCount,
		MinCostManyClients,
		MinCostByPriceProducer,
		MinCostByPriceCountProducer,
		MinCostByPriceCount,
		MinCostByPrice,
		MinCostByPriceNew,
		MinCostByPriceNewDifficult,
		MinCostNew,
		MinCostNewDificult,
		DefectureProductsWithProducer,
		DefectureProductsOnly,
		DefectureNameOnly,
		DefectureNameAndFormWithProducer,
		DefectureNameAndForm,
		DefectureNew,
		DefectureNewDifficult,
		CombineProducer,
		CombineCountAndProducer,
		CombineCount,
		Combine,
		CombineNew,
		CombineNewWithSuppliers,
		CombineNewDifficult,
		Individual,
		OptimizationEfficiency,
		OptimizationEfficiencyAllClients,
		OptimizationEfficiencyWithSupplier,
		PharmacyMixedName,
		PharmacyMixedFullName,
		PharmacyMixedNameProducer,
		PharmacyMixedNameProducerSupplierList,
		PharmacyMixedNameOld,
	}

	public static class TestHelper
	{
		public static System.Data.DataSet LoadProperties(ReportsTypes type)
		{
			DataSet result = new DataSet();
			result.ReadXml("TestData\\" + type.ToString() + ".xml");
			return result;
		}

		public static string GetFileName(ReportsTypes type)
		{
			string fileName = Path.Combine(Settings.Default.ExcelDir, type.ToString() + ".xls");
			return fileName;
		}

		public static string EnsureDeletion(ReportsTypes type)
		{
			string fileName = GetFileName(type);
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

		public static void ProcessReportWithOutDeletion(BaseReport report, ReportsTypes type)
		{
			ProfileHelper.Start();
			report.ProcessReport();
			report.ReportToFile(TestHelper.GetFileName(type));
			ProfileHelper.Stop();
		}
	}	
}
