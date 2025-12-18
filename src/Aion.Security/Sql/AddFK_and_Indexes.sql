-- FK & Indexes for Aion security tables (extend as needed)
IF OBJECT_ID('dbo.J_GROUPE_UTILISATEUR','U') IS NOT NULL
BEGIN
  IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_JGU_User_Groupe' AND object_id=OBJECT_ID('dbo.J_GROUPE_UTILISATEUR'))
    CREATE UNIQUE INDEX IX_JGU_User_Groupe ON dbo.J_GROUPE_UTILISATEUR(UserId, GroupeId);
END
