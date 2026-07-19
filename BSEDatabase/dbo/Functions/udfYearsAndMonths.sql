
CREATE FUNCTION udfYearsAndMonths (@Start datetime, @End datetime)  
RETURNS varchar(50) AS  
BEGIN 

DECLARE
	@YearStart datetime,
	@Years int,
	@MonthStart datetime,
	@Months int,
	@Result varchar(50)

SET @Years = DATEDIFF(year, @Start, @End)
SET @YearStart = DATEADD(year, @Years, @Start)
IF @YearStart > @End BEGIN
	SET @YearStart = DATEADD(year,-1, @YearStart)
	SET @Years = @Years - 1
END

SET @Months = DATEDIFF(month, @YearStart, @End)
SET @MonthStart = DATEADD(month, @Months, @YearStart)
IF @MonthStart > @End BEGIN
	SET @MonthStart = DATEADD(month,-1, @MonthStart)
	SET @Months = @Months - 1
END

SET @Result = ''

IF @Years > 0 BEGIN
	SET @Result = CONVERT(varchar(10), @Years) + ' Year'
	IF @Years > 1 SET @Result = @Result + 's'
	SET @Result = @Result + ' '
END

IF @Months > 0 OR @Years = 0 BEGIN
	SET @Result = @Result + CONVERT(varchar(10), @Months) + ' Month'
	IF @Months != 1 SET @Result = @Result + 's'
END

RETURN (@Result)
END

