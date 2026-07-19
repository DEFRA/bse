


CREATE PROCEDURE [dbo].[ChangeRBSE]
	@OldRBSE char(9),
	@NewRBSE char(9),
	@UserID int
AS

DECLARE
	@RowCount int,
	@ErrorCode int

BEGIN TRANSACTION

	SELECT
		[RBSE]
	FROM
		[Case]
	WHERE
		[RBSE] = @OldRBSE

	SET @RowCount = @@ROWCOUNT
	
	IF @RowCount = 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 1
	END

	SELECT
		[RBSE]
	FROM
		[Case]
	WHERE
		[RBSE] = @NewRBSE

	SET @RowCount = @@ROWCOUNT
	
	IF @RowCount <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 2
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
	VALUES
		(
		'Case',
		'RBSE',
		@UserID,
		LEFT(@OldRBSE, 2) + '/' + SUBSTRING(@OldRBSE, 3, 2) + '/' + RIGHT(@OldRBSE, 5),
		LEFT(@NewRBSE, 2) + '/' + SUBSTRING(@NewRBSE, 3, 2) + '/' + RIGHT(@NewRBSE, 5),
		'RBSE Change',
		@NewRBSE
		)

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
			[LabComment]
		)
	SELECT
			@NewRBSE,
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
			[LabComment]
	FROM
		[Case]
	WHERE
		[RBSE] = @OldRBSE
	
	SET @ErrorCode = @@ERROR

	IF  @ErrorCode <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 3
	END
	
	UPDATE
		[Pedigree]
	SET
		[RBSE] = @NewRBSE
	WHERE
		[RBSE] = @OldRBSE
	
	SET @ErrorCode = @@ERROR

	IF  @ErrorCode <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 4
	END

	UPDATE
		[CaseHistorical]
	SET
		[RBSE] = @NewRBSE
	WHERE
		[RBSE] = @OldRBSE

	SET @ErrorCode = @@ERROR

	IF  @ErrorCode <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 5
	END

	UPDATE
		[CaseRelation]
	SET
		[RBSE] = @NewRBSE
	WHERE
		[RBSE] = @OldRBSE
	
	SET @ErrorCode = @@ERROR

	IF  @ErrorCode <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 6
	END

	UPDATE
		[CaseRelation]
	SET
		[RelationRBSE] = @NewRBSE
	WHERE
		[RelationRBSE] = @OldRBSE
	
	SET @ErrorCode = @@ERROR

	IF  @ErrorCode <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 7
	END

	UPDATE
		[CaseBAB]
	SET
		[RBSE] = @NewRBSE
	WHERE
		[RBSE] = @OldRBSE
	
	SET @ErrorCode = @@ERROR

	IF  @ErrorCode <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 8
	END

	UPDATE
		[CaseFeed]
	SET
		[RBSE] = @NewRBSE
	WHERE
		[RBSE] = @OldRBSE
	
	SET @ErrorCode = @@ERROR

	IF  @ErrorCode <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 9
	END

	UPDATE
		[OtherOwner]
	SET
		[RBSE] = @NewRBSE
	WHERE
		[RBSE] = @OldRBSE
	
	SET @ErrorCode = @@ERROR

	IF  @ErrorCode <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 10
	END

	UPDATE
		[CaseTest]
	SET
		[RBSE] = @NewRBSE
	WHERE
		[RBSE] = @OldRBSE
	
	SET @ErrorCode = @@ERROR

	IF  @ErrorCode <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 14
	END

	INSERT INTO [CaseClinical]
		(
			[RBSE],
			[Apprehension],
			[HypersensitiveTouch],
			[HypersensitiveSound],
			[Maniacal],
			[PanicStricken],
			[TemperamentChange],
	 		[AbnormalHeadCarriage],
			[EarTwitching], 
			[EarsOddAngle], 
			[AbnormalBehaviour],
			[HeadShyness],
			[LickingFlank],
			[LickingNose], 
			[Kicking], 
			[ReluctantDoorways], 
			[HeadPressing], 
			[HeadRubbing], 
			[TeethGrinding], 
			[Blindness], 
			[Circling], 
			[HindAtaxia], 
			[Falling], 
			[Paresis], 
			[ForeAtaxia],
			[Recumbent],
			[Tremor], 
			[KnucklingFetlock],
			[WeightLoss],
			[ConditionLoss],
			[MilkYield]
		)
	SELECT
			@NewRBSE,
			[Apprehension],
			[HypersensitiveTouch],
			[HypersensitiveSound],
			[Maniacal],
			[PanicStricken],
			[TemperamentChange],
	 		[AbnormalHeadCarriage],
			[EarTwitching], 
			[EarsOddAngle], 
			[AbnormalBehaviour],
			[HeadShyness],
			[LickingFlank],
			[LickingNose], 
			[Kicking], 
			[ReluctantDoorways], 
			[HeadPressing], 
			[HeadRubbing], 
			[TeethGrinding], 
			[Blindness], 
			[Circling], 
			[HindAtaxia], 
			[Falling], 
			[Paresis], 
			[ForeAtaxia],
			[Recumbent],
			[Tremor], 
			[KnucklingFetlock],
			[WeightLoss],
			[ConditionLoss],
			[MilkYield]
	FROM
		[CaseClinical]
	WHERE
		[RBSE] = @OldRBSE
	
	SET @ErrorCode = @@ERROR

	IF  @ErrorCode <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 11
	END

	UPDATE
		[ClinicalVisit]
	SET
		[RBSE] = @NewRBSE
	WHERE
		[RBSE] = @OldRBSE
	
	SET @ErrorCode = @@ERROR

	IF  @ErrorCode <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 11
	END

	DELETE FROM
		[CaseClinical]
	WHERE
		[RBSE] = @OldRBSE

	SET @ErrorCode = @@ERROR
	
	IF  @ErrorCode <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 11
	END

