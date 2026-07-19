Imports libDataAccess.libDataAccess
Imports libDataAccess.libDataAccess.TBCultureDA
Imports System.Data.SqlClient

Public Class CaseUpdateException : Inherits ApplicationException

    Public Sub New(ByVal message As String)
        MyBase.New(message)
    End Sub

    Public Sub New(ByVal message As String, ByVal inner As Exception)
        MyBase.New(message, inner)
    End Sub

End Class

Public Class clsCase

    ' Case Entry DataSet Tables
    Public Const CASE_TABLE As Integer = 0
    Public Const CLINICAL_TABLE As Integer = 1
    Public Const BAB_TABLE As Integer = 2
    Public Const OTHER_OWNER_TABLE As Integer = 3
    Public Const FEED_TABLE As Integer = 4
    Public Const CLINICAL_VISIT_TABLE As Integer = 5
    Public Const DAM_DETAILS_TABLE As Integer = 6
    Public Const SIRE_DETAILS_TABLE As Integer = 7
    Public Const RELATION_TABLE As Integer = 8
    Public Const TEST_TABLE As Integer = 9
    Public Const CASEWORK_TABLE As Integer = 10

#Region "Non-GB Case Creation"

    Public Function CreateNonGBCase(ByVal sRBSE As String, _
                                        ByRef sEartag As String, _
                                        ByRef sEartagHerdmark As String, _
                                        ByRef sEartagCountry As String, _
                                        ByRef sFate As String, _
                                        ByRef sFinalResult As String, _
                                        ByRef sFinalResultDate As String, _
                                        ByRef sSlaughterDate As String, _
                                        ByRef sCPHH As String, _
                                        ByRef sOwnerName As String, _
                                        ByRef sAddress1 As String, _
                                        ByRef sAddress2 As String, _
                                        ByRef sAddress3 As String, _
                                        ByRef sPostcode As String, _
                                        ByRef sCounty As String, _
                                        ByRef sHerdmark1 As String, _
                                        ByRef sNumericHerdmark1 As String, _
                                        ByRef iUserID As Integer, _
                                        ByRef sRBSEDate As String, _
                                        ByRef sBarcode As String, _
                                        ByRef sAHFReference As String) As Boolean


        Dim objInParamList As New ParameterList()

        Try
            With objInParamList
                .QuickAddInputParam("RBSE", DbtType.dbtString, sRBSE)
                .QuickAddInputParam("Eartag", DbtType.dbtString, sEartag)
                .QuickAddInputParam("EartagHerdmark", DbtType.dbtString, sEartagHerdmark)
                .QuickAddInputParam("EartagCountry", DbtType.dbtString, sEartagCountry)
                .QuickAddInputParam("Fate", DbtType.dbtString, sFate)
                .QuickAddInputParam("FinalResult", DbtType.dbtString, sFinalResult)
                If sFinalResultDate = "" Then
                    .QuickAddInputParam("FinalResultDate", DbtType.dbtDateTime, DBNull.Value)
                Else
                    .QuickAddInputParam("FinalResultDate", DbtType.dbtDateTime, sFinalResultDate)
                End If
                If sSlaughterDate = "" Then
                    .QuickAddInputParam("SlaughterDate", DbtType.dbtDateTime, DBNull.Value)
                Else
                    .QuickAddInputParam("SlaughterDate", DbtType.dbtDateTime, sSlaughterDate)
                End If
                .QuickAddInputParam("CPHH", DbtType.dbtString, sCPHH)
                .QuickAddInputParam("OwnerName", DbtType.dbtString, sOwnerName)
                .QuickAddInputParam("Address1", DbtType.dbtString, sAddress1)
                .QuickAddInputParam("Address2", DbtType.dbtString, sAddress2)
                .QuickAddInputParam("Address3", DbtType.dbtString, sAddress3)
                .QuickAddInputParam("Postcode", DbtType.dbtString, sPostcode)
                .QuickAddInputParam("County", DbtType.dbtString, sCounty)
                If sHerdmark1 = "" Then
                    .QuickAddInputParam("Herdmark1", DbtType.dbtString, DBNull.Value)
                Else
                    .QuickAddInputParam("Herdmark1", DbtType.dbtString, sHerdmark1)
                End If
                If sNumericHerdmark1 = "" Then
                    .QuickAddInputParam("NumericHerdmark1", DbtType.dbtString, DBNull.Value)
                Else
                    .QuickAddInputParam("NumericHerdmark1", DbtType.dbtString, sNumericHerdmark1)
                End If
                .QuickAddInputParam("UserID", DbtType.dbtInteger, iUserID)
                .QuickAddInputParam("RBSEDate", DbtType.dbtDateTime, sRBSEDate)
                .QuickAddInputParam("Barcode", DbtType.dbtString, sBarcode)
                .QuickAddInputParam("AHFReference", DbtType.dbtString, sAHFReference)
            End With

            ExecuteNonQuery("AddNonGBCase", _
                          CommandType.StoredProcedure, _
                          objInParamList)

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsSubmissionObject)
            Return False
        End Try
    End Function

#End Region

#Region "Maintenance Functions"

    Public Function DeleteCase(ByVal iUserID As Integer, ByRef sRBSE As String) As Boolean

        Dim objFarmParamList As New libDataAccess.libDataAccess.ParameterList()

        With objFarmParamList
            .AddParameter("RETURN_VALUE", DbtType.dbtInteger, "RETURN_VALUE", daDirection:=ParameterDirection.ReturnValue)
            .QuickAddInputParam("RBSE", DbtType.dbtString, sRBSE)
            .QuickAddInputParam("UserID", DbtType.dbtInteger, iUserID)
        End With

        Try
            TBCultureDA.ExecuteNonQuery("DeleteCase", CommandType.StoredProcedure, objFarmParamList)
            Dim iReturnValue As Integer = CInt(objFarmParamList("RETURN_VALUE").Value)

            Select Case iReturnValue
                Case 1
                    Throw New Exception("Case does not exist in Case Table")
                Case 2
                    Throw New Exception("Could not check whether Farm is to be deleted")
                Case 3
                    Throw New Exception("VLA have entered Data on this Case")
                Case 4
                    Throw New Exception("Error updating the Audit Log")
                Case 5
                    Throw New Exception("Error deleting the Case")
                Case 6
                    Throw New Exception("Error deleting the Farm")

            End Select

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsSubmissionObject)
            Return False
        End Try
    End Function

    Public Function MoveCase(ByVal iUserID As Integer, ByRef sRBSE As String, ByVal sNewCPHH As String) As Boolean

        Dim objFarmParamList As New libDataAccess.libDataAccess.ParameterList()

        With objFarmParamList
            .AddParameter("RETURN_VALUE", DbtType.dbtInteger, "RETURN_VALUE", daDirection:=ParameterDirection.ReturnValue)
            .QuickAddInputParam("RBSE", DbtType.dbtString, sRBSE)
            .QuickAddInputParam("NewCPHH", DbtType.dbtString, sNewCPHH)
            .QuickAddInputParam("UserID", DbtType.dbtInteger, iUserID)
        End With

        Try
            TBCultureDA.ExecuteNonQuery("MoveCase", CommandType.StoredProcedure, objFarmParamList)
            Dim iReturnValue As Integer = CInt(objFarmParamList("RETURN_VALUE").Value)

            Select Case iReturnValue
                Case 1
                    Throw New Exception("New CPHH does not exist in Farm Table")
                Case 2
                    Throw New Exception("Case does not exist in Case Table")
                Case 3
                    Throw New Exception("Unable to get number of Cases with same CPHH")
                Case 4
                    Throw New Exception("Error updating the Audit Log")
                Case 5
                    Throw New Exception("Error updating the Case table")
                Case 6
                    Throw New Exception("Error deleting row from the Farm table")
            End Select

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsSubmissionObject)
            Return False
        End Try
    End Function

    Public Function MoveCaseNewFarm(ByVal iUserID As Integer, _
                                    ByRef sRBSE As String, _
                                    ByVal sNewCPHH As String, _
                                    ByVal sOwnerName As String, _
                                    ByVal sAddress1 As String, _
                                    ByVal sAddress2 As String, _
                                    ByVal sAddress3 As String, _
                                    ByVal sPostCode As String, _
                                    ByVal sParish As String, _
                                    ByVal sDistrict As String, _
                                    ByVal sCounty As String, _
                                    ByVal sCorrespondenceAddress1 As String, _
                                    ByVal sCorrespondenceAddress2 As String, _
                                    ByVal sCorrespondenceAddress3 As String, _
                                    ByVal sCorrespondencePostcode As String, _
                                    ByVal sMapReference As String, _
                                    ByVal sHerdmark1 As String, _
                                    ByVal sHerdmark2 As String, _
                                    ByVal sHerdmark3 As String, _
                                    ByVal sNumericHerdmark1 As String, _
                                    ByVal sNumericHerdmark2 As String, _
                                    ByVal sAHO As String, _
                                    ByVal iADNSRegionID As Integer) As Boolean

        Dim objParamList As New libDataAccess.libDataAccess.ParameterList()

        With objParamList
            .AddParameter("RETURN_VALUE", DbtType.dbtInteger, "RETURN_VALUE", daDirection:=ParameterDirection.ReturnValue)
            .QuickAddInputParam("RBSE", DbtType.dbtString, sRBSE)
            .QuickAddInputParam("NewCPHH", DbtType.dbtString, sNewCPHH)
            .QuickAddInputParam("OwnerName", DbtType.dbtString, sOwnerName)
            .QuickAddInputParam("Address1", DbtType.dbtString, sAddress1)
            .QuickAddInputParam("Address2", DbtType.dbtString, sAddress2)
            .QuickAddInputParam("Address3", DbtType.dbtString, sAddress3)
            .QuickAddInputParam("Postcode", DbtType.dbtString, sPostCode)
            .QuickAddInputParam("Parish", DbtType.dbtString, sParish)
            .QuickAddInputParam("District", DbtType.dbtString, sDistrict)
            .QuickAddInputParam("County", DbtType.dbtString, sCounty)
            .QuickAddInputParam("CorrespondenceAddress1", DbtType.dbtString, sCorrespondenceAddress1)
            .QuickAddInputParam("CorrespondenceAddress2", DbtType.dbtString, sCorrespondenceAddress2)
            .QuickAddInputParam("CorrespondenceAddress3", DbtType.dbtString, sCorrespondenceAddress3)
            .QuickAddInputParam("CorrespondencePostcode", DbtType.dbtString, sCorrespondencePostcode)
            If sMapReference.Trim = "" Then
                .QuickAddInputParam("MapReference", DbtType.dbtString, DBNull.Value)
            Else
                .QuickAddInputParam("MapReference", DbtType.dbtString, sMapReference)
            End If
            .QuickAddInputParam("Herdmark1", DbtType.dbtString, sHerdmark1)
            .QuickAddInputParam("Herdmark2", DbtType.dbtString, sHerdmark2)
            .QuickAddInputParam("Herdmark3", DbtType.dbtString, sHerdmark3)
            If sNumericHerdmark1.Trim = "" Then
                .QuickAddInputParam("NumericHerdmark1", DbtType.dbtString, DBNull.Value)
            Else
                .QuickAddInputParam("NumericHerdmark1", DbtType.dbtString, sNumericHerdmark1)
            End If
            If sNumericHerdmark2.Trim = "" Then
                .QuickAddInputParam("NumericHerdmark2", DbtType.dbtString, DBNull.Value)
            Else
                .QuickAddInputParam("NumericHerdmark2", DbtType.dbtString, sNumericHerdmark2)
            End If
            .QuickAddInputParam("AHO", DbtType.dbtString, sAHO)
            .QuickAddInputParam("HerdType", DbtType.dbtString, DBNull.Value)
            .QuickAddInputParam("PedigreeType", DbtType.dbtString, DBNull.Value)
            .QuickAddInputParam("IsDealer", DbtType.dbtBoolean, False)
            .QuickAddInputParam("ADNSRegionID", DbtType.dbtInteger, iADNSRegionID)
            .QuickAddInputParam("UserID", DbtType.dbtInteger, iUserID)
        End With

        Try
            TBCultureDA.ExecuteNonQuery("MoveCaseNewFarm", CommandType.StoredProcedure, objParamList)
            Dim iReturnValue As Integer = CInt(objParamList("RETURN_VALUE").Value)

            Select Case iReturnValue
                Case 1
                    Throw New Exception("A farm with this CPHH already exists")
                Case 2
                    Throw New Exception("Error writing Farm Creation to the Audit Log")
                Case 3
                    Throw New Exception("There was an Error Creating the new Farm")
                Case 4
                    Throw New Exception("The new Farm was not successfully created")
                Case 5
                    Throw New Exception("Error finding the CPHH of the Case in the Case Table")
                Case 6
                    Throw New Exception("Error finding the Case in the Case Table")
                Case 7
                    Throw New Exception("There was an Error writing the Case Move to the Audit Log")
                Case 8
                    Throw New Exception("There was an error updating the Case to the new Farm")
                Case 9
                    Throw New Exception("There was an Error writing a row into the Audit Log showing the deletion of the old Farm")
                Case 10
                    Throw New Exception("There was an Error deleting the old farm")
            End Select

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsSubmissionObject)
            Return False
        End Try
    End Function

    Public Function ChangeRBSE(ByVal iUserID As Integer, ByRef sOldRBSE As String, ByVal sNewRBSE As String) As Boolean

        Dim objFarmParamList As New libDataAccess.libDataAccess.ParameterList()

        With objFarmParamList
            .AddParameter("RETURN_VALUE", DbtType.dbtInteger, "RETURN_VALUE", daDirection:=ParameterDirection.ReturnValue)
            .QuickAddInputParam("OldRBSE", DbtType.dbtString, sOldRBSE)
            .QuickAddInputParam("NewRBSE", DbtType.dbtString, sNewRBSE)
            .QuickAddInputParam("UserID", DbtType.dbtInteger, iUserID)
        End With

        Try
            TBCultureDA.ExecuteNonQuery("ChangeRBSE", CommandType.StoredProcedure, objFarmParamList)
            Dim iReturnValue As Integer = CInt(objFarmParamList("RETURN_VALUE").Value)

            Select Case iReturnValue
                Case 1
                    Throw New Exception("Case does not exist in the Case table")
                Case 2
                    Throw New Exception("New RBSE already exists in the Case table")
                Case 3
                    Throw New Exception("Unable to add new Row to Case table")
                Case 4
                    Throw New Exception("Error updating the Pedigree table")
                Case 5
                    Throw New Exception("Error updating the CaseHistorical table")
                Case 6
                    Throw New Exception("Error updating the CaseRelation table")
                Case 7
                    Throw New Exception("Error updating the CaseRelation table")
                Case 8
                    Throw New Exception("Error updating the CaseBAB table")
                Case 9
                    Throw New Exception("Error updating the CaseFeed table")
                Case 10
                    Throw New Exception("Error updating the OtherOwner table")
                Case 11
                    Throw New Exception("Error updating the CaseClinical table")
                Case 12
                    Throw New Exception("Error updating the lnkBatchCase table")
                Case 13
                    Throw New Exception("Error deleting a row from the Case table")
                Case 14
                    Throw New Exception("Error updating the CaseTest table")
                Case 15
                    Throw New Exception("Error updating the CaseWork table")

            End Select

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsSubmissionObject)
            Return False
        End Try
    End Function

