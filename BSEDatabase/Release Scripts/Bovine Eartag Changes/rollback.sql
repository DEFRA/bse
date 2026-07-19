
DROP INDEX [herd_mk_ind_cs] ON [dbo].[expCase]
GO
ALTER TABLE [Case] DROP CONSTRAINT DF_Case_IsoFormatEartag;
GO
ALTER TABLE CaseRelation DROP CONSTRAINT DF_CaseRelation_IsoFormatEartag;
GO
ALTER TABLE [Case] ALTER COLUMN EartagCountry VARCHAR(2) NULL;
GO
ALTER TABLE [Case] DROP COLUMN IsoFormatEartag;
GO
ALTER TABLE CaseRelation ALTER COLUMN EartagCountry VARCHAR(2) NULL;
GO
ALTER TABLE CaseRelation DROP COLUMN IsoFormatEartag;
GO
ALTER TABLE [expCase] ALTER COLUMN herd_mark VARCHAR(8) NULL;
GO

DROP FUNCTION dbo.IsIsoEarTag
GO

ALTER PROCEDURE [dbo].[AddCase]
	@RBSE char(9),
	@CPHH char(11),
	@EartagCountry varchar(2),
	@EartagHerdmark varchar(8),
	@Eartag varchar(25),
	@PreviousEartag varchar(25),
	@BSE1ReceivedDate datetime,
	@FormADate datetime,
	@FormAResubmittedDate datetime,
	@FormBDate datetime,
	@Fate varchar(4),
	@FormCDate datetime,
	@IsPurchaserBSE1Received bit,
	@IsBreederBSE1Received bit,
	@IsVendor1BSE1Received bit,
	@IsHomebredBSE1Received bit,
	@IsSummarySheetReceived bit,
	@IsPaperworkComplete bit,
	@ReportedLocation varchar(5),
	@Survey varchar(4),
	@Notes varchar(500),
	@BirthDate datetime,
	@IsBirthDateEst bit,
	@DamStatus varchar(9),
	@BirthDateSource varchar(5),
	@ValuationAge char(1),
	@Sex char(1),
	@Breed varchar(20),
	@Origin char(1),
	@PurchaseDate datetime,
	@PurchaseAgeInMonths smallint,
	@PurchasedCounty varchar(15),
	@HerdEntryDate datetime,
	@OnsetDate datetime,
	@IsOnsetDateEst bit,
	@MonthsPregnant tinyint,
	@MonthsPostCalving tinyint,
	@OnsetAgeInMonths smallint,
	@SlaughterDate datetime,
	@UserID int,
	@AlternateDiagnosis varchar(255),
	@LabComment varchar(255),
	@CaseType varchar(2) AS
	
	DECLARE
		@ErrorCode int,
		@RowCount int

	/*
	This stored procedure is designed to be called from within a transaction, which should be rolled back if a return code greater than 0 is returned
	*/

	SELECT
		[RBSE]
	FROM
		[Case]
	WHERE
		[RBSE] = @RBSE

	SET @RowCount = @@ROWCOUNT
	
	IF @RowCount > 0 BEGIN
		RETURN 3
	END

	SET NOCOUNT ON

	INSERT INTO [AuditLog]
			(
			[TableName],
			[UserID],
			[AfterValue],
			[Reason],
			[RBSE]
			)
		VALUES
			(
			'Case',
			@UserID,
			LEFT(@RBSE, 2) + '/' + SUBSTRING(@RBSE, 3, 2) + '/' + RIGHT(@RBSE, 5),
			'Creation',
			@RBSE
			)

	SET @ErrorCode = @@ERROR

	IF @ErrorCode <> 0 BEGIN
		RETURN 1
	END

	SET NOCOUNT OFF

	INSERT INTO [Case]
		(
		[RBSE],
		[CPHH],
		[EartagCountry],
		[EartagHerdmark],
		[Eartag],
		[IsNonGBCase],
		[PreviousEartag],
		[BSE1ReceivedDate],
		[FormADate],
		[FormAResubmittedDate],
		[FormBDate],
		[Fate],
		[FormCDate],
		[IsPurchaserBSE1Received],
		[IsBreederBSE1Received],
		[IsVendor1BSE1Received],
		[IsHomebredBSE1Received],
		[IsSummarySheetReceived],
		[IsPaperworkComplete],
		[ReportedLocation],
		[Survey],
		[Notes],
		[BirthDate],
		[IsBAB],
		[IsBirthDateEst],
		[DamStatus],
		[BirthDateSource],
		[ValuationAge],
		[Sex],
		[Breed],
		[Origin],
		[PurchaseDate],
		[PurchaseAgeInMonths],
		[PurchasedCounty],
		[HerdEntryDate],
		[OnsetDate],
		[IsOnsetDateEst],
		[MonthsPregnant],
		[MonthsPostCalving],
		[OnsetAgeInMonths],
		[SlaughterDate],
		[AlternateDiagnosis],
		[LabComment],
		[CaseType]
		)
	VALUES
		(
		@RBSE,
		@CPHH,
		@EartagCountry,
		@EartagHerdmark,
		@Eartag,
		CASE LEFT(@RBSE, 4) WHEN '6300' THEN 1 WHEN '2300' THEN 1 ELSE 0 END,
		@PreviousEartag,
		@BSE1ReceivedDate,
		@FormADate,
		@FormAResubmittedDate,
		@FormBDate,
		@Fate,
		@FormCDate,
		@IsPurchaserBSE1Received,
		@IsBreederBSE1Received,
		@IsVendor1BSE1Received,
		@IsHomebredBSE1Received,
		@IsSummarySheetReceived,
		@IsPaperworkComplete, 
		@ReportedLocation,
		@Survey,
		@Notes,
		@BirthDate,
		CASE WHEN @BirthDate >= '18 July 1988' THEN 1 ELSE 0 END,
		@IsBirthDateEst,
		@DamStatus,
		@BirthDateSource,
		@ValuationAge,
		@Sex,
		@Breed,
		@Origin,
		@PurchaseDate,
		@PurchaseAgeInMonths,
		@PurchasedCounty,
		@HerdEntryDate,
		@OnsetDate,
		@IsOnsetDateEst,
		@MonthsPregnant,
		@MonthsPostCalving,
		@OnsetAgeInMonths,
		@SlaughterDate,
		@AlternateDiagnosis,
		@LabComment,
		@CaseType
		)

	SET @ErrorCode = @@ERROR

	IF @ErrorCode <> 0 BEGIN
		RETURN 2
	END

	RETURN 0
GO

