-- Create SystemSettings table for configurable system parameters
USE [PostalIdempotencyDemo]
GO

-- Create SystemSettings table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[SystemSettings]') AND type in (N'U'))
BEGIN
    CREATE TABLE [SystemSettings](
        [SettingKey] [nvarchar](100) NOT NULL,
        [SettingValue] [nvarchar](500) NOT NULL,
        [Description] [nvarchar](1000) NULL,
        [DataType] [nvarchar](20) NOT NULL DEFAULT 'string',
        [CreatedAt] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_SystemSettings] PRIMARY KEY CLUSTERED ([SettingKey])
    )
END
GO

-- Insert default system settings
MERGE dbo.SystemSettings AS target
USING (VALUES 
    ('UseIdempotencyKey', 'true', 'Enable or disable idempotency protection globally', 'boolean'),
    ('IdempotencyExpirationHours', '24', 'Number of hours after which idempotency entries expire', 'integer')
) AS source (SettingKey, SettingValue, Description, DataType)
ON target.SettingKey = source.SettingKey
WHEN NOT MATCHED THEN
    INSERT (SettingKey, SettingValue, Description, DataType, CreatedAt, UpdatedAt)
    VALUES (source.SettingKey, source.SettingValue, source.Description, source.DataType, GETUTCDATE(), GETUTCDATE())
WHEN MATCHED THEN
    UPDATE SET 
        target.SettingValue = source.SettingValue,
        target.Description  = source.Description,
        target.DataType     = source.DataType,
        target.UpdatedAt    = GETUTCDATE()
;
GO


-- Create index for faster lookups
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[SystemSettings]') AND name = N'IX_SystemSettings_DataType')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SystemSettings_DataType] ON [SystemSettings] ([DataType])
END
GO

PRINT 'SystemSettings table created and populated with default values'
