CREATE TABLE [dbo].[User] (
    [ID]        INT          IDENTITY (1, 1) NOT NULL,
    [NTLogin]   VARCHAR (25) NOT NULL,
    [Name]      VARCHAR (35) NOT NULL,
    [Email]     VARCHAR (60) NULL,
    [UserGroup] INT          NOT NULL,
    [IsActive]  BIT          CONSTRAINT [DF_User_IsActive] DEFAULT (1) NOT NULL,
    CONSTRAINT [PK_User] PRIMARY KEY CLUSTERED ([ID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_User_luUserGroup] FOREIGN KEY ([UserGroup]) REFERENCES [dbo].[luUserGroup] ([ID]),
    CONSTRAINT [UQ_User_NTLogin] UNIQUE NONCLUSTERED ([NTLogin] ASC) WITH (FILLFACTOR = 90)
);


GO
CREATE NONCLUSTERED INDEX [IX_User_NTLogin]
    ON [dbo].[User]([NTLogin] ASC) WITH (FILLFACTOR = 90);

