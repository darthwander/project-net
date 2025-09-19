CREATE TABLE `IpedCompleteReport` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `AccountId` varchar(255) DEFAULT NULL,
  `CollaboratorId` bigint DEFAULT NULL,
  `Name` varchar(255) DEFAULT NULL,
  `Cpf` char(11) NOT NULL,
  `Position` varchar(255) DEFAULT NULL,
  `Email` varchar(255) DEFAULT NULL,
  `Group` varchar(255) DEFAULT NULL,
  `SubGroup1` varchar(255) DEFAULT NULL,
  `SubGroup2` varchar(255) DEFAULT NULL,
  `EducationTrack` varchar(255) DEFAULT NULL,
  `CourseId` bigint DEFAULT NULL,
  `CourseName` varchar(255) DEFAULT NULL,
  `CourseCategory` varchar(255) DEFAULT NULL,
  `CourseProgress` varchar(255) DEFAULT NULL,
  `ReleaseDate` datetime DEFAULT NULL,
  `StartDate` datetime DEFAULT NULL,
  `EndDate` datetime DEFAULT NULL,
  `LastAccess` datetime DEFAULT NULL,
  `Duration` varchar(255) DEFAULT NULL,
  `PerformanceRate` varchar(255) DEFAULT NULL,
  `Mandatory` varchar(10) DEFAULT NULL,
  `Status` varchar(100) DEFAULT NULL,
  `IsCourseCompleted` BIT DEFAULT 0,
  `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE INDEX idx_complete_report_cpf ON IpedCompleteReport(Cpf);
CREATE INDEX idx_complete_reportc_course_id ON IpedCompleteReport(CourseId);
CREATE INDEX idx_complete_report_status ON IpedCompleteReport(Status);
CREATE INDEX idx_complete_report_cpf_status ON IpedCompleteReport(Cpf, Status);

CREATE TRIGGER trg_iped_complete_report_before_insert
BEFORE INSERT ON IpedCompleteReport
FOR EACH ROW
BEGIN
    IF NEW.EndDate IS NOT NULL AND NEW.EndDate > '1000-01-01 00:00:00' THEN
        SET NEW.IsCourseCompleted = 1;
    ELSE
        SET NEW.IsCourseCompleted = 0;
    END IF;
END; 

CREATE TRIGGER trg_iped_complete_report_before_update
BEFORE UPDATE ON IpedCompleteReport
FOR EACH ROW
BEGIN
    IF NEW.EndDate IS NOT NULL AND NEW.EndDate > '1000-01-01 00:00:00' THEN
        SET NEW.IsCourseCompleted = 1;
    ELSE
        SET NEW.IsCourseCompleted = 0;
    END IF;
END;