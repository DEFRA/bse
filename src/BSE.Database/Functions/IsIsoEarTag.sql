CREATE FUNCTION dbo.IsIsoEarTag
(
	@EartagCountry VARCHAR(4),
	@EarTagHerdmark VARCHAR(8),
	@EarTag VARCHAR(25)
)
RETURNS BIT
AS
BEGIN
	DECLARE @IsoFormatEarTag BIT
	DECLARE @CompleteEarTag VARCHAR(MAX)

	SET @CompleteEarTag = ISNULL(@EartagCountry, '') + ISNULL(@EarTagHerdmark, '') + ISNULL(@EarTag, '')

	IF LEN(@CompleteEarTag) = 14 OR LEN(@CompleteEarTag) = 15 BEGIN 
		RETURN 1
	END

	RETURN 0
END
