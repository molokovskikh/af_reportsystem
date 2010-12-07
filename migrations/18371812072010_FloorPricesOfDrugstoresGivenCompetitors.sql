INSERT INTO reports.reporttypes
 (ReportTypeCode,
 ReportTypeName,
 ReportTypeFilePrefix,
 AlternateSubject,
 ReportClassname ) VALUES
 (17,
 "Минимальные цены аптек по заданным конкурентам",
 "FloorPricesOfDrugstoresGivenCompetitors",
 "Минимальные цены аптек по заданным конкурентам",
 "Inforoom.ReportSystem.PricesOfCompetitorsReport");
 
 INSERT INTO reports.report_type_properties
(ID,
ReportTypeCode,
PropertyName,
DisplayName,
PropertyType,
Optional,
SelectStoredProcedure,
DefaultValue) values
(218,
17,
"Clients",
"Список аптек",
"LIST",
0,
"GetClientCodeWithNewUsers",
0),
(219,
17,
"Suppliers",
"Список поставщиков",
"LIST",
0,
"GetFirmCode",
0);