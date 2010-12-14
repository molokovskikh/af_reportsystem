DELETE FROM `reports`.`report_type_properties` WHERE `ID`='255';

DELETE FROM `reports`.`report_type_properties` WHERE `ID`='257';

INSERT INTO `reports`.`report_type_properties` (`ReportTypeCode`, `PropertyName`, `DisplayName`, `PropertyType`, `Optional`, `SelectStoredProcedure`, `DefaultValue`) VALUES ('17', 'Clients', 'Список аптек', 'LIST', 1, 'GetClientCodeWithNewUsers', '0');

INSERT INTO `reports`.`report_type_properties` (`ReportTypeCode`, `PropertyName`, `DisplayName`, `PropertyType`, `Optional`, `SelectStoredProcedure`) VALUES ('17', 'PayerEqual', 'Список значений \"Плательщик\"', 'LIST', 1, 'GetPayerCode');

INSERT INTO `reports`.`report_type_properties` (`ReportTypeCode`, `PropertyName`, `DisplayName`, `PropertyType`, `Optional`, `SelectStoredProcedure`) VALUES ('17', 'PayerNonEqual', 'Список исключений \"Плательщик\"', 'LIST', 1, 'GetPayerCode');