
    create table reports.FilesSendWithReport (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       FileName VARCHAR(255),
       Report BIGINT UNSIGNED,
       primary key (Id)
    );
alter table reports.FilesSendWithReport add index (Report), add constraint FK_reports_FilesSendWithReport_Report foreign key (Report) references reports.general_reports (GeneralReportCode);