ALTER PROCEDURE [dbo].[EditCase]
	@RBSE char(9),
	@EartagCountry varchar(2),
	@EartagHerdmark varchar(8),
	@Eartag varchar(25),
	@PreviousEartag varchar(25),
	@BSE1ReceivedDate datetime,
	@FormADate datetime,
	@FormAResubmittedDate datetime,
	@FormBDate datetime,
	@Fate varchar(4),
	@FormCDate datetime,
	@IsPurchaserBSE1Received bit,
	@IsBreederBSE1Received bit,
	@IsVendor1BSE1Received bit,
	@IsHomebredBSE1Received bit,
	@IsSummarySheetReceived bit,
	@IsPaperworkComplete bit,
	@ReportedLocation varchar(5),
	@Survey varchar(4),
	@Notes varchar(500),
	@BirthDate datetime,
	@IsBirthDateEst bit,
	@DamStatus varchar(9),
	@BirthDateSource varchar(5),
	@ValuationAge char(1),
	@Sex char(1),
	@Breed varchar(20),
	@Origin char(1),
	@PurchaseDate datetime,
	@PurchaseAgeInMonths smallint,
	@PurchasedCounty varchar(15),
	@HerdEntryDate datetime,
	@OnsetDate datetime,
	@IsOnsetDateEst bit,
	@MonthsPregnant tinyint,
	@MonthsPostCalving tinyint,
	@OnsetAgeInMonths smallint,
	@SlaughterDate datetime,
	@RowStamp timestamp,
	@UserID int,
	@AlternateDiagnosis varchar(255),
	@LabComment varchar(255),
	@CaseType varchar(2) AS
	
	DECLARE
		@RowCount int,
		@ErrorCode int,
		@IsBAB bit

	/*
	This stored procedure is designed to be called from within a transaction, which should be rolled back if a return code greater than 0 is returned
	*/
	IF @BirthDate >= '18 July 1988' SET @IsBAB = 1 
	ELSE SET @IsBAB = 0 

	SELECT
		[RBSE]
	FROM
		[Case]
	WHERE
		[RBSE] = @RBSE

	SET @RowCount = @@ROWCOUNT
	
	IF @RowCount = 0 BEGIN
		RETURN 1
	END

	SET NOCOUNT ON

	INSERT INTO [AuditLog]
		(
		[TableName],
		[FieldName],
		[UserID],
		[BeforeValue],
		[AfterValue],
		[Reason],
		[RBSE]
		)
	SELECT
		'Case',
		'EartagHerdmark',
		@UserID,
		[EartagHerdmark],
		@EartagHerdmark,
		'Amendment',
		[RBSE]
	FROM
		[Case]
	WHERE
		[RBSE] = @RBSE AND
		(([EartagHerdmark] != @EartagHerdmark) OR ([EartagHerdmark] IS NULL AND @EartagHerdmark IS NOT NULL) OR ([EartagHerdmark] IS NOT NULL AND @EartagHerdmark IS NULL))

	SET @ErrorCode = @@ERROR

	IF  @ErrorCode <> 0 BEGIN
		RETURN 2
	END

	INSERT INTO [AuditLog]
		(
		[TableName],
		[FieldName],
		[UserID],
		[BeforeValue],
		[AfterValue],
		[Reason],
		[RBSE]
		)
	SELECT
		'Case',
		'Eartag',
		@UserID,
		[Eartag],
		@Eartag,
		'Amendment',
		[RBSE]
	FROM
		[Case]
	WHERE
		[RBSE] = @RBSE AND
		(([Eartag] != @Eartag) OR ([Eartag] IS NULL AND @Eartag IS NOT NULL) OR ([Eartag] IS NOT NULL AND @Eartag IS NULL))

	SET @ErrorCode = @@ERROR

	IF  @ErrorCode <> 0 BEGIN
		RETURN 2
	END

	INSERT INTO [AuditLog]
		(
		[TableName],
		[FieldName],
		[UserID],
		[BeforeValue],
		[AfterValue],
		[Reason],
		[RBSE]
		)
	SELECT
		'Case',
		'FormADate',
		@UserID,
		[FormADate],
		@FormADate,
		'Amendment',
		[RBSE]
	FROM
		[Case]
	WHERE
		[RBSE] = @RBSE AND
		(([FormADate] != @FormADate) OR ([FormADate] IS NULL AND @FormADate IS NOT NULL) OR ([FormADate] IS NOT NULL AND @FormADate IS NULL))

	SET @ErrorCode = @@ERROR

	IF  @ErrorCode <> 0 BEGIN
		RETURN 2
	END

	INSERT INTO [AuditLog]
		(
		[TableName],
		[FieldName],
		[UserID],
		[BeforeValue],
		[AfterValue],
		[Reason],
		[RBSE]
		)
	SELECT
		'Case',
		'Survey',
		@UserID,
		[Survey],
		@Survey,
		'Amendment',
		[RBSE]
	FROM
		[Case]
	WHERE
		[RBSE] = @RBSE AND
		(([Survey] != @Survey) OR ([Survey] IS NULL AND @Survey IS NOT NULL) OR ([Survey] IS NOT NULL AND @Survey IS NULL))

	SET @ErrorCode = @@ERROR

	IF  @ErrorCode <> 0 BEGIN
		RETURN 2
	END

	INSERT INTO [AuditLog]
		(
		[TableName],
		[FieldName],
		[UserID],
		[BeforeValue],
		[AfterValue],
		[Reason],
		[RBSE]
		)
	SELECT
		'Case',
		'BirthDate',
		@UserID,
		[BirthDate],
		@BirthDate,
		'Amendment',
		[RBSE]
	FROM
		[Case]
	WHERE
		[RBSE] = @RBSE AND
		(([BirthDate] != @BirthDate) OR ([BirthDate] IS NULL AND @BirthDate IS NOT NULL) OR ([BirthDate] IS NOT NULL AND @BirthDate IS NULL))

	SET @ErrorCode = @@ERROR


	IF  @ErrorCode <> 0 BEGIN
		RETURN 2
	END

	INSERT INTO [AuditLog]
		(
		[TableName],
		[FieldName],
		[UserID],
		[BeforeValue],
		[AfterValue],
		[Reason],
		[RBSE]
		)
	SELECT
		'Case',
		'Sex',
		@UserID,
		[Sex],
		@Sex,
		'Amendment',
		[RBSE]
	FROM
		[Case]
	WHERE
		[RBSE] = @RBSE AND
		(([Sex] != @Sex) OR ([Sex] IS NULL AND @Sex IS NOT NULL) OR ([Sex] IS NOT NULL AND @Sex IS NULL))

	SET @ErrorCode = @@ERROR

	IF  @ErrorCode <> 0 BEGIN
		RETURN 2
	END

	INSERT INTO [AuditLog]
		(
		[TableName],
		[FieldName],
		[UserID],
		[BeforeValue],
		[AfterValue],
		[Reason],
		[RBSE]
		)
	SELECT
		'Case',
		'Origin',
		@UserID,
		[Origin],
		@Origin,
		'Amendment',
		[RBSE]
	FROM
		[Case]
	WHERE
		[RBSE] = @RBSE AND
		(([Origin] != @Origin) OR ([Origin] IS NULL AND @Origin IS NOT NULL) OR ([Origin] IS NOT NULL AND @Origin IS NULL))

	SET @ErrorCode = @@ERROR

	IF  @ErrorCode <> 0 BEGIN
		RETURN 2
	END

	INSERT INTO [AuditLog]
		(
		[TableName],
		[FieldName],
		[UserID],
		[BeforeValue],
		[AfterValue],
		[Reason],
		[RBSE]
		)
	SELECT
		'Case',
		'Fate',
		@UserID,
		[Fate],
		@Fate,
		'Amendment',
		[RBSE]
	FROM
		[Case]
	WHERE
		[RBSE] = @RBSE AND
		(([Fate] != @Fate) OR ([Fate] IS NULL AND @Fate IS NOT NULL) OR ([Fate] IS NOT NULL AND @Fate IS NULL))

	SET @ErrorCode = @@ERROR

	IF  @ErrorCode <> 0 BEGIN
		RETURN 2
	END

	INSERT INTO [AuditLog]
		(
		[TableName],
		[FieldName],
		[UserID],
		[BeforeValue],
		[AfterValue],
		[Reason],
		[RBSE]
		)
	SELECT
		'Case',
		'FormBDate',
		@UserID,
		[FormBDate],
		@FormBDate,
		'Amendment',
		[RBSE]
	FROM
		[Case]
	WHERE
		[RBSE] = @RBSE AND
		(([FormBDate] != @FormBDate) OR ([FormBDate] IS NULL AND @FormBDate IS NOT NULL) OR ([FormBDate] IS NOT NULL AND @FormBDate IS NULL))

	SET @ErrorCode = @@ERROR

	IF  @ErrorCode <> 0 BEGIN
		RETURN 2
	END

	INSERT INTO [AuditLog]
		(
		[TableName],
		[FieldName],
		[UserID],
		[BeforeValue],
		[AfterValue],
		[Reason],
		[RBSE]
		)
	SELECT
		'Case',
		'IsBAB',
		@UserID,
		[IsBAB],
		@IsBAB,
		'Amendment',
		[RBSE]
	FROM
		[Case]
	WHERE
		[RBSE] = @RBSE AND
		(([IsBAB] != @IsBAB) OR ([IsBAB] IS NULL AND @IsBAB IS NOT NULL) OR ([IsBAB] IS NOT NULL AND @IsBAB IS NULL))

	SET @ErrorCode = @@ERROR

	IF  @ErrorCode <> 0 BEGIN
		RETURN 2
	END

	INSERT INTO [AuditLog]
		(
		[TableName],
		[FieldName],
		[UserID],
		[BeforeValue],
		[AfterValue],
		[Reason],
		[RBSE]
		)
	SELECT
		'Case',
		'IsBirthDateEst',
		@UserID,
		[IsBirthDateEst],
		@IsBirthDateEst,
		'Amendment',
		[RBSE]
	FROM
		[Case]
	WHERE
		[RBSE] = @RBSE AND
		(([IsBirthDateEst] != @IsBirthDateEst) OR ([IsBirthDateEst] IS NULL AND @IsBirthDateEst IS NOT NULL) OR ([IsBirthDateEst] IS NOT NULL AND @IsBirthDateEst IS NULL))

	SET @ErrorCode = @@ERROR

	IF  @ErrorCode <> 0 BEGIN
		RETURN 2
	END

	INSERT INTO [AuditLog]
		(
		[TableName],
		[FieldName],
		[UserID],
		[BeforeValue],
		[AfterValue],
		[Reason],
		[RBSE]
		)
	SELECT
		'Case',
		'OnsetAgeInMonths',
		@UserID,
		[OnsetAgeInMonths],
		@OnsetAgeInMonths,
		'Amendment',
		[RBSE]
	FROM
		[Case]
	WHERE
		[RBSE] = @RBSE AND
		(([OnsetAgeInMonths] != @OnsetAgeInMonths) OR ([OnsetAgeInMonths] IS NULL AND @OnsetAgeInMonths IS NOT NULL) OR ([OnsetAgeInMonths] IS NOT NULL AND @OnsetAgeInMonths IS NULL))

	SET @ErrorCode = @@ERROR

	IF  @ErrorCode <> 0 BEGIN
		RETURN 2
	END

	INSERT INTO [AuditLog]
		(
		[TableName],
		[FieldName],
		[UserID],
		[BeforeValue],
		[AfterValue],
		[Reason],
		[RBSE]
		)
	SELECT
		'Case',
		'OnsetDate',
		@UserID,
		[OnsetDate],
		@OnsetDate,
		'Amendment',
		[RBSE]
	FROM
		[Case]
	WHERE
		[RBSE] = @RBSE AND
		(([OnsetDate] != @OnsetDate) OR ([OnsetDate] IS NULL AND @OnsetDate IS NOT NULL) OR ([OnsetDate] IS NOT NULL AND @OnsetDate IS NULL))

	SET @ErrorCode = @@ERROR

	IF  @ErrorCode <> 0 BEGIN
		RETURN 2
	END

	INSERT INTO [AuditLog]
		(
		[TableName],
		[FieldName],
		[UserID],
		[BeforeValue],
		[AfterValue],
		[Reason],
		[RBSE]
		)
	SELECT
		'Case',
		'SlaughterDate',
		@UserID,
		[SlaughterDate],
		@SlaughterDate,
		'Amendment',
		[RBSE]
	FROM
		[Case]
	WHERE
		[RBSE] = @RBSE AND
		(([SlaughterDate] != @SlaughterDate) OR ([SlaughterDate] IS NULL AND @SlaughterDate IS NOT NULL) OR ([SlaughterDate] IS NOT NULL AND @SlaughterDate IS NULL))

	SET @ErrorCode = @@ERROR

	IF  @ErrorCode <> 0 BEGIN
		RETURN 2
	END

	SET NOCOUNT OFF

	UPDATE
		[Case]
	SET
		[EartagCountry] = @EartagCountry,
		[EartagHerdmark] = @EartagHerdmark,
		[Eartag] = @Eartag,
		[PreviousEartag] = @PreviousEartag,
		[BSE1ReceivedDate] = @BSE1ReceivedDate,
		[FormADate] = @FormADate,
		[FormAResubmittedDate] = @FormAResubmittedDate,
		[FormBDate] = @FormBDate,
		[Fate] = @Fate,
		[FormCDate] = @FormCDate,
		[IsPurchaserBSE1Received] = @IsPurchaserBSE1Received,
		[IsBreederBSE1Received] = @IsBreederBSE1Received,
		[IsVendor1BSE1Received] = @IsVendor1BSE1Received,
		[IsHomebredBSE1Received] = @IsHomebredBSE1Received,
		[IsSummarySheetReceived] = @IsSummarySheetReceived,
		[IsPaperworkComplete] = @IsPaperworkComplete,
		[ReportedLocation] = @ReportedLocation,
		[Survey] = @Survey,
		[Notes] = @Notes,
		[BirthDate] = @BirthDate,
		[IsBAB] = @IsBAB,
		[IsBirthDateEst] = @IsBirthDateEst,
		[DamStatus] = @DamStatus,
		[BirthDateSource] = @BirthDateSource,
		[ValuationAge] = @ValuationAge,
		[Sex] = @Sex,
		[Breed] = @Breed,
		[Origin] = @Origin,
		[PurchaseDate] = @PurchaseDate,
		[PurchaseAgeInMonths] = @PurchaseAgeInMonths,
		[PurchasedCounty] = @PurchasedCounty,
		[HerdEntryDate] = @HerdEntryDate,
		[OnsetDate] = @OnsetDate,
		[IsOnsetDateEst] = @IsOnsetDateEst,
		[MonthsPregnant] = @MonthsPregnant,
		[MonthsPostCalving] = @MonthsPostCalving,
		[OnsetAgeInMonths] = @OnsetAgeInMonths,
		[SlaughterDate] = @SlaughterDate,
		[AlternateDiagnosis] = @AlternateDiagnosis,
		[LabComment] = @LabComment,
		[CaseType] = @CaseType
	WHERE
		[RBSE] = @RBSE AND
		[RowStamp] = @RowStamp

	SELECT
		@RowCount = @@ROWCOUNT,
		@ErrorCode = @@ERROR

	IF @RowCount <> 1  BEGIN
		RETURN 3
	END

	 IF @ErrorCode <> 0 BEGIN
		RETURN 4
	END

	RETURN 0
