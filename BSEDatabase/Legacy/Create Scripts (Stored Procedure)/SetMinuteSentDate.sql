set ANSI_NULLS ON
set QUOTED_IDENTIFIER ON
go


CREATE PROCEDURE [dbo].[SetMinuteSentDate]
	@RBSE char(9),
	@MinuteType char(10)
AS
BEGIN
	IF @MinuteType = 'ActiveMemo' 
		UPDATE [CaseWork] SET ActiveMemoDate = CONVERT(char(8),GetDate(),112) WHERE [RBSE] = @RBSE
	 
	ELSE IF @MinuteType = 'AnnexA' 
		UPDATE [CaseWork] SET AnnexADate = CONVERT(char(8),GetDate(),112) WHERE [RBSE] = @RBSE
	 
	ELSE IF @MinuteType = 'AnnexB' 
		UPDATE [CaseWork] SET AnnexBDate = CONVERT(char(8),GetDate(),112) WHERE [RBSE] = @RBSE
	 
	ELSE IF @MinuteType = 'AnnexC' 
		UPDATE [CaseWork] SET AnnexCDate = CONVERT(char(8),GetDate(),112) WHERE [RBSE] = @RBSE
	
	ELSE IF @MinuteType = 'AnnexD'
		UPDATE [CaseWork] SET AnnexDDate = CONVERT(char(8),GetDate(),112) WHERE [RBSE] = @RBSE
	
END

 