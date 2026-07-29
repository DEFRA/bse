
CREATE PROCEDURE AddCaseClinical
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
	@MilkYield bit AS
	
	DECLARE @ErrorCode int

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
	VALUES
		(
		@RBSE,
		@Apprehension,
		@HypersensitiveTouch,
		@HypersensitiveSound,
		@Maniacal,
		@PanicStricken, 
		@TemperamentChange, 
		@AbnormalHeadCarriage, 
		@EarTwitching, 
		@EarsOddAngle, 
		@AbnormalBehaviour, 
		@HeadShyness, 
		@LickingFlank, 
		@LickingNose, 
		@Kicking, 
		@ReluctantDoorways, 
		@HeadPressing, 
		@HeadRubbing, 
		@TeethGrinding, 
		@Blindness, 
		@Circling, 
		@HindAtaxia, 
		@Falling, 
		@Paresis, 
		@ForeAtaxia, 
		@Recumbent, 
		@Tremor, 
		@KnucklingFetlock, 
		@WeightLoss, 
		@ConditionLoss, 
		@MilkYield
		)

	SET @ErrorCode = @@ERROR

	IF @ErrorCode <> 0 BEGIN
		RETURN 1
	END

	RETURN 0
