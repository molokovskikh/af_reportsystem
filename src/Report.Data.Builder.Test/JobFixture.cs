using System;
using Common.Web.Ui.Models.Jobs;
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
				NextRun = DateTime.Now.AddMinutes(1)
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

		[Test]
		public void NotUpdateNextRunIfError()
		{
			var nextRun = DateTime.Now;
			var job = new Job {
				LastRun = DateTime.Now.AddDays(-1),
				NextRun = nextRun,
				RunInterval = new TimeSpan(1, 13, 00, 00),
				WorkJob = new ErrorJob()
			};
			job.Run();
			Assert.That(job.NextRun == nextRun);
		}

		public class ErrorJob : IJob
		{
			public void Work()
			{
				var param1 = 1;
				var param2 = 0;
				var result = param1 / param2;
			}
		}
	}
}