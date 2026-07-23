


CREATE PROCEDURE [dbo].[ChangeRBSE]
	@OldRBSE char(9),
	@NewRBSE char(9),
	@UserID int
AS

/*
Refactored from @@ERROR to BEGIN TRY / BEGIN CATCH / ROLLBACK TRANSACTION.
Return codes: 0=success, 1=OldRBSE not found, 2=NewRBSE already exists.
Business rule pre-checks retain specific codes; SQL errors propagate via THROW.
*/
BEGIN TRY
	BEGIN TRANSACTION

	IF NOT EXISTS (SELECT [RBSE] FROM [Case] WHERE [RBSE] = @OldRBSE)
	BEGIN ROLLBACK TRANSACTION; RETURN 1 END

	IF EXISTS (SELECT [RBSE] FROM [Case] WHERE [RBSE] = @NewRBSE)
	BEGIN ROLLBACK TRANSACTION; RETURN 2 END

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
	
	
	UPDATE
		[Pedigree]
	SET
		[RBSE] = @NewRBSE
	WHERE
		[RBSE] = @OldRBSE
	

	UPDATE
		[CaseHistorical]
	SET
		[RBSE] = @NewRBSE
	WHERE
		[RBSE] = @OldRBSE

	UPDATE
		[CaseRelation]
	SET
		[RBSE] = @NewRBSE
	WHERE
		[RBSE] = @OldRBSE
	

	UPDATE
		[CaseRelation]
	SET
		[RelationRBSE] = @NewRBSE
	WHERE
		[RelationRBSE] = @OldRBSE
	

	UPDATE
		[CaseBAB]
	SET
		[RBSE] = @NewRBSE
	WHERE
		[RBSE] = @OldRBSE
	

	UPDATE
		[CaseFeed]
	SET
		[RBSE] = @NewRBSE
	WHERE
		[RBSE] = @OldRBSE
	

	UPDATE
		[OtherOwner]
	SET
		[RBSE] = @NewRBSE
	WHERE
		[RBSE] = @OldRBSE
	

	UPDATE
		[CaseTest]
	SET
		[RBSE] = @NewRBSE
	WHERE
		[RBSE] = @OldRBSE
	

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
	

	UPDATE
		[ClinicalVisit]
	SET
		[RBSE] = @NewRBSE
	WHERE
		[RBSE] = @OldRBSE
	

	DELETE FROM
		[CaseClinical]
	WHERE
		[RBSE] = @OldRBSE

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

DELETE FROM
		[CaseWork]
	WHERE
		[RBSE] = @OldRBSE

	UPDATE
		[lnkBatchCase]
	SET
		[RBSE] = @NewRBSE
	WHERE
		[RBSE] = @OldRBSE
	

	DELETE FROM
		[Case]
	WHERE
		[RBSE] = @OldRBSE

        COMMIT TRANSACTION
        RETURN 0

END TRY
BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION
        THROW
END CATCH
