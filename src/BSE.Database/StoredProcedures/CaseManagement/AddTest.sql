
CREATE PROCEDURE AddTest
	@RBSE char(9),
	@TestType varchar(10),
	@TestResult varchar(5)
AS

	INSERT INTO [CaseTest]
		(
		[RBSE],
		[TestType],
		[TestResult]
		)
	VALUES
		(
		@RBSE,
		@TestType,
		@TestResult
		)

