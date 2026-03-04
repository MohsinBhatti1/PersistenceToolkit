-- ===========================
-- User
-- ===========================
CREATE TABLE [User] (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    TenantId INT NOT NULL,
    CreatedBy INT NULL,
    CreatedOn DATETIME2 NULL,
    UpdatedBy INT NULL,
    UpdatedOn DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedBy INT NULL,
    DeletedOn DATETIME2 NULL
);

-- ===========================
-- ParentTable
-- ===========================
CREATE TABLE ParentTable (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(255) NULL,
    UserId INT NULL,
    MainChildId INT NULL,
    TenantId INT NOT NULL,
    CreatedBy INT NULL,
    CreatedOn DATETIME2 NULL,
    UpdatedBy INT NULL,
    UpdatedOn DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedBy INT NULL,
    DeletedOn DATETIME2 NULL
);

-- ===========================
-- ChildTable
-- ===========================
CREATE TABLE ChildTable (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ParentId INT NULL,
    Title NVARCHAR(255) NULL,
    MainGrandChildId INT NULL,
    TenantId INT NOT NULL,
    CreatedBy INT NULL,
    CreatedOn DATETIME2 NULL,
    UpdatedBy INT NULL,
    UpdatedOn DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedBy INT NULL,
    DeletedOn DATETIME2 NULL
);

-- ===========================
-- GrandChildTable
-- ===========================
CREATE TABLE GrandChildTable (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ChildId INT NULL,
    Title NVARCHAR(255) NULL,
    TenantId INT NOT NULL,
    CreatedBy INT NULL,
    CreatedOn DATETIME2 NULL,
    UpdatedBy INT NULL,
    UpdatedOn DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedBy INT NULL,
    DeletedOn DATETIME2 NULL
);

-- ===========================
-- Foreign Keys
-- ===========================

-- ParentTable.MainChild (optional, one-to-one or one-to-many)
ALTER TABLE ParentTable
    ADD CONSTRAINT FK_ParentTable_MainChild
    FOREIGN KEY (MainChildId) REFERENCES ChildTable(Id);

-- ParentTable.User (optional, if you have a User table)
ALTER TABLE ParentTable
    ADD CONSTRAINT FK_ParentTable_User
    FOREIGN KEY (UserId) REFERENCES [User](Id);

-- ChildTable.Parent (many-to-one)
ALTER TABLE ChildTable
    ADD CONSTRAINT FK_ChildTable_Parent
    FOREIGN KEY (ParentId) REFERENCES ParentTable(Id);

-- ChildTable.MainGrandChild (optional, one-to-one or one-to-many)
ALTER TABLE ChildTable
    ADD CONSTRAINT FK_ChildTable_MainGrandChild
    FOREIGN KEY (MainGrandChildId) REFERENCES GrandChildTable(Id);

-- GrandChildTable.Child (many-to-one)
ALTER TABLE GrandChildTable
    ADD CONSTRAINT FK_GrandChildTable_Child
    FOREIGN KEY (ChildId) REFERENCES ChildTable(Id); 