using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;

namespace ReportTuner.Models
{
	[ActiveRecord("FilesSendWithReport", Schema = "reports")]
	public class FileSendWithReport
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string FileName { get; set; }

		[BelongsTo(Lazy = FetchWhen.OnInvoke)]
		public virtual GeneralReport Report { get; set; }

		public string FileNameForSave
		{
			get { return Path.Combine(Global.Config.SavedFilesPath, Id.ToString()); }
		}

		public string NavigateUrl
		{
			get { return String.Format("~/Properties/FileGeneral.rails?id={0}", Id); }
		}
	}
}