GO

ALTER PROCEDURE [dbo].[AddCaseRelation]
	@RBSE char(9),
	@RelationType varchar(11),
	@RelationRBSE char(9),
	@Sex char(1),
	@BirthDay tinyint,
	@BirthMonth tinyint,
	@BirthYear smallint,
	@RelationFate varchar(12),
	@LeftDate datetime,
	@EartagCountry varchar(2),
	@EartagHerdmark varchar(8),
	@Eartag varchar(40),
	@Sire varchar(80) AS

	IF @RelationRBSE IS NULL BEGIN	
		INSERT INTO [CaseRelation]
			(
			[RBSE],
			[RelationType],
			[RelationRBSE],
			[Sex],
			[BirthDay],
			[BirthMonth],
			[BirthYear],
			[RelationFate],
			[LeftDate],
			[EartagCountry],
			[EartagHerdmark],
			[Eartag],
			[Sire]
			)
		VALUES
			(
			@RBSE,
			@RelationType,
			@RelationRBSE,
			@Sex,
			@BirthDay,
			@BirthMonth,
			@BirthYear,
			@RelationFate,
			@LeftDate,
			@EartagCountry,
			@EartagHerdmark,
			@Eartag,
			@Sire
			)
		
	END ELSE BEGIN

		INSERT INTO [CaseRelation]
			(
			[RBSE],
			[RelationType],
			[RelationRBSE],
			[Sex],
			[BirthDay],
			[BirthMonth],
			[BirthYear],
			[RelationFate],
			[LeftDate],
			[EartagCountry],
			[EartagHerdmark],
			[Eartag],
			[Sire]
			)
		VALUES
			(
			@RBSE,
			@RelationType,
			@RelationRBSE,
			NULL,
			NULL,
			NULL,
			NULL,
			NULL,
			NULL,
			NULL,
			NULL,
			NULL,
			NULL
			)
	END

