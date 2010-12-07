INSERT INTO reports.reporttypes
 (ReportTypeCode,
 ReportTypeName,
 ReportTypeFilePrefix,
 AlternateSubject,
 ReportClassname ) VALUES
 (17,
 "����������� ���� ����� �� �������� �����������",
 "FloorPricesOfDrugstoresGivenCompetitors",
 "����������� ���� ����� �� �������� �����������",
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
"������ �����",
"LIST",
0,
"GetClientCodeWithNewUsers",
0),
(219,
17,
"Suppliers",
"������ �����������",
"LIST",
0,
"GetFirmCode",
0);