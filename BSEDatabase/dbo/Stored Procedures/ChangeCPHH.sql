
CREATE PROCEDURE [ChangeCPHH] 
	@OldCPHH char(11),
	@NewCPHH char(11),
	@UserID int
AS

DECLARE
	@RowCount int,
	@ErrorCode int

BEGIN TRANSACTION

	SELECT
		[CPHH]
	FROM
		[Farm]
	WHERE
		[CPHH] = @OldCPHH

	SET @RowCount = @@ROWCOUNT
	
	IF @RowCount = 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 1
	END

	SELECT
		[CPHH]
	FROM
		[Farm]
	WHERE
		[CPHH] = @NewCPHH

	SET @RowCount = @@ROWCOUNT
	
	IF @RowCount <> 0 BEGIN
		ROLLBACK TRANSACTION
		RETURN 1
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
		[CPHH]
		)
	VALUES
		(
		'Farm',
		'CPHH',
		@UserID,
		LEFT(@OldCPHH, 2) + '/' + SUBSTRING(@OldCPHH, 3, 3) + '/' + SUBSTRING(@OldCPHH, 6, 4) + '/' + RIGHT(@OldCPHH, 2) ,
		LEFT(@NewCPHH, 2) + '/' + SUBSTRING(@NewCPHH, 3, 3) + '/' + SUBSTRING(@NewCPHH, 6, 4) + '/' + RIGHT(@NewCPHH, 2) ,
		'CPHH Change',
		@NewCPHH
		)

	INSERT INTO [Farm]
		(
			[CPHH],
			[IsNonGBFarm],
			[OwnerName],
			[Address1],
			[Address2],
			[Address3],
			[Postcode],
			[Parish],
			[District],
			[County],
			[CorrespondenceAddress1],
			[CorrespondenceAddress2],
			[CorrespondenceAddress3],
			[CorrespondencePostcode],
			[MapReference],
			[Herdmark1],
			[Herdmark2],
			[Herdmark3],
			[NumericHerdmark1],
			[NumericHerdmark2],
			[AHO],
			[HerdType],
			[PedigreeType],
			[IsDealer],
			[ADNSRegionID]
		)
	SELECT
			@NewCPHH,
			[IsNonGBFarm],
			[OwnerName],
			[Address1],
			[Address2],
			[Address3],
			[Postcode],
			[Parish],
			[District],
			[County],
			[CorrespondenceAddress1],
			[CorrespondenceAddress2],
			[CorrespondenceAddress3],
			[CorrespondencePostcode],
			[MapReference],
			[Herdmark1],
			[Herdmark2],
			[Herdmark3],
			[NumericHerdmark1],
			[NumericHerdmark2],
			[AHO],
			[HerdType],
			[PedigreeType],
			[IsDealer],
			[ADNSRegionID]
	FROM
		[Farm]
	WHERE
		[CPHH] = @OldCPHH
	
		SET @ErrorCode = @@ERROR
	
		IF  @ErrorCode <> 0 BEGIN
			ROLLBACK TRANSACTION
			RETURN 2
		END
	
	UPDATE
		[FarmHistorical]
	SET
		[CPHH] = @NewCPHH
	WHERE
		[CPHH] = @OldCPHH
	
		SET @ErrorCode = @@ERROR
	
		IF  @ErrorCode <> 0 BEGIN
			ROLLBACK TRANSACTION
			RETURN 3
		END
	
	UPDATE
		[FarmRelation]
	SET
		[CPHH] = @NewCPHH
	WHERE
		[CPHH] = @OldCPHH
	
		SET @ErrorCode = @@ERROR
	
		IF  @ErrorCode <> 0 BEGIN
			ROLLBACK TRANSACTION
			RETURN 4
		END
	
	UPDATE
		[FarmRelation]
	SET
		[RelatedCPHH] = @NewCPHH
	WHERE
		[RelatedCPHH] = @OldCPHH
	
		SET @ErrorCode = @@ERROR
	
		IF  @ErrorCode <> 0 BEGIN
			ROLLBACK TRANSACTION
			RETURN 4
		END
	
	UPDATE
		[HerdSize]
	SET
		[CPHH] = @NewCPHH
	WHERE
		[CPHH] = @OldCPHH
	
		SET @ErrorCode = @@ERROR
	
		IF  @ErrorCode <> 0 BEGIN
			ROLLBACK TRANSACTION
			RETURN 5
		END
	
	UPDATE
		[Case]
	SET
		[CPHH] = @NewCPHH
	WHERE
		[CPHH] = @OldCPHH
	
		SET @ErrorCode = @@ERROR
	
		IF  @ErrorCode <> 0 BEGIN
			ROLLBACK TRANSACTION
			RETURN 6
		END

	UPDATE
		[OtherOwner]
	SET
		[CPHH] = @NewCPHH
	WHERE
		[CPHH] = @OldCPHH
	
		SET @ErrorCode = @@ERROR
	
		IF  @ErrorCode <> 0 BEGIN
			ROLLBACK TRANSACTION
			RETURN 7
		END
	
	DELETE FROM
		[Farm]
	WHERE
		[CPHH] = @OldCPHH
	
		SET @ErrorCode = @@ERROR
	
		IF  @ErrorCode <> 0 BEGIN
			ROLLBACK TRANSACTION
			RETURN 8
		END
	
COMMIT TRANSACTION
