CREATE TABLE [dbo].[VetnetHerdmark] (
    [CPHH]            CHAR (11)   NOT NULL,
    [Herdmark]        VARCHAR (8) NULL,
    [NumericHerdmark] CHAR (6)    NULL,
    CONSTRAINT [PK_VetnetHerdmark] PRIMARY KEY CLUSTERED ([CPHH] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [CK_VetnetHerdmark_CPHH] CHECK ([CPHH] like '[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]')
);

