using System;
using System.Linq;
using Castle.ActiveRecord.Framework;
using Common.Web.Ui.Models.Jobs;
using NUnit.Framework;
using Rhino.Mocks;
using Test.Support;

namespace Report.Data.Builder.Test.ActiveRecord
{
	[TestFixture]
	public class JobRunnerFixture : IntegrationFixture
	{
		private JobRunner runner;
		private IJob job;
		private string name;

		[SetUp]
		public void Setup()
		{
			runner = new JobRunner();
			job = MockRepository.GenerateStub<IJob>();
			name = job.GetType().Name;
			runner.Jobs.Add(job);
		}

		[Test]
		public void Create_and_save_new_job()
		{
			runner.Run();

			var activeRecordJob = GetJob();
			Assert.That(activeRecordJob, Is.Not.Null);
			Assert.That(activeRecordJob.NextRun, Is.EqualTo(DateTime.Today.AddDays(1).AddHours(1)));
			Assert.That(activeRecordJob.LastRun, Is.Null);
			Assert.That(activeRecordJob.RunInterval, Is.EqualTo(new TimeSpan(1, 1, 0, 0)));
		}

		[Test]
		public void Run_exists_job()
		{
			runner.DefaultInterval = TimeSpan.Zero;
			runner.Run();

			var activeRecordJob = GetJob();
			Assert.That(activeRecordJob, Is.Not.Null);
			Assert.That(activeRecordJob.LastRun, Is.EqualTo(DateTime.Now).Within(1).Seconds);
			Assert.That(activeRecordJob.NextRun, Is.EqualTo(DateTime.Today));
			Assert.That(activeRecordJob.RunInterval, Is.EqualTo(TimeSpan.Zero));

			var logs = ActiveRecordLinqBase<JobLog>.Queryable.Where(l => l.Name == name).ToList();
			Assert.That(logs.Count, Is.EqualTo(1));
		}

		private Job GetJob()
		{
			return ActiveRecordLinqBase<Job>.Queryable.FirstOrDefault(j => j.Name == name);
		}
	}
}