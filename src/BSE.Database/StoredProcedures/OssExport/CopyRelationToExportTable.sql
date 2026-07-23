

CREATE PROCEDURE [dbo].[CopyRelationToExportTable] AS

	IF EXISTS (SELECT * FROM dbo.sysobjects WHERE ID = object_id(N'[dbo].[expRelation]') and OBJECTPROPERTY(id, N'IsUserTable') = 1) BEGIN
		DROP TABLE [expRelation]
	END

	CREATE TABLE [dbo].[expRelation] (
		[seq_r] [numeric](7, 0) NOT NULL IDENTITY(1,1) ,
		[rbse_case] [char] (9) NOT NULL ,
		[cphh] [char] (11) NULL ,
		[relation] [varchar] (11) NOT NULL ,
		[rel_sex] [char] (1) NULL ,
		[dob_day] [numeric](2, 0) NULL ,
		[dob_month] [numeric](2, 0) NULL ,
		[dob_year] [numeric](4, 0) NULL ,
		[rel_fate] [varchar] (12) NULL ,
		[r_rel_fate] [varchar] (12) NULL ,
		[date_left] [datetime] NULL ,
		[herd_mark] [varchar] (15) NULL ,
		[eartag] [varchar] (80) NULL ,
		[rbse_rel] [char] (9) NULL 
	) ON [PRIMARY]

	ALTER TABLE [dbo].[expRelation] WITH NOCHECK ADD 
		CONSTRAINT [rl_pk_seq_r] PRIMARY KEY  CLUSTERED 
		(
			[seq_r]
		) WITH  FILLFACTOR = 90  ON [PRIMARY] 

	CREATE  INDEX [rbse_c_ind_rel] ON [dbo].[expRelation]([rbse_case]) ON [PRIMARY]

	CREATE  INDEX [rbse_r_ind_rel] ON [dbo].[expRelation]([rbse_rel]) ON [PRIMARY]

	CREATE  INDEX [herd_mk_ind_rel] ON [dbo].[expRelation]([herd_mark]) ON [PRIMARY]

	CREATE  INDEX [cphh_ind_rel] ON [dbo].[expRelation]([cphh]) ON [PRIMARY]

	SET IDENTITY_INSERT expRelation ON

	-- insert offspring, twins and full sisters where the related animals are
	-- not cases
	INSERT INTO expRelation
		(
		seq_r,
		rbse_case,
		cphh,
		relation,
		rel_sex,
		dob_day,
		dob_month,
		dob_year,
		rel_fate,
		r_rel_fate,
		date_left,
		herd_mark,
		eartag,
		rbse_rel
		)
	SELECT
		CONVERT(numeric(7, 0), CaseRelation.ID) AS seq_r,
		CaseRelation.RBSE AS rbse_case,
		[Case].CPHH, 
	        CaseRelation.RelationType AS relation,
		CaseRelation.Sex AS rel_sex,
		CONVERT(numeric(2, 0), CaseRelation.BirthDay) AS dob_day, 
	        CONVERT(numeric(2, 0), CaseRelation.BirthMonth) AS dob_month,
		CONVERT(numeric(4, 0), CaseRelation.BirthYear) AS dob_year, 
		CASE CaseRelation.RelationFate
			WHEN 'D' THEN 'DIED/SOLD'
			WHEN 'S' THEN 'DIED/SOLD'
			WHEN 'TS' THEN 'TO BE SOLD'
			WHEN 'U' THEN NULL
			WHEN 'CS' THEN 'DIED/SOLD'
			WHEN 'OP' THEN 'DIED/SOLD'
			WHEN 'OF' THEN 'DIED/SOLD'
			WHEN NULL THEN NULL
		END AS rel_fate,
		CASE CaseRelation.RelationFate
			WHEN 'D' THEN 'DIED/SOLD'
			WHEN 'S' THEN 'DIED/SOLD'
			WHEN 'TS' THEN 'TO BE SOLD'
			WHEN 'U' THEN NULL
			WHEN 'CS' THEN 'DIED/SOLD'
			WHEN 'OP' THEN 'DIED/SOLD'
			WHEN 'OF' THEN 'DIED/SOLD'
			WHEN NULL THEN NULL
		END AS r_rel_fate,
		CaseRelation.LeftDate AS date_left,
		ISNULL([CaseRelation].[EartagCountry], '') + ISNULL([CaseRelation].[EartagHerdmark], '') AS herd_mark,
		CaseRelation.Eartag,
		CaseRelation.RelationRBSE AS rbse_rel
	FROM
		CaseRelation INNER JOIN [Case] ON CaseRelation.RBSE = [Case].RBSE
	WHERE 
		[Case].IsNonGBCase = 0 AND
		CaseRelation.RelationRBSE IS NULL
	
	-- insert offspring, twins and full sisters where the related animals are cases
	INSERT INTO expRelation
		(
		seq_r,
		rbse_case,
		cphh,
		relation,
		rel_sex,
		dob_day,
		dob_month,
		dob_year,
		rel_fate,
		r_rel_fate,
		date_left,
		herd_mark,
		eartag,
		rbse_rel
		)
	SELECT
		CONVERT(numeric(7, 0), CaseRelation.ID) AS seq_r,
		CaseRelation.RBSE AS rbse_case,
		[Case].CPHH, 
	        CaseRelation.RelationType AS relation,
		relatedCase.Sex AS rel_sex,
		CONVERT(numeric(2, 0), DAY(relatedCase.BirthDate)) AS dob_day, 
	        CONVERT(numeric(2, 0), MONTH(relatedCase.BirthDate)) AS dob_month,
		CONVERT(numeric(4, 0), YEAR(relatedCase.BirthDate)) AS dob_year, 
		CASE ISNULL(relatedCase.FinalResult, relatedCase.Fate)
			WHEN 'DIED' THEN 'PEND'
			WHEN 'CVL' THEN 'PEND'
			WHEN 'SL' THEN 'PEND'
			WHEN NULL THEN 'PEND'
			ELSE ISNULL(relatedCase.FinalResult, relatedCase.Fate)
		END AS rel_fate,
		CASE ISNULL(relatedCase.FinalResult, relatedCase.Fate)
			WHEN 'DIED' THEN 'PEND'
			WHEN 'CVL' THEN 'PEND'
			WHEN 'SL' THEN 'PEND'
			WHEN NULL THEN 'PEND'
			WHEN 'Inc' THEN 'Neg'
			ELSE ISNULL(relatedCase.FinalResult, relatedCase.Fate)
		END AS r_rel_fate, 
		relatedCase.SlaughterDate AS date_left,
		ISNULL([relatedCase].[EartagCountry], '') + ISNULL([relatedCase].[EartagHerdmark], '') AS herd_mark,
		relatedCase.Eartag,
		CaseRelation.RelationRBSE AS rbse_rel
	FROM
		CaseRelation INNER JOIN [Case] ON CaseRelation.RBSE = [Case].RBSE
		INNER JOIN [Case] relatedCase ON CaseRelation.RelationRBSE = relatedCase.RBSE
	WHERE 
		[Case].IsNonGBCase = 0 AND
		CaseRelation.RelationRBSE IS NOT NULL
	
	SET IDENTITY_INSERT expRelation OFF
	
	
	-- insert dams where the dams are not cases
	INSERT INTO expRelation
		(
		rbse_case,
		cphh,
		relation,
		rel_sex,
		dob_day,
		dob_month,
		dob_year,
		rel_fate,
		r_rel_fate,
		date_left,
		herd_mark,
		eartag,
		rbse_rel
		)
	SELECT
		casePedigree.RBSE AS rbse_case,
		[Case].CPHH,
		'DAM' AS relation,
		damPedigree.Sex AS rel_sex,
		CONVERT(numeric(2, 0), damPedigree.BirthDay) AS dob_day,
		CONVERT(numeric(2, 0), damPedigree.BirthMonth) AS dob_month,
		CONVERT(numeric(4, 0), damPedigree.BirthYear) AS dob_year, 
		CASE [Case].DamStatus 
			WHEN 'CVL' THEN 'PEND'
			WHEN NULL THEN
				CASE damPedigree.DamID
					WHEN NULL THEN 'UNTRACED'
					ELSE 'UNK'
				END
			ELSE [Case].DamStatus
		END AS rel_fate,
		CASE [Case].DamStatus 
			WHEN 'CVL' THEN 'PEND'
			WHEN NULL THEN
				CASE damPedigree.DamID
					WHEN NULL THEN 'UNTRACED'
					ELSE 'UNK'
				END
			ELSE [Case].DamStatus
		END AS r_rel_fate, 
		NULL AS date_left,
		damPedigree.Eartag AS herd_mark,
		damPedigree.Name AS eartag,
		damPedigree.RBSE AS rbse_rel
	FROM
		Pedigree casePedigree INNER JOIN dbo.[Case] ON casePedigree.RBSE = dbo.[Case].RBSE
		INNER JOIN Pedigree damPedigree ON casePedigree.DamID = damPedigree.ID
	WHERE
		dbo.[Case].IsNonGBCase = 0 AND
		damPedigree.RBSE IS NULL
	
	-- insert dams where the dams are cases
	INSERT INTO expRelation
		(
		rbse_case,
		cphh,
		relation,
		rel_sex,
		dob_day,
		dob_month,
		dob_year,
		rel_fate,
		r_rel_fate,
		date_left,
		herd_mark,
		eartag,
		rbse_rel
		)
	SELECT
		casePedigree.RBSE AS rbse_case,
		[Case].CPHH,
		'DAM' AS relation,
		damCase.Sex AS rel_sex,
		CONVERT(numeric(2, 0), DAY(damCase.BirthDate)) AS dob_day,
		CONVERT(numeric(2, 0), MONTH(damCase.BirthDate)) AS dob_month,
		CONVERT(numeric(4, 0), YEAR(damCase.BirthDate)) AS dob_year, 
		CASE ISNULL(damCase.FinalResult, damCase.Fate)
			WHEN 'DIED' THEN 'PEND'
			WHEN 'CVL' THEN 'PEND'
			WHEN 'SL' THEN 'PEND'
			WHEN NULL THEN 'PEND'
			ELSE ISNULL(damCase.FinalResult, damCase.Fate)
		END AS rel_fate,
		CASE ISNULL(damCase.FinalResult, damCase.Fate)
			WHEN 'DIED' THEN 'PEND'
			WHEN 'CVL' THEN 'PEND'
			WHEN 'SL' THEN 'PEND'
			WHEN NULL THEN 'PEND'
			WHEN 'Inc' THEN 'Neg'
			ELSE ISNULL(damCase.FinalResult, damCase.Fate)
		END AS r_rel_fate, 
		damCase.SlaughterDate AS date_left,
		ISNULL([damCase].[EartagCountry], '') + ISNULL([damCase].[EartagHerdmark], '') AS herd_mark,
		damCase.Eartag AS eartag,
		damCase.RBSE AS rbse_rel
	FROM
		Pedigree casePedigree INNER JOIN dbo.[Case] ON casePedigree.RBSE = dbo.[Case].RBSE
		INNER JOIN Pedigree damPedigree ON casePedigree.DamID = damPedigree.ID
		INNER JOIN [Case] damCase ON damPedigree.RBSE = damCase.RBSE
		LEFT JOIN CaseBAB ON casePedigree.RBSE = CaseBAB.RBSE
	WHERE
		dbo.[Case].IsNonGBCase = 0 AND
		damPedigree.RBSE IS NOT NULL
	
	-- insert sires where the sires are not cases
	INSERT INTO expRelation
		(
		rbse_case,
		cphh,
		relation,
		rel_sex,
		dob_day,
		dob_month,
		dob_year,
		rel_fate,
		r_rel_fate,
		date_left,
		herd_mark,
		eartag,
		rbse_rel
		)
	SELECT
		casePedigree.RBSE AS rbse_case,
		[Case].CPHH,
		'SIRE' AS relation,
		sirePedigree.Sex AS rel_sex,
		CONVERT(numeric(2, 0), sirePedigree.BirthDay) AS dob_day,
		CONVERT(numeric(2, 0), sirePedigree.BirthMonth) AS dob_month,
		CONVERT(numeric(4, 0), sirePedigree.BirthYear) AS dob_year, 
		NULL AS rel_fate,
		NULL AS r_rel_fate, 
		NULL AS date_left,
		sirePedigree.Eartag AS herd_mark,
		sirePedigree.Name AS eartag,
		sirePedigree.RBSE AS rbse_rel
	FROM
		Pedigree casePedigree INNER JOIN dbo.[Case] ON casePedigree.RBSE = dbo.[Case].RBSE
		INNER JOIN Pedigree sirePedigree ON casePedigree.SireID = sirePedigree.ID
	WHERE
		dbo.[Case].IsNonGBCase = 0 AND
		sirePedigree.RBSE IS NULL
	
	-- insert sires where the sires are cases
	INSERT INTO expRelation
		(
		rbse_case,
		cphh,
		relation,
		rel_sex,
		dob_day,
		dob_month,
		dob_year,
		rel_fate,
		r_rel_fate,
		date_left,
		herd_mark,
		eartag,
		rbse_rel
		)
	SELECT
		casePedigree.RBSE AS rbse_case,
		[Case].CPHH,
		'SIRE' AS relation,
		sireCase.Sex AS rel_sex,
		CONVERT(numeric(2, 0), DAY(sireCase.BirthDate)) AS dob_day,
		CONVERT(numeric(2, 0), MONTH(sireCase.BirthDate)) AS dob_month,
		CONVERT(numeric(4, 0), YEAR(sireCase.BirthDate)) AS dob_year, 
		CASE ISNULL(sireCase.FinalResult, sireCase.Fate)
			WHEN 'DIED' THEN 'PEND'
			WHEN 'CVL' THEN 'PEND'
			WHEN 'SL' THEN 'PEND'
			WHEN NULL THEN 'PEND'
			ELSE ISNULL(sireCase.FinalResult, sireCase.Fate)
		END AS rel_fate,
		CASE ISNULL(sireCase.FinalResult, sireCase.Fate)
			WHEN 'DIED' THEN 'PEND'
			WHEN 'CVL' THEN 'PEND'
			WHEN 'SL' THEN 'PEND'
			WHEN NULL THEN 'PEND'
			WHEN 'Inc' THEN 'Neg'
			ELSE ISNULL(sireCase.FinalResult, sireCase.Fate)
		END AS r_rel_fate, 
		sireCase.SlaughterDate AS date_left,
		ISNULL([sireCase].[EartagCountry], '') + ISNULL([sireCase].[EartagHerdmark], '') AS herd_mark,
		sireCase.Eartag AS eartag,
		sireCase.RBSE AS rbse_rel
	FROM
		Pedigree casePedigree INNER JOIN dbo.[Case] ON casePedigree.RBSE = dbo.[Case].RBSE
		INNER JOIN Pedigree sirePedigree ON casePedigree.SireID = sirePedigree.ID
		INNER JOIN [Case] sireCase ON sirePedigree.RBSE = sireCase.RBSE
	WHERE
		dbo.[Case].IsNonGBCase = 0 AND
		sirePedigree.RBSE IS NOT NULL

 
