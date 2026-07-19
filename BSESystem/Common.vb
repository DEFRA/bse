Module Common

#Region "Public Constants"
    ' Lookup constants
    Public Const LOOKUP_ANIMAL_ORIGIN As Integer = 1
    Public Const LOOKUP_FEED_RISK As Integer = 2
    Public Const LOOKUP_HORIZONTAL_RISK As Integer = 3
    Public Const LOOKUP_MATERNAL_RISK As Integer = 4
    Public Const LOOKUP_ANIMAL_STATUS As Integer = 5
    Public Const LOOKUP_CASE_FATE As Integer = 6
    Public Const LOOKUP_TEST_TYPE As Integer = 7
    Public Const LOOKUP_TEST_RESULT As Integer = 8
    Public Const LOOKUP_REPORTED_LOCATION As Integer = 9
    Public Const LOOKUP_SURVEY As Integer = 10
    Public Const LOOKUP_VALUATION_AGE As Integer = 11
    Public Const LOOKUP_SEX As Integer = 12
    Public Const LOOKUP_BREED As Integer = 13
    Public Const LOOKUP_HERD_TYPE As Integer = 14
    Public Const LOOKUP_PEDIGREE As Integer = 15
    Public Const LOOKUP_AHO As Integer = 16
    Public Const LOOKUP_SUPPLIER As Integer = 17
    Public Const LOOKUP_RELATION_TYPE As Integer = 18
    Public Const LOOKUP_RELATION_FATE As Integer = 19
    Public Const LOOKUP_RATION_TYPE As Integer = 20
    Public Const LOOKUP_OWNER_TYPE As Integer = 21
    Public Const LOOKUP_DOCUMENT_TYPE As Integer = 22
    Public Const LOOKUP_BSE_COUNTY As Integer = 23
    Public Const LOOKUP_BIRTH_DATE_SOURCE As Integer = 24
    Public Const LOOKUP_REGIONAL_LAB As Integer = 25
    Public Const LOOKUP_BSE_FORM As Integer = 26
    Public Const LOOKUP_TSE_TESTING_SITE As Integer = 27
    Public Const LOOKUP_AHRO As Integer = 28
    Public Const LOOKUP_CASE_TYPE As Integer = 29


    ' Case Entry Constants
    Public Const RBSE_CAPTION As String = "RBSE Number: "

#End Region

