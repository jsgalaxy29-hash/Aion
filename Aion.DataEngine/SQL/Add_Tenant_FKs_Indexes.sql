-- Ajoute TenantId INT + index (exemple SUser) — décliner sur chaque table système
IF COL_LENGTH('dbo.SUser','TenantId') IS NULL
  ALTER TABLE dbo.SUser ADD TenantId INT NOT NULL DEFAULT(1);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_SUser_Tenant_Id' AND object_id=OBJECT_ID('dbo.SUser'))
  CREATE INDEX IX_SUser_Tenant_Id ON dbo.SUser(TenantId, Id);