GO

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
GO

ALTER PROCEDURE [dbo].[EditCaseRelation]
	@ID int,
	@RelationType varchar(11),
	@RelationRBSE char(9),
	@Sex char(1),
	@BirthDay tinyint,
	@BirthMonth tinyint,
	@BirthYear smallint,
	@RelationFate varchar(12),
	@LeftDate datetime,
	@EartagCountry varchar(2),
	@EartagHerdmark varchar(8),
	@Eartag varchar(40),
	@Sire varchar(80),
	@RowStamp timestamp AS

	IF @RelationRBSE IS NULL BEGIN

		UPDATE
			[CaseRelation]
		SET
			[RelationType] = @RelationType,
			[RelationRBSE] = @RelationRBSE,
			[Sex] = @Sex,
			[BirthDay] = @BirthDay,
			[BirthMonth] = @BirthMonth,
			[BirthYear] = @BirthYear,
			[RelationFate] = @RelationFate,
			[LeftDate] = @LeftDate,
			[EartagCountry] = @EartagCountry,
			[EartagHerdmark] = @EartagHerdmark,
			[Eartag] = @Eartag,
			[Sire] = @Sire
		WHERE
			[ID] = @ID AND
			[RowStamp] = @RowStamp

	END ELSE BEGIN

		UPDATE
			[CaseRelation]
		SET
			[RelationType] = @RelationType,
			[RelationRBSE] = @RelationRBSE,
			[Sex] = NULL,
			[BirthDay] = NULL,
			[BirthMonth] = NULL,
			[BirthYear] = NULL,
			[RelationFate] = NULL,
			[LeftDate] = NULL,
			[EartagCountry] = NULL,
			[EartagHerdmark] = NULL,
			[Eartag] = NULL,
			[Sire] = NULL
		WHERE
			[ID] = @ID AND
			[RowStamp] = @RowStamp

	END
GO

