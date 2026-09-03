
CREATE PROCEDURE AddCaseFeed
	@RBSE char(9),
	@YearFrom smallint,
	@YearTo smallint,
	@RationType varchar(2),
	@SupplierID int,
	@RationName varchar(60),
	@IsPrePurchase bit AS
	
	INSERT INTO [CaseFeed]
		(
		RBSE,
		YearFrom,
		YearTo,
		RationType,
		SupplierID,
		RationName,
		IsPrePurchase
		)
	VALUES
		(
		@RBSE,
		@YearFrom,
		@YearTo,
		@RationType,
		@SupplierID,
		@RationName,
		@IsPrePurchase
		)
