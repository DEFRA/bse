Imports System.Web
Imports System.Web.Services
Imports System.Collections.Generic

Partial Public Class CaseWorkEntry
    Inherits System.Web.UI.Page
    Protected WithEvents ctlSamplingDateValue As Global.BSESystem.CalendarDate
    Private Shared dvAutoCompleteData As DataView
    Dim mSurveyTypeFS As String = "fallen stock"
    Dim mSurveyTypeSC As String = "surveillance cohort"
    Dim strUnknown As String = "unknown"
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        If IsNothing(Session(SessionVars.SV_RBSENumber)) Then
            Response.Redirect("SessionError.aspx")
        End If

        VLAHeader1.PageTitle = "Casework Entry"
        lblError.Visible = False

        Dim sGroupName As String = CStr(Session(SessionVars.SV_HeaderGroupName))

        If sGroupName <> "VLA Maintenance" Then
            Response.Redirect("Home.aspx")
        End If

        If Not IsPostBack Then
            LoadLookupLists()
            LoadCaseWorkDetails()
        End If

        SetEnterKeys()
    End Sub

    Private Sub SetEnterKeys()
        SetTextboxControlOnEnter(txtBarcodeValue, txtAHFReferenceValue.ClientID)
        SetTextboxControlOnEnter(txtAHFReferenceValue, ddlRegionalLabValue.ClientID)

        ctlRegionalLabReceivedDateValue.SetControlOnEnter(ctlInitialReceivedDateValue.FirstClientID)
        ctlInitialReceivedDateValue.SetControlOnEnter(ctlFinalReceivedDateValue.FirstClientID)
        ctlFinalReceivedDateValue.SetControlOnEnter(ctlFinalSentDateValue.FirstClientID)

        ctlFinalSentDateValue.SetControlOnEnter(ctlPurchaserBSE1ReceivedDateValue.FirstClientID)

        ctlPurchaserBSE1ReceivedDateValue.SetControlOnEnter(ctlBreederBSE1ReceivedDateValue.FirstClientID)
        ctlBreederBSE1ReceivedDateValue.SetControlOnEnter(ctlVendor1BSE1ReceivedDateValue.FirstClientID)
        ctlVendor1BSE1ReceivedDateValue.SetControlOnEnter(ctlHomeBredBSE1ReceivedDateValue.FirstClientID)
        ctlHomeBredBSE1ReceivedDateValue.SetControlOnEnter(ctlSummarySheetReceivedDateValue.FirstClientID)
        ctlSummarySheetReceivedDateValue.SetControlOnEnter(ctlPaperworkCompleteDateValue.FirstClientID)
        ctlPaperworkCompleteDateValue.SetControlOnEnter(ctlDataCompleteDateValue.FirstClientID)

        ctlDataCompleteDateValue.SetControlOnEnter(ctlLabChasedDateValue.FirstClientID)

        ctlLabChasedDateValue.SetControlOnEnter(ctlBarbMemoDateValue.FirstClientID)
        If (ctlPost2000SentDateValue.Visible) Then
            ctlBarbMemoDateValue.SetControlOnEnter(ctlPost2000SentDateValue.FirstClientID)
        Else
            ctlBarbMemoDateValue.SetControlOnEnter(chkOpenCloseCase.ClientID)
        End If
    End Sub

#Region "Lookup Population"

    Private Sub LoadLookupLists()

        Dim blnResult As Boolean
        Dim objDataTable As DataTable
        Dim objLookup As New BSELib.LookupData()

        Try
            objDataTable = objLookup.GetLookupData(LOOKUP_REGIONAL_LAB)
            If Not (objDataTable Is Nothing) Then
                ddlRegionalLabValue.DataSource = objDataTable
                ddlRegionalLabValue.DataValueField = "Code"
                ddlRegionalLabValue.DataTextField = "Description"
                ddlRegionalLabValue.DataBind()
                Common.AddItemToDropDownList(ddlRegionalLabValue)
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve lookup data", ex)
        End Try

        Try
            objDataTable.Clear()
            objDataTable = objLookup.GetLookupData(LOOKUP_AHRO)
            If Not (objDataTable Is Nothing) Then
                ddlAHRO.DataSource = objDataTable
                ddlAHRO.DataValueField = "ID"
                ddlAHRO.DataTextField = "Name"
                ddlAHRO.DataBind()
                Common.AddItemToDropDownList(ddlAHRO)
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve lookup data", ex)
        End Try

    End Sub

#End Region