ALTER PROCEDURE [dbo].[GetSearchCase]
	@RBSE varchar(9) = '',
	@Eartag varchar(35) = '',
	@DBSE varchar(7) = '',
	@Fate varchar(4) = '',
	@FinalResult varchar(5) = '',
	@Sex varchar(1) = '',
	@Survey varchar(4) = '',
	@Notes varchar(500) = '',
	@EarliestFormADate datetime,
	@LatestFormADate datetime,
	@EarliestFinalResultDate datetime,
	@LatestFinalResultDate datetime,
	@EarliestBirthDate datetime,
	@LatestBirthDate datetime,
	@IncludeNonGBCases bit = 0,
	@PassiveActive varchar(1) = '',
	@IsImportedCase bit = 0  AS

	SET NOCOUNT ON

	SELECT
		LEFT([Case].[RBSE], 2) + '/' + SUBSTRING([Case].[RBSE], 3, 2) + '/' + RIGHT([Case].[RBSE], 5) AS [RBSE],
		LEFT([Case].[CPHH], 2) + '/' + SUBSTRING([Case].[CPHH], 3, 3) + '/' + SUBSTRING([Case].[CPHH], 6, 4) + '/' + RIGHT([Case].[CPHH], 2) AS [CPHH],
		[luSex].[Description] AS [Sex],
		[luSurvey].[Description] AS [Survey],
		ISNULL([Case].[EartagCountry], '') + ISNULL([Case].[EartagHerdmark] + ' ', '') + ISNULL([Case].[Eartag], '') AS [Eartag],
		[BirthDate],
		CASE [IsBirthDateEst] WHEN 1 THEN 'Y' ELSE 'N' END AS [IsBirthDateEst],
		[FormADate],
		[luCaseFate].[Description] AS [Fate],
		[luTestResult].[Description] AS [FinalResult],
		[FinalResultDate],
		LEFT([Case].[DBSE], 2) + '/' + RIGHT([Case].[DBSE], 5) AS [DBSE],
		[Case].[Notes],
		[CaseBAB].[Notes] AS [BabNotes],
		[luAnimalOrigin].[Description] AS [Origin],
		[luValuationAge].[Description] AS [ValuationAge]
	FROM
		[Case] LEFT JOIN [CaseBAB] ON [Case].[RBSE] = [CaseBAB].[RBSE]
		LEFT JOIN [luSex] ON [Case].[Sex] = [luSex].[Code]
		LEFT JOIN [luSurvey] ON [Case].[Survey] = [luSurvey].[Code]
		LEFT JOIN [luCaseFate] ON [Case].[Fate] = [luCaseFate].[Code]
		LEFT JOIN [luTestResult] ON [Case].[FinalResult] = [luTestResult].[Code]
		LEFT JOIN [luAnimalOrigin] ON [Case].[Origin] = [luAnimalOrigin].[Code]
		LEFT JOIN [luValuationAge] ON [Case].[ValuationAge] = [luValuationAge].[Code]
	WHERE
		[Case].[RBSE] LIKE @RBSE + '%' AND
		ISNULL([Case].[EartagCountry], '') + ISNULL([Case].[EartagHerdmark] + ' ', '') + ISNULL([Case].[Eartag], '') LIKE @Eartag + '%' AND
		ISNULL([Case].[DBSE], '') LIKE @DBSE + '%' AND
		ISNULL([Case].[Fate], '') LIKE @Fate + '%' AND
		ISNULL([Case].[FinalResult], '') LIKE @FinalResult + '%' AND
		ISNULL([Case].[Sex], '') LIKE @Sex + '%' AND
		ISNULL([Case].[Survey], '') LIKE @Survey + '%' AND
		ISNULL([Case].[Notes], '') LIKE '%' + @Notes + '%' AND
		ISNULL([Case].[FormADate], '1 January 1900') BETWEEN ISNULL(@EarliestFormADate, '1 January 1900') AND ISNULL(@LatestFormADate, GETDATE()) AND
		ISNULL([Case].[FinalResultDate], '1 January 1900') BETWEEN ISNULL(@EarliestFinalResultDate, '1 January 1900') AND ISNULL(@LatestFinalResultDate, GETDATE()) AND
		ISNULL([Case].[BirthDate], '1 January 1900') BETWEEN ISNULL(@EarliestBirthDate, '1 January 1900') AND ISNULL(@LatestBirthDate, GETDATE())  AND
		([Case].[IsNonGBCase] = 0 OR [Case].[IsNonGBCase] = @IncludeNonGBCases) AND
		((@PassiveActive = 'P' AND (ISNULL([Case].[Survey], 'PS') = 'PS')) OR
		(@PassiveActive = 'A' AND (ISNULL([Case].[Survey], 'PS') != 'PS')) OR
		(@PassiveActive = '')) AND
		(@IsImportedCase = 0 OR (ISNULL([Case].[EartagCountry], '') != '' AND ISNULL([Case].[EartagCountry], '') != 'UK'))
	ORDER BY
		[Case].[RBSE]

	SET NOCOUNT OFF
GO

CREATE NONCLUSTERED INDEX [herd_mk_ind_cs] ON [dbo].[expCase]
(
	[herd_mark] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 75) ON [PRIMARY]
GO

ALTER PROCEDURE [dbo].[GetSearchCaseByEartagHerdmark]
	@EartagHerdmark varchar(8),
	@IncludeNonGBCases bit = 0 AS

	SET NOCOUNT ON

	IF @EartagHerdmark = '' BEGIN
		SET @EartagHerdmark = '%'
	END

	SELECT
		LEFT([Case].[RBSE], 2) + '/' + SUBSTRING([Case].[RBSE], 3, 2) + '/' + RIGHT([Case].[RBSE], 5) AS [RBSE],
		LEFT([Case].[CPHH], 2) + '/' + SUBSTRING([Case].[CPHH], 3, 3) + '/' + SUBSTRING([Case].[CPHH], 6, 4) + '/' + RIGHT([Case].[CPHH], 2) AS [CPHH],
		[luSex].[Description] AS [Sex],
		ISNULL([Case].[EartagCountry], '') + ISNULL([Case].[EartagHerdmark] + ' ', '') + ISNULL([Case].[Eartag], '') AS [Eartag],
		[BirthDate],
		[PurchaseDate],
		[PurchaseAgeInMonths],
		[OnsetDate],
		[FormADate],
		[SlaughterDate],
		[OnsetAgeInMonths],
		[luCaseFate].[Description] AS [Fate],
		[luTestResult].[Description] AS [FinalResult],
		[luSurvey].[Description] AS [Survey],
		CASE WHEN [Case].[FinalResult] = 'Pos' THEN 'Positive' WHEN [Case].[FinalResult] IS NULL AND [Case].[Fate] NOT IN ('Alt', 'Rec') THEN 'Pending' ELSE 'Negative' END AS [CaseStatus],
		CASE WHEN [Case].[FinalResult] = 'Pos' THEN dbo.udfYearsAndMonths([Case].[FinalResultDate], GETDATE()) WHEN [Case].[FinalResult] IS NULL AND [Case].[Fate] NOT IN ('Alt', 'Rec') THEN dbo.udfYearsAndMonths([Case].[FormADate], GETDATE()) ELSE NULL END  AS [TimeElapsed],
		CASE WHEN [Case].[FinalResult] = 'Pos' THEN DATEDIFF(day, [Case].[FinalResultDate], GETDATE()) WHEN [Case].[FinalResult] IS NULL AND [Case].[Fate] NOT IN ('Alt', 'Rec') THEN DATEDIFF(day, [Case].[FormADate], GETDATE()) ELSE NULL END  AS [DaysElapsed],
		[luAnimalOrigin].[Description] AS [Origin],
		[Case].[FinalResultDate] AS [FinalResultDate]
	FROM
		[Case] LEFT JOIN [luSex] ON [Case].[Sex] = [luSex].[Code]
		LEFT JOIN [luSurvey] ON [Case].[Survey] = [luSurvey].[Code]
		LEFT JOIN [luCaseFate] ON [Case].[Fate] = [luCaseFate].[Code]
		LEFT JOIN [luTestResult] ON [Case].[FinalResult] = [luTestResult].[Code]
		LEFT JOIN [luAnimalOrigin] ON [Case].[Origin] = [luAnimalOrigin].[Code]
	WHERE
		[EartagCountry] + [EartagHerdmark] LIKE @EartagHerdmark AND
		([Case].[IsNonGBCase] = 0 OR [Case].[IsNonGBCase] = @IncludeNonGBCases)
	ORDER BY
		[DaysElapsed],
		[Case].[RBSE]

	SET NOCOUNT OFF
 
