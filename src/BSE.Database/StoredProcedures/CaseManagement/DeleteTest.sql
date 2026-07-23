
CREATE PROCEDURE DeleteTest
	@ID int,
	@RowStamp timestamp AS

	DELETE
		[CaseTest]
	WHERE
		[ID] = @ID AND
		[RowStamp] = @RowStamp

