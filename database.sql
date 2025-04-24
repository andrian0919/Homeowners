-- Homeowners Subdivision Database Setup Script
-- For MySQL/MariaDB (XAMPP)

CREATE DATABASE homeownerssubdivision CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci
USE homeownerssubdivision;

-- Create Homeowners table
CREATE TABLE Homeowners (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    FirstName VARCHAR(50) NOT NULL,
    LastName VARCHAR(50) NOT NULL,
    Address VARCHAR(100) NOT NULL,
    Email VARCHAR(100) NOT NULL,
    Phone VARCHAR(20) NOT NULL,
    LotNumber VARCHAR(20) NOT NULL,
    BlockNumber VARCHAR(20) NOT NULL,
    JoinDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create Users table with reference to Homeowners
CREATE TABLE Users (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Username VARCHAR(50) NOT NULL,
    Email VARCHAR(100) NOT NULL,
    Password VARCHAR(100) NOT NULL,
    FirstName VARCHAR(50) NOT NULL,
    LastName VARCHAR(50) NOT NULL,
    PhoneNumber VARCHAR(20) NULL,
    Role INT NOT NULL,  -- 0=Homeowner, 1=Administrator, 2=Staff
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    LastLoginDate DATETIME NULL,
    HomeownerId INT NULL,
    CONSTRAINT FK_Users_Homeowners FOREIGN KEY (HomeownerId) 
        REFERENCES Homeowners(Id) ON DELETE SET NULL,
    CONSTRAINT UQ_Users_Username UNIQUE (Username),
    CONSTRAINT UQ_Users_Email UNIQUE (Email)
);

-- Create index on Homeowners email for faster lookups
CREATE INDEX IX_Homeowners_Email ON Homeowners(Email);

-- Create index on Homeowners lot and block number for faster lookups
CREATE INDEX IX_Homeowners_LotBlock ON Homeowners(LotNumber, BlockNumber);


-- Additional tables that might be useful for a homeowners association system

-- Create Events table for community events
CREATE TABLE Events (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Title VARCHAR(100) NOT NULL,
    Description TEXT NULL,
    Location VARCHAR(100) NOT NULL,
    StartDate DATETIME NOT NULL,
    EndDate DATETIME NOT NULL,
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_Events_Users FOREIGN KEY (CreatedBy) 
        REFERENCES Users(Id) ON DELETE CASCADE
);

-- Create Announcements table
CREATE TABLE Announcements (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Title VARCHAR(100) NOT NULL,
    Content TEXT NOT NULL,
    IsPublished TINYINT(1) NOT NULL DEFAULT 1,
    PublishDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ExpireDate DATETIME NULL,
    CreatedBy INT NOT NULL,
    CONSTRAINT FK_Announcements_Users FOREIGN KEY (CreatedBy) 
        REFERENCES Users(Id) ON DELETE CASCADE
);

-- Create MaintenanceRequests table
CREATE TABLE MaintenanceRequests (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    HomeownerId INT NOT NULL,
    Title VARCHAR(100) NOT NULL,
    Description TEXT NOT NULL,
    Status INT NOT NULL DEFAULT 0, -- 0=New, 1=InProgress, 2=Completed, 3=Rejected
    Priority INT NOT NULL DEFAULT 1, -- 1=Low, 2=Medium, 3=High
    SubmissionDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CompletionDate DATETIME NULL,
    AssignedToId INT NULL,
    CONSTRAINT FK_MaintenanceRequests_Homeowners FOREIGN KEY (HomeownerId) 
        REFERENCES Homeowners(Id) ON DELETE CASCADE,
    CONSTRAINT FK_MaintenanceRequests_Users FOREIGN KEY (AssignedToId) 
        REFERENCES Users(Id) ON DELETE SET NULL
); 

-- Add view for active homeowners with their users
CREATE VIEW ActiveHomeownersView AS
SELECT 
    h.Id AS HomeownerId,
    h.FirstName,
    h.LastName,
    h.Address,
    h.Email,
    h.Phone,
    h.LotNumber,
    h.BlockNumber,
    h.JoinDate,
    u.Id AS UserId,
    u.Username,
    u.IsActive
FROM Homeowners h
LEFT JOIN Users u ON h.Id = u.HomeownerId
WHERE u.IsActive = 1; 