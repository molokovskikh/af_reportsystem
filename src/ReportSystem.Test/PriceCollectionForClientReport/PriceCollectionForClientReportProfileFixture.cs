using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.ReportSystem;
using NUnit.Framework;

namespace ReportSystem.Test
{
    [TestFixture]
    class PriceCollectionForClientReportProfileFixture : BaseProfileFixture
    {
        [Test]
        public void CheckReport()
        {
            var props = TestHelper.LoadProperties(ReportsTypes.PriceCollectionForClientReport);
            var report = new PriceCollectionForClientReport(0, "Automate Created Report", Conn, false, ReportFormats.Excel, props);
            TestHelper.ProcessReport(report, ReportsTypes.PriceCollectionForClientReport);
        }
    }
}
