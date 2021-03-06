﻿ INSERT INTO reports.report_type_properties
(ReportTypeCode,
PropertyName,
DisplayName,
PropertyType,
Optional,
SelectStoredProcedure,
DefaultValue) values
(17,
"FirmCodeEqual",
"Список поставщиков",
"LIST",
0,
"GetFirmCode",
0),
(17,
"IgnoredSuppliers",
"Список игнорируемых поставщиков",
"LIST",
0,
"GetFirmCode",
0),
(17,
"RegionEqual",
"Список значений \"Региона\"",
"LIST",
0,
"GetRegion",
0),
(17,
"RegionNonEqual",
"Список исключений \"Региона\"",
"LIST",
0,
"GetRegion",
0),
(17,
"PriceCodeValues",
"Список значений \"Прайс\"",
"LIST",
0,
"GetAllPriceCode",
0),
(17,
"PriceCodeNonValues",
"Список исключений \"Прайс\"",
"LIST",
0,
"GetAllPriceCode",
0),
(17,
"PriceCode",
"Прайс по поставщику для сопоставления позиций",
"INT",
0,
"GetPriceCode",
0),
(17,
"ClientsNON",
"Список исключений аптек",
"LIST",
0,
"GetClientCodeWithNewUsers",
0),
(17,
"PayerEqual",
"Список значений \"Плательщик\"",
"LIST",
0,
"GetPayerCode",
0),
(17,
"PayerNonEqual",
"Список исключений \"Плательщик\"",
"LIST",
0,
"GetPayerCode",
0);