INSERT INTO `reports`.`report_type_properties` 
(`ReportTypeCode`, `PropertyName`, `DisplayName`, `PropertyType`, `Optional`, `DefaultValue`, `SelectStoredProcedure`) 
VALUES ('5', 'Clients', 'Список аптек', 'LIST', 0, '0', 'GetClientCodeWithNewUsers');

insert into reports.report_property_values
(ReportPropertyID, Value)
select
  rp.Id,
  lastRP.PropertyValue
from
reports.report_type_properties rtp,
reports.report_properties rp,
reports.report_properties lastRP
where
    rtp.ReportTypeCode = 5
and rtp.PropertyName = 'Clients'
and rp.PropertyId = rtp.Id
and lastRP.ReportCode = rp.ReportCode
and lastRP.PropertyId = 32;