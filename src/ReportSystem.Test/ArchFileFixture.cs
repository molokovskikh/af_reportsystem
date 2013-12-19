using System.IO;
using Inforoom.ReportSystem;
using NUnit.Framework;

namespace ReportSystem.Test
{
	[TestFixture]
	public class ArchFileFixture
	{
		[Test]
		public void TestArchBase()
		{
			var gr = new GeneralReport();
			gr.NoArchive = true;
			gr.Reports.Add(new FakeReport());
			var file = gr.BuildResultFile()[0];
			Assert.That(Path.GetExtension(file), Is.EqualTo(".xls"));
			gr = new GeneralReport();
			gr.Reports.Add(new FakeReport());
			file = gr.BuildResultFile()[0];
			Assert.That(Path.GetExtension(file), Is.EqualTo(".zip"));
		}
	}
}