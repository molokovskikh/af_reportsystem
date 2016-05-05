alter table Reports.general_reports
change column Format `Format` enum('Excel','DBF','CSV', 'InfoDrugstore') NOT NULL DEFAULT 'Excel';