#End Region

#Region "Get number of cases on farm"

    Public Function GetNumberOfCasesByCPHH(ByVal sCPHH As String, _
                                    ByRef iNumberOfCases As Int32) As Boolean

        Dim objInParamList As New ParameterList()

        Try
            objInParamList.QuickAddInputParam("CPHH", DbtType.dbtString, sCPHH)

            Dim dtData As New DataTable()

            FillDataTable("GetNumberOfCasesByCPHH", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            If Not (dtData Is Nothing) Then
                If (dtData.Rows.Count() > 0) Then
                    iNumberOfCases = CInt(dtData.Rows(0)(0))
                End If
            End If

            dtData = Nothing

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsSubmissionObject)
            Return False
        End Try
    End Function

#End Region

#Region "Final Result Methods"

    Public Function FinalResultEntry(ByVal sRBSE As String, _
                                    ByRef sFinalResult As String, _
                                    ByRef dFinalResultDate As Date, _
                                    ByRef sRetrospectiveTestType As String, _
                                    ByRef sRetrospectiveResult As String, _
                                    ByRef sRetrospectiveResultDate As String, _
                                    ByRef sRetrospectiveComment As String, _
                                    ByRef bRowstamp() As Byte, _
                                    ByRef iUserID As Integer, _
                                    ByRef sDBSE As String, _
                                    ByRef sAlternateDiagnosis As String, _
                                    ByRef sLabComment As String) As Boolean

        Dim objInParamList As New ParameterList()

        Try
            With objInParamList
                .QuickAddInputParam("RBSE", DbtType.dbtString, sRBSE)
                .QuickAddInputParam("FinalResult", DbtType.dbtString, sFinalResult)
                .QuickAddInputParam("FinalResultDate", DbtType.dbtDateTime, dFinalResultDate)
                .QuickAddInputParam("RetrospectiveTestType", DbtType.dbtString, sRetrospectiveTestType)
                .QuickAddInputParam("RetrospectiveResult", DbtType.dbtString, sRetrospectiveResult)
                If sRetrospectiveResultDate = "" Then
                    .QuickAddInputParam("RetrospectiveResultDate", DbtType.dbtDateTime, DBNull.Value)
                Else
                    .QuickAddInputParam("RetrospectiveResultDate", DbtType.dbtDateTime, sRetrospectiveResultDate)
                End If
                .QuickAddInputParam("RetrospectiveComment", DbtType.dbtString, sRetrospectiveComment)
                .QuickAddInputParam("RowStamp", DbtType.dbtBinary, bRowstamp)
                .QuickAddInputParam("UserID", DbtType.dbtInteger, iUserID)
                If sAlternateDiagnosis = "" Then
                    .QuickAddInputParam("AlternateDiagnosis", DbtType.dbtString, DBNull.Value)
                Else
                    .QuickAddInputParam("AlternateDiagnosis", DbtType.dbtString, sAlternateDiagnosis)
                End If
                .QuickAddInputParam("LabComment", DbtType.dbtString, sLabComment)
                .AddParameter("@DBSE", DbtType.dbtString, "@DBSE", daDirection:=ParameterDirection.Output, intSize:=7)
            End With

            ExecuteNonQuery("EditCaseFinalResult", _
                          CommandType.StoredProcedure, _
                          objInParamList)

            sDBSE = CStr(objInParamList("@DBSE").Value)
            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsSubmissionObject)
            Return False
        End Try
    End Function

    Public Function GetFinalResultByRBSE(ByVal sRBSE As String, ByRef dtData As DataTable) As Boolean

        Dim objInParamList As New ParameterList()

        Try
            objInParamList.QuickAddInputParam("RBSE", DbtType.dbtString, sRBSE)

            FillDataTable("GetFinalResultByRBSE", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsSubmissionObject)
            Return False
        End Try
    End Function

    Public Function GetTestDataByRBSE(ByVal sRBSE As String, ByRef dtData As DataTable) As Boolean

        Dim objInParamList As New ParameterList()

        Try
            objInParamList.QuickAddInputParam("RBSE", DbtType.dbtString, sRBSE)

            FillDataTable("GetTestByRBSE", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsSubmissionObject)
            Return False
        End Try
    End Function

#End Region

