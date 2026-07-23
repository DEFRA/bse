
CREATE PROCEDURE GetluSupplier AS

SELECT
	[ID],
	[Name],
	[Details]
FROM
	luSupplier
ORDER BY
	[Name]

RETURN
