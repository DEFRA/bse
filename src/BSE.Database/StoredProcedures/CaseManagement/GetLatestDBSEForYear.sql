
CREATE PROCEDURE GetLatestDBSEForYear
	@TwoDigitYear char(2),
	@LatestDBSE char(7) OUTPUT AS

	DECLARE
		@LatestNumber int,
		@LatestNumberLength int

	SELECT
		@LatestNumber = MAX(CONVERT(int, RIGHT([DBSE], 5)))
	FROM
		[Case]
	WHERE
		SUBSTRING([DBSE], 1, 2) = @TwoDigitYear

	SET @LatestNumberLength = LEN(CONVERT(varchar(5), @LatestNumber))

	SELECT
		 @LatestDBSE = @TwoDigitYear + STUFF('00000', 6 - @LatestNumberLength, @LatestNumberLength, CONVERT(varchar(5), @LatestNumber))
