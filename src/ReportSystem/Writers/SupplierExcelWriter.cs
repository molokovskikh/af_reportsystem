using System;
using System.Data;
using System.Data.OleDb;
using Inforoom.ReportSystem.Helpers;
using Inforoom.ReportSystem.ReportSettings;
using MSExcel = Microsoft.Office.Interop.Excel;

namespace Inforoom.ReportSystem.Writers
{
    class SupplierExcelWriter : BaseExcelWriter
    {
         public override void WriteReportToFile(DataSet reportData, string fileName, BaseReportSettings settings)
         {
             DataTableToExcel(reportData.Tables["Results"], fileName, settings.ReportCode);
             UseExcel.Workbook(fileName, b =>
                                             {
                                                 var ws = (MSExcel._Worksheet)b.Worksheets["rep" + settings.ReportCode.ToString()];
                                                 base.FormatExcelFile(ws, reportData.Tables["Results"], settings.ReportCaption, 6);
                                             });
             ProfileHelper.End();
         }
    }
}
