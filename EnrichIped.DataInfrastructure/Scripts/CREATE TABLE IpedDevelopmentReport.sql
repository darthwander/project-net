CREATE TABLE IpedDevelopmentReport (
    Id BIGINT AUTO_INCREMENT PRIMARY KEY,
    CollaboratorId BIGINT NULL,
    AccountId VARCHAR(255) NULL,
    Cpf VARCHAR(11) NULL,
    Name VARCHAR(255) NULL,
    Email VARCHAR(255) NULL,
    Points INT NULL,
    InProgressCourses INT NULL,
    CompletedCourses INT NULL,
    PerformancePercentage INT NULL,
    CommitmentPercentage INT NULL,
    EngagementPercentage INT NULL,
    Score INT NULL,
    Status VARCHAR(50) NULL,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    
    INDEX idx_DevelopmentReport_CollaboratorId (CollaboratorId),
    INDEX idx_DevelopmentReport_Cpf (Cpf),
    INDEX idx_DevelopmentReport_Status (Status)
);