#Region "Handle Data"

    Private Function GetDescriptionFromLookupTable(ByVal iTableNumber As Integer, ByVal sCode As String) As String

        If sCode = "" Then
            Return Nothing
        End If

        Dim objDataTable As DataTable
        Dim objLookup As New BSELib.LookupData()
        Dim sFilterExpression As String = "Code = '" & sCode & "'"

        Try
            objDataTable = objLookup.GetLookupData(iTableNumber)
            If Not (objDataTable Is Nothing) Then
                Dim objDataRow As DataRow = objDataTable.Select(sFilterExpression)(0)
                Return objDataRow.Item("Description")
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve lookup data", ex)
        End Try

    End Function

    Private Function LoadCaseWorkDetails() As Boolean
        Try
            Dim sRBSE As String = Session.Item(SessionVars.SV_RBSENumber)
            Dim objCase As New BSELib.clsCase()
            Dim dtData As DataTable
            Dim dtTestData As DataTable

            If Not (objCase.GetCaseWorkEntryByRBSE(sRBSE, dtData)) Then
                Throw New Exception("Case.GetCaseWorkEntryByRBSE returned False")
            End If

            If Not (objCase.GetTestDataByRBSE(Replace(sRBSE, "/", ""), dtTestData)) Then
                Throw New Exception("Case.GetTestDataByRBSE returned False")
            End If

            If dtData.Rows(0).Item("IsCaseClosed") = "1" Then
                Session.Item(SessionVars.SV_IsCaseClosed) = True
            Else
                Session.Item(SessionVars.SV_IsCaseClosed) = False
            End If

            If dtData.Rows.Count <> 0 Then
                With dtData.Rows(0)
                    lblRBSEValue.Text = FormatRBSE(.Item("RBSE").ToString())
                    lblRBSEDateValue.Text = FormatDate(.Item("RBSEDate").ToString())
                    lblFormADateValue.Text = FormatDate(.Item("FormADate").ToString())
                    lblFormBDateValue.Text = FormatDate(.Item("FormBDate").ToString())
                    lblSlaughterDateValue.Text = FormatDate(.Item("SlaughterDate").ToString())
                    lblFateValue.Text = GetDescriptionFromLookupTable(LOOKUP_CASE_FATE, .Item("Fate").ToString())
                    lblOriginValue.Text = GetDescriptionFromLookupTable(LOOKUP_ANIMAL_ORIGIN, .Item("Origin").ToString())
                    lblSurveyValue.Text = GetDescriptionFromLookupTable(LOOKUP_SURVEY, .Item("Survey").ToString())
                    txtBarcodeValue.Text = .Item("Barcode").ToString()
                    txtAHFReferenceValue.Text = .Item("AHFReference").ToString()

                    SelectItemInDropDownList(ddlRegionalLabValue, .Item("RegionalLab").ToString())
                    ctlRegionalLabReceivedDateValue.DateField = FormatDate(.Item("ReceivedByRegionalLabDate").ToString())
                    ctlInitialReceivedDateValue.DateField = FormatDate(.Item("InitialReceivedDate").ToString())
                    ctlFinalReceivedDateValue.DateField = FormatDate(.Item("FinalReceivedDate").ToString())
                    lblFinalResultValue.Text = GetDescriptionFromLookupTable(LOOKUP_TEST_RESULT, .Item("FinalResult").ToString())
                    lblFinalResultDateValue.Text = FormatDate(.Item("FinalResultDate").ToString())
                    lblDBSEValue.Text = FormatDBSE(.Item("DBSE").ToString())
                    lblAlternateDiagnosisValue.Text = .Item("AlternateDiagnosis").ToString()
                    ctlFinalSentDateValue.DateField = FormatDate(.Item("FinalSentDate").ToString())
                    lblBirthDateValue.Text = FormatDate(.Item("BirthDate").ToString())

                    ctlPurchaserBSE1ReceivedDateValue.DateField = FormatDate(.Item("PurchaserBSE1ReceivedDate").ToString())
                    ctlBreederBSE1ReceivedDateValue.DateField = FormatDate(.Item("BreederBSE1ReceivedDate").ToString())
                    ctlVendor1BSE1ReceivedDateValue.DateField = FormatDate(.Item("Vendor1BSE1ReceivedDate").ToString())
                    ctlHomeBredBSE1ReceivedDateValue.DateField = FormatDate(.Item("HomeBredBSE1ReceivedDate").ToString())
                    ctlSummarySheetReceivedDateValue.DateField = FormatDate(.Item("SummarySheetReceivedDate").ToString())
                    ctlPaperworkCompleteDateValue.DateField = FormatDate(.Item("PaperworkCompleteDate").ToString())
                    ctlDataCompleteDateValue.DateField = FormatDate(.Item("DataCompleteDate").ToString())

                    lblActiveMemoDueDateValue.Text = FormatDate(.Item("ActiveMemoDueDate").ToString())
                    lblAnnexADueDateValue.Text = FormatDate(.Item("AnnexADueDate").ToString())
                    lblAnnexBDueDateValue.Text = FormatDate(.Item("AnnexBDueDate").ToString())
                    lblAnnexCDueDateValue.Text = FormatDate(.Item("AnnexCDueDate").ToString())
                    lblAnnexDDueDateValue.Text = FormatDate(.Item("AnnexDDueDate").ToString())
                    lblActiveMemoDateValue.Text = FormatDate(.Item("ActiveMemoDate").ToString())
                    lblAnnexADateValue.Text = FormatDate(.Item("AnnexADate").ToString())
                    lblAnnexBDateValue.Text = FormatDate(.Item("AnnexBDate").ToString())
                    lblAnnexCDateValue.Text = FormatDate(.Item("AnnexCDate").ToString())
                    lblAnnexDDateValue.Text = FormatDate(.Item("AnnexDDate").ToString())

                    If CDate(.Item("RBSEDate")) >= Date.Today Then
                        btnAnnexASend.Enabled = False
                        btnAnnexASend.ToolTip = "Annex A cannot be sent until after the RBSE Date"
                        btnAnnexBSend.Enabled = False
                        btnAnnexBSend.ToolTip = "Annex B cannot be sent until after the RBSE Date"
                        btnAnnexCSend.Enabled = False
                        btnAnnexCSend.ToolTip = "Annex C cannot be sent until after the RBSE Date"
                        btnAnnexDSend.Enabled = False
                        btnAnnexDSend.ToolTip = "Annex D cannot be sent until after the RBSE Date"
                    End If

                    If .Item("AnnexADate").ToString() = "" Then
                        btnAnnexBSend.Enabled = False
                        btnAnnexBSend.ToolTip = "Annex B cannot be sent before Annex A"
                    End If
                    If .Item("AnnexCDate").ToString() = "" Then
                        btnAnnexDSend.Enabled = False
                        btnAnnexDSend.ToolTip = "Annex D cannot be sent before Annex C"
                    End If

                    lblLabChaseDueDateValue.Text = FormatDate(.Item("LabChaseDueDate").ToString())
                    ctlLabChasedDateValue.DateField = FormatDate(.Item("LabChasedDate").ToString())
                    lblBarbMemoDueValue.Text = .Item("BarbMemoDue").ToString()
                    ctlBarbMemoDateValue.DateField = FormatDate(.Item("BarbMinuteSentDate").ToString())

                    'always unchecked on load, but text changes according to closed/open status:
                    chkOpenCloseCase.Checked = False
                    If .Item("IsCaseClosed") = "1" Then
                        lblStatusValue.Text = "Closed"
                        chkOpenCloseCase.Text = "Open Case"
                    Else
                        lblStatusValue.Text = "Open"
                        chkOpenCloseCase.Text = "Close Case"
                    End If

                    txtCaseworkComments.Text = .Item("CaseWorkNotes").ToString()

                    'Check if any result in test data is not negative
                    Dim isResultPositive As Boolean = False
                    For Each dr As DataRow In dtTestData.Rows
                        If dr.Item("TestResult").ToString() <> "Neg" Then
                            isResultPositive = True
                        End If
                    Next

                    'Rearrange date into order suitable for comparison as text (YYYYMMDD)
                    Dim sBirthDate As String = FormatDate(.Item("BirthDate").ToString())
                    Dim sISOStandardBirthDate As String = Mid(sBirthDate, 7, 4) & Mid(sBirthDate, 4, 2) & Left(sBirthDate, 2)

                    If sISOStandardBirthDate > "20001231" Then
                        If .Item("Fate").ToString() = "DIED" Or .Item("Fate").ToString() = "SL" Then
                            If isResultPositive = True Then
                                lblBirthDateAfter2000.Visible = True
                                lblDefraInformedOn.Visible = True
                                ctlPost2000SentDateValue.Visible = True
                            End If
                        End If
                    End If

                    ctlPost2000SentDateValue.DateField = FormatDate(.Item("Post2000SentDate").ToString())

                    Dim strSamplingDate As String = (.Item("SamplingDate").ToString())

                    If (strSamplingDate = Date.MaxValue.ToString) Then
                        ctlSamplingDateValue.DateField = strUnknown
                    Else
                        ctlSamplingDateValue.DateField = FormatDate(strSamplingDate)
                    End If

                    txtTseTestingsite.Text = .Item("TseTestingSite").ToString()
                    SelectItemInDropDownList(ddlAHRO, .Item("AHRO").ToString())
                End With

                SetEditableFields(lblSurveyValue.Text)
                SetLabelColours(dtData.Rows(0))

                Return True
            End If
            Return False

        Catch ex As Exception
            clsAppError.DisplayError("Failed to Load Case Details.", ex)
            Return False
        End Try
    End Function

    Private Sub SetLabelColours(ByRef drDataRow As DataRow)

        If Not IsDBNull(drDataRow.Item("BarbMemoDue")) Then
            If lblBarbMemoDueValue.Text = "Yes" Then
                lblBarbMemoDueValue.ForeColor = Color.Red
                lblBarbMemoDueValue.Font.Bold = True
            End If
        End If

        If Not IsDBNull(drDataRow.Item("ActiveMemoDueDate")) Then
            If lblActiveMemoDateValue.Text <> "" Then
                lblActiveMemoDueDateValue.Visible = False
            Else
                If Date.Compare(drDataRow.Item("ActiveMemoDueDate"), Today) <= 0 Then
                    lblActiveMemoDueDateValue.ForeColor = Color.Red
                    lblActiveMemoDueDateValue.Font.Italic = True
                End If
            End If
        End If

        If Not IsDBNull(drDataRow.Item("AnnexADueDate")) Then
            If lblAnnexADateValue.Text <> "" Then
                lblAnnexADueDateValue.Visible = False
            Else
                If Date.Compare(drDataRow.Item("AnnexADueDate"), Today) <= 0 Then
                    lblAnnexADueDateValue.ForeColor = Color.Red
                    lblAnnexADueDateValue.Font.Italic = True
                End If
            End If
        End If

        If Not IsDBNull(drDataRow.Item("AnnexBDueDate")) Then
            If lblAnnexBDateValue.Text <> "" Then
                lblAnnexBDueDateValue.Visible = False
            Else
                If Date.Compare(drDataRow.Item("AnnexBDueDate"), Today) <= 0 Then
                    lblAnnexBDueDateValue.ForeColor = Color.Red
                    lblAnnexBDueDateValue.Font.Italic = True
                End If
            End If
        End If

        If Not IsDBNull(drDataRow.Item("AnnexCDueDate")) Then
            If lblAnnexCDateValue.Text <> "" Then
                lblAnnexCDueDateValue.Visible = False
            Else
                If Date.Compare(drDataRow.Item("AnnexCDueDate"), Today) <= 0 AndAlso lblActiveMemoDateValue.Text = "" Then
                    lblAnnexCDueDateValue.ForeColor = Color.Red
                    lblAnnexCDueDateValue.Font.Italic = True
                End If
            End If
        End If

        If Not IsDBNull(drDataRow.Item("AnnexDDueDate")) Then
            If lblAnnexDDateValue.Text <> "" Then
                lblAnnexDDueDateValue.Visible = False
            Else
                If Date.Compare(drDataRow.Item("AnnexDDueDate"), Today) <= 0 AndAlso lblActiveMemoDateValue.Text = "" Then
                    lblAnnexDDueDateValue.ForeColor = Color.Red
                    lblAnnexDDueDateValue.Font.Italic = True
                End If
            End If
        End If

        If Not IsDBNull(drDataRow.Item("LabChaseDueDate")) Then
            If ctlLabChasedDateValue.DateField.ToString() <> "" Then
                lblLabChaseDueDateValue.ForeColor = Color.Blue
            Else
                If Date.Compare(drDataRow.Item("LabChaseDueDate"), Today) <= 0 AndAlso lblActiveMemoDateValue.Text = "" Then
                    lblLabChaseDueDateValue.ForeColor = Color.Red
                    lblLabChaseDueDateValue.Font.Italic = True
                End If
            End If
        End If
    End Sub

    Private Function SaveCaseworkDetails(Optional ByRef isCaseClosedAfterEdit As Boolean = False) As Boolean
        Dim sRBSE As String = Replace(Session.Item(SessionVars.SV_RBSENumber), "/", "")
        Dim isCaseClosedBeforeEdit As Boolean = Session.Item(SessionVars.SV_IsCaseClosed)

        Dim objCase As New BSELib.clsCase()

        Dim objCaseWorkData As New Hashtable

        If Not PurchaserBSE1ReceivedDateValid() _
        OrElse Not BreederBSE1ReceivedDateValid() _
        OrElse Not Vendor1BSE1ReceivedDateValid() _
        OrElse Not HomebredBSE1ReceivedDateValid() _
        OrElse Not SummarySheetReceivedDateValid() _
        OrElse Not PaperworkCompleteDateValid() _
        OrElse Not FinalReceivedDateValid() _
        OrElse Not FinalSentDateValid() _
        OrElse Not LabChasedDateValid() _
        OrElse Not BarbMemoDateValid() _
        OrElse Not Post2000SentDateValid() _
        OrElse Not DataCompleteDateValid() _
        OrElse Not TseTestingSiteValid() _
        OrElse Not SamplingDateValid() Then
            Return False
        End If
        Try
            With objCaseWorkData
                .Add("RBSE", sRBSE)
                .Add("Barcode", txtBarcodeValue.Text)
                .Add("AHFReference", txtAHFReferenceValue.Text)
                .Add("PurchaserBSE1ReceivedDate", FormatEmptyString(ctlPurchaserBSE1ReceivedDateValue.DateField))
                .Add("BreederBSE1ReceivedDate", FormatEmptyString(ctlBreederBSE1ReceivedDateValue.DateField))
                .Add("Vendor1BSE1ReceivedDate", FormatEmptyString(ctlVendor1BSE1ReceivedDateValue.DateField))
                .Add("HomebredBSE1ReceivedDate", FormatEmptyString(ctlHomeBredBSE1ReceivedDateValue.DateField))
                .Add("SummarySheetReceivedDate", FormatEmptyString(ctlSummarySheetReceivedDateValue.DateField))
                .Add("PaperworkCompleteDate", FormatEmptyString(ctlPaperworkCompleteDateValue.DateField))
                .Add("ActiveMemoDate", FormatEmptyString(lblActiveMemoDateValue.Text))
                .Add("AnnexADate", FormatEmptyString(lblAnnexADateValue.Text))
                .Add("AnnexBDate", FormatEmptyString(lblAnnexBDateValue.Text))
                .Add("AnnexCDate", FormatEmptyString(lblAnnexCDateValue.Text))
                .Add("AnnexDDate", FormatEmptyString(lblAnnexDDateValue.Text))
                .Add("RegionalLab", FormatEmptyString(Trim(ddlRegionalLabValue.SelectedItem.Value.ToString()))) 'use code
                .Add("ReceivedByRegionalLabDate", FormatEmptyString(ctlRegionalLabReceivedDateValue.DateField))
                .Add("InitialReceivedDate", FormatEmptyString(ctlInitialReceivedDateValue.DateField))
                .Add("FinalReceivedDate", FormatEmptyString(ctlFinalReceivedDateValue.DateField))
                .Add("FinalSentDate", FormatEmptyString(ctlFinalSentDateValue.DateField))
                .Add("LabChasedDate", FormatEmptyString(ctlLabChasedDateValue.DateField))
                .Add("BarbMinuteSentDate", FormatEmptyString(ctlBarbMemoDateValue.DateField))
                .Add("Post2000SentDate", FormatEmptyString(ctlPost2000SentDateValue.DateField))
                .Add("CaseWorkNotes", txtCaseworkComments.Text)
                .Add("DataCompleteDate", FormatEmptyString(ctlDataCompleteDateValue.DateField))
                .Add("UserID", Session(SessionVars.SV_HeaderUserID))
                .Add("TSETestingSite", FormatEmptyString(txtTseTestingsite.Text))
                If (ctlSamplingDateValue.AllowUnknown = True And ctlSamplingDateValue.DateField.ToLower = strUnknown) Then
                    .Add("SamplingDate", FormatEmptyString(Date.MaxValue))
                Else
                    .Add("SamplingDate", FormatEmptyString(ctlSamplingDateValue.DateField))
                End If
                If (String.IsNullOrEmpty(ddlAHRO.SelectedItem.Value) = True) Then
                    .Add("AHROId", DBNull.Value)
                Else
                    .Add("AHROId", ddlAHRO.SelectedItem.Value)
                End If
                If isCaseClosedBeforeEdit Then
                    .Add("IsCaseClosed", Not chkOpenCloseCase.Checked)
                    isCaseClosedAfterEdit = Not chkOpenCloseCase.Checked
                Else
                    .Add("IsCaseClosed", chkOpenCloseCase.Checked)
                    isCaseClosedAfterEdit = chkOpenCloseCase.Checked
                End If
            End With

            If objCase.EditCaseWorkEntry(objCaseWorkData) Then
                Return True
            Else
                Throw New Exception("Case.EditCaseWorkEntry returned False")
            End If

        Catch ex As Exception
            Return False
        End Try

    End Function

    Private Sub SetEditableFields(ByVal strSurveyType As String)
        If ((strSurveyType.ToLower() = mSurveyTypeFS) Or (strSurveyType.ToLower() = mSurveyTypeSC)) Then
            txtTseTestingsite.Visible = True
            ctlSamplingDateValue.Visible = True
            txtTseTestingsite.Enabled = True
            ctlSamplingDateValue.Enabled = True
            lblAHRO.Visible = True
            ddlAHRO.Visible = True
        Else
            txtTseTestingsite.Visible = False
            ctlSamplingDateValue.Visible = False
            lblTseTestingsite.Visible = False
            lblSamplingDate.Visible = False
            lblAHRO.Visible = False
            ddlAHRO.Visible = False
        End If
    End Sub

