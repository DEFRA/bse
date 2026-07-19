CREATE PROCEDURE DeleteCase
	@RBSE char(9),
	@UserID int
AS

DECLARE
	@RowCount int,
	@FarmRowCount int,
	@ErrorCode int,
	@CPHH char(11)

BEGIN TRANSACTION

	SELECT
		@CPHH = [CPHH]
	FROM
		[Case]
	WHERE
		[RBSE] = @RBSE

	SET @RowCount = @@ROWCOUNT
	
	IF @RowCount = 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 1
	END

/* Count the number of cases with this CPHH, if only 1 then the Farm record will be deleted too */
	SELECT
		[RBSE]
	FROM
		[Case]
	WHERE
		[CPHH] = @CPHH

	SET @FarmRowCount = @@ROWCOUNT
	
	IF @FarmRowCount = 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 2
	END

	SELECT 
		[RBSE]
	FROM
		[CaseFeed]
	WHERE
		[RBSE] = @RBSE
	
	SET @RowCount = @@ROWCOUNT
	
	IF @RowCount <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 3
	END

	SELECT 
		[RBSE]
	FROM
		[CaseClinical]
	WHERE
		[RBSE] = @RBSE
	
	SET @RowCount = @@ROWCOUNT
	
	IF @RowCount <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 3
	END

	SELECT 
		[RBSE]
	FROM
		[CaseRelation]
	WHERE
		[RBSE] = @RBSE
	
	SET @RowCount = @@ROWCOUNT
	
	IF @RowCount <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 3
	END

	SELECT 
		[RBSE]
	FROM
		[OtherOwner]
	WHERE
		[RBSE] = @RBSE
	
	SET @RowCount = @@ROWCOUNT
	
	IF @RowCount <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 3
	END

	SELECT 
		[RBSE]
	FROM
		[lnkBatchCase]
	WHERE
		[RBSE] = @RBSE
	
	SET @RowCount = @@ROWCOUNT
	
	IF @RowCount <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 3
	END

	SELECT 
		[RBSE]
	FROM
		[ClinicalVisit]
	WHERE
		[RBSE] = @RBSE
	
	SET @RowCount = @@ROWCOUNT
	
	IF @RowCount <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 3
	END

	SET NOCOUNT ON	

	INSERT INTO [AuditLog]
		(
		[TableName],
		[UserID],
		[BeforeValue],
		[Reason],
		[RBSE]
		)
	VALUES
		(
		'Case',
		@UserID,
		LEFT(@RBSE, 2) + '/' + SUBSTRING(@RBSE, 3, 2) + '/' + RIGHT(@RBSE, 5),
		'Deletion',
		@RBSE
		)

	SET @ErrorCode = @@ERROR

	IF  @ErrorCode <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 4
	END

	DELETE FROM 
		[CaseBAB]
	WHERE
		[RBSE] = @RBSE
	
	SET @ErrorCode = @@ERROR

	IF  @ErrorCode <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 5
	END

	DELETE FROM 
		[CaseTest]
	WHERE
		[RBSE] = @RBSE
	
	SET @ErrorCode = @@ERROR

	IF  @ErrorCode <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 5
	END
	
	DELETE FROM 
		[CaseWork]
	WHERE
		[RBSE] = @RBSE
	
	SET @ErrorCode = @@ERROR

	IF  @ErrorCode <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 5
	END
	
	DELETE FROM 
		[Case]
	WHERE
		[RBSE] = @RBSE
	
	SET @ErrorCode = @@ERROR

	IF  @ErrorCode <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 5
	END


	IF @FarmRowCount = 1 BEGIN

		INSERT INTO [AuditLog]
			(
			[TableName],
			[UserID],
			[BeforeValue],
			[Reason],
			[CPHH]
			)
		VALUES
			(
			'Farm',
			@UserID,
			LEFT(@CPHH, 2) + '/' + SUBSTRING(@CPHH, 3, 3) + '/' + SUBSTRING(@CPHH, 6, 4) + '/' + RIGHT(@CPHH, 2) ,
			'Deletion',
			@CPHH
			)
	
		SET @ErrorCode = @@ERROR
	
		IF  @ErrorCode <> 0 BEGIN
			ROLLBACK TRANSACTION
			RETURN 6
		END

		DELETE FROM
			[Farm]
		WHERE
			[CPHH] = @CPHH
	
		SET @ErrorCode = @@ERROR
		
		IF  @ErrorCode <> 0 BEGIN
			ROLLBACK TRANSACTION
			RETURN 6
		END
	END
	
COMMIT TRANSACTION


