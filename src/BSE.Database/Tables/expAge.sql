CREATE TABLE [dbo].[expAge] (
    [seq_a]      NUMERIC (5) NOT NULL,
    [cphh]       CHAR (11)   NOT NULL,
    [year]       NUMERIC (4) NOT NULL,
    [herd_size]  NUMERIC (5) NULL,
    [lact_1]     NUMERIC (5) NULL,
    [lact_2]     NUMERIC (5) NULL,
    [lact_3]     NUMERIC (5) NULL,
    [lact_4]     NUMERIC (5) NULL,
    [lact_5]     NUMERIC (5) NULL,
    [lact_6]     NUMERIC (5) NULL,
    [lact_7]     NUMERIC (5) NULL,
    [lact_8]     NUMERIC (5) NULL,
    [lact_9]     NUMERIC (5) NULL,
    [lact_10]    NUMERIC (5) NULL,
    [lact_10_pl] NUMERIC (5) NULL,
    CONSTRAINT [ag_pk_seq_a] PRIMARY KEY CLUSTERED ([seq_a] ASC)
);


GO
CREATE NONCLUSTERED INDEX [cphh_ind_age]
    ON [dbo].[expAge]([cphh] ASC);


GO
CREATE NONCLUSTERED INDEX [cphh_yr_ind_age]
    ON [dbo].[expAge]([cphh] ASC, [year] ASC);

