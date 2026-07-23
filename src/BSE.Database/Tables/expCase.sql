CREATE TABLE [dbo].[expCase] (
    [rbse]              CHAR (9)     NOT NULL,
    [cphh]              CHAR (11)    NOT NULL,
    [cphh_natal]        CHAR (11)    NULL,
    [homebred]          CHAR (1)     NULL,
    [herd_mark]         VARCHAR (10) NULL,
    [eartag]            VARCHAR (25) NULL,
    [date_of_birth]     DATETIME     NULL,
    [dob_est_flag]      CHAR (1)     NULL,
    [bab_flag]          CHAR (1)     NULL,
    [sex]               CHAR (1)     NULL,
    [breed]             VARCHAR (15) NULL,
    [date_of_purchase]  DATETIME     NULL,
    [age_purchased]     NUMERIC (3)  NULL,
    [vendor_county]     VARCHAR (15) NULL,
    [date_enter_herd]   DATETIME     NULL,
    [onset_date]        DATETIME     NULL,
    [onset_est_flag]    CHAR (1)     NULL,
    [mths_pregnant]     NUMERIC (1)  NULL,
    [mths_post_calving] NUMERIC (1)  NULL,
    [onset_age]         NUMERIC (3)  NULL,
    [age_est_flag]      CHAR (1)     NULL,
    [form_a_date]       DATETIME     NULL,
    [form_b_date]       DATETIME     NULL,
    [form_c_date]       DATETIME     NULL,
    [fate]              VARCHAR (4)  NULL,
    [slght_date]        DATETIME     NULL,
    [path_result]       VARCHAR (4)  NULL,
    [r_path_result]     VARCHAR (4)  NULL,
    [path_date]         DATETIME     NULL,
    [survey]            VARCHAR (4)  NULL,
    CONSTRAINT [cs_pk_rbse] PRIMARY KEY CLUSTERED ([rbse] ASC)
);


GO
CREATE NONCLUSTERED INDEX [cphh_ind_cs]
    ON [dbo].[expCase]([cphh] ASC);


GO
CREATE NONCLUSTERED INDEX [cphh_n_ind_cs]
    ON [dbo].[expCase]([cphh_natal] ASC);


GO
CREATE NONCLUSTERED INDEX [herd_mk_ind_cs]
    ON [dbo].[expCase]([herd_mark] ASC) WITH (FILLFACTOR = 75);


GO
CREATE NONCLUSTERED INDEX [pathrslt_ind_cs]
    ON [dbo].[expCase]([path_result] ASC, [rbse] ASC);

