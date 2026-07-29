CREATE TABLE [dbo].[expFarm] (
    [cphh]        CHAR (11)    NOT NULL,
    [county]      VARCHAR (15) NULL,
    [x_coord]     NUMERIC (6)  NULL,
    [y_coord]     NUMERIC (7)  NULL,
    [herd_mark]   CHAR (6)     NULL,
    [aho]         CHAR (2)     NULL,
    [pedigree]    CHAR (2)     NULL,
    [herd_type]   CHAR (1)     NULL,
    [dealer_flag] CHAR (1)     NULL,
    [status]      VARCHAR (4)  NULL,
    CONSTRAINT [fm_pk_cphh] PRIMARY KEY CLUSTERED ([cphh] ASC) WITH (FILLFACTOR = 90)
);


GO
CREATE NONCLUSTERED INDEX [fm_herd_mark]
    ON [dbo].[expFarm]([herd_mark] ASC);


GO
CREATE NONCLUSTERED INDEX [fm_county]
    ON [dbo].[expFarm]([county] ASC);


GO
CREATE NONCLUSTERED INDEX [fm_status]
    ON [dbo].[expFarm]([status] ASC, [cphh] ASC);

