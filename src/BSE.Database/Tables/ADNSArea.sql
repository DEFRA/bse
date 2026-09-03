CREATE TABLE [dbo].[ADNSArea] (
    [Area]                    CHAR (2)     NOT NULL,
    [LastEmailReference]      VARCHAR (50) NOT NULL,
    [LastADNSReferenceYear]   SMALLINT     NOT NULL,
    [LastADNSReferenceNumber] INT          NOT NULL,
    CONSTRAINT [PK_ADNS_Refs] PRIMARY KEY CLUSTERED ([Area] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [CK_ADNS_Refs] CHECK ([Area] = 'GB' or [Area] = 'CI' or [Area] = 'NI')
);


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Area must be GB, CI, or NI', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'ADNSArea', @level2type = N'CONSTRAINT', @level2name = N'CK_ADNS_Refs';

