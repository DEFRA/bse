CREATE TABLE [dbo].[expRelation] (
    [seq_r]      NUMERIC (7)  IDENTITY (1, 1) NOT NULL,
    [rbse_case]  CHAR (9)     NOT NULL,
    [cphh]       CHAR (11)    NULL,
    [relation]   VARCHAR (11) NOT NULL,
    [rel_sex]    CHAR (1)     NULL,
    [dob_day]    NUMERIC (2)  NULL,
    [dob_month]  NUMERIC (2)  NULL,
    [dob_year]   NUMERIC (4)  NULL,
    [rel_fate]   VARCHAR (12) NULL,
    [r_rel_fate] VARCHAR (12) NULL,
    [date_left]  DATETIME     NULL,
    [herd_mark]  VARCHAR (15) NULL,
    [eartag]     VARCHAR (80) NULL,
    [rbse_rel]   CHAR (9)     NULL,
    CONSTRAINT [rl_pk_seq_r] PRIMARY KEY CLUSTERED ([seq_r] ASC) WITH (FILLFACTOR = 90)
);


GO
CREATE NONCLUSTERED INDEX [rbse_c_ind_rel]
    ON [dbo].[expRelation]([rbse_case] ASC);


GO
CREATE NONCLUSTERED INDEX [rbse_r_ind_rel]
    ON [dbo].[expRelation]([rbse_rel] ASC);


GO
CREATE NONCLUSTERED INDEX [herd_mk_ind_rel]
    ON [dbo].[expRelation]([herd_mark] ASC);


GO
CREATE NONCLUSTERED INDEX [cphh_ind_rel]
    ON [dbo].[expRelation]([cphh] ASC);

