
    create table reports.FileForReportTypes (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       File VARCHAR(255),
       ReportType BIGINT UNSIGNED,
       primary key (Id)
    );
alter table reports.FileForReportTypes add index (ReportType), add constraint FK_reports_FileForReportTypes_ReportType foreign key (ReportType) references reports.reporttypes (ReportTypeCode);
