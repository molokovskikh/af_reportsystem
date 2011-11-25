
    create table Reports.Jobs (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Name VARCHAR(255),
       NextRun DATETIME not null,
       LastRun DATETIME,
       RunInterval DATETIME not null,
       primary key (Id)
    );
