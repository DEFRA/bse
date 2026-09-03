
CREATE PROCEDURE EditTest
	@ID int,
	@TestType varchar(10),
	@TestResult varchar(5),
	@RowStamp timestamp AS

	UPDATE
		[CaseTest]
	SET
		[TestType] = @TestType,
		[TestResult] = @TestResult
	WHERE
		[ID] = @ID AND
		[RowStamp] = @RowStamp

