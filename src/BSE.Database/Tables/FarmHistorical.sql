CREATE TABLE [dbo].[FarmHistorical] (
    [CPHH]                     CHAR (11)    NOT NULL,
    [VIC]                      SMALLINT     NULL,
    [VeterinarySurgeon]        VARCHAR (30) NULL,
    [FirstVisitDate]           DATETIME     NULL,
    [SheepKept]                VARCHAR (4)  NULL,
    [SheepFromYear]            SMALLINT     NULL,
    [SheepToYear]              SMALLINT     NULL,
    [FemalesPurchased]         VARCHAR (4)  NULL,
    [MalesPurchased]           VARCHAR (4)  NULL,
    [PurchaseRecordsAvailable] CHAR (1)     NULL,
    [LastPurchaseYear]         SMALLINT     NULL,
    [Sales]                    VARCHAR (30) NULL,
    [BullsHired]               CHAR (1)     NULL,
    [Contact]                  CHAR (1)     NULL,
    [FirstCalveAge]            CHAR (1)     NULL,
    [AI]                       VARCHAR (3)  NULL,
    CONSTRAINT [PK_FarmHistorical] PRIMARY KEY CLUSTERED ([CPHH] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [CK_FarmHistorical_CPHH] CHECK ([CPHH] like '[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'),
    CONSTRAINT [FK_FarmHistorical_Farm] FOREIGN KEY ([CPHH]) REFERENCES [dbo].[Farm] ([CPHH])
);

