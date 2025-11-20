CREATE DATABASE CMCSPOE;
GO

USE CMCSPOE;
GO

CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(100),
    Email NVARCHAR(150),
    PasswordHash NVARCHAR(255),
    Role NVARCHAR(50)
);
GO

CREATE TABLE Lecturers (
    LecturerId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NULL, -- optional link to Users
    LecturerName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100),
    Department NVARCHAR(100)
);
GO

ALTER TABLE Lecturers
    ADD CONSTRAINT FK_Lecturers_Users FOREIGN KEY (UserId) REFERENCES Users(UserId);
GO

CREATE TABLE Claims (
    ClaimId INT IDENTITY(1,1) PRIMARY KEY,
    LecturerId INT,
    HoursWorked DECIMAL(10,2),
    HourlyRate DECIMAL(10,2),
    TotalAmount DECIMAL(18,2) NULL,
    DateSubmitted DATETIME NULL,
    DocumentPath NVARCHAR(255),
    Notes NVARCHAR(500),
    Status NVARCHAR(50) NOT NULL DEFAULT('Pending'),
    FOREIGN KEY (LecturerId) REFERENCES Lecturers(LecturerId)
);
GO

CREATE TABLE Approvals (
    ApprovalId INT IDENTITY(1,1) PRIMARY KEY,
    ClaimId INT,
    ApprovedBy NVARCHAR(150),
    ApprovalDate DATETIME,
    Status NVARCHAR(50),
    Comments NVARCHAR(255),
    FOREIGN KEY (ClaimId) REFERENCES Claims(ClaimId)
);
GO

CREATE TABLE ClaimDocuments(
    DocumentId INT IDENTITY(1,1) PRIMARY KEY,
    ClaimId INT NOT NULL,
    FileName NVARCHAR(255),
    FilePath NVARCHAR(255),
    DateUploaded DATETIME,
    FOREIGN KEY (ClaimId) REFERENCES Claims(ClaimId)
);
GO


-- CREATE VIEW must be the only statement in the batch
CREATE VIEW vw_ClaimSummary AS
SELECT 
 c.ClaimId,
    COALESCE(l.LecturerName, u.FullName) AS LecturerName,
    c.HoursWorked,
    c.HourlyRate,
    c.TotalAmount,
    c.Status,
    c.DateSubmitted,

    -- New fields from Approvals table
    a.ApprovedBy,
    a.ApprovalDate,
    a.Comments

FROM Claims c
JOIN Lecturers l ON c.LecturerId = l.LecturerId
LEFT JOIN Users u ON l.UserId = u.UserId
LEFT JOIN Approvals a ON c.ClaimId = a.ClaimId;
GO

DROP VIEW IF EXISTS vw_ClaimSummary;
GO
/* --------------------------------------------------------------
   9. STORED PROCEDURES
----------------------------------------------------------------*/

-- User Login
CREATE PROCEDURE sp_LoginUser
    @Email NVARCHAR(150),
    @PasswordHash NVARCHAR(255)
AS
BEGIN
    SELECT UserId, FullName, Role 
    FROM Users
    WHERE Email = @Email AND PasswordHash = @PasswordHash;
END
GO

-- Register User
CREATE PROCEDURE sp_RegisterUser
    @FullName NVARCHAR(150),
    @Email NVARCHAR(150),
    @PasswordHash NVARCHAR(255),
    @Role NVARCHAR(50)
AS
BEGIN
    INSERT INTO Users (FullName, Email, PasswordHash, Role)
    VALUES (@FullName, @Email, @PasswordHash, @Role);

    SELECT SCOPE_IDENTITY() AS NewUserId;
END
GO

-- Submit Claim
CREATE PROCEDURE sp_SubmitClaim
    @LecturerId INT,
    @HoursWorked DECIMAL(10,2),
    @HourlyRate DECIMAL(10,2)
AS
BEGIN
    INSERT INTO Claims (LecturerId, HoursWorked, HourlyRate, TotalAmount, DateSubmitted)
    VALUES (@LecturerId, @HoursWorked, @HourlyRate, @HoursWorked * @HourlyRate, GETDATE());
END
GO

-- Approve Claim
CREATE PROCEDURE sp_ApproveClaim
    @ClaimId INT,
    @ApprovedBy NVARCHAR(150),
    @Comments NVARCHAR(MAX)
AS
BEGIN
    UPDATE Claims
    SET Status = 'Approved'
    WHERE ClaimId = @ClaimId;

    INSERT INTO Approvals (ClaimId, ApprovedBy, ApprovalDate, Status, Comments)
    VALUES (@ClaimId, @ApprovedBy, GETDATE(), 'Approved', @Comments);
END
GO

-- Reject Claim
CREATE PROCEDURE sp_RejectClaim
    @ClaimId INT,
    @ApprovedBy NVARCHAR(150),
    @Comments NVARCHAR(MAX)
AS
BEGIN
    UPDATE Claims
    SET Status = 'Rejected'
    WHERE ClaimId = @ClaimId;

    INSERT INTO Approvals (ClaimId, ApprovedBy, ApprovalDate, Status, Comments)
    VALUES (@ClaimId, @ApprovedBy, GETDATE(), 'Rejected', @Comments);
END
GO

-- Upload Document
CREATE PROCEDURE sp_UploadDocument
    @ClaimId INT,
    @FileName NVARCHAR(255),
    @FilePath NVARCHAR(500)
AS
BEGIN
    INSERT INTO ClaimDocuments (ClaimId, FileName, FilePath, DateUploaded)
    VALUES (@ClaimId, @FileName, @FilePath, GETDATE());
END
GO
CREATE VIEW vw_HrReport AS
SELECT
c.ClaimId,
 COALESCE(l.LecturerName, u.FullName) AS LecturerName,
    c.HoursWorked,
    c.HourlyRate,
    (c.HoursWorked * c.HourlyRate) AS TotalAmount,
    a.ApprovalDate AS ApprovedDate
FROM Claims c
JOIN Lecturers l ON c.LecturerId = l.LecturerId
LEFT JOIN Users u ON l.UserId = u.UserId
LEFT JOIN Approvals a ON c.ClaimId = a.ClaimId
WHERE c.Status = 'Approved';
GO

CREATE PROCEDURE sp_GetHrReport AS
BEGIN
    SELECT * FROM vw_HrReport;
END
GO