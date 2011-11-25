using System;
using Castle.ActiveRecord;
using NUnit.Framework;
using Test.Support;

namespace Report.Data.Builder.Test.ActiveRecord
{
	[TestFixture]
	public class JobFixture : IntegrationFixture
	{
		[Test]
		public void Save_job()
		{
			var job = new Job {
				Name = "test",
				NextRun = DateTime.Now,
				RunInterval = new TimeSpan(1, 1, 0, 0),
			};
			ActiveRecordMediator.Save(job);

			scope.Dispose();
			scope = new SessionScope();

			var loadedJob = ActiveRecordMediator<Job>.FindByPrimaryKey(job.Id);
			Assert.That(job, Is.Not.EqualTo(loadedJob));
			Assert.That(loadedJob.RunInterval, Is.EqualTo(job.RunInterval));
		}
	}
}