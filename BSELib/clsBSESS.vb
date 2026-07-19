Imports libDataAccess.libDataAccess
Imports libDataAccess.libDataAccess.TBCultureDA

Public Class clsBSESS

    Public Shared Function GetBSESSCheckByDate(ByVal dStartDate As Date, ByVal dEndDate As Date, ByRef dtData As DataTable) As Boolean

        Dim objInParamList As New ParameterList()

        Try
            With objInParamList
                .QuickAddInputParam("StartDate", DbtType.dbtDateTime, dStartDate)
                .QuickAddInputParam("EndDate", DbtType.dbtDateTime, dEndDate)
            End With

            FillDataTable("GetBSESSCheckByDate", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            Return False
        End Try

    End Function

    Public Shared Function GetBSESSCheckByRBSE(ByVal sRBSE As String, _
                                                ByRef sNotificationDate As String, _
                                                ByRef sBSESSEartag As String, _
                                                ByRef sBSESSBirthDate As String, _
                                                ByRef sTestGroupName As String, _
                                                ByRef sBSESSFinalResult As String, _
                                                ByRef sBarcode As String, _
                                                ByRef sFormADate As String, _
                                                ByRef sBSEEartag As String, _
                                                ByRef sBSEBirthDate As String, _
                                                ByRef sSurvey As String, _
                                                ByRef sBSEFinalResult As String, _
                                                ByRef sAHFReference As String) As Boolean

        Dim objParamList As New ParameterList()

        Try
            With objParamList
                .QuickAddInputParam("RBSE", DbtType.dbtString, sRBSE)
                .AddParameter("NotificationDate", DbtType.dbtString, "@NotificationDate", intSize:=30, daDirection:=ParameterDirection.Output)
                .AddParameter("BSESSEartag", DbtType.dbtString, "@BSESSEartag", intSize:=20, daDirection:=ParameterDirection.Output)
                .AddParameter("BSESSBirthDate", DbtType.dbtString, "@BSESSBirthDate", intSize:=30, daDirection:=ParameterDirection.Output)
                .AddParameter("TestGroupName", DbtType.dbtString, "@TestGroupName", intSize:=50, daDirection:=ParameterDirection.Output)
                .AddParameter("BSESSFinalResult", DbtType.dbtString, "@BSESSFinalResult", intSize:=25, daDirection:=ParameterDirection.Output)
                .AddParameter("Barcode", DbtType.dbtString, "@Barcode", intSize:=20, daDirection:=ParameterDirection.Output)
                .AddParameter("FormADate", DbtType.dbtString, "@FormADate", intSize:=30, daDirection:=ParameterDirection.Output)
                .AddParameter("BSEEartag", DbtType.dbtString, "@BSEEartag", intSize:=33, daDirection:=ParameterDirection.Output)
                .AddParameter("BSEBirthDate", DbtType.dbtString, "@BSEBirthDate", intSize:=30, daDirection:=ParameterDirection.Output)
                .AddParameter("Survey", DbtType.dbtString, "@Survey", intSize:=50, daDirection:=ParameterDirection.Output)
                .AddParameter("BSEFinalResult", DbtType.dbtString, "@BSEFinalResult", intSize:=50, daDirection:=ParameterDirection.Output)

                ExecuteNonQuery("GetBSESSCheckByRBSE", _
                                        CommandType.StoredProcedure, _
                                        objParamList)

                sNotificationDate = CStr(.Item("NotificationDate").Value)
                sBSESSEartag = CStr(.Item("BSESSEartag").Value)
                sBSESSBirthDate = CStr(.Item("BSESSBirthDate").Value)
                sTestGroupName = CStr(.Item("TestGroupName").Value)
                sBSESSFinalResult = CStr(.Item("BSESSFinalResult").Value)
                sBarcode = CStr(.Item("Barcode").Value)
                sFormADate = CStr(.Item("FormADate").Value)
                sBSEEartag = CStr(.Item("BSEEartag").Value)
                sBSEBirthDate = CStr(.Item("BSEBirthDate").Value)
                sSurvey = CStr(.Item("Survey").Value)
                sBSEFinalResult = CStr(.Item("BSEFinalResult").Value)
            End With
            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            Return False

        End Try

    End Function
End Class
