CREATE TABLE [dbo].[Batch] (
    [BatchID]     INT      IDENTITY (1, 1) NOT NULL,
    [BatchYear]   SMALLINT NULL,
    [BatchNumber] INT      NOT NULL,
    CONSTRAINT [PK_Batch] PRIMARY KEY CLUSTERED ([BatchID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [IX_Batch_BatchYearBatchNumber] UNIQUE NONCLUSTERED ([BatchYear] ASC, [BatchNumber] ASC) WITH (FILLFACTOR = 90)
);


GO
CREATE NONCLUSTERED INDEX [IX_Batch_BatchYear_BatchNumber]
    ON [dbo].[Batch]([BatchYear] ASC, [BatchNumber] ASC) WITH (FILLFACTOR = 90);

