using Common.MySql;
using NUnit.Framework;

namespace Report.Data.Builder.Test
{
	[SetUpFixture]
	public class FixtureSetup
	{
		[SetUp]
		public void Setup()
		{
			With.DefaultConnectionStringName = "DB";
		}
	}
}