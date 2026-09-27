
CREATE PROCEDURE GetBatchIDForBatch
	@BatchYear smallint,
	@BatchNumber int,
	@BatchID int OUTPUT AS

	SELECT
		@BatchID = [BatchID]
	FROM
		[Batch]
	WHERE
		([BatchYear] = @BatchYear OR ([BatchYear] IS NULL AND @BatchYear IS NULL)) AND
		[BatchNumber] = @BatchNumber

