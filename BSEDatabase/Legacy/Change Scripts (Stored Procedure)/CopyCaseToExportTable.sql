set ANSI_NULLS ON
set QUOTED_IDENTIFIER ON
go


ALTER PROCEDURE [dbo].[CopyCaseToExportTable] AS

	IF EXISTS (SELECT * FROM dbo.sysobjects WHERE ID = object_id(N'[dbo].[expCase]') and OBJECTPROPERTY(id, N'IsUserTable') = 1) BEGIN
		DROP TABLE [expCase]
	END

	CREATE TABLE [expCase] (
		[rbse] [char] (9) NOT NULL ,
		[cphh] [char] (11) NOT NULL ,
		[cphh_natal] [char] (11) NULL ,
		[homebred] [char] (1) NULL ,
		[herd_mark] [varchar] (8) NULL ,
		[eartag] [varchar] (25) NULL ,
		[date_of_birth] [datetime] NULL ,
		[dob_est_flag] [char] (1) NULL ,
		[bab_flag] [char] (1) NULL ,
		[sex] [char] (1) NULL ,
		[breed] [varchar] (15) NULL ,
		[date_of_purchase] [datetime] NULL ,
		[age_purchased] [numeric](3, 0) NULL ,
		[vendor_county] [varchar] (15) NULL ,
		[date_enter_herd] [datetime] NULL ,
		[onset_date] [datetime] NULL ,
		[onset_est_flag] [char] (1) NULL ,
		[mths_pregnant] [numeric](1, 0) NULL ,
		[mths_post_calving] [numeric](1, 0) NULL ,
		[onset_age] [numeric](3, 0) NULL ,
		[age_est_flag] [char] (1) NULL ,
		[form_a_date] [datetime] NULL ,
		[form_b_date] [datetime] NULL ,
		[form_c_date] [datetime] NULL ,
		[fate] [varchar] (4) NULL ,
		[slght_date] [datetime] NULL ,
		[path_result] [varchar] (4) NULL ,
		[r_path_result] [varchar] (4) NULL ,
		[path_date] [datetime] NULL ,
		[survey] [varchar] (4) NULL 
	) ON [PRIMARY]

	ALTER TABLE [dbo].[expCase] WITH NOCHECK ADD 
		CONSTRAINT [cs_pk_rbse] PRIMARY KEY  CLUSTERED 
		(
			[rbse]
		)  ON [PRIMARY] 

	CREATE  INDEX [cphh_ind_cs] ON [dbo].[expCase]([cphh]) ON [PRIMARY]

	CREATE  INDEX [cphh_n_ind_cs] ON [dbo].[expCase]([cphh_natal]) ON [PRIMARY]

	CREATE  INDEX [herd_mk_ind_cs] ON [dbo].[expCase]([herd_mark]) ON [PRIMARY]

	CREATE  INDEX [pathrslt_ind_cs] ON [dbo].[expCase]([path_result], [rbse]) ON [PRIMARY]

	INSERT INTO [expCase]
	SELECT
		[Case].RBSE as rbse,
		CPHH as cphh, 
		ISNULL([CaseBAB].NatalCPHH, CASE WHEN Origin = 'H' THEN CPHH ELSE NULL END) AS cphh_natal,
		Origin AS homebred,
		ISNULL([Case].[EartagCountry], '') + ISNULL([Case].[EartagHerdmark], '')  AS herd_mark,
		Eartag AS eartag, 
                      	BirthDate AS date_of_birth,
		CASE WHEN IsBirthDateEst = 1 THEN 'E' END AS dob_est_flag, 
                      	CASE IsBAB WHEN 1 THEN 'Y' WHEN 0 THEN 'N' END AS bab_flag,
		Sex as sex,
		Breed as breed,
		PurchaseDate AS date_of_purchase,
		CONVERT(numeric(3, 0), PurchaseAgeInMonths) AS age_purchased,
		PurchasedCounty AS vendor_county,
		HerdEntryDate AS date_enter_herd,
		OnsetDate AS onset_date, 
                      	CASE IsOnsetDateEst WHEN 1 THEN 'E' END AS onset_est_flag,
		CONVERT(numeric(1, 0), MonthsPregnant) AS mths_pregnant,
		CONVERT(numeric(1, 0), MonthsPostCalving) AS mths_post_calving,
		CONVERT(numeric(3, 0), OnsetAgeInMonths) AS onset_age, 
                     	CASE WHEN IsBirthDateEst = 1 OR IsOnsetDateEst = 1 THEN 'E' END AS age_est_flag,
		FormADate AS form_a_date,
		FormBDate AS form_b_date,
		FormCDate AS form_c_date, 
                      	CASE Fate WHEN 'CVL' THEN 'SL' WHEN NULL THEN 'PEND' ELSE Fate END as fate,
		SlaughterDate AS slght_date,
		CASE WHEN (Fate = 'DIED' OR Fate = 'SL') AND FinalResult IS NULL THEN 'PEND' ELSE FinalResult END AS path_result,
		CASE WHEN FinalResult = 'Inc' THEN 'Neg' ELSE CASE WHEN (Fate = 'DIED' OR Fate = 'SL') AND FinalResult IS NULL THEN 'PEND' ELSE FinalResult END END AS r_path_result, 
                      	FinalResultDate AS path_date,
		Survey as survey
	FROM
		[Case] LEFT JOIN [CaseBAB] ON [Case].[RBSE] = [CaseBAB].[RBSE]
	WHERE
		IsNonGBCase = 0

 