GO

ALTER PROCEDURE [dbo].[GetBSESSCheckByRBSE]
	@RBSE char(9),
	@NotificationDate varchar(30) OUTPUT,
	@BSESSEartag varchar(20) OUTPUT,
	@BSESSBirthDate varchar(30) OUTPUT,
	@TestGroupName varchar(50) OUTPUT,
	@BSESSFinalResult varchar(25) OUTPUT,
	@Barcode varchar(20) OUTPUT,
	@FormADate varchar(30) OUTPUT,
	@BSEEartag varchar(33) OUTPUT,
	@BSEBirthDate varchar(30) OUTPUT,
	@Survey varchar(50) OUTPUT,
	@BSEFinalResult varchar(50)  OUTPUT,
	@AHFReference varchar(40) OUTPUT AS

	SELECT
		@NotificationDate = CONVERT(varchar(30), [BSESSImport].[NotificationDate], 103),
		@BSESSEartag = [BSESSImport].[Eartag],
		@BSESSBirthDate = CONVERT(varchar(30), [BSESSImport].[BirthDate], 103),
		@TestGroupName = [BSESSImport].[TestGroupName],
		@BSESSFinalResult = [BSESSImport].[FinalResultName],
		@Barcode = [BSESSImport].[Barcode],
		@AHFReference = [BSESSImport].[AHFReference]
	FROM
		[BSESSImport] 
	WHERE
		[BSESSImport].[RBSE] = @RBSE
	
	SELECT
		@FormADate = CONVERT(varchar(30), [Case].[FormADate], 103),
		@BSEEartag = ISNULL([Case].[EartagCountry], '') + ISNULL([Case].[EartagHerdmark] + ' ', '') + ISNULL([Case].[Eartag], ''),
		@BSEBirthDate = CONVERT(varchar(30), [Case].[BirthDate], 103),
		@Survey = [luSurvey].[Description],
		@BSEFinalResult = [luTestResult].[Description]
	FROM
		[Case] LEFT JOIN [luSurvey] ON [Case].[Survey] = [luSurvey].[Code]
		LEFT JOIN [luTestResult] ON [Case].[FinalResult] = [luTestResult].[Code]
	WHERE
		[Case].[RBSE] = @RBSE
GO

ALTER PROCEDURE [dbo].[AddNonGBCase] 
	@RBSE char(9),
	@Eartag varchar(25),
	@EartagHerdmark char(6),
	@Fate varchar(4),
	@FinalResult varchar(5),
	@FinalResultDate datetime,
	@SlaughterDate datetime,
	@CPHH char(11),
	@OwnerName varchar(100),
	@Address1 varchar(50),
	@Address2 varchar(50),
	@Address3 varchar(50),
	@Postcode varchar(10),
	@County varchar(15),
	@Herdmark1 varchar(8),
	@NumericHerdmark1 char(6),
	@UserID int,
	@RBSEDate datetime,
	@Barcode varchar(20),
	@AHFReference varchar(40)
AS

	DECLARE
		@RowCount int,
		@ErrorCode int

BEGIN TRANSACTION

	-- Check to ensure that Case doesn't already exist
	SELECT
		[RBSE]
	FROM
		[Case]
	WHERE
		[RBSE] = @RBSE

	SET @RowCount = @@ROWCOUNT
	
	IF @RowCount <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 1
	END

	-- Check to see if the Farm already exists, don't need to raise an error if it does.
	SELECT
		[CPHH]
	FROM
		[Farm]
	WHERE
		[CPHH] = @CPHH

	SET @RowCount = @@ROWCOUNT

	-- If the Farm already exists, perform any updates to the information
	IF @RowCount = 1 BEGIN

		UPDATE
			[Farm]
		SET
			[OwnerName] = @OwnerName,
			[IsNonGBFarm] = 1,
			[Address1] = @Address1,
			[Address2] = @Address2,
			[Address3] = @Address3,
			[Postcode] = @Postcode,
			[County] = @County,
			[Herdmark1] = @Herdmark1,
			[NumericHerdmark1] = @NumericHerdmark1
		WHERE
			[CPHH] = @CPHH

		SET @ErrorCode = @@ERROR
		IF  @ErrorCode <> 0 BEGIN
			ROLLBACK TRANSACTION
			RETURN 2
		END

		-- Update the Audit Log when updating the Farm
		INSERT INTO [AuditLog]
			(
			[TableName],
			[FieldName],
			[UserID],
			[BeforeValue],
			[AfterValue],
			[Reason],
			[CPHH]
			)
		SELECT
			'Farm',
			'County',
			@UserID,
			[County],
			@County,
			'Amendment',
			[CPHH]
		FROM
			[Farm]
		WHERE
			[CPHH] = @CPHH AND [County] != @County

		SET @ErrorCode = @@ERROR
		IF  @ErrorCode <> 0 BEGIN
			ROLLBACK TRANSACTION
			RETURN 3
		END

	END ELSE BEGIN
		-- Create the Farm
		INSERT INTO [Farm]
			(
				[CPHH],
				[OwnerName],
				[IsNonGBFarm],
				[Address1],
				[Address2],
				[Address3],
				[Postcode],
				[County],
				[Herdmark1],
				[NumericHerdmark1],
				[IsDealer]
			)
		VALUES
			(
				@CPHH,
				@OwnerName,
				1,
				@Address1,
				@Address2,
				@Address3,
				@Postcode,
				@County,
				@Herdmark1,
				@NumericHerdmark1,
				0
			)

		SET @ErrorCode = @@ERROR
		IF  @ErrorCode <> 0 BEGIN
			ROLLBACK TRANSACTION
			RETURN 4
		END

		--Update the Audit Log with the Farm creation
		INSERT INTO [AuditLog]
			(
			[TableName],
			[UserID],
			[AfterValue],
			[Reason],
			[CPHH]
			)
		VALUES
			(
			'Farm',
			@UserID,
			LEFT(@CPHH, 2) + '/' + SUBSTRING(@CPHH, 3, 3) + '/' + SUBSTRING(@CPHH, 6, 4) + '/' + RIGHT(@CPHH, 2) ,
			'Creation',
			@CPHH
			)

		SET @ErrorCode = @@ERROR
		IF  @ErrorCode <> 0 BEGIN
			ROLLBACK TRANSACTION
			RETURN 5
		END

	END

	-- Create the Case
	INSERT INTO [Case]
		(
			[RBSE] ,
			[CPHH],
			[IsNonGBCase],
			[Eartag],
			[EartagHerdmark],
			[Fate],
			[FinalResult],
			[FinalResultDate],
			[SlaughterDate]
		)
	VALUES
		(
			@RBSE,
			@CPHH,
			1,
			@Eartag,
			@EartagHerdmark,
			@Fate,
			@FinalResult,
			@FinalResultDate,
			@SlaughterDate
		)	
	
	SET @ErrorCode = @@ERROR
	IF  @ErrorCode <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 6
	END

	-- Update the Audit Log with the Case Creation
	INSERT INTO [AuditLog]
		(
		[TableName],
		[UserID],
		[AfterValue],
		[Reason],
		[RBSE]
		)
	VALUES
		(
		'Case',
		@UserID,
		LEFT(@RBSE, 2) + '/' + SUBSTRING(@RBSE, 3, 2) + '/' + RIGHT(@RBSE, 5),
		'Creation',
		@RBSE
		)

	SET @ErrorCode = @@ERROR
	IF  @ErrorCode <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 7
	END

	-- Create CaseWork
	INSERT INTO [CaseWork]
           (
           [RBSE],
           [RBSEDate],
           [Barcode],
           [AHFReference]
           )
     VALUES
           (
            @RBSE,
            @RBSEDate,
            @Barcode,
            @AHFReference
           )

	SET @ErrorCode = @@ERROR
	IF  @ErrorCode <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 8
	END

