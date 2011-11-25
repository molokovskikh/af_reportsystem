using System;
using NUnit.Framework;

namespace Report.Data.Builder.Test
{
	[TestFixture]
	public class JobFixture
	{
		[Test]
		public void Is_job_ready()
		{
			var job = new Job {
				NextRun = DateTime.Now
			};

			Assert.That(job.IsReady(), Is.False);
			job.NextRun = DateTime.Now.AddHours(-1);
			Assert.That(job.IsReady(), Is.True);
		}

		[Test]
		public void Update_next_run()
		{
			var job = new Job {
				LastRun = DateTime.Now.AddDays(-1),
				NextRun = DateTime.Now,
				RunInterval = new TimeSpan(1, 13, 00, 00)
			};
			job.Run();
			Assert.That(job.NextRun, Is.EqualTo(DateTime.Now.Date.AddDays(1).Add(new TimeSpan(13, 00, 00))));
		}
	}
}