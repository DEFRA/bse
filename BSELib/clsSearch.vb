Imports libDataAccess.libDataAccess
Imports libDataAccess.libDataAccess.TBCultureDA

Public Class clsSearch

    Public Shared Sub GetFarmSearchResults(ByVal sCPHH As String, _
                                             ByVal sOwnerName As String, _
                                             ByVal sAddress As String, _
                                             ByVal sCounty As String, _
                                             ByVal sHerdmark As String, _
                                             ByVal sNumericHerdmark As String, _
                                             ByVal sIsDealer As String, _
                                             ByVal sAHO As String, _
                                             ByVal bIncludeNonGBFarms As Boolean, _
                                             ByRef dtData As DataTable)

        Dim objInParamList As New ParameterList()

        Try
            With objInParamList
                .QuickAddInputParam("CPHH", DbtType.dbtString, sCPHH)
                .QuickAddInputParam("OwnerName", DbtType.dbtString, sOwnerName)
                .QuickAddInputParam("Address", DbtType.dbtString, sAddress)
                .QuickAddInputParam("County", DbtType.dbtString, sCounty)
                .QuickAddInputParam("Herdmark", DbtType.dbtString, sHerdmark)
                .QuickAddInputParam("NumericHerdmark", DbtType.dbtString, sNumericHerdmark)
                Select Case sIsDealer
                    Case ""
                        .QuickAddInputParam("IsDealer", DbtType.dbtBoolean, DBNull.Value)
                    Case "N"
                        .QuickAddInputParam("IsDealer", DbtType.dbtBoolean, False)
                    Case "Y"
                        .QuickAddInputParam("IsDealer", DbtType.dbtBoolean, True)
                End Select

                .QuickAddInputParam("AHO", DbtType.dbtString, sAHO)
                .QuickAddInputParam("IncludeNonGBFarms", DbtType.dbtBoolean, bIncludeNonGBFarms)
            End With

            FillDataTable("GetSearchFarm", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList, lngCmdTimeout:=60)

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            Throw ex
        End Try

    End Sub

    Public Shared Sub GetCaseSearchResults(ByVal sRBSE As String, _
                                                ByVal sEartag As String, _
                                                ByVal sDBSE As String, _
                                                ByVal sFate As String, _
                                                ByVal sFinalResult As String, _
                                                ByVal sSex As String, _
                                                ByVal sSurvey As String, _
                                                ByVal sNotes As String, _
                                                ByVal sEarliestFormADate As String, _
                                                ByVal sLatestFormADate As String, _
                                                ByVal sEarliestFinalResultDate As String, _
                                                ByVal sLatestFinalResultDate As String, _
                                                ByVal sEarliestBirthDate As String, _
                                                ByVal sLatestBirthDate As String, _
                                                ByVal bIncludeNonGBCases As Boolean, _
                                                ByVal sPassiveActive As String, _
                                                ByVal bIsImportedCase As Boolean, _
                                                ByRef dtData As DataTable)

        If sRBSE Is Nothing Then
            sRBSE = ""
        End If

        If sDBSE Is Nothing Then
            sDBSE = ""
        End If

        Dim objInParamList As New ParameterList()

        Try
            With objInParamList
                .QuickAddInputParam("RBSE", DbtType.dbtString, sRBSE)
                .QuickAddInputParam("Eartag", DbtType.dbtString, sEartag)
                .QuickAddInputParam("DBSE", DbtType.dbtString, sDBSE)
                .QuickAddInputParam("Fate", DbtType.dbtString, sFate)
                .QuickAddInputParam("FinalResult", DbtType.dbtString, sFinalResult)
                .QuickAddInputParam("Sex", DbtType.dbtString, sSex)
                .QuickAddInputParam("Survey", DbtType.dbtString, sSurvey)
                .QuickAddInputParam("Notes", DbtType.dbtString, sNotes)
                .QuickAddInputParam("EarliestFormADate", DbtType.dbtDate, FormatEmptyString(sEarliestFormADate))
                .QuickAddInputParam("LatestFormADate", DbtType.dbtDate, FormatEmptyString(sLatestFormADate))
                .QuickAddInputParam("EarliestFinalResultDate", DbtType.dbtDate, FormatEmptyString(sEarliestFinalResultDate))
                .QuickAddInputParam("LatestFinalResultDate", DbtType.dbtDate, FormatEmptyString(sLatestFinalResultDate))
                .QuickAddInputParam("EarliestBirthDate", DbtType.dbtDate, FormatEmptyString(sEarliestBirthDate))
                .QuickAddInputParam("LatestBirthDate", DbtType.dbtDate, FormatEmptyString(sLatestBirthDate))
                .QuickAddInputParam("IncludeNonGBCases", DbtType.dbtBoolean, bIncludeNonGBCases)
                .QuickAddInputParam("PassiveActive", DbtType.dbtString, sPassiveActive)
                .QuickAddInputParam("IsImportedCase", DbtType.dbtBoolean, bIsImportedCase)
            End With

            FillDataTable("GetSearchCase", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList, lngCmdTimeout:=60)

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            Throw ex
        End Try

    End Sub

    Public Shared Sub GetCaseByEartagHerdmarkSearchResults(ByVal sEartagHerdmark As String, _
                                                ByVal bIncludeNonGBCases As Boolean, _
                                                ByRef dtData As DataTable)
        Dim objInParamList As New ParameterList()

        Try
            With objInParamList
                .QuickAddInputParam("EartagHerdmark", DbtType.dbtString, sEartagHerdmark)
                .QuickAddInputParam("IncludeNonGBCases", DbtType.dbtBoolean, bIncludeNonGBCases)
            End With

            FillDataTable("GetSearchCaseByEartagHerdmark", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList, lngCmdTimeout:=60)


        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            Throw ex
        End Try

    End Sub

    Public Shared Sub GetRelatedAnimalSearchResults(ByVal sRBSE As String, _
                                                ByVal sName As String, _
                                                ByVal sEartag As String, _
                                                ByVal sRelationRBSE As String, _
                                                ByVal sRelationType As String, _
                                                ByRef dtData As DataTable)

        If sRBSE Is Nothing Then
            sRBSE = ""
        End If

        If sRelationRBSE Is Nothing Then
            sRelationRBSE = ""
        End If

        Dim objInParamList As New ParameterList()

        Try
            With objInParamList
                .QuickAddInputParam("RBSE", DbtType.dbtString, sRBSE)
                .QuickAddInputParam("Name", DbtType.dbtString, sName)
                .QuickAddInputParam("Eartag", DbtType.dbtString, sEartag)
                .QuickAddInputParam("RelationRBSE", DbtType.dbtString, sRelationRBSE)
                .QuickAddInputParam("RelationType", DbtType.dbtString, sRelationType)
            End With

            FillDataTable("GetSearchRelatedAnimals", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList, lngCmdTimeout:=60)

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            Throw ex
        End Try

    End Sub

    Public Enum OutstandingDataType
        OutstandingResults = 1
        OutstandingFates = 2
        OutstandingBSE1s = 3
    End Enum

    Public Shared Sub GetOutstandingDataResults(ByVal iOutstandingDataType As OutstandingDataType, _
                                                ByVal sEarliestFormADate As String, _
                                                ByVal sLatestFormADate As String, _
                                                ByVal bIncludeNonGBCases As Boolean, _
                                                ByRef dtData As DataTable)

        Dim objInParamList As New ParameterList()
        Dim sStoredProcName As String

        Select Case iOutstandingDataType
            Case OutstandingDataType.OutstandingResults
                sStoredProcName = "GetSearchOutstandingResults"
            Case OutstandingDataType.OutstandingFates
                sStoredProcName = "GetSearchOutstandingFates"
            Case OutstandingDataType.OutstandingBSE1s
                sStoredProcName = "GetSearchOutstandingBSE1s"
        End Select
        Try
            With objInParamList
                .QuickAddInputParam("EarliestFormADate", DbtType.dbtDate, FormatEmptyString(sEarliestFormADate))
                .QuickAddInputParam("LatestFormADate", DbtType.dbtDate, FormatEmptyString(sLatestFormADate))
                .QuickAddInputParam("IncludeNonGBCases", DbtType.dbtBoolean, bIncludeNonGBCases)
            End With

            FillDataTable(sStoredProcName, _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList, lngCmdTimeout:=60)

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            Throw ex
        End Try

    End Sub

    Public Shared Sub GetCaseByCPHHResults(ByVal sCPHH As String, _
                                                ByVal sHerdmark As String, _
                                                ByVal sNumericHerdmark As String, _
                                                ByVal bIncludeNonGBCases As Boolean, _
                                                ByRef dtData As DataTable)

        Dim objInParamList As New ParameterList()

        Try
            With objInParamList
                .QuickAddInputParam("CPHH", DbtType.dbtString, sCPHH)
                .QuickAddInputParam("Herdmark", DbtType.dbtString, sHerdmark)
                .QuickAddInputParam("NumericHerdmark", DbtType.dbtString, sNumericHerdmark)
                .QuickAddInputParam("IncludeNonGBCases", DbtType.dbtBoolean, bIncludeNonGBCases)
            End With

            FillDataTable("GetSearchCaseByCPHH", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList, lngCmdTimeout:=60)


        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            Throw ex
        End Try

    End Sub

    Private Shared Function FormatEmptyString(ByVal sValue As String) As Object

        If sValue = "" Then
            Return DBNull.Value
        Else
            Return sValue
        End If

    End Function

End Class
