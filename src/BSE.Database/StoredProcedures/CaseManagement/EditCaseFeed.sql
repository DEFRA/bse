
CREATE PROCEDURE EditCaseFeed
	@ID int,
	@YearFrom smallint,
	@YearTo smallint,
	@RationType varchar(2),
	@SupplierID int,
	@RationName varchar(60),
	@IsPrePurchase bit,
	@RowStamp timestamp AS
	
	UPDATE
		[CaseFeed]
	SET
		[YearFrom] = @YearFrom,
		[YearTo] = @YearTo,
		[RationType] = @RationType,
		[SupplierID] = @SupplierID,
		[RationName] = @RationName,
		[IsPrePurchase] = @IsPrePurchase 
	WHERE
		[ID] = @ID AND
		[RowStamp] = @RowStamp
