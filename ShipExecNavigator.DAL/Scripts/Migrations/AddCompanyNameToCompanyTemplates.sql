-- ============================================================
-- Migration: Add CompanyName column to CompanyTemplates table
-- Run against an existing ShipExecNavigator database.
-- ============================================================

USE ShipExecNavigator;
GO

IF NOT EXISTS (
    SELECT 1
    FROM   sys.columns
    WHERE  object_id = OBJECT_ID('dbo.CompanyTemplates')
      AND  name      = 'CompanyName'
)
BEGIN
    ALTER TABLE dbo.CompanyTemplates
        ADD CompanyName NVARCHAR(500) NULL;

    PRINT 'CompanyName column added to CompanyTemplates.';
END
ELSE
BEGIN
    PRINT 'CompanyName column already exists — skipped.';
END
GO
