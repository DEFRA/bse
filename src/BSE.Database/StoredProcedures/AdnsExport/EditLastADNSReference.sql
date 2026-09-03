


CREATE PROCEDURE [dbo].[EditLastADNSReference]
	@Area					char(2),
	@EmailReference			varchar(50),
	@ADNSReferenceYear		smallint,
	@ADNSReferenceNumber	int
	AS
	
	IF NOT EXISTS (SELECT * FROM [ADNSArea] WHERE [Area] = @Area)
		INSERT INTO [ADNSArea] VALUES(
			@Area,
			@EmailReference,
			@ADNSReferenceYear,
			@ADNSReferenceNumber	
		)			
    ELSE
		UPDATE
			[ADNSArea]
		SET
			[LastEmailReference] = @EmailReference,
			[LastADNSReferenceYear] = @ADNSReferenceYear,
			[LastADNSReferenceNumber] = @ADNSReferenceNumber
		WHERE
			[Area] = @Area
