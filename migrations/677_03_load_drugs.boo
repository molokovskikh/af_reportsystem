import System
import System.IO
import System.Linq.Enumerable
import System.Text
import System.Collections.Generic
import file from Db.boo

using reader = StreamReader("migrations\\drugs.csv", Encoding.GetEncoding(1251)):
	columns as string
	eanIndex = -1
	inserted = List of string()
	while line = reader.ReadLine():
		values = line.Split(char(';'))
		if eanIndex > -1:
			ean = values[eanIndex]
			continue if inserted.Contains(ean)
			inserted.Add(ean)
		if not columns:
			eanIndex = Array.IndexOf(values, "EAN")
			columns = String.Join(",", values)
		else:
			valuesSql = String.Join(",", values.Select({v| "'" + v.Replace("'", "\\'") + "'"}).ToArray())
			sql = "insert into Reports.Drugs($columns) values ($valuesSql)"
			Db.Execute(sql)
