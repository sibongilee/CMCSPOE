CREATE DATABASE CMCSPOE;
GO
USE CMCSPOE;

CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(100),
    Email NVARCHAR(100),
    Password NVARCHAR(100),
    Role NVARCHAR(50)
);

CREATE TABLE Lecturers (
    LecturerId INT IDENTITY(1,1) PRIMARY KEY,
    LecturerName NVARCHAR(100),
    Email NVARCHAR(100),
    Department NVARCHAR(100),
    Qualification NVARCHAR(100)
);

CREATE TABLE Claims (
    ClaimId INT IDENTITY(1,1) PRIMARY KEY,
    LecturerId INT,
    Month NVARCHAR(50),
    HoursWorked DECIMAL(10,2),
    HourlyRate DECIMAL(10,2),
    Notes NVARCHAR(255),
    FileName NVARCHAR(255),
    FilePath NVARCHAR(255),
    Status NVARCHAR(50),
    FOREIGN KEY (LecturerId) REFERENCES Lecturers(LecturerId)
);

-- Optional for tracking approvals
CREATE TABLE Approvals (
    ApprovalId INT IDENTITY(1,1) PRIMARY KEY,
    ClaimId INT,
    ApprovedBy INT,
    Decision NVARCHAR(50),
    Comments NVARCHAR(255),
    DecisionDate DATETIME,
    FOREIGN KEY (ClaimId) REFERENCES Claims(ClaimId)
);


-- Add ClaimStatus if missing
IF COL_LENGTH('Claims', 'ClaimStatus') IS NULL
BEGIN
    ALTER TABLE Claims
    ADD ClaimStatus NVARCHAR(50) DEFAULT 'Pending';
END
GO

-- Add SupportingDocument if missing
IF COL_LENGTH('Claims', 'SupportingDocument') IS NULL
BEGIN
    ALTER TABLE Claims
    ADD SupportingDocument NVARCHAR(255) NULL;
END
GO

-- Add Remarks or Approval columns if missing
IF COL_LENGTH('Claims', 'Remarks') IS NULL
BEGIN
    ALTER TABLE Claims
    ADD Remarks NVARCHAR(255) NULL;
END
GO