#End Region

#Region "Event Handlers"

    Protected Sub btnSave_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSave.Click
        Dim sRBSE As String = Replace(Session.Item(SessionVars.SV_RBSENumber), "/", "")
        Dim isCaseClosedAfterEdit As Boolean

        If SaveCaseworkDetails(isCaseClosedAfterEdit) Then
            If isCaseClosedAfterEdit Then
                Response.Redirect("CaseWorkClosedReport.aspx?rbse=" & sRBSE)
            Else
                Response.Redirect("CaseWorkOpenReport.aspx?rbse=" & sRBSE)
            End If
        End If
    End Sub

    Protected Sub btnSaveTop_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSaveTop.Click
        Dim sRBSE As String = Replace(Session.Item(SessionVars.SV_RBSENumber), "/", "")
        Dim isCaseClosedAfterEdit As Boolean

        If SaveCaseworkDetails(isCaseClosedAfterEdit) Then
            If isCaseClosedAfterEdit Then
                Response.Redirect("CaseWorkClosedReport.aspx?rbse=" & sRBSE)
            Else
                Response.Redirect("CaseWorkOpenReport.aspx?rbse=" & sRBSE)
            End If
        End If
    End Sub

    Protected Sub btnCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancel.Click
        Dim sRBSE As String = Replace(Session.Item(SessionVars.SV_RBSENumber), "/", "")
        Dim isCaseClosed As Boolean = Session.Item(SessionVars.SV_IsCaseClosed)

        If isCaseClosed Then
            Response.Redirect("CaseWorkClosedReport.aspx?rbse=" & sRBSE)
        Else
            Response.Redirect("CaseWorkOpenReport.aspx?rbse=" & sRBSE)
        End If
    End Sub

    Protected Sub btnCancelTop_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancelTop.Click
        Dim sRBSE As String = Replace(Session.Item(SessionVars.SV_RBSENumber), "/", "")
        Dim isCaseClosed As Boolean = Session.Item(SessionVars.SV_IsCaseClosed)

        If isCaseClosed Then
            Response.Redirect("CaseWorkClosedReport.aspx?rbse=" & sRBSE)
        Else
            Response.Redirect("CaseWorkOpenReport.aspx?rbse=" & sRBSE)
        End If
    End Sub

    Protected Sub lblRBSEValue_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lblRBSEValue.Click
        Dim sRBSE As String = Session.Item(SessionVars.SV_RBSENumber)

        GetCaseDetailsFromDatabase(sRBSE, Session)
        Dim dsData As DataSet = Session.Item(SessionVars.SV_CaseDetails)

        Dim dtCase As DataTable = CType(dsData.Tables(BSELib.clsCase.CASE_TABLE), DataTable)

        'if the existing case is a non-GB case
        If (dtCase.Rows.Count <> 0) AndAlso CBool((dtCase.Rows(0)("IsNonGBCase")) = True) Then
            GetFarmDetailsFromDatabase(Session.Item(SessionVars.SV_CPHHNumber), Session)
            GetBatchNumbersFromDatabase(sRBSE, Session)
            Response.Redirect("CaseEntryDEFRA.aspx")
        Else 'the existing case is a GB case
            GetFarmDetailsFromDatabase(Session.Item(SessionVars.SV_CPHHNumber), Session)
            GetBatchNumbersFromDatabase(sRBSE, Session)
            Response.Redirect("CaseEntryDEFRA.aspx")
        End If

    End Sub

    Protected Sub btnActiveMemoSend_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnActiveMemoSend.Click
        If SaveCaseworkDetails() Then
            If lblActiveMemoDateValue.Text = "" Then
                Dim objCase As New BSELib.clsCase()
                objCase.SetMinuteSentDate(Session.Item(SessionVars.SV_RBSENumber), "ActiveMemo")
            End If
            If ((lblSurveyValue.Text.ToLower = mSurveyTypeFS) Or (lblSurveyValue.Text.ToLower = mSurveyTypeSC)) Then
                Response.Redirect("CaseWorkMinuteConfirmation.aspx?minute=ActiveMemoFS")
            Else
                Response.Redirect("CaseWorkMinuteConfirmation.aspx?minute=ActiveMemo")
            End If
        End If
    End Sub

    Protected Sub btnAnnexASend_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnAnnexASend.Click
        If SaveCaseworkDetails() Then
            If lblAnnexADateValue.Text = "" Then
                Dim objCase As New BSELib.clsCase()
                objCase.SetMinuteSentDate(Session.Item(SessionVars.SV_RBSENumber), "AnnexA")
            End If
            Response.Redirect("CaseWorkMinuteConfirmation.aspx?minute=AnnexA")
        End If
    End Sub

    Protected Sub btnAnnexBSend_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnAnnexBSend.Click

        If SaveCaseworkDetails() Then
            If lblAnnexBDateValue.Text = "" Then
                Dim objCase As New BSELib.clsCase()
                objCase.SetMinuteSentDate(Session.Item(SessionVars.SV_RBSENumber), "AnnexB")
            End If
            Response.Redirect("CaseWorkMinuteConfirmation.aspx?minute=AnnexB")
        End If
    End Sub

    Protected Sub btnAnnexCSend_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnAnnexCSend.Click
        If SaveCaseworkDetails() Then
            If lblAnnexCDateValue.Text = "" Then
                Dim objCase As New BSELib.clsCase()
                objCase.SetMinuteSentDate(Session.Item(SessionVars.SV_RBSENumber), "AnnexC")
            End If
            Response.Redirect("CaseWorkMinuteConfirmation.aspx?minute=AnnexC")
        End If
    End Sub

    Protected Sub btnAnnexDSend_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnAnnexDSend.Click
        If SaveCaseworkDetails() Then
            If lblAnnexDDateValue.Text = "" Then
                Dim objCase As New BSELib.clsCase()
                objCase.SetMinuteSentDate(Session.Item(SessionVars.SV_RBSENumber), "AnnexD")
            End If
            Response.Redirect("CaseWorkMinuteConfirmation.aspx?minute=AnnexD")
        End If
    End Sub

    Private Sub ctlPurchaserBSE1ReceivedDateValue_DateChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ctlPurchaserBSE1ReceivedDateValue.DateChanged
        PurchaserBSE1ReceivedDateValid()
    End Sub

    Private Sub ctlBreederBSE1ReceivedDateValue_DateChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ctlBreederBSE1ReceivedDateValue.DateChanged
        BreederBSE1ReceivedDateValid()
    End Sub

    Private Sub ctlVendor1BSE1ReceivedDateValue_DateChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ctlVendor1BSE1ReceivedDateValue.DateChanged
        Vendor1BSE1ReceivedDateValid()
    End Sub

    Private Sub ctlHomebredBSE1ReceivedDateValue_DateChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ctlHomeBredBSE1ReceivedDateValue.DateChanged
        HomebredBSE1ReceivedDateValid()
    End Sub

    Private Sub ctlSummarySheetReceivedDateValue_DateChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ctlSummarySheetReceivedDateValue.DateChanged
        SummarySheetReceivedDateValid()
    End Sub

    Private Sub ctlPaperworkCompleteDateValue_DateChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ctlPaperworkCompleteDateValue.DateChanged
        PaperworkCompleteDateValid()
    End Sub

    Private Sub ctlFinalReceivedDateValue_DateChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ctlFinalReceivedDateValue.DateChanged
        FinalReceivedDateValid()
    End Sub

    Private Sub ctlFinalSentReceivedDateValue_DateChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ctlFinalSentDateValue.DateChanged
        FinalSentDateValid()
    End Sub

    Private Sub ctlLabChasedDateValue_DateChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ctlLabChasedDateValue.DateChanged
        LabChasedDateValid()
    End Sub

    Private Sub ctlBarbMemoDateValue_DateChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ctlBarbMemoDateValue.DateChanged
        BarbMemoDateValid()
    End Sub

    Private Sub ctlPost2000SentDateValue_DateChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ctlPost2000SentDateValue.DateChanged
        Post2000SentDateValid()
    End Sub

    Private Sub ctlDataCompleteDateValue_DateChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ctlDataCompleteDateValue.DateChanged
        DataCompleteDateValid()
    End Sub

    Private Sub ctlSamplingDateValue_DateChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ctlSamplingDateValue.DateChanged
        If (lblSurveyValue.Text.ToLower = mSurveyTypeFS) Or (lblSurveyValue.Text.ToLower = mSurveyTypeSC) Then
            SamplingDateValid()
        End If
    End Sub
