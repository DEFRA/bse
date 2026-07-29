

CREATE PROCEDURE GetClinicalByBatchID 
	@BatchID int
AS
	
SELECT
	[CaseClinical].[RBSE],
	LEFT([Case].[RBSE], 2) + '/' + SUBSTRING([Case].[RBSE], 3, 2) + '/' + RIGHT([Case].[RBSE], 5) AS [DisplayRBSE],
	CONVERT(char(1), [CaseClinical].[Apprehension]) AS Apprehension,
	CONVERT(char(1),[CaseClinical].[HypersensitiveTouch]) AS HypersensitiveTouch,
	CONVERT(char(1),[CaseClinical].[HypersensitiveSound]) AS HypersensitiveSound,
	CONVERT(char(1),[CaseClinical].[Maniacal]) AS Maniacal,
	CONVERT(char(1),[CaseClinical].[PanicStricken]) AS PanicStricken,
	CONVERT(char(1),[CaseClinical].[TemperamentChange]) AS TemperamentChange,
	CONVERT(char(1),[CaseClinical].[AbnormalHeadCarriage]) AS AbnormalHeadCarriage,
	CONVERT(char(1),[CaseClinical].[EarTwitching]) AS EarTwitching,
	CONVERT(char(1),[CaseClinical].[EarsOddAngle]) AS EarsOddAngle,
	CONVERT(char(1),[CaseClinical].[AbnormalBehaviour]) AS AbnormalBehaviour, 
	CONVERT(char(1),[CaseClinical].[HeadShyness]) AS HeadShyness, 
	CONVERT(char(1),[CaseClinical].[LickingFlank]) AS LickingFlank, 
	CONVERT(char(1),[CaseClinical].[LickingNose]) AS LickingNose, 
	CONVERT(char(1),[CaseClinical].[Kicking]) AS Kicking,
	CONVERT(char(1),[CaseClinical].[ReluctantDoorways]) AS ReluctantDoorways,
	CONVERT(char(1),[CaseClinical].[HeadPressing]) AS HeadPressing,
	CONVERT(char(1),[CaseClinical].[HeadRubbing]) AS HeadRubbing,
	CONVERT(char(1),[CaseClinical].[TeethGrinding]) AS TeethGrinding,
	CONVERT(char(1),[CaseClinical].[Blindness]) AS Blindness,
	CONVERT(char(1),[CaseClinical].[Circling]) AS Circling,
	CONVERT(char(1),[CaseClinical].[HindAtaxia]) AS HindAtaxia,
	CONVERT(char(1),[CaseClinical].[Falling]) AS Falling,
	CONVERT(char(1),[CaseClinical].[Paresis]) AS Paresis,
	CONVERT(char(1),[CaseClinical].[ForeAtaxia]) AS ForeAtaxia,
	CONVERT(char(1),[CaseClinical].[Recumbent]) AS Recumbent,
	CONVERT(char(1),[CaseClinical].[Tremor]) AS Tremor,
	CONVERT(char(1),[CaseClinical].[KnucklingFetlock]) AS KnucklingFetlock,
	CONVERT(char(1),[CaseClinical].[WeightLoss]) AS WeightLoss,
	CONVERT(char(1),[CaseClinical].[ConditionLoss]) AS ConditionLoss,
	CONVERT(char(1),[CaseClinical].[MilkYield]) AS MilkYield,
	[Case].[CPHH],
	LEFT([Case].[CPHH], 2) + '/' + SUBSTRING([Case].[CPHH], 3, 3) + '/' + SUBSTRING([Case].[CPHH], 6, 4) + '/' + RIGHT([Case].[CPHH], 2) AS [DisplayCPHH]
FROM
	[CaseClinical] INNER JOIN [lnkBatchCase] ON [lnkBatchCase].[RBSE] = [CaseClinical].[RBSE]
	INNER JOIN [Case] on [Case].[RBSE] = [CaseClinical].[RBSE]
	
WHERE [lnkBatchCase].[BatchID] = @BatchID
