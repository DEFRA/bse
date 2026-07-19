/* Creates EditLastADNSReference stored procedure for inserting/updating ADNS reference details */

GO
/****** Object:  StoredProcedure [dbo].[EditLastADNSReference]    Script Date: 01/22/2008 11:07:04 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO



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