
    create table Reports.Jobs (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Name VARCHAR(255),
       NextRun DATETIME,
       LastRun DATETIME,
       RunInterval BIGINT,
       primary key (Id)
    );
