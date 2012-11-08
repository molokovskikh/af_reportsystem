import ReportTuner.Models

type = ReportType("Отчет для Росздравнадзора по накладным", "Inforoom.ReportSystem.ByOrders.WaybillsReport")
type.AddProperty(ReportTypeProperty("ByPreviousMonth", "BOOL", "За предыдущий месяц"))
type.AddProperty(ReportTypeProperty("ReportInterval", "INT", "Интервал отчета (дни) от текущей даты", DefaultValue: "1"))
type.AddProperty(ReportTypeProperty("OrgId", "INT", "Юридическое лицо накладные которого будут включены в отчет", SelectStoredProcedure: "GetOrg"))
type.Save()
