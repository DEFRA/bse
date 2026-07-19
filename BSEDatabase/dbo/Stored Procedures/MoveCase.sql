
CREATE PROCEDURE MoveCase
	@RBSE char(9),
	@NewCPHH char(11),
	@UserID int
AS

DECLARE
	@RowCount int,
	@ErrorCode int,
	@OldCPHH char(11)

BEGIN TRANSACTION

	SELECT
		[CPHH]
	FROM
		[Farm]
	WHERE
		[CPHH] = @NewCPHH

	SET @RowCount = @@ROWCOUNT
	
	IF @RowCount = 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 1
	END

	SELECT
		@OldCPHH = [CPHH]
	FROM
		[Case]
	WHERE
		[RBSE] = @RBSE

	SET @RowCount = @@ROWCOUNT
	
	IF @RowCount = 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 2
	END

	SELECT
		[RBSE]
	FROM
		[Case]
	WHERE
		[CPHH] = @OldCPHH

	SET @RowCount = @@ROWCOUNT
	
	IF @RowCount = 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 3
	END

	SET NOCOUNT ON	

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
	VALUES
		(
		'Case',
		'CPHH',
		@UserID,
		LEFT(@OldCPHH, 2) + '/' + SUBSTRING(@OldCPHH, 3, 3) + '/' + SUBSTRING(@OldCPHH, 6, 4) + '/' + RIGHT(@OldCPHH, 2) ,
		LEFT(@NewCPHH, 2) + '/' + SUBSTRING(@NewCPHH, 3, 3) + '/' + SUBSTRING(@NewCPHH, 6, 4) + '/' + RIGHT(@NewCPHH, 2) ,
		'Case Move',
		@RBSE
		)

	SET @ErrorCode = @@ERROR

	IF  @ErrorCode <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 4
	END
	
	UPDATE
		[Case]
	SET
		[CPHH] = @NewCPHH
	WHERE
		[RBSE] = @RBSE
	
	SET @ErrorCode = @@ERROR

	IF  @ErrorCode <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 5
	END

	IF @RowCount = 1 BEGIN

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
			LEFT(@OldCPHH, 2) + '/' + SUBSTRING(@OldCPHH, 3, 3) + '/' + SUBSTRING(@OldCPHH, 6, 4) + '/' + RIGHT(@OldCPHH, 2) ,
			'Deletion',
			@OldCPHH
			)
	
		SET @ErrorCode = @@ERROR
	
		IF  @ErrorCode <> 0 BEGIN
			ROLLBACK TRANSACTION
			RETURN 6
		END

		DELETE FROM
			[Farm]
		WHERE
			[CPHH] = @OldCPHH
	
		SET @ErrorCode = @@ERROR
		
		IF  @ErrorCode <> 0 BEGIN
			ROLLBACK TRANSACTION
			RETURN 6
		END
	END
	
COMMIT TRANSACTION
