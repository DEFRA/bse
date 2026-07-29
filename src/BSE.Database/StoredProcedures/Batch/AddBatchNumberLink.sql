
CREATE PROCEDURE AddBatchNumberLink 
	@BatchID int,
	@RBSE char(9),
	@Document varchar(5)

AS

DECLARE
	@ErrorCode int,
	@RowCount int

	SELECT
		[BatchID]
	FROM
		[Batch]
	WHERE
		[BatchID] = @BatchID

	SELECT
		@RowCount = @@ROWCOUNT,
		@ErrorCode = @@ERROR

	IF @ErrorCode <> 0 OR @RowCount = 0 BEGIN
		RETURN 1
	END

	SELECT
		[BatchID]
	FROM
		[lnkBatchCase]
	WHERE
		[BatchID] = @BatchID AND
		[RBSE] = @RBSE AND
		[Document] = @Document

	SELECT
		@RowCount = @@ROWCOUNT,
		@ErrorCode = @@ERROR

	IF @ErrorCode <> 0 OR @RowCount <> 0 BEGIN
		RETURN 2
	END

	INSERT INTO [lnkBatchCase]
		(
		[BatchID],
		[RBSE],
		[Document]
		)
	VALUES
		(
		@BatchID,
		@RBSE,
		@Document
		)

	SET @ErrorCode = @@ERROR

	IF @ErrorCode <> 0 BEGIN
		RETURN 3
	END
