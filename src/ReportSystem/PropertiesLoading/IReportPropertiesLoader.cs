using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using MySql.Data.MySqlClient;

namespace Inforoom.ReportSystem
{
	public interface IReportPropertiesLoader
	{
		DataSet LoadProperties(MySqlConnection conn, ulong reportCode);
	}
}
