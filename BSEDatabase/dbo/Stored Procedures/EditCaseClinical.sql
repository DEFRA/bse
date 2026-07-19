
CREATE PROCEDURE EditCaseClinical
	@RBSE char(9),
	@Apprehension bit,
	@HypersensitiveTouch bit,
	@HypersensitiveSound bit,
	@Maniacal bit,
	@PanicStricken bit,
	@TemperamentChange bit,
	@AbnormalHeadCarriage bit,
	@EarTwitching bit,
	@EarsOddAngle bit,
	@AbnormalBehaviour bit,
	@HeadShyness bit,
	@LickingFlank bit,
	@LickingNose bit,
	@Kicking bit,
	@ReluctantDoorways bit,
	@HeadPressing bit,
	@HeadRubbing bit,
	@TeethGrinding bit,
	@Blindness bit,
	@Circling bit,
	@HindAtaxia bit,
	@Falling bit,
	@Paresis bit,
	@ForeAtaxia bit,
	@Recumbent bit,
	@Tremor bit,
	@KnucklingFetlock bit,
	@WeightLoss bit,
	@ConditionLoss bit,
	@MilkYield bit,
	@RowStamp timestamp AS

	DECLARE
		@RowCount int,
		@ErrorCode int

	UPDATE
		[CaseClinical]
	SET
		[Apprehension] = @Apprehension, 
		[HypersensitiveTouch] = @HypersensitiveTouch, 
		[HypersensitiveSound] = @HypersensitiveSound, 
		[Maniacal] = @Maniacal, 
		[PanicStricken] = @PanicStricken, 
		[TemperamentChange] = @TemperamentChange, 
		[AbnormalHeadCarriage] = @AbnormalHeadCarriage, 
		[EarTwitching] = @EarTwitching, 
		[EarsOddAngle] = @EarsOddAngle, 
		[AbnormalBehaviour] = @AbnormalBehaviour, 
		[HeadShyness] = @HeadShyness, 
		[LickingFlank] = @LickingFlank,
		[LickingNose] = @LickingNose, 
		[Kicking] = @Kicking, 
		[ReluctantDoorways] = @ReluctantDoorways,
		[HeadPressing] = @HeadPressing, 
		[HeadRubbing] = @HeadRubbing, 
		[TeethGrinding]= @TeethGrinding, 
		[Blindness] = @Blindness, 
		[Circling] = @Circling, 
		[HindAtaxia] = @HindAtaxia,
		[Falling] = @Falling, 
		[Paresis] = @Paresis, 
		[ForeAtaxia] = @ForeAtaxia, 
		[Recumbent] = @Recumbent, 
		[Tremor] = @Tremor, 
		[KnucklingFetlock] = @KnucklingFetlock, 
		[WeightLoss] = @WeightLoss, 
		[ConditionLoss] = @ConditionLoss, 
		[MilkYield] = @MilkYield 
	WHERE
		[RBSE] = @RBSE AND
		[RowStamp] = @RowStamp

	SELECT
		@RowCount = @@ROWCOUNT,
		@ErrorCode = @@ERROR

	IF @RowCount <> 1 BEGIN
		RETURN 1
	END

	IF @ErrorCode <> 0 BEGIN
		RETURN 2
	END

	RETURN 0
