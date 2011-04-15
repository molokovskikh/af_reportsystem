import ReportTuner.Models

reportType = ReportType.Find(cast(ulong, 15))
property = ReportTypeProperty("Type", "Enum", "Группировать по")
property.Enum.AddValue("пользователю", 0)
property.Enum.AddValue("адресу", 1)
property.Enum.AddValue("клиенту", 2)
property.Enum.AddValue("юридическому лицу", 3)
reportType.AddProperty(property)
reportType.Save()
