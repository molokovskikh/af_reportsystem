import ReportTuner.Models

type = ReportType("Динамика цен", "Inforoom.ReportSystem.ByOffers.CostDynamic")
type.AddProperty(ReportTypeProperty("someDate", "DATETIME", "Фиксированная дата"))
type.AddProperty(ReportTypeProperty("regions", "LIST", "Регионы"))
type.AddProperty(ReportTypeProperty("suppliers", "LIST", "Поставщики"))
type.Save()
