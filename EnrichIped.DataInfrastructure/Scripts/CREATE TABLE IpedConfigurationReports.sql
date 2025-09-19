CREATE TABLE `IpedConfigurationReports` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `TypeName` varchar(255) DEFAULT NULL,
  `LastFileName` varchar(255) DEFAULT NULL,
  `LastFileExpiresAt` datetime DEFAULT NULL,
  `LastExecution` datetime DEFAULT NULL,
  `LastCompletedSync` datetime DEFAULT NULL,
  `LastExecutionResult` varchar(255) DEFAULT NULL,
  `LastCompletedSyncResult` varchar(255) DEFAULT NULL,
  `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE INDEX idx_config_report_type ON IpedConfigurationReports(TypeName);