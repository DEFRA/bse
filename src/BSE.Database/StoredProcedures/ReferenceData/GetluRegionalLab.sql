


CREATE PROCEDURE [dbo].[GetluRegionalLab] AS

DECLARE @ttblRegionalLab TABLE
		(
		[ID] int IDENTITY(1, 1),
		[Code] char(4),
		[Description] varchar(50)
		)

	SET NOCOUNT ON

	INSERT INTO @ttblRegionalLab
		(
		[Code],
		[Description]
		)
	SELECT
		[Code],
		[Description]
	FROM
		[luRegionalLab]
	ORDER BY
		[Code]

	SELECT
		[ID],
		[Code],
		[Description]
	FROM
		@ttblRegionalLab

	SET NOCOUNT OFF

RETURN



