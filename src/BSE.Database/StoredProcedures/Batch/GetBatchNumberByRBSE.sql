
CREATE PROCEDURE GetBatchNumberByRBSE
	@RBSE char(9)
AS

	SELECT
		[Batch].[BatchID],
		ISNULL(CONVERT(varchar(10),[Batch].[BatchYear]), '') + '/' + ISNULL(CONVERT(varchar(10),[Batch].[BatchNumber]), '') AS BatchNumber,
		[lnkBatchCase].[RBSE],
		[lnkBatchCase].[Document] 
	FROM
		[lnkBatchCase] INNER JOIN [Batch] ON [lnkBatchCase].[BatchID] = [Batch].[BatchID] 
	WHERE
		RBSE =@RBSE

RETURN
