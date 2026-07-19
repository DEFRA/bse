
CREATE PROCEDURE EditHerdSize
	@ID int,
	@HerdYear smallint,
	@TotalSize smallint,
	@Lactation1Size smallint,
	@Lactation2Size smallint,
	@Lactation3Size smallint,
	@Lactation4Size smallint,
	@Lactation5Size smallint,
	@Lactation6Size smallint,
	@Lactation7Size smallint,
	@Lactation8Size smallint,
	@Lactation9Size smallint,
	@Lactation10Size smallint,
	@Lactation10PlusSize smallint,
	@RowStamp timestamp  AS

	UPDATE
		[HerdSize]
	SET
		[HerdYear] = @HerdYear ,
		[TotalSize] = @TotalSize,
		[Lactation1Size] = @Lactation1Size,
		[Lactation2Size] = @Lactation2Size,
		[Lactation3Size] =  @Lactation3Size,
		[Lactation4Size] =  @Lactation4Size ,
		[Lactation5Size] =  @Lactation5Size,
		[Lactation6Size] =  @Lactation6Size,
		[Lactation7Size] = @Lactation7Size,
		[Lactation8Size] =  @Lactation8Size,
		[Lactation9Size] =  @Lactation9Size,
		[Lactation10Size] =  @Lactation10Size,
		[Lactation10PlusSize] =  @Lactation10PlusSize
	WHERE
		[ID] = @ID AND
		[RowStamp] = @RowStamp
