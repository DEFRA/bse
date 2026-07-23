
CREATE PROCEDURE GetClinicalByRBSE
	@RBSE char(9)
AS

SELECT
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
	[MilkYield],
	[RowStamp]
FROM
	[CaseClinical]
WHERE
	RBSE = @RBSE

RETURN