#End Region

#Region "Validation"

    Private Function PurchaserBSE1ReceivedDateValid() As Boolean
        Dim dEarliestDate As Date
        Dim sMessage As String

        dEarliestDate = CDate(lblRBSEDateValue.Text)
        sMessage = "You must enter a date in the past but after the RBSE Date."

        If Not ctlPurchaserBSE1ReceivedDateValue.Validate(dEarliestDate.AddDays(1), Today(), sMessage) Then
            Return False
        End If
        Return True
    End Function

    Private Function BreederBSE1ReceivedDateValid() As Boolean
        Dim dEarliestDate As Date
        Dim sMessage As String

        dEarliestDate = CDate(lblRBSEDateValue.Text)
        sMessage = "You must enter a date in the past but after the RBSE Date."

        If Not ctlBreederBSE1ReceivedDateValue.Validate(dEarliestDate.AddDays(1), Today(), sMessage) Then
            Return False
        End If
        Return True
    End Function

    Private Function Vendor1BSE1ReceivedDateValid() As Boolean
        Dim dEarliestDate As Date
        Dim sMessage As String

        dEarliestDate = CDate(lblRBSEDateValue.Text)
        sMessage = "You must enter a date in the past but after the RBSE Date."

        If Not ctlVendor1BSE1ReceivedDateValue.Validate(dEarliestDate.AddDays(1), Today(), sMessage) Then
            Return False
        End If
        Return True
    End Function

    Private Function HomebredBSE1ReceivedDateValid() As Boolean
        Dim dEarliestDate As Date
        Dim sMessage As String

        dEarliestDate = CDate(lblRBSEDateValue.Text)
        sMessage = "You must enter a date in the past but after the RBSE Date."

        If Not ctlHomeBredBSE1ReceivedDateValue.Validate(dEarliestDate.AddDays(1), Today(), sMessage) Then
            Return False
        End If
        Return True
    End Function

    Private Function SummarySheetReceivedDateValid() As Boolean
        Dim dEarliestDate As Date
        Dim sMessage As String

        dEarliestDate = CDate(lblRBSEDateValue.Text)
        sMessage = "You must enter a date in the past but after the RBSE Date."

        If Not ctlSummarySheetReceivedDateValue.Validate(dEarliestDate.AddDays(1), Today(), sMessage) Then
            Return False
        End If
        Return True
    End Function

    Private Function PaperworkCompleteDateValid() As Boolean
        Dim dEarliestDate As Date
        Dim sMessage As String

        dEarliestDate = CDate(lblRBSEDateValue.Text)
        sMessage = "You must enter a date in the past but after the RBSE Date."

        If Not ctlPaperworkCompleteDateValue.Validate(dEarliestDate.AddDays(1), Today(), sMessage) Then
            Return False
        End If
        Return True
    End Function

    Private Function FinalReceivedDateValid() As Boolean
        Dim dEarliestDate As Date
        Dim sMessage As String

        dEarliestDate = CDate(lblRBSEDateValue.Text)
        sMessage = "You must enter a date in the past but after the RBSE Date."

        If Not ctlFinalReceivedDateValue.Validate(dEarliestDate.AddDays(1), Today(), sMessage) Then
            Return False
        End If
        Return True
    End Function

    Private Function FinalSentDateValid() As Boolean
        Dim dEarliestDate As Date
        Dim sMessage As String

        If ctlFinalReceivedDateValue.DateField = "" Then
            Return True
        End If

        dEarliestDate = CDate(ctlFinalReceivedDateValue.DateField)
        sMessage = "You must enter a date in the past but after (or equal to) the Final Received Date."

        If Not ctlFinalSentDateValue.Validate(dEarliestDate, Today(), sMessage) Then
            Return False
        End If
        Return True
    End Function

    Private Function LabChasedDateValid() As Boolean
        Dim dEarliestDate As Date
        Dim sMessage As String

        dEarliestDate = CDate(lblRBSEDateValue.Text)
        sMessage = "You must enter a date in the past but after the RBSE Date."

        If Not ctlLabChasedDateValue.Validate(dEarliestDate.AddDays(1), Today(), sMessage) Then
            Return False
        End If
        Return True
    End Function

    Private Function BarbMemoDateValid() As Boolean
        Dim dEarliestDate As Date
        Dim sMessage As String

        dEarliestDate = CDate(lblRBSEDateValue.Text)
        sMessage = "You must enter a date in the past but after the RBSE Date."

        If Not ctlBarbMemoDateValue.Validate(dEarliestDate.AddDays(1), Today(), sMessage) Then
            Return False
        End If
        Return True
    End Function

    Private Function Post2000SentDateValid() As Boolean
        Dim dEarliestDate As Date
        Dim sMessage As String

        dEarliestDate = CDate(lblRBSEDateValue.Text)
        sMessage = "You must enter a date in the past but after the RBSE Date."

        If Not ctlPost2000SentDateValue.Validate(dEarliestDate.AddDays(1), Today(), sMessage) Then
            Return False
        End If
        Return True
    End Function

    Private Function DataCompleteDateValid() As Boolean
        Dim dEarliestDate As Date
        Dim sMessage As String

        dEarliestDate = CDate(lblRBSEDateValue.Text)
        sMessage = "You must enter a date in the past but after the RBSE Date."

        If Not ctlDataCompleteDateValue.Validate(dEarliestDate.AddDays(1), Today(), sMessage) Then
            Return False
        End If
        Return True
    End Function

    Private Function SamplingDateValid() As Boolean
        Dim dSamplingDate As Date
        Dim sMessage As String

        If ((lblSurveyValue.Text.ToLower = mSurveyTypeFS) Or (lblSurveyValue.Text.ToLower = mSurveyTypeSC)) Then
            Try
                If (ctlSamplingDateValue.AllowUnknown = True And ctlSamplingDateValue.DateField.ToLower = strUnknown) Then
                    Return True
                Else
                    If Not IsDate(ctlSamplingDateValue.DateField) Then
                        Return False
                    Else
                        Return True
                    End If
                End If
            Catch ex As Exception
                Return False
            End Try
        Else
            Return True
        End If



    End Function

    Private Function TseTestingSiteValid() As Boolean
        Dim sMessage As String
        Try
            If ((lblSurveyValue.Text.ToLower = mSurveyTypeFS) Or (lblSurveyValue.Text.ToLower = mSurveyTypeSC)) Then
                If (Not HttpContext.Current.Cache("suggestTSETestingSite") Is Nothing) Then
                    If Not dvAutoCompleteData Is Nothing Then
                        dvAutoCompleteData.RowFilter = [String].Format("name = '{0}'", txtTseTestingsite.Text.Trim())
                        If dvAutoCompleteData.Count = 0 Then
                            lblError.Visible = True
                            Return False
                        Else
                            lblError.Visible = False
                            Return True
                        End If
                    End If
                Else
                    GetAutoTestSiteEntries(txtTseTestingsite.Text, 10)
                    dvAutoCompleteData.RowFilter = [String].Format("name = '{0}'", txtTseTestingsite.Text.Trim())
                    If dvAutoCompleteData.Count = 0 Then
                        lblError.Visible = True
                        Return False
                    Else
                        lblError.Visible = False
                        Return True
                    End If
                End If
            Else
                lblError.Visible = False
                Return True
            End If


        Catch ex As Exception

        End Try
    End Function
#End Region


#Region "AutoComplete data"
    <WebMethod()>
    Public Shared Function GetAutoTestSiteEntries(ByVal prefixText As String, ByVal count As Integer) As String()

        Dim objLookup As New BSELib.LookupData()
        Dim suggestTSETestingSite As New List(Of String)()
        dvAutoCompleteData = objLookup.GetAutoCompleteData()

        dvAutoCompleteData = FilterData(dvAutoCompleteData, prefixText)

        For Each row As DataRowView In dvAutoCompleteData
            suggestTSETestingSite.Add(CType(row("name"), String))
        Next

        Return suggestTSETestingSite.ToArray()
    End Function
    Private Shared Function FilterData(ByVal view As DataView, ByVal prefix As String) As DataView
        view.RowFilter = [String].Format("name LIKE '{0}%'", prefix)
        Return (view)
    End Function
#End Region
End Class