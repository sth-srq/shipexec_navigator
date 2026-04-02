-- ============================================================
-- Migration: Add CompanyTemplates table
-- Run against an existing ShipExecNavigator database.
-- ============================================================

USE ShipExecNavigator;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CompanyTemplates')
BEGIN
    CREATE TABLE dbo.CompanyTemplates
        (
            Id           BIGINT           NOT NULL IDENTITY(1,1),
            CompanyId    UNIQUEIDENTIFIER NOT NULL,
            TemplateId   INT              NOT NULL,
            TemplateName NVARCHAR(500)    NOT NULL,
            TemplateType NVARCHAR(100)    NOT NULL,
            TemplateData NVARCHAR(MAX)        NULL,
            EndpointUrl  NVARCHAR(2000)       NULL,
            FetchedOn    DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),

            CONSTRAINT PK_CompanyTemplates        PRIMARY KEY (Id),
            CONSTRAINT UQ_CompanyTemplates_Key    UNIQUE      (CompanyId, TemplateId)
        );

    CREATE INDEX IX_CompanyTemplates_CompanyId ON dbo.CompanyTemplates (CompanyId);

    PRINT 'CompanyTemplates table created.';
END
ELSE
BEGIN
    PRINT 'CompanyTemplates table already exists — skipped.';
END
GO
