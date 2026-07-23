
CREATE PROCEDURE AddBatchNumber
	@BatchID int OUTPUT,
	@BatchYear smallint OUTPUT,
	@BatchNumber int OUTPUT AS

	DECLARE
		@RowCount int,
		@ErrorCode int,
		@tmpBatchID int,
		@tmpBatchNumber int,
		@CurrentYear smallint

	BEGIN TRANSACTION

	SET @CurrentYear = YEAR(getdate())

	SELECT
		@tmpBatchNumber = ISNULL(MAX([BatchNumber]), 0)
	FROM
		[Batch]
	WHERE
		[BatchYear] = @CurrentYear

	SELECT
		@tmpBatchID = [BatchID]
	FROM
		[Batch]
	WHERE
		[BatchYear] = @CurrentYear AND [BatchNumber] = @tmpBatchNumber
		
	SELECT
		@ErrorCode = @@ERROR
	IF @ErrorCode <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 1
	END

	SELECT
		[RBSE]
	FROM
		[lnkBatchCase]
	WHERE
		[BatchID] = @tmpBatchID

	SELECT
		@ErrorCode = @@ERROR,
		@RowCount = @@ROWCOUNT

	IF @RowCount = 0 AND @tmpBatchNumber <> 0 BEGIN  -- Return the last Batch Number as no cases have been matched against it.
		SELECT
			@BatchID = [BatchID],
			@BatchYear = [BatchYear],
			@BatchNumber = [BatchNumber]
		FROM
			[Batch]
		WHERE
			[BatchID] = @tmpBatchID

	END ELSE BEGIN	--Create the next Batch Number
		INSERT INTO [Batch]
			(
			[BatchYear],
			[BatchNumber]
			)
		SELECT
			@CurrentYear,
			ISNULL(MAX([BatchNumber]),0) + 1  -- add 1 to the highest existing batch number for this year.  If this year has no batches, start with 1
		FROM
			[Batch]
		WHERE
			[BatchYear] = @CurrentYear
	
		SELECT
			@ErrorCode = @@ERROR,
			@RowCount = @@ROWCOUNT,	
		 	@BatchID = SCOPE_IDENTITY()
	
		IF @RowCount <> 1 OR @ErrorCode <> 0 BEGIN
			ROLLBACK TRANSACTION
			RETURN 1
		END
	
		SELECT
			@BatchYear = [BatchYear],
			@BatchNumber = [BatchNumber]
		FROM
			[Batch]
		WHERE
			[BatchID] = @BatchID
	
		SELECT
			@ErrorCode = @@ERROR,
			@RowCount = @@ROWCOUNT
	
		IF @RowCount <> 1 OR @ErrorCode <> 0 BEGIN
			ROLLBACK TRANSACTION
			RETURN 2
		END

	END		
	COMMIT TRANSACTION
	RETURN 0
