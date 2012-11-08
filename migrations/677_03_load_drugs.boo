import System
import System.IO
import System.Linq.Enumerable
import System.Text
import file from Db.boo

using reader = StreamReader("migrations\\drugs.csv", Encoding.GetEncoding(1251)):
	columns as string
	while line = reader.ReadLine():
		if not columns:
			columns = String.Join(",", line.Split(char(';')))
		else:
			values = String.Join(",", line.Split(char(';')).Select({v| "'" + v.Replace("'", "\\'") + "'"}).ToArray())
			sql = "insert into Reports.Drugs($columns) values ($values)"
			Db.Execute(sql)
