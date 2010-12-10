INSERT INTO `reports`.`report_type_properties` (`ReportTypeCode`, `PropertyName`, `DisplayName`, `PropertyType`, `Optional`, `DefaultValue`) VALUES ('17', 'ProducerAccount', 'С учетом производителя', 'BOOL', 0, '0');

INSERT INTO `reports`.`report_type_properties` (`ReportTypeCode`, `PropertyName`, `DisplayName`, `PropertyType`, `Optional`, `DefaultValue`) VALUES ('17', 'WithWithoutProperties', 'Расчет по каталогу', 'BOOL', 0, '1');

INSERT INTO `reports`.`report_type_properties` (`ReportTypeCode`, `PropertyName`, `DisplayName`, `PropertyType`, `Optional`, `DefaultValue`) VALUES ('17', 'AllAssortment', 'По всему ассортименту', 'BOOL', 0, '1');

INSERT INTO `reports`.`report_type_properties` (`ReportTypeCode`, `PropertyName`, `DisplayName`, `PropertyType`, `Optional`, `DefaultValue`) VALUES ('17', 'SortForPrice', 'Сортировка по прайсу', 'BOOL', 0, '0');

INSERT INTO `reports`.`report_type_properties` (`ReportTypeCode`, `PropertyName`, `DisplayName`, `PropertyType`, `Optional`, `SelectStoredProcedure`, `DefaultValue`) VALUES ('17', 'FullNameEqual', 'Список значений \"Полного наименования\"', 'LIST', 1, 'GetFullCode', '0');

INSERT INTO `reports`.`report_type_properties` (`ReportTypeCode`, `PropertyName`, `DisplayName`, `PropertyType`, `Optional`, `SelectStoredProcedure`, `DefaultValue`) VALUES ('17', 'FullNameNonEqual', 'Список исключений \"Полного наименования\"', 'LIST', 1, 'GetFullCode', '0');

INSERT INTO `reports`.`report_type_properties` (`ReportTypeCode`, `PropertyName`, `DisplayName`, `PropertyType`, `Optional`, `SelectStoredProcedure`, `DefaultValue`) VALUES ('17', 'FirmCrEqual', 'Список значений \"Производителя\"', 'LIST', 1, 'GetFirmCr', '0');

INSERT INTO `reports`.`report_type_properties` (`ReportTypeCode`, `PropertyName`, `DisplayName`, `PropertyType`, `Optional`, `SelectStoredProcedure`, `DefaultValue`) VALUES ('17', 'FirmCrNonEqual', 'Список исключений \"Производителя\"', 'LIST', 1, 'GetFirmCr', '0');

