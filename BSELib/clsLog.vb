Public Class clsLog

    Public Enum LogType
        ltInformation = 1
        ltError = 2
        ltWarning = 3
        ltCritical = 4
    End Enum

    Public Enum LogSource
        lsStoredProcedure = 1
        lsUserInterface = 2
        lsDataAccess = 3
        lsLookupData = 4
        lsUserObject = 5
        lsLogView = 6
        lsCultureObject = 7
        lsSubmissionObject = 8
        lsHistologyObject = 9
        lsSuspensionObject = 10
        lsTissueObject = 11
        lsParcelObject = 12
        lsSearchObject = 13
        lsFarmFileObject = 14
        lsVetnetComms = 15
        lsVetnetObject = 16
    End Enum

    Public Shared Sub LogException(ByVal Exc As Exception, _
                                   ByVal iSource As Integer, _
                                   Optional ByVal sUser As String = "")
        Try
            Dim sMsg As String
            sMsg = "Exception of type " & Exc.GetType.ToString & " caught"
            sMsg &= vbNewLine & "Message: " & Exc.Message
            sMsg &= vbNewLine & "Exception Source: " & Exc.Source
            sMsg &= vbNewLine & "Stack trace:" & vbNewLine & Exc.StackTrace

            Dim ExInner As Exception
            Dim iCount As Integer = 1

            ExInner = Exc.InnerException
            While Not ExInner Is Nothing
                Dim sInEx As String = vbNewLine & "Inner exception " & iCount.ToString & " "
                sMsg &= vbNewLine
                sMsg &= sInEx & "message: " & ExInner.Message
                sMsg &= sInEx & "source: " & ExInner.Source
                sMsg &= sInEx & "stack trace:" & vbNewLine & ExInner.StackTrace

                iCount += 1
                ExInner = ExInner.InnerException
            End While

            LogMessage(sMsg, LogType.ltError, iSource, sUser)
        Catch
        End Try
    End Sub

    Public Shared Sub LogMessage(ByVal sMessage As String, _
                          ByVal Type As LogType, _
                          ByVal iSource As Integer, _
                          Optional ByVal sUser As String = "")
        libDataAccess.libDataAccess.InfoLog.LogToEventViewer(CType(Type, libDataAccess.libDataAccess.LogType), CStr(iSource), sMessage)
    End Sub
End Class
