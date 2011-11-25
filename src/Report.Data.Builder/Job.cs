using System;
using Castle.ActiveRecord;
using Integration.Models;
using log4net;

namespace Report.Data.Builder
{
	[ActiveRecord(Schema = "Reports")]
	public class Job
	{
		private ILog log = LogManager.GetLogger(typeof (Job));

		public Job()
		{}

		public Job(string name, TimeSpan interval)
		{
			Name = name;
			RunInterval = interval;
			UpdateNextRun();
		}

		[PrimaryKey]
		public uint Id { get; set; }

		[Property]
		public string Name { get; set; }

		[Property]
		public DateTime NextRun { get; set; }

		[Property]
		public DateTime? LastRun { get; set; }

		[Property]
		public TimeSpan RunInterval { get; set; }

		public IJob WorkJob;

		public bool IsReady()
		{
			return NextRun <= DateTime.Now;
		}

		public JobLog Run()
		{
			LastRun = DateTime.Now;
			UpdateNextRun();

			var jobLog = new JobLog(Name);
			try
			{
				WorkJob.Work();
				jobLog.End();
			}
			catch(Exception e)
			{
				jobLog.Message = e.ToString();
				log.Error(String.Format("Ошибка при запуске комманды {0}", Name), e);
			}
			return jobLog;
		}

		private void UpdateNextRun()
		{
			NextRun = DateTime.Now.Date + RunInterval;
		}

		public JobLog RunIfReady()
		{
			if (IsReady())
				return Run();
			return null;
		}
	}
}