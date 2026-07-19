Imports libDataAccess.libDataAccess
Imports libDataAccess.libDataAccess.TBCultureDA

Public Class clsRelations

    Public Function GetRelationDetailsOfRelatedCase(ByVal sRBSE As String, _
                                                    ByRef sSex As String, _
                                                    ByRef sSexDesc As String, _
                                                    ByRef iBirthDay As Integer, _
                                                    ByRef iBirthMonth As Integer, _
                                                    ByRef iBirthYear As Integer, _
                                                    ByRef sFate As String, _
                                                    ByRef sFateDesc As String, _
                                                    ByRef sLeftDate As String, _
                                                    ByRef sEartagCountry As String, _
                                                    ByRef sEartagHerdmark As String, _
                                                    ByRef sEartag As String, _
                                                    ByRef sSire As String _
                                                    ) As Boolean

        Dim objInParamList As New ParameterList()
        Dim dtData As DataTable

        Try
            objInParamList.QuickAddInputParam("RBSE", DbtType.dbtString, sRBSE)

            FillDataTable("GetRelationDetailsOfRelatedCase", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            If Not (dtData Is Nothing) Then
                If (dtData.Rows.Count() = 1) Then
                    With dtData.Rows(0)
                        sSex = GetRowColumnData(.Item("Sex"))
                        sSexDesc = GetRowColumnData(.Item("SexDesc"))
                        iBirthDay = CInt(GetRowColumnData(.Item("BirthDay")))
                        iBirthMonth = CInt(GetRowColumnData(.Item("BirthMonth")))
                        iBirthYear = CInt(GetRowColumnData(.Item("BirthYear")))
                        sFate = GetRowColumnData(.Item("Fate"))
                        sFateDesc = GetRowColumnData(.Item("FateDesc"))
                        sLeftDate = GetRowColumnData(.Item("LeftDate"))
                        sEartagCountry = GetRowColumnData(.Item("EartagCountry"))
                        sEartagHerdmark = GetRowColumnData(.Item("EartagHerdmark"))
                        sEartag = GetRowColumnData(.Item("Eartag"))
                        sSire = GetRowColumnData(.Item("Name"))
                        Return True
                    End With
                End If

            End If


        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            Return False
        End Try

    End Function

    Public Function GetDamSireDetailsMatches(ByVal sEartag As String, ByVal sName As String, ByVal sRBSE As String, ByVal sHerdbook As String, ByVal bIsDam As Boolean, ByRef dtData As DataTable) As Boolean

        Dim objInParamList As New ParameterList()

        Try
            objInParamList.QuickAddInputParam("Eartag", DbtType.dbtString, sEartag)
            objInParamList.QuickAddInputParam("Name", DbtType.dbtString, sName)
            If (sRBSE Is Nothing) OrElse sRBSE = "" Then
                objInParamList.QuickAddInputParam("RBSE", DbtType.dbtString, DBNull.Value)
            Else
                objInParamList.QuickAddInputParam("RBSE", DbtType.dbtString, sRBSE)
            End If
            objInParamList.QuickAddInputParam("Herdbook", DbtType.dbtString, sHerdbook)
            If bIsDam Then
                objInParamList.QuickAddInputParam("Sex", DbtType.dbtString, "F")
            Else
                objInParamList.QuickAddInputParam("Sex", DbtType.dbtString, "M")
            End If

            FillDataTable("GetDamSireDetailsMatches", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            Return False
        End Try

    End Function

    Public Function GetRelationsDetails(ByVal sRBSE As String, ByRef dsData As DataSet) As Boolean

        Dim objInParamList As New ParameterList()

        Try
            objInParamList.QuickAddInputParam("RBSE", DbtType.dbtString, sRBSE)

            FillDataSet("GetRelationsDetailsByRBSE", _
                          CommandType.StoredProcedure, _
                          dsData, _
                          objInParamList)

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            Return False
        End Try

    End Function

    Private Function GetRowColumnData(ByRef objValue As Object) As String
        If (IsDBNull(objValue)) Then
            Return Nothing
        Else
            Return objValue.ToString()
        End If
    End Function
End Class
