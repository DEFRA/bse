Public Class clsAppError

    Public Shared Sub DisplayError(ByVal sMessage As String, _
                                   Optional ByVal Exc As Exception = Nothing)

        System.Web.HttpContext.Current.Session("ErrorMessage") = sMessage

        Dim sLogMsg As String
        sLogMsg = "Application error" & vbNewLine & sMessage


        If Not Exc Is Nothing Then
            System.Web.HttpContext.Current.Session("ErrorException") = Exc

            sLogMsg &= vbNewLine & vbNewLine & "Exception info:-"
            sLogMsg &= vbNewLine & Exc.Source
            sLogMsg &= vbNewLine & Exc.Message
            sLogMsg &= vbNewLine & Exc.StackTrace
        End If

        Try
            LogMessage(sLogMsg)
        Catch ex As Exception
        End Try

        Try
            System.Web.HttpContext.Current.Response.Redirect("AppError.aspx")
        Catch taex As Threading.ThreadAbortException
        End Try
    End Sub

    Public Shared Sub LogMessage(ByVal sMessage As String, Optional ByVal type As BSELib.clsLog.LogType = BSELib.clsLog.LogType.ltError, Optional ByVal source As Byte = 2)
        Dim sUser As String
        Try
            sUser = HttpContext.Current.User.Identity.Name
            Dim iSlashPos As Integer = sUser.IndexOf("\")
            If iSlashPos >= 0 And sUser.Length > iSlashPos + 1 Then
                sUser = sUser.Substring(iSlashPos + 1)
            End If
        Catch
        End Try

        '   save the message to the log
        BSELib.clsLog.LogMessage(sMessage, type, source, sUser)
    End Sub
End Class