#Region "Map Reference Methods"

    Public Function EstimateMapReference(ByVal sCounty As String, _
                                    ByVal sParish As String) As String
        Dim dtParishCoords As DataTable
        Dim sXCoord As String
        Dim sYCoord As String
        Dim iYCoord1 As Int32
        Dim iYCoord2 As Int32

        ' Get the coords for the stated County Parish
        If Not GetMapReferenceByCountyParish(sCounty, sParish, dtParishCoords) Then
            Throw New Exception("Case.GetMapReferenceByCountyParish returned False")
        End If

        ' Cycle through the rows and find the middle row.
        ' The row describes a strip of land.  We want the centre of that strip.
        If Not (dtParishCoords Is Nothing) Then
            Dim dRowCount As Double = dtParishCoords.Rows.Count()
            Dim dMiddleRow As Double
            If (dRowCount > 0) Then
                If dRowCount Mod 2 <> 0 Then
                    dMiddleRow = dRowCount / 2 + 0.5
                Else
                    dMiddleRow = dRowCount / 2
                End If
                ' Make the row zero based
                dMiddleRow = dMiddleRow - 1
                sXCoord = dtParishCoords.Rows(CInt(dMiddleRow))("XReference1").ToString
                sYCoord = GetCentreCoordinate(dtParishCoords.Rows(CInt(dMiddleRow))("YReference1").ToString, dtParishCoords.Rows(CInt(dMiddleRow))("YReference2").ToString)

                Return ConvertToMapReference(sXCoord, sYCoord)
            End If
        End If

        Return ""
    End Function

    Private Function GetCentreCoordinate(ByVal sYCoord1 As String, ByVal sYCoord2 As String) As String
        Dim iYCoord1 As Int32 = Convert.ToInt32(sYCoord1)
        Dim iYCoord2 As Int32 = Convert.ToInt32(sYCoord2)
        Dim iOutputCoord As Int32
        Dim sOutputCoord As String

        If (iYCoord2 - iYCoord1) Mod 2 <> 0 Then
            iOutputCoord = CInt(iYCoord1 + (iYCoord2 - iYCoord1) / 2 + 0.5)
        Else
            iOutputCoord = CInt(iYCoord1 + (iYCoord2 - iYCoord1) / 2)
        End If

        sOutputCoord = CStr(iOutputCoord)
        Select Case Len(sOutputCoord)
            Case 1
                sOutputCoord = "000" & sOutputCoord
            Case 2
                sOutputCoord = "00" & sOutputCoord
            Case 3
                sOutputCoord = "0" & sOutputCoord
        End Select

        Return sOutputCoord
    End Function

    Private Function ConvertToMapReference(ByVal sXCoord As String, ByVal sYCoord As String) As String
        Dim sCode As String
        Dim sXPrefixCoord As String = Mid$(sXCoord, 2, 1)
        Dim sYPrefixCoord As String = Mid$(sYCoord, 1, 2)

        If Left$(sYPrefixCoord, 1) = "0" Then
            sYPrefixCoord = Mid$(sYPrefixCoord, 2, 1)
        End If

        If Not GetPrefixCodeByXYReference(sCode, sXPrefixCoord, sYPrefixCoord) Then
            Throw New Exception("Case.GetPrefixCodeByXYReference returned False")
        End If

        Return sCode & Mid$(sXCoord, 3, 2) & "5" & Mid$(sYCoord, 3, 2) & "5"

    End Function

    Private Function GetPrefixCodeByXYReference(ByRef sCode As String, _
                                ByVal sXCoord As String, _
                                ByVal sYCoord As String) As Boolean

        Dim objInParamList As New ParameterList()

        Try
            objInParamList.QuickAddInputParam("XCoordPrefix", DbtType.dbtString, sXCoord)
            objInParamList.QuickAddInputParam("YCoordPrefix", DbtType.dbtString, sYCoord)

            Dim dtData As New DataTable()

            FillDataTable("GetPrefixCodeByXYReference", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            If Not (dtData Is Nothing) Then
                If (dtData.Rows.Count() > 0) Then
                    sCode = dtData.Rows(0)("Code").ToString()
                End If
            End If

            dtData = Nothing
            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsSubmissionObject)
            Return False
        End Try
    End Function


    Public Function ValidateMapReference(ByVal sCounty As String, _
                                    ByVal sParish As String, _
                                    ByVal sMapReference As String) As Boolean

        Dim dtParishCoords As DataTable
        Dim sXCoord As String = Nothing
        Dim sYCoord As String = Nothing
        Dim iIndex As Int32
        Dim sParishXCoord As String
        Dim sParishYCoord1 As String
        Dim sParishYCoord2 As String

        ' Get the coords for the stated County Parish
        If Not GetMapReferenceByCountyParish(sCounty, sParish, dtParishCoords) Then
            Throw New Exception("Case.GetMapReferenceByCountyParish returned False")
        End If

        ' Convert the Map Reference using the prefix e.g. HP123456 -> 04123 12456
        If Not GetXYReferenceByPrefixCode(Left$(sMapReference, 2), sXCoord, sYCoord) Then
            Throw New Exception("Case.GetXYReferenceByPrefixCode returned False")
        End If

        ' Calculate the X & Y Coords.  N.B. We are given the coordinate in 5 
        ' figures but we are comparing it to the parish coordinate in 4 figures
        sXCoord = sXCoord & Mid$(sMapReference, 3, 2)
        sYCoord = sYCoord & Mid$(sMapReference, 6, 2)

        If Not (dtParishCoords Is Nothing) Then
            If (dtParishCoords.Rows.Count() > 0) Then
                For iIndex = 0 To dtParishCoords.Rows.Count() - 1
                    sParishXCoord = dtParishCoords.Rows(iIndex)("XReference1").ToString()
                    sParishYCoord1 = dtParishCoords.Rows(iIndex)("YReference1").ToString()
                    sParishYCoord2 = dtParishCoords.Rows(iIndex)("YReference2").ToString()
                    ' 1. Check to see if the Parish X Coord matches the Given XCoord
                    If sXCoord = sParishXCoord Then
                        ' 2. Check to see if Given Y Coord is between the two Parish Y Coords
                        If sYCoord >= sParishYCoord1 And sYCoord <= sParishYCoord2 Then
                            Return True
                        End If
                    End If
                Next
            End If
        End If

        Return False
    End Function

    Private Function GetXYReferenceByPrefixCode(ByRef sCode As String, _
                                    ByRef sXCoord As String, _
                                    ByRef sYCoord As String) As Boolean

        Dim objInParamList As New ParameterList()

        Try
            objInParamList.QuickAddInputParam("Code", DbtType.dbtString, sCode)

            Dim dtData As New DataTable()

            FillDataTable("GetXYReferenceByPrefixCode", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            If Not (dtData Is Nothing) Then
                If (dtData.Rows.Count() > 0) Then
                    sXCoord = dtData.Rows(0)("XCoordPrefix").ToString()
                    sYCoord = dtData.Rows(0)("YCoordPrefix").ToString()
                End If
            End If

            dtData = Nothing
            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsSubmissionObject)
            Return False
        End Try
    End Function

    Private Function GetMapReferenceByCountyParish(ByVal sCounty As String, ByVal sParish As String, ByRef dtData As DataTable) As Boolean

        Dim objInParamList As New ParameterList()

        Try
            objInParamList.QuickAddInputParam("County", DbtType.dbtString, sCounty)
            objInParamList.QuickAddInputParam("Parish", DbtType.dbtString, sParish)

            FillDataTable("GetMapReferenceByCountyParish", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsSubmissionObject)
            Return False
        End Try
    End Function

#End Region

#Region "Suppliers Methods"

    Public Function GetPossibleSuppliers(ByRef sName As String, ByRef dtData As DataTable) As Boolean

        Dim objInParamList As New ParameterList()

        Try
            objInParamList.QuickAddInputParam("Name", DbtType.dbtString, sName)

            FillDataTable("GetPossibleSuppliers", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsSubmissionObject)
            Return False
        End Try

    End Function

    Public Function GetSupplierByName(ByRef sName As String, _
                                    ByRef iID As Int32, _
                                    ByRef sDetails As String) As Boolean

        Dim objInParamList As New ParameterList()

        Try
            objInParamList.QuickAddInputParam("Name", DbtType.dbtString, sName)

            Dim dtData As New DataTable()

            FillDataTable("GetSupplierByName", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            If Not (dtData Is Nothing) Then
                If (dtData.Rows.Count() > 0) Then
                    iID = CInt(dtData.Rows(0)("ID"))
                    sDetails = CStr(dtData.Rows(0)("Details"))
                Else
                    sName = ""
                End If
            End If

            dtData = Nothing

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsSubmissionObject)
            Return False
        End Try
    End Function

#End Region

#Region "Get Batch Number"

    Public Function GetBatchNumberByRBSE(ByVal sRBSE As String, ByRef dtData As DataTable) As Boolean

        Dim objInParamList As New ParameterList()

        Try
            objInParamList.QuickAddInputParam("RBSE", DbtType.dbtString, sRBSE)

            FillDataTable("GetBatchNumberByRBSE", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsSubmissionObject)
            Return False
        End Try

    End Function

#End Region

#Region "Get Case Details"

    Public Function GetCaseDetails(ByVal sRBSE As String, ByRef dsData As DataSet) As Boolean

        Dim objInParamList As New ParameterList()

        Try
            objInParamList.QuickAddInputParam("RBSE", DbtType.dbtString, sRBSE)

            FillDataSet("GetCaseDetailsByRBSE", _
                          CommandType.StoredProcedure, _
                          dsData, _
                          objInParamList)

            With dsData
                SetPrimaryKey(.Tables(FEED_TABLE), "ID", True)
                SetPrimaryKey(.Tables(CLINICAL_VISIT_TABLE), "ID", True)
                SetPrimaryKey(.Tables(OTHER_OWNER_TABLE), "ID", True)
                SetPrimaryKey(.Tables(RELATION_TABLE), "ID", True)
                SetPrimaryKey(.Tables(TEST_TABLE), "ID", True)
            End With

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsSubmissionObject)
            Return False
        End Try
    End Function

#End Region