COMMIT TRANSACTION

GO

ALTER PROCEDURE [dbo].[GetSearchRelatedAnimals]
	@RBSE varchar(9),
	@Name varchar(80),
	@Eartag varchar(48),
	@RelationRBSE varchar(9),
	@RelationType varchar(11) AS

	SET NOCOUNT ON

	DECLARE @ttblRelatedAnimals TABLE
		(
		RBSE varchar(11),
		CPHH varchar(14),
		RelationType varchar(50),
		RelSex varchar(50),
		Eartag varchar(80),
		RelBirthDate varchar(30),
		RelFate varchar(50),
		LeftDate datetime,
		RelName varchar(80),
		RelEartag varchar(80),
		RelationRBSE varchar(11)
		)
	
	INSERT INTO @ttblRelatedAnimals
	SELECT
		LEFT([CaseRelation].[RBSE], 2) + '/' + SUBSTRING([CaseRelation].[RBSE], 3, 2) + '/' + RIGHT([CaseRelation].[RBSE], 5) AS [RBSE],
		LEFT([Case].[CPHH], 2) + '/' + SUBSTRING([Case].[CPHH], 3, 3) + '/' + SUBSTRING([Case].[CPHH], 6, 4) + '/' + RIGHT([Case].[CPHH], 2) AS [CPHH],
		[luRelationType].[Description] AS [RelationType],
		ISNULL([RelatedCaseSex].[Description], [luSex].[Description]) AS [RelSex],
		ISNULL([Case].[EartagCountry], '') + ISNULL([Case].[EartagHerdmark] + ' ', '') + ISNULL([Case].[Eartag], '') AS [Eartag],
		ISNULL(CONVERT(varchar(30), [RelatedCase].[BirthDate], 103), ISNULL(CONVERT(varchar(2),[CaseRelation].[BirthDay]) +'/', '') + ISNULL(convert(varchar(2),[CaseRelation].[BirthMonth]) + '/', '') + CONVERT(varchar(4),[CaseRelation].[BirthYear])) AS [RelBirthDate],
		ISNULL([luCaseFate].[Description], [luRelationFate].[Description]) AS [RelFate],
		ISNULL([RelatedCase].[SlaughterDate], [CaseRelation].[LeftDate]) AS [LeftDate],
		NULL, -- no name for offspring
		CASE WHEN [RelatedCase].[RBSE] IS NOT NULL THEN ISNULL([RelatedCase].[EartagCountry], '') + ISNULL([RelatedCase].[EartagHerdmark] + ' ', '') + ISNULL([RelatedCase].[Eartag], '') ELSE ISNULL([CaseRelation].[EartagCountry], '') + ISNULL([CaseRelation].[EartagHerdmark] + ' ', '') + ISNULL([CaseRelation].[Eartag], '') END AS [RelEartag],
		LEFT([CaseRelation].[RelationRBSE], 2) + '/' + SUBSTRING([CaseRelation].[RelationRBSE], 3, 2) + '/' + RIGHT([CaseRelation].[RelationRBSE], 5) AS [RelationRBSE]
	FROM
		[CaseRelation] INNER JOIN [Case] ON [CaseRelation].[RBSE] = [Case].[RBSE]
		INNER JOIN [luRelationType] ON [CaseRelation].[RelationType] = [luRelationType].[Code] 
		LEFT JOIN [luSex] ON [CaseRelation].[Sex] = [luSex].[Code]
		LEFT JOIN [luRelationFate] ON [CaseRelation].[RelationFate] = [luRelationFate].[Code]
		LEFT JOIN [Case] [RelatedCase] ON [CaseRelation].[RelationRBSE] = [RelatedCase].[RBSE]
		LEFT JOIN [luSex] [RelatedCaseSex] ON [RelatedCase].[Sex] = [RelatedCaseSex].[Code]
		LEFT JOIN [luCaseFate] ON [RelatedCase].[Fate] = [luCaseFate].[Code]
	WHERE
		[CaseRelation].[RBSE] LIKE @RBSE + '%' AND
		@Name = '' AND
		CASE WHEN [RelatedCase].[RBSE] IS NOT NULL THEN ISNULL([RelatedCase].[EartagCountry], '') + ISNULL([RelatedCase].[EartagHerdmark] + ' ', '') + ISNULL([RelatedCase].[Eartag], '') ELSE ISNULL([CaseRelation].[EartagCountry], '') + ISNULL([CaseRelation].[EartagHerdmark] + ' ', '') + ISNULL([CaseRelation].[Eartag], '') END LIKE @Eartag + '%' AND
		ISNULL([CaseRelation].[RelationRBSE], '') LIKE @RelationRBSE + '%' AND
		[CaseRelation].[RelationType] LIKE @RelationType + '%' 
		

	INSERT INTO @ttblRelatedAnimals
	SELECT
		LEFT([CasePedigree].[RBSE], 2) + '/' + SUBSTRING([CasePedigree].[RBSE], 3, 2) + '/' + RIGHT([CasePedigree].[RBSE], 5) AS [RBSE],
		LEFT([Case].[CPHH], 2) + '/' + SUBSTRING([Case].[CPHH], 3, 3) + '/' + SUBSTRING([Case].[CPHH], 6, 4) + '/' + RIGHT([Case].[CPHH], 2) AS [CPHH],
		'Dam' AS [RelationType],
		ISNULL([DamCaseSex].[Description],[luSex].[Description]) AS [RelSex],
		ISNULL([Case].[EartagCountry], '') + ISNULL([Case].[EartagHerdmark] + ' ', '') + ISNULL([Case].[Eartag], '') AS [Eartag],
		ISNULL(CONVERT(varchar(30), [DamCase].[BirthDate], 103), ISNULL(CONVERT(varchar(2),[DamPedigree].[BirthDay]) +'/', '') + ISNULL(CONVERT(varchar(2),[DamPedigree].[BirthMonth]) + '/', '') + CONVERT(varchar(4),[DamPedigree].[BirthYear])) AS [RelBirthDate],
		[luCaseFate].[Description] AS [RelFate],
		[DamCase].[SlaughterDate]  AS [LeftDate],
		[DamPedigree].[Name],
		CASE WHEN [DamCase].[RBSE] IS NOT NULL THEN  ISNULL([DamCase].[EartagCountry], '') + ISNULL([DamCase].[EartagHerdmark] + ' ', '') + ISNULL([DamCase].[Eartag], '') ELSE [DamPedigree].[Eartag] END AS [Eartag],
		LEFT([DamPedigree].[RBSE], 2) + '/' + SUBSTRING([DamPedigree].[RBSE], 3, 2) + '/' + RIGHT([DamPedigree].[RBSE], 5) AS [RelationRBSE]
	FROM
		[Pedigree] [CasePedigree] INNER JOIN [Pedigree] [DamPedigree] ON [CasePedigree].[DamID] = [DamPedigree].[ID]
		INNER JOIN [Case] ON [CasePedigree].[RBSE] = [Case].[RBSE]
		LEFT JOIN [luSex] ON [DamPedigree].[Sex] = [luSex].[Code]
		LEFT JOIN [Case] [DamCase] ON [DamPedigree].[RBSE] = [DamCase].[RBSE]
		LEFT JOIN [luSex] [DamCaseSex] ON [DamCase].[Sex] = [DamCaseSex].[Code]
		LEFT JOIN [luCaseFate] ON [DamCase].[Fate] = [luCaseFate].[Code]
	WHERE
		[CasePedigree].[RBSE] LIKE @RBSE + '%' AND
		ISNULL([DamPedigree].[Name], '') LIKE @Name + '%' AND
		CASE WHEN [DamCase].[RBSE] IS NOT NULL THEN  ISNULL([DamCase].[EartagCountry], '') + ISNULL([DamCase].[EartagHerdmark] + ' ', '') + ISNULL([DamCase].[Eartag], '') ELSE ISNULL([DamPedigree].[Eartag], '') END LIKE @Eartag + '%' AND
		ISNULL([DamPedigree].[RBSE], '') LIKE @RelationRBSE + '%' AND
		(@RelationType = 'DAM' OR @RelationType = '')
	
	INSERT INTO @ttblRelatedAnimals
	SELECT
		LEFT([CasePedigree].[RBSE], 2) + '/' + SUBSTRING([CasePedigree].[RBSE], 3, 2) + '/' + RIGHT([CasePedigree].[RBSE], 5) AS [RBSE],
		LEFT([Case].[CPHH], 2) + '/' + SUBSTRING([Case].[CPHH], 3, 3) + '/' + SUBSTRING([Case].[CPHH], 6, 4) + '/' + RIGHT([Case].[CPHH], 2) AS [CPHH],
		'Sire' AS [RelationType],
		ISNULL([SireCaseSex].[Description],[luSex].[Description]) AS [RelSex],
		ISNULL([Case].[EartagCountry], '') + ISNULL([Case].[EartagHerdmark]+ ' ', '') + ISNULL([Case].[Eartag], '') AS [Eartag],
		ISNULL(CONVERT(varchar(30), [SireCase].[BirthDate], 103), ISNULL(CONVERT(varchar(2),[SirePedigree].[BirthDay]) +'/', '') + ISNULL(CONVERT(varchar(2),[SirePedigree].[BirthMonth]) + '/', '') + CONVERT(varchar(4),[SirePedigree].[BirthYear])) AS [RelBirthDate],
		[luCaseFate].[Description] AS [RelFate],
		[SireCase].[SlaughterDate] AS [LeftDate],
		[SirePedigree].[Name],
		CASE WHEN [SireCase].[RBSE] IS NOT NULL THEN  ISNULL([SireCase].[EartagCountry], '') + ISNULL([SireCase].[EartagHerdmark] + ' ', '') + ISNULL([SireCase].[Eartag], '') ELSE [SirePedigree].[Eartag] END AS [Eartag],
		LEFT([SirePedigree].[RBSE], 2) + '/' + SUBSTRING([SirePedigree].[RBSE], 3, 2) + '/' + RIGHT([SirePedigree].[RBSE], 5) AS [RelationRBSE]
	FROM
		[Pedigree] [CasePedigree] INNER JOIN [Pedigree] [SirePedigree] ON [CasePedigree].[SireID] = [SirePedigree].[ID]
		INNER JOIN [Case] ON [CasePedigree].[RBSE] = [Case].[RBSE]
		LEFT JOIN [luSex] ON [SirePedigree].[Sex] = [luSex].[Code]
		LEFT JOIN [Case] [SireCase] ON [SirePedigree].[RBSE] = [SireCase].[RBSE]
		LEFT JOIN [luSex] [SireCaseSex] ON [SireCase].[Sex] = [SireCaseSex].[Code]
		LEFT JOIN [luCaseFate] ON [SireCase].[Fate] = [luCaseFate].[Code]
	WHERE
		[CasePedigree].[RBSE] LIKE @RBSE + '%' AND
		ISNULL([SirePedigree].[Name], '') LIKE @Name + '%' AND
		CASE WHEN [SireCase].[RBSE] IS NOT NULL THEN  ISNULL([SireCase].[EartagCountry], '') + ISNULL([SireCase].[EartagHerdmark] + ' ', '') + ISNULL([SireCase].[Eartag], '') ELSE ISNULL([SirePedigree].[Eartag], '') END LIKE @Eartag + '%' AND
		ISNULL([SirePedigree].[RBSE], '') LIKE @RelationRBSE + '%' AND
		(@RelationType = 'SIRE' OR @RelationType = '')

	SELECT
		RBSE,
		CPHH,
		RelationType,
		RelSex,
		Eartag,
		RelBirthDate,
		RelFate,
		LeftDate,
		RelName,
		RelEartag,
		RelationRBSE 
	FROM
		@ttblRelatedAnimals

	SET NOCOUNT OFF

 GO