#Region "Public Methods"

    Public Function FormatRBSE(ByVal sRBSE As String) As String
        If sRBSE <> "" Then
            sRBSE = Replace(sRBSE, "/", "")
            sRBSE = Left$(sRBSE, 2) & "/" & Mid$(sRBSE, 3, 2) & "/" & Mid$(sRBSE, 5, 5)
        End If
        Return sRBSE
    End Function

    Public Function FormatDBSE(ByVal sDBSE As String) As String
        If sDBSE <> "" Then
            sDBSE = Replace(sDBSE, "/", "")
            sDBSE = Left$(sDBSE, 2) & "/" & Mid$(sDBSE, 3, 5)
        End If
        Return sDBSE
    End Function

    Public Function FormatCPHH(ByVal sCPHH As String) As String
        If sCPHH <> "" Then
            If Len(sCPHH) > 9 Then
                sCPHH = Left$(sCPHH, 2) & "/" & Mid$(sCPHH, 3, 3) & "/" & Mid$(sCPHH, 6, 4) & "/" & Mid$(sCPHH, 10, (Len(sCPHH) - 9))
            Else
                sCPHH = Left$(sCPHH, 2) & "/" & Mid$(sCPHH, 3, 3) & "/" & Mid$(sCPHH, 6, 4)
            End If
        End If

        Return sCPHH
    End Function

    Public Function GetRowColumnData(ByRef objValue As Object) As Object
        If (IsDBNull(objValue)) Then
            Return Nothing
        Else
            Return objValue
        End If
    End Function

    Public Sub GetBatchNumbersFromDatabase(ByVal sRBSE As String, ByRef objSession As System.Web.SessionState.HttpSessionState)
        Dim objCase As New BSELib.clsCase()
        Dim dtData As DataTable

        sRBSE = Replace(sRBSE, "/", "")

        If Not (objCase.GetBatchNumberByRBSE(sRBSE, dtData)) Then
            Throw New Exception("Case.GetBatchNumberByRBSE returned False")
        End If

        objSession.Item(SessionVars.SV_BatchNumbersTable) = dtData
    End Sub

    Public Sub GetCaseDetailsFromDatabase(ByVal sRBSE As String, ByRef objSession As System.Web.SessionState.HttpSessionState)
        Dim objCase As New BSELib.clsCase()
        Dim dsData As DataSet

        Try
            sRBSE = Replace(sRBSE, "/", "")
            If (sRBSE <> "") Then
                If Not (objCase.GetCaseDetails(sRBSE, dsData)) Then
                    Throw New Exception("Case.GetCaseDetails returned False")
                End If

                If dsData.Tables.Count <> 0 Then
                    '-----
                    If dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows.Count <> 0 Then
                        With objSession
                            .Item(SessionVars.SV_CPHHNumber) = dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows(0)("CPHH").ToString()

                            .Item(SessionVars.SV_CaseDetails) = dsData
                            .Item(SessionVars.SV_FeedTable) = dsData.Tables(BSELib.clsCase.FEED_TABLE)
                            .Item(SessionVars.SV_OtherOwnerTable) = dsData.Tables(BSELib.clsCase.OTHER_OWNER_TABLE)
                            .Item(SessionVars.SV_ClinicalVisitTable) = dsData.Tables(BSELib.clsCase.CLINICAL_VISIT_TABLE)
                            .Item(SessionVars.SV_TestTable) = dsData.Tables(BSELib.clsCase.TEST_TABLE)
                            .Item(SessionVars.SV_CaseWorkTable) = dsData.Tables(BSELib.clsCase.CASEWORK_TABLE)
                        End With
                    Else
                        With objSession
                            .Item(SessionVars.SV_CPHHNumber) = ""
                            ' Add row to each table
                            AddEmptyRow(dsData.Tables(BSELib.clsCase.CASE_TABLE), sRBSE)
                            With dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows(0)
                                .Item("IsPurchaserBSE1Received") = False
                                .Item("IsBreederBSE1Received") = False
                                .Item("IsVendor1BSE1Received") = False
                                .Item("IsHomebredBSE1Received") = False
                                .Item("IsSummarySheetReceived") = False
                                .Item("IsPaperworkComplete") = False
                            End With

                            Dim sBarcode As String
                            Dim sAHFReference As String
                            GetBarcodeAHFReferenceFromBSESS(sRBSE, sBarcode, sAHFReference)

                            AddEmptyRow(dsData.Tables(BSELib.clsCase.CASEWORK_TABLE), sRBSE)
                            With dsData.Tables(BSELib.clsCase.CASEWORK_TABLE).Rows(0)
                                .Item("RBSEDate") = FormatDate(Date.Today)
                                .Item("AHFReference") = sAHFReference
                                .Item("Barcode") = sBarcode
                            End With

                            'AddEmptyRow(dsData.Tables(CLINICAL_TABLE), sRBSE)
                            'AddEmtpyRow(dsData.Tables(BAB_TABLE), sRBSE)
                            'Put the tables into the Session Object
                            .Item(SessionVars.SV_CaseDetails) = dsData
                            .Item(SessionVars.SV_FeedTable) = dsData.Tables(BSELib.clsCase.FEED_TABLE)
                            .Item(SessionVars.SV_OtherOwnerTable) = dsData.Tables(BSELib.clsCase.OTHER_OWNER_TABLE)
                            .Item(SessionVars.SV_ClinicalVisitTable) = dsData.Tables(BSELib.clsCase.CLINICAL_VISIT_TABLE)
                            .Item(SessionVars.SV_TestTable) = dsData.Tables(BSELib.clsCase.TEST_TABLE)
                            .Item(SessionVars.SV_CaseWorkTable) = dsData.Tables(BSELib.clsCase.CASEWORK_TABLE)
                        End With
                    End If

                    If dsData.Tables(BSELib.clsCase.DAM_DETAILS_TABLE).Rows.Count = 0 Then
                        AddEmptyRow(dsData.Tables(BSELib.clsCase.DAM_DETAILS_TABLE), "", True)
                    End If
                    objSession.Item(SessionVars.SV_DamDetailsTable) = dsData.Tables(BSELib.clsCase.DAM_DETAILS_TABLE)

                    If dsData.Tables(BSELib.clsCase.SIRE_DETAILS_TABLE).Rows.Count = 0 Then
                        AddEmptyRow(dsData.Tables(BSELib.clsCase.SIRE_DETAILS_TABLE), "", True)
                    End If
                    objSession.Item(SessionVars.SV_SireDetailsTable) = dsData.Tables(BSELib.clsCase.SIRE_DETAILS_TABLE)

                    objSession.Item(SessionVars.SV_RelationsTable) = dsData.Tables(BSELib.clsCase.RELATION_TABLE)

                    '-----
                Else
                    Throw New ApplicationException("Case.GetCaseDetails returned no tables")
                End If
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to Get Case Details.", ex)
        End Try
    End Sub

    Public Sub GetBarcodeAHFReferenceFromBSESS(ByVal sRBSE As String, ByRef sBarcode As String, ByRef sAHFReference As String)
        'These are not used, but need to be provided as references to GetBSSESSCheckByRBSE stored procedure
        Dim sNotificationDate As String
        Dim sBSESSEartag As String
        Dim sBSESSBirthDate As String
        Dim sTestGroupName As String
        Dim sBSESSFinalResult As String
        Dim sFormADate As String
        Dim sBSEEartag As String
        Dim sBSEBirthDate As String
        Dim sSurvey As String
        Dim sBSEFinalResult As String

        BSELib.clsBSESS.GetBSESSCheckByRBSE(sRBSE, _
                                            sNotificationDate, _
                                            sBSESSEartag, _
                                            sBSESSBirthDate, _
                                            sTestGroupName, _
                                            sBSESSFinalResult, _
                                            sBarcode, _
                                            sFormADate, _
                                            sBSEEartag, _
                                            sBSEBirthDate, _
                                            sSurvey, _
                                            sBSEFinalResult, _
                                            sAHFReference)
    End Sub

    Public Sub AddEmptyRow(ByRef dtData As DataTable, ByVal sRBSE As String, Optional ByVal bAcceptChanges As Boolean = False)
        Dim drRow As DataRow

        drRow = dtData.NewRow()
        drRow.Item("RBSE") = sRBSE
        dtData.Rows.Add(drRow)
        If bAcceptChanges Then
            dtData.AcceptChanges()
        End If
    End Sub

    Public Sub RemoveCaseFromSession(ByRef objSession As System.Web.SessionState.HttpSessionState)

        With objSession
            .Remove(SessionVars.SV_RBSENumber)
            .Remove(SessionVars.SV_CPHHNumber)
            .Remove(SessionVars.SV_FarmDetails)
            .Remove(SessionVars.SV_RelatedFarmsTable)
            .Remove(SessionVars.SV_RelatedFarmsView)
            .Remove(SessionVars.SV_HerdSizeTable)
            .Remove(SessionVars.SV_HerdSizeView)
            .Remove(SessionVars.SV_CaseDetails)
            .Remove(SessionVars.SV_FeedTable)
            .Remove(SessionVars.SV_FeedView)
            .Remove(SessionVars.SV_OtherOwnerTable)
            .Remove(SessionVars.SV_OtherOwnerView)
            .Remove(SessionVars.SV_ClinicalVisitTable)
            .Remove(SessionVars.SV_ClinicalVisitView)
            .Remove(SessionVars.SV_DamDetailsTable)
            .Remove(SessionVars.SV_SireDetailsTable)
            .Remove(SessionVars.SV_RelationsTable)
            .Remove(SessionVars.SV_RelationsView)
            .Remove(SessionVars.SV_CaseWorkTable)
        End With

    End Sub

    Public Sub GetFarmDetailsFromDatabase(ByVal sCPHH As String, ByRef objSession As System.Web.SessionState.HttpSessionState)
        Dim objFarm As New BSELib.clsFarm()
        Dim dsData As DataSet

        Try
            If (sCPHH <> "") Then
                If Not (objFarm.GetFarmDetails(Replace(sCPHH, "/", ""), dsData)) Then
                    Throw New Exception("Case.GetFarmDetails returned False")
                End If
                objSession.Item(SessionVars.SV_FarmDetails) = dsData
                objSession.Item(SessionVars.SV_RelatedFarmsTable) = dsData.Tables(BSELib.clsFarm.RELATED_FARMS_TABLE)
                objSession.Item(SessionVars.SV_HerdSizeTable) = dsData.Tables(BSELib.clsFarm.HERDSIZE_TABLE)
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to 'Get Farm Details'.", ex)
        End Try
    End Sub


    Public Function FormatDate(ByVal sDate As String) As String
        Return Trim(Replace(sDate, "00:00:00", ""))
    End Function

    Public Function FormatEmptyString(ByVal sString As String) As Object
        If sString = "" Then
            Return DBNull.Value
        Else
            Return sString
        End If
    End Function

    Public Function GetLoggedOnUser() As String
        Dim strUser As String

        Try
            strUser = HttpContext.Current.User.Identity.Name
            Dim intSlashPos As Integer = strUser.IndexOf("\")
            If intSlashPos >= 0 And strUser.Length > intSlashPos + 1 Then
                strUser = strUser.Substring(intSlashPos + 1)
            End If

            Return strUser
        Catch
            Return Nothing
        End Try
    End Function

    Public Function IsVLAAllowedMainCaseEdit(ByRef objSession As System.Web.SessionState.HttpSessionState) As Boolean

        IsVLAAllowedMainCaseEdit = (CStr(objSession(SessionVars.SV_BatchNumber)) <> "" OrElse CType(objSession(SessionVars.SV_BatchNumbersTable), DataTable).Rows.Count > 0)

    End Function

    Public Function IsVLAAllowedAdditionalCaseEdit(ByRef objSession As System.Web.SessionState.HttpSessionState) As Boolean

        Dim dtBatch As DataTable = CType(objSession(SessionVars.SV_BatchNumbersTable), DataTable)
        IsVLAAllowedAdditionalCaseEdit = CStr(objSession(SessionVars.SV_BatchNumber)) = "" OrElse (dtBatch.Rows.Count > 0 AndAlso dtBatch.Select("BatchNumber = '" & CStr(objSession(SessionVars.SV_BatchNumber)) & "'").GetLength(0) = 0)

    End Function

    Public Sub AddItemToDropDownList(ByRef ddlList As DropDownList, _
                                     Optional ByVal sText As String = "", _
                                     Optional ByVal sValue As String = "", _
                                     Optional ByVal iIndex As Integer = 0)

        Dim liItem As New System.Web.UI.WebControls.ListItem()

        liItem.Text = sText
        liItem.Value = sValue
        ddlList.Items.Insert(iIndex, liItem)

    End Sub

    Public Sub AddItemToEndOfDropDownList(ByRef ddlList As DropDownList, _
                                          ByVal strText As String, _
                                          ByVal strValue As String)

        Dim liItem As New System.Web.UI.WebControls.ListItem()

        liItem.Text = strText
        liItem.Value = strValue
        ddlList.Items.Add(liItem)
    End Sub

    Public Sub SelectItemInDropDownList(ByRef ddlList As DropDownList, _
                                        ByVal strValue As String)

        Dim liItem As System.Web.UI.WebControls.ListItem

        For Each liItem In ddlList.Items
            If (Trim(liItem.Value) = Trim(strValue)) Then
                ddlList.SelectedItem.Selected = False
                liItem.Selected = True
                Exit For
            End If
        Next
    End Sub

    Public Function GetSelectedItemFromDropDownList(ByRef ddlList As DropDownList) As String
        If ddlList.SelectedItem Is Nothing Then
            Return ""
        Else
            Return Trim(ddlList.SelectedItem.Value)
        End If
    End Function

    Public Function GetSelectedTextFromDropDownList(ByRef ddlList As DropDownList) As String
        If ddlList.SelectedItem Is Nothing Then
            Return ""
        Else
            Return ddlList.SelectedItem.Text
        End If
    End Function

    ' Send down Javascript to set focus to a particular control
    Public Function SetFocus(ByVal ctl As System.Web.UI.Control, _
                                    Optional ByVal bSelectAll As Boolean = False) As Boolean
        If HttpContext.Current.Request.Browser.JavaScript Then
            Dim scr As String
            scr = "<script language='javascript'><!--" & vbNewLine
            scr &= "var ctl = document.getElementById(""" & ctl.UniqueID & """);" & vbNewLine
            scr &= "ctl.focus();" & vbNewLine

            If bSelectAll Then
                scr &= "ctl.select();" & vbNewLine
            End If

            scr &= "--></script>"

            ctl.Page.RegisterStartupScript("SetFocus", scr)

            Return True
        Else
            Return False
        End If
    End Function

    ' Sets default button for a textbox
    Public Function SetTextboxDefaultButton(ByRef ctlTextbox As TextBox, _
                                            ByRef ctlButton As Button) As Boolean

        If HttpContext.Current.Request.Browser.JavaScript Then
            Dim scr As New System.Text.StringBuilder()

            With scr
                .Append("<SCRIPT language=""javascript"">" & vbNewLine)
                .Append("function " & ctlTextbox.ClientID & "_OnKeyDown(btn)" & vbNewLine + "{" + vbNewLine)
                .Append("   if (event.keyCode == 13) {" & vbNewLine)
                .Append("       event.returnValue=false;" & vbNewLine)
                .Append("       event.cancel = true;" & vbNewLine)
                .Append("       if ((btn)) {" & vbNewLine)
                .Append("           if (!(btn.disabled)) {" & vbNewLine)
                .Append("               btn.click();" & vbNewLine)
                .Append("           }" & vbNewLine)
                .Append("       }" & vbNewLine)
                .Append("   } " & vbNewLine)
                .Append("}" & vbNewLine)
                .Append("</SCRIPT>" & vbNewLine)
            End With
            ctlTextbox.Attributes.Add("onkeydown", ctlTextbox.ClientID & "_OnKeyDown(document.all(""" & ctlButton.UniqueID & """))")
            ctlButton.Page.RegisterStartupScript(ctlTextbox.ClientID & "DefaultButton", scr.ToString())
            Return True
        Else
            Return False
        End If
    End Function

    Public Sub SetTextboxControlOnEnter(ByRef ctlTextbox As TextBox, _
                                        ByVal sControlClientID As String)

        If HttpContext.Current.Request.Browser.JavaScript Then
            Dim scr As New System.Text.StringBuilder()

            With scr
                .Append("<SCRIPT language=""javascript"">" & vbNewLine)
                .Append("function " & ctlTextbox.ClientID & "_OnKeyDown(bIsYear)" & vbNewLine + "{" + vbNewLine)
                .Append("    if (event.keyCode == 13) {" & vbNewLine)
                .Append("        event.returnValue=false;" & vbNewLine)
                .Append("        event.cancel = true;" & vbNewLine)
                .Append("       if ((Form1." & sControlClientID & ")) {" & vbNewLine)
                .Append("           if (!(Form1." & sControlClientID & ".disabled)) {" & vbNewLine)
                .Append("               Form1." & sControlClientID & ".focus()" & vbNewLine)
                .Append("           }" & vbNewLine)
                .Append("       }" & vbNewLine)
                .Append("    } " + vbNewLine)
                .Append("}" + vbNewLine)
                .Append("</SCRIPT>" + vbNewLine)
            End With
            ctlTextbox.Attributes.Add("onkeydown", ctlTextbox.ClientID & "_OnKeyDown()")
            ctlTextbox.Page.RegisterStartupScript(ctlTextbox.ClientID & "EnterKey", scr.ToString())
        End If
    End Sub

    Public Function IsDateRangeValid(ByVal ctlDateFrom As CalendarDate, ByVal ctlDateTo As CalendarDate, ByVal sName As String) As Boolean

        Dim bDatesValid As Boolean = True
        Dim dDate As Date

        If ctlDateFrom.DateField <> "" AndAlso IsDate(ctlDateTo.DateField) Then
            bDatesValid = ctlDateFrom.Validate(CDate(ctlDateTo.DateField), CalendarDate.ValidationType.eValidateLatest, "Must be earlier than the specified latest " & sName)
        End If
        If ctlDateTo.DateField <> "" AndAlso IsDate(ctlDateFrom.DateField) Then
            bDatesValid = bDatesValid And ctlDateTo.Validate(CDate(ctlDateFrom.DateField), CalendarDate.ValidationType.eValidateEarliest, "Must be later than the specified earliest " & sName)
        End If
        bDatesValid = bDatesValid And ctlDateFrom.Validate(dDate) And ctlDateTo.Validate(dDate)

        Return bDatesValid

    End Function

    Public Function PromptBeforeNavigateScript(ByVal sPrompt As String, ByVal sURL As String) As String

        Dim jScript As System.Text.StringBuilder = New System.Text.StringBuilder()
        With jScript
            .Append("<script language=""JavaScript"">")
            .Append("if (confirm(""")
            .Append(sPrompt)
            .Append(""")) { location.href=""")
            .Append(sURL)
            .Append(""" }")
            .Append("</script>")
        End With
        Return jScript.ToString()

    End Function

    Public Function SplitBatchNumber(ByVal sHoleBatchNumber As String, _
                                     ByRef sBatchYear As String, _
                                     ByRef sBatchNumber As String)

        sHoleBatchNumber = Replace(sHoleBatchNumber, "/", "")
        sBatchYear = Left$(sHoleBatchNumber, 4)
        sBatchNumber = Mid$(sHoleBatchNumber, 5, (Len(sHoleBatchNumber) - 4))

    End Function

    Public Function GetSpolSiteButtonCode(ByVal sId As String) As String

        Dim htmlCode As New System.Text.StringBuilder()
        With htmlCode
            .Append("<input type=""button"" value=""")
            .Append("View Docs")
            .Append(""" onclick=""javascript:window.open('")
            .Append(CStr(System.Configuration.ConfigurationSettings.AppSettings("SPOLSite")))
            .Append(Replace(sId, "/", ""))
            .Append("', 'doc', 'width=400,height=300,toolbar=yes,location=yes,menubar=yes,scrollbars=yes,resizable=yes')"">")
        End With
        Return htmlCode.ToString()

    End Function

#End Region

End Module
