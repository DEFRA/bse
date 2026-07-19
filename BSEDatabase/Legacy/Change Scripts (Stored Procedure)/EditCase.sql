set ANSI_NULLS ON
set QUOTED_IDENTIFIER ON
go






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
	@LabComment varchar(255) AS
	
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
		[LabComment] = @LabComment
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













