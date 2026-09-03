
CREATE PROCEDURE AddFarmRelation
	@CPHH char(11),
	@RelatedCPHH char(11) AS

	INSERT INTO [FarmRelation]
		(
		[CPHH],
		[RelatedCPHH]
		)
	VALUES
		(
		@CPHH,
		@RelatedCPHH
		)
