
CREATE PROCEDURE GetLatestRBSEForYear
	@TwoDigitYear char(2),
	@LatestRBSE char(9) OUTPUT AS

	DECLARE
		@LatestNumber int,
		@LatestNumberLength int

	SELECT
		@LatestNumber = MAX(CONVERT(int, RIGHT([RBSE], 5)))
	FROM
		[Case]
	WHERE
		SUBSTRING([RBSE], 3, 2) = @TwoDigitYear

	SET @LatestNumberLength = LEN(CONVERT(varchar(5), @LatestNumber))

	SELECT
		 @LatestRBSE = [RBSE]
	FROM
		[Case]
	WHERE
		RIGHT([RBSE], 7) = @TwoDigitYear + STUFF('00000', 6 - @LatestNumberLength, @LatestNumberLength, CONVERT(varchar(5), @LatestNumber))
