CREATE TABLE IpedLogReport (
    Id BIGINT AUTO_INCREMENT PRIMARY KEY,
    CollaboratorId BIGINT,
    AccountId VARCHAR(255) NULL,
    Cpf CHAR(11) NOT NULL,
    Name VARCHAR(255) NULL,
    Email VARCHAR(255) NULL,
    Phone VARCHAR(20) NULL,
    CourseId INT NULL,
    CourseName VARCHAR(255) NULL,
    CourseCategory VARCHAR(100) NULL,
    RecordType VARCHAR(100) NULL,
    RecordDate DATETIME NULL,
    Reason TEXT NULL,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY uq_Log_CPF_Course_Record (CPF, CourseId, RecordType, RecordDate)
);