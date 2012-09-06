using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using ReportTuner.Helpers;

namespace ReportTuner.Models
{
	[ActiveRecord(Table = "FileForReportTypes", Schema = "reports")]
	public class FileForReportType
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[BelongsTo]
		public ReportType ReportType { get; set; }

		[Property]
		public string File { get; set; }

		public string FillPath
		{
			get { return Path.Combine(Global.Config.SavedFilesReportTypePath, Id.ToString()); }
		}
	}
}