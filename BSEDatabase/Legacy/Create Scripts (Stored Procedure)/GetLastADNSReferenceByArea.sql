/* Creates GetLastADNSReferenceByArea stored procedure for retrieving last ADNS reference details */

GO
/****** Object:  StoredProcedure [dbo].[GetLastADNSReferenceByArea]    Script Date: 01/22/2008 11:07:26 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[GetLastADNSReferenceByArea]
	@Area char(2)
	AS

	SELECT
		[LastEmailReference],
		[LastADNSReferenceYear],
		[LastADNSReferenceNumber]
	FROM
		[ADNSArea]
	WHERE
		(Area = @Area)