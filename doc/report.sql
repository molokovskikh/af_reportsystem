create table CombineReports(
  `CombineReportCode` bigint(11) unsigned NOT NULL auto_increment,
  `ClientsData_FirmCode` int(11) unsigned NOT NULL,
  `Allow` tinyint unsigned default 0,  
  PRIMARY KEY (`CombineReportCode`),
  CONSTRAINT `combinereports_ibfk_1` FOREIGN KEY (`ClientsData_FirmCode`) REFERENCES usersettings.`clientsdata` (`FirmCode`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=cp1251;

create table Reports(
  `ReportCode` bigint(11) unsigned NOT NULL auto_increment,
  `CombineReports_CombineReportCode` bigint(11) unsigned NOT NULL,
  `ReportType` varchar(255) NOT NULL,  -- Spec, Spec1, DBF, Rating
  `ReportCaption` varchar(255) NOT NULL,
  PRIMARY KEY (`ReportCode`),
  CONSTRAINT `reports_ibfk_1` FOREIGN KEY (`CombineReports_CombineReportCode`) REFERENCES `CombineReports` (`CombineReportCode`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=cp1251;

create table ReportProperties(
  `Reports_ReportCode` bigint(11) unsigned NOT NULL,
  `PropertyName` varchar(255) NOT NULL,
  `PropertyValue` varchar(255) NOT NULL,
  INDEX (`Reports_ReportCode`),
  CONSTRAINT `reportproperties_ibfk_1` FOREIGN KEY (`Reports_ReportCode`) REFERENCES `reports` (`ReportCode`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=cp1251;


insert into CombineReports (ClientsData_FirmCode, Allow) values (1881, 1);

insert into Reports (CombineReports_CombineReportCode, ReportType, ReportCaption) values (1, 'Rating', 'По полному наименованию и фирме-производителю');

insert into temp.ReportProperties values (1, 'FromDate', '2004-02-25');
insert into temp.ReportProperties values (1, 'ToDate', '2004-02-27');
insert into temp.ReportProperties values (1, 'JunkState', '0');

insert into temp.ReportProperties values (1, 'FullNamePosition', '0');
insert into temp.ReportProperties values (1, 'FullNameVisible', '1');

insert into temp.ReportProperties values (1, 'FirmCrPosition', '1');
insert into temp.ReportProperties values (1, 'FirmCrVisible', '1');


insert into temp.ReportProperties values (1, 'FTGPosition', '2');
insert into temp.ReportProperties values (1, 'FTGVisible', '0');
insert into temp.ReportProperties values (1, 'FTGEqual', '1');


--insert into CombineReports (ClientsData_FirmCode, Allow) values (1889, 1);
insert into CombineReports (ClientsData_FirmCode, Allow) values (1882, 1);

insert into Reports (CombineReports_CombineReportCode, ReportType, ReportCaption) values (2, 'Spec1', 'Специальный отчет без учета производителя');

--insert into temp.ReportProperties values (2, 'PriceCode', '1493');
insert into temp.ReportProperties values (2, 'PriceCode', '165');
insert into temp.ReportProperties values (2, 'ReportType', '2');
insert into temp.ReportProperties values (2, 'ReportIsFull', '0');
insert into temp.ReportProperties values (2, 'ReportSortedByPrice', '1');


insert into CombineReports (ClientsData_FirmCode, Allow) values (1882, 1);
insert into Reports (CombineReports_CombineReportCode, ReportType, ReportCaption) values (3, 'DBF', 'Специальный отчет без учета производителя');
insert into temp.ReportProperties values (3, 'PriceCode', '165');
insert into temp.ReportProperties values (3, 'ReportType', '2');
insert into temp.ReportProperties values (3, 'ReportIsFull', '0');
insert into temp.ReportProperties values (3, 'ReportSortedByPrice', '1');

insert into CombineReports (ClientsData_FirmCode, Allow) values (2080, 1);
insert into Reports (CombineReports_CombineReportCode, ReportType, ReportCaption) values (4, 'DBF', 'Индивидуальный отчет');
insert into temp.ReportProperties values (4, 'PriceCode', '1584');
insert into temp.ReportProperties values (4, 'ReportType', '4');
insert into temp.ReportProperties values (4, 'ReportIsFull', '0');
insert into temp.ReportProperties values (4, 'ReportSortedByPrice', '1');

insert into CombineReports (ClientsData_FirmCode, Allow) values (2081, 1);
insert into Reports (CombineReports_CombineReportCode, ReportType, ReportCaption) values (3, 'DBF', 'Индивидуальный отчет');
insert into ReportProperties values (3, 'PriceCode', '1584');
insert into ReportProperties values (3, 'ReportType', '4');
insert into ReportProperties values (3, 'ReportIsFull', '0');
insert into ReportProperties values (3, 'ReportSortedByPrice', '1');


insert into CombineReports (ClientsData_FirmCode, Allow) values (2081, 1);
insert into Reports (CombineReports_CombineReportCode, ReportType, ReportCaption) values (4, 'Spec1', 'Индивидуальный отчет');
insert into ReportProperties values (4, 'PriceCode', '1584');
insert into ReportProperties values (4, 'ReportType', '4');
insert into ReportProperties values (4, 'ReportIsFull', '0');
insert into ReportProperties values (4, 'ReportSortedByPrice', '1');



--Отчет для Орла

insert into CombineReports (ClientsData_FirmCode, Allow) values (1912, 1);

set @ID=
insert into Reports (CombineReports_CombineReportCode, ReportType, ReportCaption) values (@ID, 'Rating', 'По полному наименованию и фирме-производителю');

set @ID=
insert into temp.ReportProperties values (@ID, 'FromDate', '2004-10-01');
insert into temp.ReportProperties values (@ID, 'ToDate', '2004-10-07');
insert into temp.ReportProperties values (@ID, 'JunkState', '0');

insert into temp.ReportProperties values (@ID, 'FullNamePosition', '0');
insert into temp.ReportProperties values (@ID, 'FullNameVisible', '1');

insert into temp.ReportProperties values (@ID, 'FirmCrPosition', '1');
insert into temp.ReportProperties values (@ID, 'FirmCrVisible', '1');


insert into temp.ReportProperties values (@ID, 'RegionPosition', '2');
insert into temp.ReportProperties values (@ID, 'RegionVisible', '0');
insert into temp.ReportProperties values (@ID, 'RegionEqual', '32');

40323122,91