#Region "Update Case Details"

    Public Function CheckMandatoryFields(ByRef dsCase As DataSet, ByRef dsFarm As DataSet, ByRef objErrorList As ArrayList) As Boolean

        Dim bSuccess As Boolean


        'check the case
        With dsCase.Tables(CASE_TABLE).Rows(0)
            If .IsNull("CPHH") Then
                objErrorList.Add("No farm has been specified for the case.")
            Else
                'check the farm
                With dsFarm.Tables(BSELib.clsFarm.FARM_TABLE).Rows(0)
                    If .IsNull("OwnerName") Then
                        objErrorList.Add("Please enter an owner name for the farm.")
                    End If
                    If .IsNull("Address1") Then
                        objErrorList.Add("Please enter the first line of the farm address.")
                    End If
                    If .IsNull("Parish") And Not (.Item("IsNonGBFarm").ToString = "True") Then
                        objErrorList.Add("Please enter a parish for the farm.")
                    End If
                    If .IsNull("County") Then
                        objErrorList.Add("Please specify a county for the farm.")
                    End If
                    If .IsNull("AHO") And Not (.Item("IsNonGBFarm").ToString = "True") Then
                        objErrorList.Add("Please specify an AHO for the farm.")
                    End If
                    If .IsNull("ADNSRegionID") And Not (.Item("IsNonGBFarm").ToString = "True") Then
                        objErrorList.Add("Please specify an ADNS Region for the farm.")
                    End If
                End With
            End If
            If .IsNull("EartagCountry") And .IsNull("Eartag") And .IsNull("EartagHerdmark") Then
                objErrorList.Add("Please specify an eartag for the case.")
            End If
            If .IsNull("FormADate") And Not (.Item("IsNonGBCase").ToString = "True") Then
                objErrorList.Add("Please specify a Form A date for the case.")
            End If
            If (Not .IsNull("FormBDate")) And .IsNull("Fate") Then
                objErrorList.Add("Please specify a fate (Form B Reason) for the case.")
            End If
            If .IsNull("FormBDate") And Not (.IsNull("FormBDate")) Then
                objErrorList.Add("Please specify a Form B Date for the case.")
            End If
        End With

        If objErrorList.Count > 0 Then
            Return False
        Else
            Return True
        End If

    End Function

    
    Public Function UpdateCaseDetails(ByVal iUserID As Integer, ByVal iBatchID As Integer, ByRef dsCase As DataSet, ByRef dsFarm As DataSet, ByRef objErrorList As ArrayList) As Boolean

        Dim clsDataCheck As New BSELib.clsDataCheck()
        Dim bCaseHasChanges As Boolean = clsDataCheck.DataSetHasChanges(dsCase)
        Dim bFarmHasChanges As Boolean = clsDataCheck.DataSetHasChanges(dsFarm)

        If Not bCaseHasChanges AndAlso Not bFarmHasChanges AndAlso iBatchID = 0 Then
            Return True
        End If

        Dim objDBConn As SqlConnection = Nothing
        Dim objDBTran As SqlTransaction = Nothing

        Try

            'open a database connection and begin a transaction
            objDBConn = TBCultureDA.OpenConnection()
            objDBTran = TBCultureDA.BeginTransaction(objDBConn)

            'update farm data
            If bFarmHasChanges Then
                Dim objFarm As New clsFarm()
                objFarm.UpdateFarmDetails(iUserID, dsFarm, objDBConn, objDBTran, objErrorList)
            End If

            'add batch link if required

            If bCaseHasChanges Then

                'update case data
                If clsDataCheck.DataTableHasChanges(dsCase.Tables(CASE_TABLE)) Then
                    UpdateCaseRecord(iUserID, dsCase.Tables(CASE_TABLE).Rows(0), objDBConn, objDBTran, objErrorList)
                End If

                If iBatchID <> 0 Then
                    objDBConn = CreateBatchLink(dsCase, iBatchID, objDBConn, objDBTran)
                End If

                'update BAB data
                If clsDataCheck.DataTableHasChanges(dsCase.Tables(BAB_TABLE)) Then
                    UpdateBABRecord(dsCase.Tables(BAB_TABLE).Rows(0), objDBConn, objDBTran, objErrorList)
                End If

                'update other owners data
                If clsDataCheck.DataTableHasChanges(dsCase.Tables(OTHER_OWNER_TABLE)) Then
                    UpdateOtherOwnerRecords(dsCase.Tables(OTHER_OWNER_TABLE), objDBConn, objDBTran, objErrorList)
                End If

                'update tests data
                If clsDataCheck.DataTableHasChanges(dsCase.Tables(TEST_TABLE)) Then
                    UpdateTestRecords(dsCase.Tables(TEST_TABLE), objDBConn, objDBTran, objErrorList)
                End If

                'update clinical data
                If clsDataCheck.DataTableHasChanges(dsCase.Tables(CLINICAL_TABLE)) Then
                    UpdateClinicalRecord(dsCase.Tables(CLINICAL_TABLE).Rows(0), objDBConn, objDBTran, objErrorList)
                End If

                'update the clinical visit records
                If clsDataCheck.DataTableHasChanges(dsCase.Tables(CLINICAL_VISIT_TABLE)) Then
                    UpdateClinicalVisitRecords(dsCase.Tables(CLINICAL_VISIT_TABLE), objDBConn, objDBTran, objErrorList)
                End If

                'update the feed records
                If clsDataCheck.DataTableHasChanges(dsCase.Tables(FEED_TABLE)) Then
                    UpdateFeedRecords(dsCase.Tables(FEED_TABLE), objDBConn, objDBTran, objErrorList)
                End If

                'update the relation records
                If clsDataCheck.DataTableHasChanges(dsCase.Tables(RELATION_TABLE)) Then
                    UpdateRelationRecords(dsCase.Tables(RELATION_TABLE), objDBConn, objDBTran, objErrorList)
                End If

                'update the dam and sire details, and the case herdbook
                UpdateDamSireRecords(dsCase.Tables(CASE_TABLE).Rows(0), dsCase.Tables(DAM_DETAILS_TABLE), dsCase.Tables(SIRE_DETAILS_TABLE), objDBConn, objDBTran, objErrorList)

                'update the casework table
                If clsDataCheck.DataTableHasChanges(dsCase.Tables(CASEWORK_TABLE)) Then
                    UpdateCaseWorkRecord(dsCase.Tables(CASEWORK_TABLE).Rows(0), objDBConn, objDBTran, objErrorList)
                End If

            Else If Not bCaseHasChanges And iBatchID <> 0 Then
                objDBConn = CreateBatchLink(dsCase, iBatchID, objDBConn, objDBTran)
            End If

            'commit the database transaction
            TBCultureDA.CommitTransaction(objDBTran)

        Catch exCaseUpdate As CaseUpdateException
            objErrorList.Add(exCaseUpdate.Message)
            If Not objDBTran Is Nothing Then
                TBCultureDA.RollbackTransaction(objDBTran)
            End If
            clsLog.LogException(exCaseUpdate, clsLog.LogSource.lsStoredProcedure)
            Return False
        Catch ex As Exception
            If Not objDBTran Is Nothing Then
                TBCultureDA.RollbackTransaction(objDBTran)
            End If
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            Return False
        Finally
            If Not objDBConn Is Nothing Then
                TBCultureDA.CloseConnection(objDBConn)
            End If
        End Try

        Return True

    End Function

    Private Function CreateBatchLink(dsCase As DataSet, iBatchID As Integer, objDBConn As SqlConnection, ByRef objDBTran As SqlTransaction) As SqlConnection

        Try
            clsBatch.CreateBatchNumberLink(iBatchID, dsCase.Tables(CASE_TABLE).Rows(0).Item("RBSE").ToString(), "BSE1", objDBConn, objDBTran)
        Catch ex As Exception
            Throw New CaseUpdateException("Failed to add the case to a batch", ex)
        End Try
        Return objDBConn
    End Function

    Private Sub UpdateCaseRecord(ByVal iUserID As Integer, ByRef drCaseRow As DataRow, ByRef objDBConn As SqlConnection, ByRef objDBTran As SqlTransaction, ByRef objErrorList As ArrayList)

        If drCaseRow.RowState <> DataRowState.Added And drCaseRow.RowState <> DataRowState.Modified Then
            Exit Sub
        End If

        Dim objCaseParamList As New libDataAccess.libDataAccess.ParameterList()

        With objCaseParamList
            .AddParameter("RETURN_VALUE", DbtType.dbtInteger, "RETURN_VALUE", daDirection:=ParameterDirection.ReturnValue)
            .QuickAddInputParam("RBSE", DbtType.dbtString, drCaseRow.Item("RBSE"))
            If drCaseRow.RowState = DataRowState.Added Then
                .QuickAddInputParam("CPHH", DbtType.dbtString, drCaseRow.Item("CPHH"))
            End If
            .QuickAddInputParam("EartagCountry", DbtType.dbtString, drCaseRow.Item("EartagCountry"))
            .QuickAddInputParam("EartagHerdmark", DbtType.dbtString, drCaseRow.Item("EartagHerdmark"))
            .QuickAddInputParam("Eartag", DbtType.dbtString, drCaseRow.Item("Eartag"))
            .QuickAddInputParam("PreviousEartag", DbtType.dbtString, drCaseRow.Item("PreviousEartag"))
            .QuickAddInputParam("BSE1ReceivedDate", DbtType.dbtDateTime, drCaseRow.Item("BSE1ReceivedDate"))
            .QuickAddInputParam("FormADate", DbtType.dbtDateTime, drCaseRow.Item("FormADate"))
            .QuickAddInputParam("FormAResubmittedDate", DbtType.dbtDateTime, drCaseRow.Item("FormAResubmittedDate"))
            .QuickAddInputParam("FormBDate", DbtType.dbtDateTime, drCaseRow.Item("FormBDate"))
            .QuickAddInputParam("Fate", DbtType.dbtString, drCaseRow.Item("Fate"))
            .QuickAddInputParam("FormCDate", DbtType.dbtDateTime, drCaseRow.Item("FormCDate"))
            .QuickAddInputParam("IsPurchaserBSE1Received", DbtType.dbtBoolean, drCaseRow.Item("IsPurchaserBSE1Received"))
            .QuickAddInputParam("IsBreederBSE1Received", DbtType.dbtBoolean, drCaseRow.Item("IsBreederBSE1Received"))
            .QuickAddInputParam("IsVendor1BSE1Received", DbtType.dbtBoolean, drCaseRow.Item("IsVendor1BSE1Received"))
            .QuickAddInputParam("IsHomebredBSE1Received", DbtType.dbtBoolean, drCaseRow.Item("IsHomebredBSE1Received"))
            .QuickAddInputParam("IsSummarySheetReceived", DbtType.dbtBoolean, drCaseRow.Item("IsSummarySheetReceived"))
            .QuickAddInputParam("IsPaperworkComplete", DbtType.dbtBoolean, drCaseRow.Item("IsPaperworkComplete"))
            .QuickAddInputParam("ReportedLocation", DbtType.dbtString, drCaseRow.Item("ReportedLocation"))
            .QuickAddInputParam("Survey", DbtType.dbtString, drCaseRow.Item("Survey"))
            .QuickAddInputParam("Notes", DbtType.dbtString, drCaseRow.Item("Notes"))
            .QuickAddInputParam("BirthDate", DbtType.dbtDateTime, drCaseRow.Item("BirthDate"))
            .QuickAddInputParam("IsBirthDateEst", DbtType.dbtBoolean, drCaseRow.Item("IsBirthDateEst"))
            .QuickAddInputParam("DamStatus", DbtType.dbtString, drCaseRow.Item("DamStatus"))
            .QuickAddInputParam("BirthDateSource", DbtType.dbtString, drCaseRow.Item("BirthDateSource"))
            .QuickAddInputParam("ValuationAge", DbtType.dbtString, drCaseRow.Item("ValuationAge"))
            .QuickAddInputParam("Sex", DbtType.dbtString, drCaseRow.Item("Sex"))
            .QuickAddInputParam("Breed", DbtType.dbtString, drCaseRow.Item("Breed"))
            .QuickAddInputParam("Origin", DbtType.dbtString, drCaseRow.Item("Origin"))
            .QuickAddInputParam("PurchaseDate", DbtType.dbtDateTime, drCaseRow.Item("PurchaseDate"))
            .QuickAddInputParam("PurchaseAgeInMonths", DbtType.dbtSmallInt, drCaseRow.Item("PurchaseAgeInMonths"))
            .QuickAddInputParam("PurchasedCounty", DbtType.dbtString, drCaseRow.Item("PurchasedCounty"))
            .QuickAddInputParam("HerdEntryDate", DbtType.dbtDateTime, drCaseRow.Item("HerdEntryDate"))
            .QuickAddInputParam("OnsetDate", DbtType.dbtDateTime, drCaseRow.Item("OnsetDate"))
            .QuickAddInputParam("IsOnsetDateEst", DbtType.dbtBoolean, drCaseRow.Item("IsOnsetDateEst"))
            .QuickAddInputParam("MonthsPregnant", DbtType.dbtTinyInt, drCaseRow.Item("MonthsPregnant"))
            .QuickAddInputParam("MonthsPostCalving", DbtType.dbtTinyInt, drCaseRow.Item("MonthsPostCalving"))
            .QuickAddInputParam("OnsetAgeInMonths", DbtType.dbtSmallInt, drCaseRow.Item("OnsetAgeInMonths"))
            .QuickAddInputParam("SlaughterDate", DbtType.dbtDateTime, drCaseRow.Item("SlaughterDate"))
            .QuickAddInputParam("AlternateDiagnosis", DbtType.dbtString, drCaseRow.Item("AlternateDiagnosis"))
            .QuickAddInputParam("LabComment", DbtType.dbtString, drCaseRow.Item("LabComment"))
            .QuickAddInputParam("CaseType", DbtType.dbtString, drCaseRow.Item("CaseType"))

            If drCaseRow.RowState = DataRowState.Modified Then
                .QuickAddInputParam("RowStamp", DbtType.dbtBinary, drCaseRow.Item("RowStamp"))
            End If
            .QuickAddInputParam("UserID", DbtType.dbtInteger, iUserID)

        End With

        Select Case drCaseRow.RowState
            Case DataRowState.Added
                Try
                    TBCultureDA.ExecuteNonQuery(objDBConn, objDBTran, "AddCase", CommandType.StoredProcedure, objCaseParamList)
                Catch ex As Exception
                    clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
                    Throw New CaseUpdateException(ex.Message, ex.InnerException)
                End Try
                Dim iReturnValue As Integer = CInt(objCaseParamList("RETURN_VALUE").Value)
                Select Case iReturnValue
                    Case 1
                        Throw New CaseUpdateException("Failed to insert into the audit log")
                    Case 2
                        Throw New CaseUpdateException("Failed to insert into the Farm table")
                    Case 3
                        Throw New CaseUpdateException("A case with RBSE" & drCaseRow.Item("RBSE").ToString() & " has already been created by another user")
                End Select

            Case DataRowState.Modified
                Try
                    TBCultureDA.ExecuteNonQuery(objDBConn, objDBTran, "EditCase", CommandType.StoredProcedure, objCaseParamList)
                Catch ex As Exception
                    clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
                    Throw New CaseUpdateException(ex.Message, ex.InnerException)
                End Try
                Dim iReturnValue As Integer = CInt(objCaseParamList("RETURN_VALUE").Value)
                Select Case iReturnValue
                    Case 1
                        Throw New CaseUpdateException("The case with RBSE " & drCaseRow.Item("RBSE").ToString() & " has been deleted by another user")
                    Case 2
                        objErrorList.Add("Failed to insert into the audit log when updating the Case record")
                    Case 3
                        objErrorList.Add("The case record with RBSE " & drCaseRow.Item("RBSE").ToString() & " has been modified by another user")
                    Case 4
                        Throw New CaseUpdateException("Failed to update the Case table")
                    Case 5
                        Throw New CaseUpdateException("An error occurred while reading the batch-case link table")
                    Case 6
                        Throw New CaseUpdateException("Failed to insert into the lnkBatchCase table")
                End Select
        End Select

    End Sub

    Private Sub UpdateBABRecord(ByRef drBABRow As DataRow, ByRef objDBConn As SqlConnection, ByRef objDBTran As SqlTransaction, ByRef objErrorList As ArrayList)

        'update the BAB record
        Dim objBABParamList As New libDataAccess.libDataAccess.ParameterList()

        With objBABParamList
            .AddParameter("RETURN_VALUE", DbtType.dbtInteger, "RETURN_VALUE", daDirection:=ParameterDirection.ReturnValue)
            .QuickAddInputParam("RBSE", DbtType.dbtString, drBABRow.Item("RBSE"))
            .QuickAddInputParam("NatalCPHH", DbtType.dbtString, drBABRow.Item("NatalCPHH"))
            .QuickAddInputParam("Notes", DbtType.dbtString, drBABRow.Item("Notes"))
            .QuickAddInputParam("TracedName", DbtType.dbtString, drBABRow.Item("TracedName"))
            .QuickAddInputParam("TracedAddress1", DbtType.dbtString, drBABRow.Item("TracedAddress1"))
            .QuickAddInputParam("TracedAddress2", DbtType.dbtString, drBABRow.Item("TracedAddress2"))
            .QuickAddInputParam("TracedAddress3", DbtType.dbtString, drBABRow.Item("TracedAddress3"))
            .QuickAddInputParam("TracedPostcode", DbtType.dbtString, drBABRow.Item("TracedPostcode"))
            .QuickAddInputParam("FeedRisk", DbtType.dbtString, drBABRow.Item("FeedRisk"))
            .QuickAddInputParam("HorizontalRisk", DbtType.dbtString, drBABRow.Item("HorizontalRisk"))
            .QuickAddInputParam("MaternalRisk", DbtType.dbtString, drBABRow.Item("MaternalRisk"))
            If drBABRow.RowState = DataRowState.Modified Then
                .QuickAddInputParam("RowStamp", DbtType.dbtBinary, drBABRow.Item("RowStamp"))
            End If
        End With

        Try
            Select Case drBABRow.RowState
                Case DataRowState.Added
                    TBCultureDA.ExecuteNonQuery(objDBConn, objDBTran, "AddCaseBAB", CommandType.StoredProcedure, objBABParamList)
                    Dim iReturnValue As Integer = CInt(objBABParamList("RETURN_VALUE").Value)
                    Select Case iReturnValue
                        Case 1
                            objErrorList.Add("Failed to insert into the BAB table.  The data may have been changed by another user.")
                    End Select
                Case DataRowState.Modified
                    TBCultureDA.ExecuteNonQuery(objDBConn, objDBTran, "EditCaseBAB", CommandType.StoredProcedure, objBABParamList)
                    Dim iReturnValue As Integer = CInt(objBABParamList("RETURN_VALUE").Value)
                    Select Case iReturnValue
                        Case 1
                            objErrorList.Add("Failed to update the BAB table.  The data may have been changed by another user.")
                        Case 2
                            objErrorList.Add("Failed to update the BAB table.")
                    End Select
            End Select
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            objErrorList.Add(ex.Message)
        End Try
    End Sub

    Private Sub UpdateOtherOwnerRecords(ByRef dtOtherOwner As DataTable, ByRef objDBConn As SqlConnection, ByRef objDBTran As SqlTransaction, ByRef objErrorList As ArrayList)

        Dim objOtherOwnerParamList As New libDataAccess.libDataAccess.UpdateParameterList()

        With objOtherOwnerParamList
            .AddInsertParam("RBSE", DbtType.dbtString)
            .AddInsertParam("Type", DbtType.dbtString)
            .AddInsertParam("Name", DbtType.dbtString)
            .AddInsertParam("CPHH", DbtType.dbtString)

            .AddUpdateParam("ID", DbtType.dbtInteger)
            .AddUpdateParam("Type", DbtType.dbtString)
            .AddUpdateParam("Name", DbtType.dbtString)
            .AddUpdateParam("CPHH", DbtType.dbtString)
            .AddUpdateParam("RowStamp", DbtType.dbtBinary)

            .AddDeleteParam("ID", DbtType.dbtInteger)
            .AddDeleteParam("RowStamp", DbtType.dbtBinary)
        End With
        TBCultureDA.OptimisticUpdateDataTable(objDBConn, objDBTran, AddressOf OnOtherOwnerRowUpdated, "", "AddOtherOwner", "EditOtherOwner", "DeleteOtherOwner", _
                                    CommandType.StoredProcedure, _
                                    dtOtherOwner, objOtherOwnerParamList)

        AddRowErrorsToList("other owner", "Name", dtOtherOwner, objErrorList)

    End Sub

    Private Sub OnOtherOwnerRowUpdated(ByVal sender As Object, ByVal args As SqlRowUpdatedEventArgs)

        'Dim iReturnValue As Integer = args.Command.Parameters("@RETURN_VALUE").Value

        If args.Status = UpdateStatus.ErrorsOccurred Then
            args.Row.RowError = args.Errors.Message
            args.Status = UpdateStatus.SkipCurrentRow
        Else
            If args.RecordsAffected = 0 Then
                args.Row.RowError = "Data was changed by another user"
                args.Status = UpdateStatus.SkipCurrentRow
            End If
        End If

    End Sub

    Private Sub UpdateTestRecords(ByRef dtTest As DataTable, ByRef objDBConn As SqlConnection, ByRef objDBTran As SqlTransaction, ByRef objErrorList As ArrayList)

        Dim objTestParamList As New libDataAccess.libDataAccess.UpdateParameterList()

        With objTestParamList
            .AddInsertParam("RBSE", DbtType.dbtString)
            .AddInsertParam("TestType", DbtType.dbtString)
            .AddInsertParam("TestResult", DbtType.dbtString)

            .AddUpdateParam("ID", DbtType.dbtInteger)
            .AddUpdateParam("TestType", DbtType.dbtString)
            .AddUpdateParam("TestResult", DbtType.dbtString)
            .AddUpdateParam("RowStamp", DbtType.dbtBinary)

            .AddDeleteParam("ID", DbtType.dbtInteger)
            .AddDeleteParam("RowStamp", DbtType.dbtBinary)
        End With
        TBCultureDA.OptimisticUpdateDataTable(objDBConn, objDBTran, AddressOf OnTestRowUpdated, "", "AddTest", "EditTest", "DeleteTest", _
                                    CommandType.StoredProcedure, _
                                    dtTest, objTestParamList)

        AddRowErrorsToList("test", "TestType", dtTest, objErrorList)

    End Sub

    Private Sub OnTestRowUpdated(ByVal sender As Object, ByVal args As SqlRowUpdatedEventArgs)

        If args.Status = UpdateStatus.ErrorsOccurred Then
            args.Row.RowError = args.Errors.Message
            args.Status = UpdateStatus.SkipCurrentRow
        Else
            If args.RecordsAffected = 0 Then
                args.Row.RowError = "Data was changed by another user"
                args.Status = UpdateStatus.SkipCurrentRow
            End If
        End If

    End Sub

    Private Sub UpdateClinicalRecord(ByRef drClinicalRow As DataRow, ByRef objDBConn As SqlConnection, ByRef objDBTran As SqlTransaction, ByRef objErrorList As ArrayList)

        If drClinicalRow.RowState <> DataRowState.Added And drClinicalRow.RowState <> DataRowState.Modified Then
            Exit Sub
        End If

        'update the clinical record
        Dim objClinicalParamList As New libDataAccess.libDataAccess.ParameterList()

        With objClinicalParamList
            .AddParameter("RETURN_VALUE", DbtType.dbtInteger, "RETURN_VALUE", daDirection:=ParameterDirection.ReturnValue)
            .QuickAddInputParam("RBSE", DbtType.dbtString, drClinicalRow.Item("RBSE"))
            .QuickAddInputParam("Apprehension", DbtType.dbtBoolean, drClinicalRow.Item("Apprehension"))
            .QuickAddInputParam("HypersensitiveTouch", DbtType.dbtBoolean, drClinicalRow.Item("HypersensitiveTouch"))
            .QuickAddInputParam("HypersensitiveSound", DbtType.dbtBoolean, drClinicalRow.Item("HypersensitiveSound"))
            .QuickAddInputParam("Maniacal", DbtType.dbtBoolean, drClinicalRow.Item("Maniacal"))
            .QuickAddInputParam("PanicStricken", DbtType.dbtBoolean, drClinicalRow.Item("PanicStricken"))
            .QuickAddInputParam("TemperamentChange", DbtType.dbtBoolean, drClinicalRow.Item("TemperamentChange"))
            .QuickAddInputParam("AbnormalHeadCarriage", DbtType.dbtBoolean, drClinicalRow.Item("AbnormalHeadCarriage"))
            .QuickAddInputParam("EarTwitching", DbtType.dbtBoolean, drClinicalRow.Item("EarTwitching"))
            .QuickAddInputParam("EarsOddAngle", DbtType.dbtBoolean, drClinicalRow.Item("EarsOddAngle"))
            .QuickAddInputParam("AbnormalBehaviour", DbtType.dbtBoolean, drClinicalRow.Item("AbnormalBehaviour"))
            .QuickAddInputParam("HeadShyness", DbtType.dbtBoolean, drClinicalRow.Item("HeadShyness"))
            .QuickAddInputParam("LickingFlank", DbtType.dbtBoolean, drClinicalRow.Item("LickingFlank"))
            .QuickAddInputParam("LickingNose", DbtType.dbtBoolean, drClinicalRow.Item("LickingNose"))
            .QuickAddInputParam("Kicking", DbtType.dbtBoolean, drClinicalRow.Item("Kicking"))
            .QuickAddInputParam("ReluctantDoorways", DbtType.dbtBoolean, drClinicalRow.Item("ReluctantDoorways"))
            .QuickAddInputParam("HeadPressing", DbtType.dbtBoolean, drClinicalRow.Item("HeadPressing"))
            .QuickAddInputParam("HeadRubbing", DbtType.dbtBoolean, drClinicalRow.Item("HeadRubbing"))
            .QuickAddInputParam("TeethGrinding", DbtType.dbtBoolean, drClinicalRow.Item("TeethGrinding"))
            .QuickAddInputParam("Blindness", DbtType.dbtBoolean, drClinicalRow.Item("Blindness"))
            .QuickAddInputParam("Circling", DbtType.dbtBoolean, drClinicalRow.Item("Circling"))
            .QuickAddInputParam("HindAtaxia", DbtType.dbtBoolean, drClinicalRow.Item("HindAtaxia"))
            .QuickAddInputParam("Falling", DbtType.dbtBoolean, drClinicalRow.Item("Falling"))
            .QuickAddInputParam("Paresis", DbtType.dbtBoolean, drClinicalRow.Item("Paresis"))
            .QuickAddInputParam("ForeAtaxia", DbtType.dbtBoolean, drClinicalRow.Item("ForeAtaxia"))
            .QuickAddInputParam("Recumbent", DbtType.dbtBoolean, drClinicalRow.Item("Recumbent"))
            .QuickAddInputParam("Tremor", DbtType.dbtBoolean, drClinicalRow.Item("Tremor"))
            .QuickAddInputParam("KnucklingFetlock", DbtType.dbtBoolean, drClinicalRow.Item("KnucklingFetlock"))
            .QuickAddInputParam("WeightLoss", DbtType.dbtBoolean, drClinicalRow.Item("WeightLoss"))
            .QuickAddInputParam("ConditionLoss", DbtType.dbtBoolean, drClinicalRow.Item("ConditionLoss"))
            .QuickAddInputParam("MilkYield", DbtType.dbtBoolean, drClinicalRow.Item("MilkYield"))
            If drClinicalRow.RowState = DataRowState.Modified Then
                .QuickAddInputParam("RowStamp", DbtType.dbtBinary, drClinicalRow.Item("RowStamp"))
            End If
        End With

        Try
            Select Case drClinicalRow.RowState
                Case DataRowState.Added
                    TBCultureDA.ExecuteNonQuery(objDBConn, objDBTran, "AddCaseClinical", CommandType.StoredProcedure, objClinicalParamList)
                    Dim iReturnValue As Integer = CInt(objClinicalParamList("RETURN_VALUE").Value)
                    Select Case iReturnValue
                        Case 1
                            objErrorList.Add("Failed to insert into the Clinical table.  The data may have been changed by another user.")
                    End Select
                Case DataRowState.Modified
                    TBCultureDA.ExecuteNonQuery(objDBConn, objDBTran, "EditCaseClinical", CommandType.StoredProcedure, objClinicalParamList)
                    Dim iReturnValue As Integer = CInt(objClinicalParamList("RETURN_VALUE").Value)
                    Select Case iReturnValue
                        Case 1
                            objErrorList.Add("Failed to update the Clinical table.  The data may have been changed by another user.")
                        Case 2
                            objErrorList.Add("Failed to update the Clinical table.")
                    End Select
            End Select
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            objErrorList.Add(ex.Message)
        End Try

    End Sub

    Private Sub UpdateClinicalVisitRecords(ByRef dtClinicalVisit As DataTable, ByRef objDBConn As SqlConnection, ByRef objDBTran As SqlTransaction, ByRef objErrorList As ArrayList)

        Dim objClinicalVisitParamList As New libDataAccess.libDataAccess.UpdateParameterList()

        With objClinicalVisitParamList
            .AddInsertParam("RBSE", DbtType.dbtString)
            .AddInsertParam("VisitDate", DbtType.dbtDateTime)

            .AddUpdateParam("ID", DbtType.dbtInteger)
            .AddUpdateParam("VisitDate", DbtType.dbtDateTime)
            .AddUpdateParam("RowStamp", DbtType.dbtBinary)

            .AddDeleteParam("ID", DbtType.dbtInteger)
            .AddDeleteParam("RowStamp", DbtType.dbtBinary)
        End With
        TBCultureDA.OptimisticUpdateDataTable(objDBConn, objDBTran, AddressOf OnClinicalVisitRowUpdated, "", "AddClinicalVisit", "EditClinicalVisit", "DeleteClinicalVisit", _
                                    CommandType.StoredProcedure, _
                                    dtClinicalVisit, objClinicalVisitParamList)

        AddRowErrorsToList("clinical visit", "VisitDate", dtClinicalVisit, objErrorList)

    End Sub

    Private Sub OnClinicalVisitRowUpdated(ByVal sender As Object, ByVal args As SqlRowUpdatedEventArgs)

        'Dim iReturnValue As Integer = args.Command.Parameters("@RETURN_VALUE").Value

        If args.Status = UpdateStatus.ErrorsOccurred Then
            args.Row.RowError = args.Errors.Message
            args.Status = UpdateStatus.SkipCurrentRow
        Else
            If args.RecordsAffected = 0 Then
                args.Row.RowError = "Data was changed by another user"
                args.Status = UpdateStatus.SkipCurrentRow
            End If
        End If

    End Sub

    Private Sub UpdateFeedRecords(ByRef dtFeed As DataTable, ByRef objDBConn As SqlConnection, ByRef objDBTran As SqlTransaction, ByRef objErrorList As ArrayList)

        Dim objFeedParamList As New libDataAccess.libDataAccess.UpdateParameterList()

        With objFeedParamList
            .AddInsertParam("RBSE", DbtType.dbtString)
            .AddInsertParam("YearFrom", DbtType.dbtSmallInt)
            .AddInsertParam("YearTo", DbtType.dbtSmallInt)
            .AddInsertParam("RationType", DbtType.dbtString)
            .AddInsertParam("SupplierID", DbtType.dbtInteger)
            .AddInsertParam("RationName", DbtType.dbtString)
            .AddInsertParam("IsPrePurchase", DbtType.dbtBoolean)

            .AddUpdateParam("ID", DbtType.dbtInteger)
            .AddUpdateParam("YearFrom", DbtType.dbtSmallInt)
            .AddUpdateParam("YearTo", DbtType.dbtSmallInt)
            .AddUpdateParam("RationType", DbtType.dbtString)
            .AddUpdateParam("SupplierID", DbtType.dbtInteger)
            .AddUpdateParam("RationName", DbtType.dbtString)
            .AddUpdateParam("IsPrePurchase", DbtType.dbtBoolean)
            .AddUpdateParam("RowStamp", DbtType.dbtBinary)

            .AddDeleteParam("ID", DbtType.dbtInteger)
            .AddDeleteParam("RowStamp", DbtType.dbtBinary)
        End With

        TBCultureDA.OptimisticUpdateDataTable(objDBConn, objDBTran, AddressOf OnFeedRowUpdated, "", "AddCaseFeed", "EditCaseFeed", "DeleteCaseFeed", _
                                    CommandType.StoredProcedure, _
                                    dtFeed, objFeedParamList)

        AddRowErrorsToList("feed", "RationName", dtFeed, objErrorList)

    End Sub

    Private Sub OnFeedRowUpdated(ByVal sender As Object, ByVal args As SqlRowUpdatedEventArgs)

        'Dim iReturnValue As Integer = args.Command.Parameters("@RETURN_VALUE").Value

        If args.Status = UpdateStatus.ErrorsOccurred Then
            args.Row.RowError = args.Errors.Message
            args.Status = UpdateStatus.SkipCurrentRow
        Else
            If args.RecordsAffected = 0 Then
                args.Row.RowError = "Data was changed by another user"
                args.Status = UpdateStatus.SkipCurrentRow
            End If
        End If

    End Sub

    Private Sub UpdateRelationRecords(ByRef dtRelation As DataTable, ByRef objDBConn As SqlConnection, ByRef objDBTran As SqlTransaction, ByRef objErrorList As ArrayList)

        Dim objRelationParamList As New libDataAccess.libDataAccess.UpdateParameterList()

        With objRelationParamList
            .AddInsertParam("RBSE", DbtType.dbtString)
            .AddInsertParam("RelationType", DbtType.dbtString)
            .AddInsertParam("RelationRBSE", DbtType.dbtString)
            .AddInsertParam("Sex", DbtType.dbtString)
            .AddInsertParam("BirthDay", DbtType.dbtTinyInt)
            .AddInsertParam("BirthMonth", DbtType.dbtTinyInt)
            .AddInsertParam("BirthYear", DbtType.dbtSmallInt)
            .AddInsertParam("RelationFate", DbtType.dbtString)
            .AddInsertParam("LeftDate", DbtType.dbtDateTime)
            .AddInsertParam("EartagCountry", DbtType.dbtString)
            .AddInsertParam("EartagHerdmark", DbtType.dbtString)
            .AddInsertParam("Eartag", DbtType.dbtString)
            .AddInsertParam("Sire", DbtType.dbtString)

            .AddUpdateParam("ID", DbtType.dbtInteger)
            .AddUpdateParam("RelationType", DbtType.dbtString)
            .AddUpdateParam("RelationRBSE", DbtType.dbtString)
            .AddUpdateParam("Sex", DbtType.dbtString)
            .AddUpdateParam("BirthDay", DbtType.dbtTinyInt)
            .AddUpdateParam("BirthMonth", DbtType.dbtTinyInt)
            .AddUpdateParam("BirthYear", DbtType.dbtSmallInt)
            .AddUpdateParam("RelationFate", DbtType.dbtString)
            .AddUpdateParam("LeftDate", DbtType.dbtDateTime)
            .AddUpdateParam("EartagCountry", DbtType.dbtString)
            .AddUpdateParam("EartagHerdmark", DbtType.dbtString)
            .AddUpdateParam("Eartag", DbtType.dbtString)
            .AddUpdateParam("Sire", DbtType.dbtString)
            .AddUpdateParam("RowStamp", DbtType.dbtBinary)

            .AddDeleteParam("ID", DbtType.dbtInteger)
            .AddDeleteParam("RowStamp", DbtType.dbtBinary)
        End With
        TBCultureDA.OptimisticUpdateDataTable(objDBConn, objDBTran, AddressOf OnRelationRowUpdated, "", "AddCaseRelation", "EditCaseRelation", "DeleteCaseRelation", _
                                    CommandType.StoredProcedure, _
                                    dtRelation, objRelationParamList)

        AddRowErrorsToList("relations", "Eartag", dtRelation, objErrorList)

    End Sub

    Private Sub OnRelationRowUpdated(ByVal sender As Object, ByVal args As SqlRowUpdatedEventArgs)

        'Dim iReturnValue As Integer = args.Command.Parameters("@RETURN_VALUE").Value

        If args.Status = UpdateStatus.ErrorsOccurred Then
            args.Row.RowError = args.Errors.Message
            args.Status = UpdateStatus.SkipCurrentRow
        Else
            If args.RecordsAffected = 0 Then
                args.Row.RowError = "Data was changed by another user"
                args.Status = UpdateStatus.SkipCurrentRow
            End If
        End If

    End Sub

    Private Sub UpdateDamSireRecords(ByRef drCaseRow As DataRow, ByRef dtDamDetails As DataTable, ByRef dtSireDetails As DataTable, ByRef objDBConn As SqlConnection, ByRef objDBTran As SqlTransaction, ByRef objErrorList As ArrayList)

        Dim objDamSireParamList As New libDataAccess.libDataAccess.ParameterList()

        With objDamSireParamList
            .AddParameter("RETURN_VALUE", DbtType.dbtInteger, "RETURN_VALUE", daDirection:=ParameterDirection.ReturnValue)
            .QuickAddInputParam("RBSE", DbtType.dbtString, drCaseRow.Item("RBSE"))
            Dim drDamRow As DataRow = dtDamDetails.Rows(0)
            .QuickAddInputParam("DamID", DbtType.dbtInteger, drDamRow.Item("ID"))
            .QuickAddInputParam("DamRBSE", DbtType.dbtString, drDamRow.Item("RBSE"))
            .QuickAddInputParam("DamName", DbtType.dbtString, drDamRow.Item("Name"))
            .QuickAddInputParam("DamEartag", DbtType.dbtString, drDamRow.Item("Eartag"))
            .QuickAddInputParam("DamHerdbook", DbtType.dbtString, drDamRow.Item("Herdbook"))
            .QuickAddInputParam("DamBirthDay", DbtType.dbtTinyInt, drDamRow.Item("BirthDay"))
            .QuickAddInputParam("DamBirthMonth", DbtType.dbtTinyInt, drDamRow.Item("BirthMonth"))
            .QuickAddInputParam("DamBirthYear", DbtType.dbtSmallInt, drDamRow.Item("BirthYear"))
            .QuickAddInputParam("DamRowStamp", DbtType.dbtBinary, drDamRow.Item("RowStamp"))

            Dim drSireRow As DataRow = dtSireDetails.Rows(0)
            .QuickAddInputParam("SireID", DbtType.dbtInteger, drSireRow.Item("ID"))
            .QuickAddInputParam("SireRBSE", DbtType.dbtString, drSireRow.Item("RBSE"))
            .QuickAddInputParam("SireName", DbtType.dbtString, drSireRow.Item("Name"))
            .QuickAddInputParam("SireEartag", DbtType.dbtString, drSireRow.Item("Eartag"))
            .QuickAddInputParam("SireHerdbook", DbtType.dbtString, drSireRow.Item("Herdbook"))
            .QuickAddInputParam("SireBirthDay", DbtType.dbtTinyInt, drSireRow.Item("BirthDay"))
            .QuickAddInputParam("SireBirthMonth", DbtType.dbtTinyInt, drSireRow.Item("BirthMonth"))
            .QuickAddInputParam("SireBirthYear", DbtType.dbtSmallInt, drSireRow.Item("BirthYear"))
            .QuickAddInputParam("SireRowStamp", DbtType.dbtBinary, drSireRow.Item("RowStamp"))

            .QuickAddInputParam("CaseHerdbook", DbtType.dbtString, drCaseRow.Item("Herdbook"))
            .QuickAddInputParam("CaseRowStamp", DbtType.dbtBinary, drCaseRow.Item("PedigreeRowStamp"))
        End With

        Try
            TBCultureDA.ExecuteNonQuery(objDBConn, objDBTran, "AddEditDamSireDetails", CommandType.StoredProcedure, objDamSireParamList)
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            Throw New CaseUpdateException(ex.Message, ex.InnerException)
        End Try
        Dim iReturnValue As Integer = CInt(objDamSireParamList("RETURN_VALUE").Value)
        Select Case iReturnValue
            Case 1
                objErrorList.Add("Failed to create or update a dam record.  The record may have been changed by another user")
            Case 2
                objErrorList.Add("Failed to create or update a sire record.  The record may have been changed by another user")
            Case 3
                objErrorList.Add("Failed to create a pedigree record for the case.")
            Case 4
                objErrorList.Add("Failed to update the case's pedigree record with pointers to the dam and sire information.  The record may have been changed by another user.")
        End Select

    End Sub

    Private Sub UpdateCaseWorkRecord(ByRef drCaseWorkRow As DataRow, ByRef objDBConn As SqlConnection, ByRef objDBTran As SqlTransaction, ByRef objErrorList As ArrayList)

        If drCaseWorkRow.RowState <> DataRowState.Added And drCaseWorkRow.RowState <> DataRowState.Modified Then
            Exit Sub
        End If

        'update the casework record
        Dim objCaseWorkParamList As New libDataAccess.libDataAccess.ParameterList()

        With objCaseWorkParamList
            .AddParameter("RETURN_VALUE", DbtType.dbtInteger, "RETURN_VALUE", daDirection:=ParameterDirection.ReturnValue)
            .QuickAddInputParam("RBSE", DbtType.dbtString, drCaseWorkRow.Item("RBSE"))
            .QuickAddInputParam("RBSEDate", DbtType.dbtDateTime, drCaseWorkRow.Item("RBSEDate"))
            .QuickAddInputParam("Barcode", DbtType.dbtString, drCaseWorkRow.Item("Barcode"))
            .QuickAddInputParam("AHFReference", DbtType.dbtString, drCaseWorkRow.Item("AHFReference"))
            .QuickAddInputParam("PurchaserBSE1ReceivedDate", DbtType.dbtDateTime, drCaseWorkRow.Item("PurchaserBSE1ReceivedDate"))
            .QuickAddInputParam("BreederBSE1ReceivedDate", DbtType.dbtDateTime, drCaseWorkRow.Item("BreederBSE1ReceivedDate"))
            .QuickAddInputParam("Vendor1BSE1ReceivedDate", DbtType.dbtDateTime, drCaseWorkRow.Item("Vendor1BSE1ReceivedDate"))
            .QuickAddInputParam("HomebredBSE1ReceivedDate", DbtType.dbtDateTime, drCaseWorkRow.Item("HomebredBSE1ReceivedDate"))
            .QuickAddInputParam("SummarySheetReceivedDate", DbtType.dbtDateTime, drCaseWorkRow.Item("SummarySheetReceivedDate"))
            .QuickAddInputParam("PaperworkCompleteDate", DbtType.dbtDateTime, drCaseWorkRow.Item("PaperworkCompleteDate"))

            ''Can't think of a reason to need RowStamp? 
            'If drCaseWorkRow.RowState = DataRowState.Modified Then
            '    .QuickAddInputParam("RowStamp", DbtType.dbtBinary, drCaseWorkRow.Item("RowStamp"))
            'End If
        End With

        Try
            Select Case drCaseWorkRow.RowState
                Case DataRowState.Added
                    TBCultureDA.ExecuteNonQuery(objDBConn, objDBTran, "AddCaseWork", CommandType.StoredProcedure, objCaseWorkParamList)
                    Dim iReturnValue As Integer = CInt(objCaseWorkParamList("RETURN_VALUE").Value)
                    Select Case iReturnValue
                        Case 1
                            objErrorList.Add("Failed to insert into the Case Work table.  The data may have been changed by another user.")
                    End Select
                Case DataRowState.Modified
                    TBCultureDA.ExecuteNonQuery(objDBConn, objDBTran, "EditCaseWork", CommandType.StoredProcedure, objCaseWorkParamList)
                    Dim iReturnValue As Integer = CInt(objCaseWorkParamList("RETURN_VALUE").Value)
                    Select Case iReturnValue
                        Case 1
                            objErrorList.Add("Failed to update the Case Work table.  The data may have been changed by another user.")
                        Case 2
                            objErrorList.Add("Failed to update the Case Work table.")
                    End Select
            End Select
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            objErrorList.Add(ex.Message)
        End Try

    End Sub

    Private Sub AddRowErrorsToList(ByVal sTableName As String, ByVal sReportColumn As String, ByRef dtData As DataTable, ByRef objErrorList As ArrayList)

        Dim drData As DataRow
        For Each drData In dtData.Rows
            If drData.HasErrors Then
                Dim objMessage As New System.Text.StringBuilder()
                objMessage.Append("Failed to ")
                Select Case drData.RowState
                    Case DataRowState.Added
                        objMessage.Append("add ")
                    Case DataRowState.Modified
                        objMessage.Append("update ")
                    Case DataRowState.Deleted
                        objMessage.Append("delete ")
                End Select
                objMessage.Append(sTableName)
                objMessage.Append(" with ")
                objMessage.Append(sReportColumn)
                objMessage.Append(" """)
                objMessage.Append(drData.Item(sReportColumn))
                objMessage.Append(""" :")
                objMessage.Append(drData.RowError)

                objErrorList.Add(objMessage.ToString())
            End If
        Next

    End Sub
#End Region


#Region "Latest RBSE & DBSE for Home Page"

    Public Function GetLatestRBSE(ByVal sTwoDigitYear As String, _
                                  ByRef sLatestRBSE As String) As Boolean

        Dim objInParamList As New ParameterList()

        Try
            objInParamList.QuickAddInputParam("TwoDigitYear", DbtType.dbtString, sTwoDigitYear)
            objInParamList.AddParameter("@LatestRBSE", DbtType.dbtString, "@LatestRBSE", daDirection:=ParameterDirection.Output, intSize:=9)

            ExecuteNonQuery("GetLatestRBSEForYear", _
                          CommandType.StoredProcedure, _
                          objInParamList)

            sLatestRBSE = CStr(objInParamList("@LatestRBSE").Value)
            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsSubmissionObject)
            Return False
        End Try

    End Function

    Public Function GetLatestDBSE(ByVal sTwoDigitYear As String, _
                                  ByRef sLatestDBSE As String) As Boolean

        Dim objInParamList As New ParameterList()

        Try
            objInParamList.QuickAddInputParam("TwoDigitYear", DbtType.dbtString, sTwoDigitYear)
            objInParamList.AddParameter("@LatestDBSE", DbtType.dbtString, "@LatestDBSE", daDirection:=ParameterDirection.Output, intSize:=7)

            ExecuteNonQuery("GetLatestDBSEForYear", _
                          CommandType.StoredProcedure, _
                          objInParamList)

            sLatestDBSE = CStr(objInParamList("@LatestDBSE").Value)
            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsSubmissionObject)
            Return False
        End Try

    End Function

#End Region

    'Private Function GetRowColumnData(ByRef objValue As Object) As Object
    '    If (IsDBNull(objValue)) Then
    '        Return Nothing
    '    Else
    '        Return objValue
    '    End If
    'End Function

#Region "Get Case Open/Closed Report Data"
    Public Function GetOpenCaseReportData() As DataTable

        Try
            Dim dtData As New DataTable()
            TBCultureDA.FillDataTable("GetOpenCaseReportData", _
                                      CommandType.StoredProcedure, _
                                      dtData)
            Return dtData
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            Return Nothing
        End Try

    End Function

    Public Function GetClosedCaseReportData() As DataTable

        Try
            Dim dtData As New DataTable()
            TBCultureDA.FillDataTable("GetClosedCaseReportData", _
                                      CommandType.StoredProcedure, _
                                      dtData)
            Return dtData
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            Return Nothing
        End Try

    End Function
#End Region

#Region "Get/Edit Casework entries"
    Public Function GetCaseWorkEntryByRBSE(ByVal sRBSE As String, ByRef dtData As DataTable) As Boolean

        Dim objInParamList As New ParameterList()

        Try
            objInParamList.QuickAddInputParam("RBSE", DbtType.dbtString, Replace(sRBSE, "/", ""))

            TBCultureDA.FillDataTable("GetCaseWorkEntryByRBSE", _
                                      CommandType.StoredProcedure, _
                                      dtData, _
                                      objInParamList)
            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            Return False
        End Try

    End Function

    Public Function EditCaseWorkEntry(ByRef objCaseWorkData As IDictionary) As Boolean

        Dim objInParamList As New ParameterList()

        Dim objDBConn As SqlConnection = Nothing
        Dim objDBTran As SqlTransaction = Nothing

        Try
            With objInParamList
                .QuickAddInputParam("RBSE", DbtType.dbtString, objCaseWorkData.Item("RBSE"))
                .QuickAddInputParam("Barcode", DbtType.dbtString, objCaseWorkData.Item("Barcode"))
                .QuickAddInputParam("AHFReference", DbtType.dbtString, objCaseWorkData.Item("AHFReference"))
                .QuickAddInputParam("PurchaserBSE1ReceivedDate", DbtType.dbtDate, objCaseWorkData.Item("PurchaserBSE1ReceivedDate"))
                .QuickAddInputParam("BreederBSE1ReceivedDate", DbtType.dbtDate, objCaseWorkData.Item("BreederBSE1ReceivedDate"))
                .QuickAddInputParam("Vendor1BSE1ReceivedDate", DbtType.dbtDate, objCaseWorkData.Item("Vendor1BSE1ReceivedDate"))
                .QuickAddInputParam("HomebredBSE1ReceivedDate", DbtType.dbtDate, objCaseWorkData.Item("HomebredBSE1ReceivedDate"))
                .QuickAddInputParam("SummarySheetReceivedDate", DbtType.dbtDate, objCaseWorkData.Item("SummarySheetReceivedDate"))
                .QuickAddInputParam("PaperworkCompleteDate", DbtType.dbtDate, objCaseWorkData.Item("PaperworkCompleteDate"))
                .QuickAddInputParam("ActiveMemoDate", DbtType.dbtDate, objCaseWorkData.Item("ActiveMemoDate"))
                .QuickAddInputParam("AnnexADate", DbtType.dbtDate, objCaseWorkData.Item("AnnexADate"))
                .QuickAddInputParam("AnnexBDate", DbtType.dbtDate, objCaseWorkData.Item("AnnexBDate"))
                .QuickAddInputParam("AnnexCDate", DbtType.dbtDate, objCaseWorkData.Item("AnnexCDate"))
                .QuickAddInputParam("AnnexDDate", DbtType.dbtDate, objCaseWorkData.Item("AnnexDDate"))
                .QuickAddInputParam("RegionalLab", DbtType.dbtString, objCaseWorkData.Item("RegionalLab"))
                .QuickAddInputParam("ReceivedByRegionalLabDate", DbtType.dbtDate, objCaseWorkData.Item("ReceivedByRegionalLabDate"))
                .QuickAddInputParam("InitialReceivedDate", DbtType.dbtDate, objCaseWorkData.Item("InitialReceivedDate"))
                .QuickAddInputParam("FinalReceivedDate", DbtType.dbtDate, objCaseWorkData.Item("FinalReceivedDate"))
                .QuickAddInputParam("FinalSentDate", DbtType.dbtDate, objCaseWorkData.Item("FinalSentDate"))
                .QuickAddInputParam("LabChasedDate", DbtType.dbtDate, objCaseWorkData.Item("LabChasedDate"))
                .QuickAddInputParam("BarbMinuteSentDate", DbtType.dbtDate, objCaseWorkData.Item("BarbMinuteSentDate"))
                .QuickAddInputParam("Post2000SentDate", DbtType.dbtDate, objCaseWorkData.Item("Post2000SentDate"))
                .QuickAddInputParam("CaseWorkNotes", DbtType.dbtString, objCaseWorkData.Item("CaseWorkNotes"))
                .QuickAddInputParam("DataCompleteDate", DbtType.dbtDate, objCaseWorkData.Item("DataCompleteDate"))
                .QuickAddInputParam("IsCaseClosed", DbtType.dbtBoolean, objCaseWorkData.Item("IsCaseClosed"))
                .QuickAddInputParam("UserID", DbtType.dbtInteger, objCaseWorkData.Item("UserID"))
                .QuickAddInputParam("TseTestingSite", DbtType.dbtString, objCaseWorkData.Item("TSETestingSite"))
                .QuickAddInputParam("SamplingDate", DbtType.dbtDate, objCaseWorkData.Item("SamplingDate"))
                .QuickAddInputParam("AHROId", DbtType.dbtInteger, objCaseWorkData.Item("AHROId"))

            End With

            objDBConn = TBCultureDA.OpenConnection()
            objDBTran = TBCultureDA.BeginTransaction(objDBConn)

            TBCultureDA.ExecuteNonQuery(objDBConn, objDBTran, "EditCaseWorkEntry", CommandType.StoredProcedure, objInParamList)

            TBCultureDA.CommitTransaction(objDBTran)
            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            Return False
        End Try

    End Function

    Public Function GetMinuteDetails(ByVal sRBSE As String, ByVal sMinuteType As String, ByRef dtData As DataTable) As Boolean
        Dim objInParamList As New ParameterList()

        Try
            objInParamList.QuickAddInputParam("RBSE", DbtType.dbtString, Replace(sRBSE, "/", ""))
            objInParamList.QuickAddInputParam("MinuteType", DbtType.dbtString, sMinuteType)
            TBCultureDA.FillDataTable("GetMinuteDetails", _
                                      CommandType.StoredProcedure, _
                                      dtData, _
                                      objInParamList)
            Return True
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            Return False
        End Try
    End Function

    Public Sub SetMinuteSentDate(ByVal sRBSE As String, ByVal sMinuteType As String)
        Dim objInParamList As New ParameterList()

        Try
            objInParamList.QuickAddInputParam("RBSE", DbtType.dbtString, Replace(sRBSE, "/", ""))
            objInParamList.QuickAddInputParam("MinuteType", DbtType.dbtString, sMinuteType)

            TBCultureDA.ExecuteNonQuery("SetMinuteSentDate", CommandType.StoredProcedure, objInParamList)
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
        End Try
    End Sub

#End Region

End Class
