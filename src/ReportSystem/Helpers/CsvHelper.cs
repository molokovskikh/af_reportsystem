using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace Inforoom.ReportSystem.Helpers
{
	public class CsvHelper
	{
		public static void Save(DataTable table, string file)
		{
			var lastColumn = table.Columns[table.Columns.Count - 1];
			using (var writer = new StreamWriter(file, false, Encoding.GetEncoding(1251))) {
				foreach (DataColumn column in table.Columns) {
					writer.Write(column.ColumnName);
					if (column != lastColumn)
						writer.Write(";");
				}
				writer.WriteLine();

				foreach (DataRow row in table.Rows) {
					foreach (DataColumn column in table.Columns) {
						writer.Write(row[column]);
						if (column != lastColumn)
							writer.Write(";");
					}
					writer.WriteLine();
				}
			}
		}
	}
}