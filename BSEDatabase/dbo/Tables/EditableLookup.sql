CREATE TABLE [dbo].[EditableLookup] (
    [ID]                    INT          NOT NULL,
    [TableName]             VARCHAR (20) NOT NULL,
    [Description]           VARCHAR (40) NOT NULL,
    [SelectStoredProcedure] VARCHAR (30) NOT NULL,
    [UpdateStoredProcedure] VARCHAR (30) NOT NULL,
    [InsertStoredProcedure] VARCHAR (30) NOT NULL,
    [DeleteStoredProcedure] VARCHAR (30) NOT NULL
);

