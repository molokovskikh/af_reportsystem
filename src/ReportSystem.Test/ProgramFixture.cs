using Inforoom.ReportSystem;
using NUnit.Framework;

namespace ReportSystem.Test
{
	[TestFixture]
	public class ProgramFixture
	{
		[Test]
		public void Parse()
		{
			var args = new[] {
				"/gr:1271",
				"/inter:true",
				"/dtFrom:25.03.2014",
				"/dtTo:01.04.2014",
				"/manual:true"
			};
			var parsed = new AppArgs();
			Program.Parse(args, parsed);
			Assert.IsTrue(parsed.Manual);
			Assert.IsTrue(parsed.Interval);
			Assert.AreEqual(1271, parsed.ReportId);
			Assert.AreEqual("25.03.2014", parsed.From.ToShortDateString());
			Assert.AreEqual("01.04.2014", parsed.To.ToShortDateString());
		}
	}
}