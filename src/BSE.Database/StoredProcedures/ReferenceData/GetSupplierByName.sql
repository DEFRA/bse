
CREATE PROCEDURE GetSupplierByName 
	@Name varchar(30)
AS

SELECT
	[ID],
	[Name],
	[Details]

FROM
	[luSupplier]
WHERE
	Name = @Name

RETURN
