using System;
using System.Collections.Generic;
using System.Threading;
using Common.Tools.Calendar;
using Common.Web.Ui.Models.Jobs;
using NUnit.Framework;

namespace Report.Data.Builder.Test.Unit
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
				RunInterval = new TimeSpan(1, 13, 00, 00),
				WorkJob = new CorrectJob()
			};
			job.Run(new CancellationToken());
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
			job.Run(new CancellationToken());
			Assert.That(job.NextRun == nextRun);
		}

		[Test]
		public void Cancel_work()
		{
			var barier = new Barrier(2);
			var jobRunner = new JobRunner();
			jobRunner.TestJobs = new List<Job> {
				new Job(typeof(ActionJob).Name, 1.Second())
			};
			jobRunner.Jobs = new List<IJob> {
				new ActionJob(t => {
					barier.SignalAndWait();
					t.WaitHandle.WaitOne();
				})
			};

			jobRunner.Start();
			barier.SignalAndWait();
			jobRunner.Stop();
			Assert.IsTrue(jobRunner.Join());
		}

		// для тестирования при корректном прохождении
		public class CorrectJob : IJob
		{
			public void Work()
			{
				var param1 = 1;
				var param2 = 0;
				var result = param1 * param2;
			}
		}

		// для тестирования ошибок
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

	public class ActionJob : IJob2
	{
		private Action<CancellationToken> action;

		public ActionJob(Action<CancellationToken> action)
		{
			this.action = action;
		}

		public void Work()
		{
			throw new NotImplementedException();
		}

		public void Work(CancellationToken token)
		{
			action(token);
		}
	}
}