INSERT INTO [CaseWork]
	(
		   [RBSE],
           [RBSEDate],
           [Barcode],
           [AHFReference],
           [PurchaserBSE1ReceivedDate],
           [BreederBSE1ReceivedDate],
           [Vendor1BSE1ReceivedDate],
           [HomebredBSE1ReceivedDate],
           [SummarySheetReceivedDate],
           [PaperworkCompleteDate],
           [ActiveMemoDate],
           [AnnexADate],
           [AnnexBDate],
           [AnnexCDate],
           [AnnexDDate],
           [RegionalLab],
           [ReceivedByRegionalLabDate],
           [InitialReceivedDate],
           [FinalReceivedDate],
           [FinalSentDate],
           [LabChasedDate],
           [BarbMinuteSentDate],
           [Post2000SentDate],
           [CaseWorkNotes],
           [DataCompleteDate],
           [IsCaseClosed]
	)
SELECT 
           @NewRBSE,
           [RBSEDate],
           [Barcode],
           [AHFReference],
           [PurchaserBSE1ReceivedDate],
           [BreederBSE1ReceivedDate],
           [Vendor1BSE1ReceivedDate],
           [HomebredBSE1ReceivedDate],
           [SummarySheetReceivedDate],
           [PaperworkCompleteDate],
           [ActiveMemoDate],
           [AnnexADate],
           [AnnexBDate],
           [AnnexCDate],
           [AnnexDDate],
           [RegionalLab],
           [ReceivedByRegionalLabDate],
           [InitialReceivedDate],
           [FinalReceivedDate],
           [FinalSentDate],
           [LabChasedDate],
           [BarbMinuteSentDate],
           [Post2000SentDate],
           [CaseWorkNotes],
           [DataCompleteDate],
           [IsCaseClosed]
FROM 
	[CaseWork]
WHERE
	[RBSE] = @OldRBSE

    SET @ErrorCode = @@ERROR

	IF  @ErrorCode <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 15
	END

DELETE FROM
		[CaseWork]
	WHERE
		[RBSE] = @OldRBSE

	SET @ErrorCode = @@ERROR
	
	IF  @ErrorCode <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 15
	END

	UPDATE
		[lnkBatchCase]
	SET
		[RBSE] = @NewRBSE
	WHERE
		[RBSE] = @OldRBSE
	
	SET @ErrorCode = @@ERROR

	IF  @ErrorCode <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 12
	END

	DELETE FROM
		[Case]
	WHERE
		[RBSE] = @OldRBSE

	SET @ErrorCode = @@ERROR
	
	IF  @ErrorCode <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 13
	END

	
COMMIT TRANSACTION




