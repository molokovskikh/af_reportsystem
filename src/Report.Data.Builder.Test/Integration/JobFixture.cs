using System;
using Castle.ActiveRecord;
using Common.Web.Ui.Models.Jobs;
using NUnit.Framework;
using Test.Support;

namespace Report.Data.Builder.Test.Integration
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
			session.Save(job);
			session.Flush();
			session.Clear();

			var loadedJob = session.Load<Job>(job.Id);
			Assert.That(job, Is.Not.EqualTo(loadedJob));
			Assert.That(loadedJob.RunInterval, Is.EqualTo(job.RunInterval));
		}
	}
}