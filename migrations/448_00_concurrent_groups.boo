import ReportTuner.Models
import System.Linq.Queryable

reportType = ReportType.Find(cast(ulong, 8))

propertyType = ReportTypeProperty("BusinessRivals2", "LIST", "Список конкурентов №2")
propertyType.SelectStoredProcedure = "GetFirmCode"
propertyType.Optional = true
propertyType.DefaultValue = "0"
reportType.AddProperty(propertyType)

propertyType = ReportTypeProperty("BusinessRivals3", "LIST", "Список конкурентов №3")
propertyType.SelectStoredProcedure = "GetFirmCode"
propertyType.Optional = true
propertyType.DefaultValue = "0"
reportType.AddProperty(propertyType)

reportType.Save()
