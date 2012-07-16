using System;
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Common.Web.Ui.Models.Jobs;
using NHibernate.Linq;
using NUnit.Framework;
using Rhino.Mocks;
using Test.Support;
using Test.Support.log4net;

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

			var savedJob = session.Query<Job>().FirstOrDefault(j => j.Name == name);
			if (savedJob != null)
				session.Delete(savedJob);
			var logs = session.Query<JobLog>().Where(l => l.Name == name);
			foreach (var log in logs)
				session.Delete(log);
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
		public void Run_new_job()
		{
			Close();

			runner.DefaultInterval = TimeSpan.Zero;
			runner.Run();

			Reopen();
			var activeRecordJob = GetJob();
			Assert.That(activeRecordJob, Is.Not.Null);
			Assert.That(activeRecordJob.LastRun, Is.EqualTo(DateTime.Now).Within(2).Seconds);
			Assert.That(activeRecordJob.NextRun, Is.EqualTo(DateTime.Today));
			Assert.That(activeRecordJob.RunInterval, Is.EqualTo(TimeSpan.Zero));
			job.AssertWasCalled(j => j.Work());

			var logs = ActiveRecordLinqBase<JobLog>.Queryable.Where(l => l.Name == name).ToList();
			Assert.That(logs.Count, Is.EqualTo(1));
		}

		[Test]
		public void Run_exist_job()
		{
			var activeRecordJob = new Job(name, runner.DefaultInterval);
			activeRecordJob.NextRun = DateTime.Now.AddHours(-1);
			Save(activeRecordJob);
			Reopen();

			runner.Run();

			ActiveRecordMediator.Refresh(activeRecordJob);
			Assert.That(activeRecordJob.LastRun, Is.EqualTo(DateTime.Now).Within(2).Seconds);
			Assert.That(activeRecordJob.NextRun, Is.EqualTo(DateTime.Today + runner.DefaultInterval));
			Assert.That(activeRecordJob.RunInterval, Is.EqualTo(runner.DefaultInterval));
			job.AssertWasCalled(j => j.Work());

			var logs = ActiveRecordLinqBase<JobLog>.Queryable.Where(l => l.Name == name).ToList();
			Assert.That(logs.Count, Is.EqualTo(1));
		}

		private Job GetJob()
		{
			return ActiveRecordLinqBase<Job>.Queryable.FirstOrDefault(j => j.Name == name);
		}
	}
}