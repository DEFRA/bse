
CREATE PROCEDURE AddClinicalVisit
	@RBSE char(9),
	@VisitDate datetime AS

	INSERT INTO [ClinicalVisit]
		(
		[RBSE],
		[VisitDate]
		)
	VALUES
		(
		@RBSE,
		@VisitDate
		)
