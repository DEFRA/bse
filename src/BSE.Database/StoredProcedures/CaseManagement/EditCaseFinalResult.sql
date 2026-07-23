

CREATE PROCEDURE [dbo].[EditCaseFinalResult]
	@RBSE char(9),
	@FinalResult varchar(5),
	@FinalResultDate datetime,
	@RetrospectiveTestType varchar(10),
	@RetrospectiveResult varchar(5),
	@RetrospectiveResultDate datetime,
	@RetrospectiveComment varchar(500),
	@Rowstamp timestamp,
	@UserID int,
	@AlternateDiagnosis varchar(255),
	@LabComment varchar(255),
	@DBSE char(7) OUTPUT
 AS

DECLARE
	@RowCount int,
	@ErrorCode int,
	@TwoDigitYear varchar(2),
	@NextDBSENumber int,
	@NextDBSENumberLength int

	BEGIN TRANSACTION

	SELECT
		[Rowstamp]
	FROM
		[Case]
	WHERE
		[RBSE] = @RBSE AND [Rowstamp] = @Rowstamp

	SET @RowCount = @@ROWCOUNT
	
	IF @RowCount = 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 1
	END

	IF @FinalResult = 'Pos' BEGIN
		--get the next DBSE
		SET @TwoDigitYear = SUBSTRING(@RBSE, 3, 2)
	
		SELECT
			@NextDBSENumber = ISNULL(MAX(CONVERT(int,RIGHT([DBSE],5))),0) + 1
		FROM
			[Case]
		WHERE
			LEFT([DBSE],2) = @TwoDigitYear
	
		SET @NextDBSENumberLength = LEN(CONVERT(varchar(5), @NextDBSENumber))
	
		SET @DBSE = @TwoDigitYear + STUFF('00000', 6 - @NextDBSENumberLength, @NextDBSENumberLength, CONVERT(varchar(5), @NextDBSENumber))

		SET @ErrorCode = @@ERROR
		IF  @ErrorCode <> 0 BEGIN
			ROLLBACK TRANSACTION
			RETURN 2
		END

		SET @ErrorCode = @@ERROR
		IF  @ErrorCode <> 0 BEGIN
			ROLLBACK TRANSACTION
			RETURN 3
		END
	END ELSE BEGIN
		SET @DBSE = NULL
	END

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
	SELECT
		'Case',
		'FinalResult',
		@UserID,
		[FinalResult],
		@FinalResult,
		'Amendment',
		[RBSE]
	FROM
		[Case]
	WHERE
		[RBSE] = @RBSE AND
		(([FinalResult] != @FinalResult) OR ([FinalResult] IS NULL AND @FinalResult IS NOT NULL) OR ([FinalResult] IS NOT NULL AND @FinalResult IS NULL))

	SET @ErrorCode = @@ERROR

	IF  @ErrorCode <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 5
	END

	-- perform the update
	UPDATE
		[Case]
	SET
		[FinalResult] = @FinalResult,
		[FinalResultDate] = @FinalResultDate,
		[RetrospectiveTestType] = @RetrospectiveTestType,
		[RetrospectiveResult] = @RetrospectiveResult,
		[RetrospectiveResultDate] = @RetrospectiveResultDate,
		[RetrospectiveComment] = @RetrospectiveComment,
		[AlternateDiagnosis] = @AlternateDiagnosis,
		[LabComment] = @LabComment,
		[DBSE] = @DBSE
	WHERE
		[RBSE] = @RBSE

	SET @ErrorCode = @@ERROR

	IF  @ErrorCode <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 4
	END

COMMIT TRANSACTION
