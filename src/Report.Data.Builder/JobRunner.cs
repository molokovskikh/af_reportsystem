using System;
using System.Collections.Generic;
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Common.Tools;
using log4net;

namespace Report.Data.Builder
{
	public interface IJob
	{
		void Work();
	}

	public class JobRunner : RepeatableCommand
	{
		private ILog log = LogManager.GetLogger(typeof(JobRunner));

		public TimeSpan DefaultInterval = new TimeSpan(1, 1, 0, 0);

		public JobRunner()
		{
			Delay = (int)TimeSpan.FromHours(1).TotalMilliseconds;
			Jobs = new List<IJob>();
			Action = Run;
		}

		public List<IJob> Jobs { get; set; }

		public void Run()
		{
			var jobs = GetJobs();

			foreach (var job in jobs)
			{
				job.WorkJob = Jobs.First(j => String.Equals(j.GetType().Name, job.Name, StringComparison.OrdinalIgnoreCase));
				var log = job.RunIfReady();

				using(new SessionScope())
				{
					if (log != null)
						ActiveRecordMediator.Save(log);
					ActiveRecordMediator.Save(job);
				}
			}
		}

		private List<Job> GetJobs()
		{
			using (new SessionScope())
			{
				var names = Jobs.Select(j => j.GetType().Name).ToList();
				var jobs = ActiveRecordLinqBase<Job>.Queryable.Where(j => names.Contains(j.Name)).ToList();
				var newJobs = names.Where(n => !jobs.Any(j => j.Name == n)).Select(n => new Job(n, DefaultInterval));
				jobs.AddRange(newJobs);
				return jobs;
			}
		}

		public override void Error(Exception e)
		{
			log.Error("Ошибка при запуске задания", e);
		}
	}
}