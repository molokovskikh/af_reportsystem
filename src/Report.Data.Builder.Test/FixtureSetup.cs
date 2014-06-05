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
			global::Test.Support.Setup.Initialize();
			With.DefaultConnectionStringName = ConnectionHelper.GetConnectionName();
		}
	}
}