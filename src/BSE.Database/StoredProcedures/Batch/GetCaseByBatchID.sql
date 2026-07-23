CREATE PROCEDURE GetCaseByBatchID
        @BatchID int
AS

SELECT
        [Case].[RBSE],
        [Case].[CPHH]
        --CONVERT(int, CASE SUBSTRING([Case].[RBSE],3,2) WHEN  '0X' THEN '9999' + RIGHT([Case].[RBSE], 5) ELSE CASE WHEN CONVERT(int, SUBSTRING([Case].[RBSE],3,2)) >69 THEN '19' + SUBSTRING([Case].[RBSE],3,2) + RIGHT([Case].[RBSE], 5) ELSE '20' + SUBSTRING([Case].[RBSE],3,2) + RIGHT([Case].[RBSE], 5)END END)

FROM
        [lnkBatchCase] LEFT JOIN [Case] ON [lnkBatchCase].[RBSE] = [Case].[RBSE]
WHERE
        [lnkBatchCase].[BatchID] = @BatchID
ORDER BY
        CONVERT(int, CASE SUBSTRING([Case].[RBSE],3,2) WHEN  '0X' THEN '9999' + RIGHT([Case].[RBSE], 5) ELSE CASE WHEN CONVERT(int, SUBSTRING([Case].[RBSE],3,2)) >69 THEN '19' + SUBSTRING([Case].[RBSE],3,2) + RIGHT([Case].[RBSE], 5) ELSE '20' + SUBSTRING([Case].[RBSE],3,2) + RIGHT([Case].[RBSE], 5)END END)
