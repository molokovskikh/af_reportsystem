import ReportTuner.Models
import System.Linq.Queryable

reportType = ReportType.Find(cast(ulong, 24))
propertyType = ReportTypeProperty("DescriptionFile", "FILE", "Файл описание отчета (будет добавлен в архив)")
propertyType.Optional = true
propertyType.DefaultValue = ""
reportType.AddProperty(propertyType)
reportType.Save()
