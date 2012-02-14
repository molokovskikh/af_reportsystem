alter table Reports.mailing_addresses
drop foreign key `FK_Mailing_Addresses_1`;

alter table Reports.mailing_addresses
add constraint `FK_mailing_addresses_GeneralReport` foreign key (GeneralReport) references Reports.general_reports(GeneralReportCode) on delete cascade;
