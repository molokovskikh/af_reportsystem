alter table Logs.ReportsLogs
add column ResultId int unsigned,
add constraint FK_ReportsLogs_ResultId foreign key (ResultId) references Logs.ReportExecuteLogs(Id) on delete cascade;
