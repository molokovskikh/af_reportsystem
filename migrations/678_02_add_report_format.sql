alter table Reports.General_reports
change column Format Format enum('Excel','DBF', 'CSV') NOT NULL DEFAULT 'Excel';
