-- ============================================================
-- ShipExecNavigator Database
-- ============================================================
-- Run against a SQL Server instance with CREATE DATABASE rights.
-- ============================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'ShipExecNavigator')
BEGIN
    CREATE DATABASE ShipExecNavigator;
END
GO

USE ShipExecNavigator;
GO

-- ============================================================
-- Variances
-- ============================================================
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Variances')
BEGIN
    DROP TABLE dbo.Variances;
END
GO

CREATE TABLE dbo.Variances
    (
        Id             BIGINT         NOT NULL IDENTITY(1,1),
        BatchId        UNIQUEIDENTIFIER   NULL,
        CompanyId      UNIQUEIDENTIFIER   NULL,
        UserId         UNIQUEIDENTIFIER   NULL,
        NewEntity      NVARCHAR(MAX)      NULL,
        OriginalEntity NVARCHAR(MAX)      NULL,
        VarianceData   NVARCHAR(MAX)      NULL,
        Comments       NVARCHAR(MAX)      NULL,
        Endpoint       NVARCHAR(2000)     NULL,
        CreatedOn      DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
        IsActive       BIT            NOT NULL DEFAULT 1,

        CONSTRAINT PK_Variances PRIMARY KEY (Id)
    );
GO

PRINT 'ShipExecNavigator database created successfully.';
GO

-- ============================================================
-- CompanyTemplates
-- ============================================================
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CompanyTemplates')
BEGIN
    DROP TABLE dbo.CompanyTemplates;
END
GO

CREATE TABLE dbo.CompanyTemplates
    (
        Id           BIGINT           NOT NULL IDENTITY(1,1),
        CompanyId    UNIQUEIDENTIFIER NOT NULL,
        TemplateId   INT              NOT NULL,
        CompanyName  NVARCHAR(500)        NULL,
        TemplateName NVARCHAR(500)    NOT NULL,
        TemplateType NVARCHAR(100)    NOT NULL,
        TemplateData NVARCHAR(MAX)        NULL,
        EndpointUrl  NVARCHAR(2000)       NULL,
        FetchedOn    DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_CompanyTemplates        PRIMARY KEY (Id),
        CONSTRAINT UQ_CompanyTemplates_Key    UNIQUE      (CompanyId, TemplateId)
    );
GO

CREATE INDEX IX_CompanyTemplates_CompanyId ON dbo.CompanyTemplates (CompanyId);
GO

PRINT 'CompanyTemplates table created successfully.';
GO

-- ============================================================
-- ApiLogs  (structured API request/response log)
-- Switch Serilog from file to this table by enabling
-- Serilog.Sinks.MSSqlServer in appsettings.json.
-- The ApiLogManager also writes directly to this table.
-- ============================================================
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ApiLogs')
BEGIN
    DROP TABLE dbo.ApiLogs;
END
GO

CREATE TABLE dbo.ApiLogs
    (
        Id             BIGINT           NOT NULL IDENTITY(1,1),
        OccurredOn     DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        Category       NVARCHAR(100)        NULL,
        Operation      NVARCHAR(200)        NULL,
        RequestData    NVARCHAR(MAX)        NULL,
        ResponseData   NVARCHAR(MAX)        NULL,
        DurationMs     BIGINT               NULL,
        IsSuccess      BIT              NOT NULL DEFAULT 1,
        ErrorMessage   NVARCHAR(MAX)        NULL,
        CompanyId      UNIQUEIDENTIFIER     NULL,
        AdditionalInfo NVARCHAR(MAX)        NULL,

        CONSTRAINT PK_ApiLogs PRIMARY KEY (Id)
    );
GO

CREATE INDEX IX_ApiLogs_OccurredOn ON dbo.ApiLogs (OccurredOn DESC);
CREATE INDEX IX_ApiLogs_CompanyId  ON dbo.ApiLogs (CompanyId);
GO

PRINT 'ApiLogs table created successfully.';
GO
