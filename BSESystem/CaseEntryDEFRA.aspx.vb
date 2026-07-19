Partial Class CaseEntryDEFRA
    Inherits System.Web.UI.Page

    Protected WithEvents lblMethodology As System.Web.UI.WebControls.Label
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents BatchNumberDisplay1 As BatchNumberDisplay
    Protected WithEvents ctlFormADate As CalendarDate
    Protected WithEvents ctlFormBDate As CalendarDate
    Protected WithEvents ctlFormCDate As CalendarDate
    Protected WithEvents ctlFormAResubmittedDate As CalendarDate
    Protected WithEvents lblMethodologyValue As System.Web.UI.WebControls.Label
    Protected WithEvents ctlBSE1DateReceived As CalendarDate
    Protected WithEvents ctlEartag As ThreePartEartag
    Protected WithEvents ctlExitConfirmation As ExitConfirmation

    Protected WithEvents ctlDateOfBirth As CalendarDate
    Protected WithEvents TestsPager As DataGridPager

    Protected WithEvents ctlPurchaserBSE1ReceivedDate As CalendarDate
    Protected WithEvents ctlBreederBSE1ReceivedDate As CalendarDate
    Protected WithEvents ctlVendor1BSE1ReceivedDate As CalendarDate
    Protected WithEvents ctlHomebredBSE1ReceivedDate As CalendarDate
    Protected WithEvents ctlSummarySheetReceivedDate As CalendarDate
    Protected WithEvents ctlPaperworkCompleteDate As CalendarDate

#Region " Web Form Designer Generated Code "

    'This call is required by the Web Form Designer.
    <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()

    End Sub

    Private Sub Page_Init(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Init
        'CODEGEN: This method call is required by the Web Form Designer
        'Do not modify it using the code editor.
        InitializeComponent()
    End Sub

#End Region

    Private Sub Page_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

        If IsNothing(Session(SessionVars.SV_RBSENumber)) Then
            Response.Redirect("SessionError.aspx")
        End If

        VLAHeader1.PageTitle = "Case Details"
        SetEnterKeys()
        lblRBSEHeader.Text = RBSE_CAPTION & Session(SessionVars.SV_RBSENumber)
        VLAHeader1.BatchNumber = Session(SessionVars.SV_BatchNumber)
        TestsPager.SetGrid(grdTests)
        If Not IsPostBack Then
            LoadLookupLists()
            BindTestsGrid()
            LoadCaseDetails()
        End If

        litViewDocs.Text = GetSpolSiteButtonCode(Session(SessionVars.SV_RBSENumber))
        litViewDocs2.Text = litViewDocs.Text

        EnableControls()

        If ctlEartag.EartagCountry <> "" Or ctlEartag.EartagHerdmark <> "" Or ctlEartag.EartagAnimal <> "" Then
            ctlEartag.Validate()
        End If
    End Sub

    Private Sub SetEnterKeys()

        ctlEartag.SetControlOnEnter(txtPreviousEartag.ClientID)
        SetTextboxControlOnEnter(txtPreviousEartag, ctlBSE1DateReceived.FirstClientID)
        ctlBSE1DateReceived.SetControlOnEnter(ctlFormADate.FirstClientID)
        ctlFormADate.SetControlOnEnter(ctlFormAResubmittedDate.FirstClientID)
        ctlFormAResubmittedDate.SetControlOnEnter(ctlFormBDate.FirstClientID)
        ctlFormBDate.SetControlOnEnter(ddlFate.ClientID)
        ctlDateOfBirth.SetControlOnEnter(ddlBirthDateSource.ClientID)

    End Sub

#Region "Lookup list population"

    Private Sub LoadLookupLists()

        Dim blnResult As Boolean
        Dim objDataTable As DataTable
        Dim objLookup As New BSELib.LookupData()

        Try
            objDataTable = objLookup.GetLookupData(LOOKUP_BIRTH_DATE_SOURCE)
            If Not (objDataTable Is Nothing) Then
                ddlBirthDateSource.DataSource = objDataTable
                ddlBirthDateSource.DataValueField = "Code"
                ddlBirthDateSource.DataTextField = "Description"
                ddlBirthDateSource.DataBind()
                Common.AddItemToDropDownList(ddlBirthDateSource)
            End If

            objDataTable = objLookup.GetLookupData(LOOKUP_CASE_FATE)
            If Not (objDataTable Is Nothing) Then
                ddlFate.DataSource = objDataTable
                ddlFate.DataValueField = "Code"
                ddlFate.DataTextField = "Description"
                ddlFate.DataBind()
                Common.AddItemToDropDownList(ddlFate)
            End If

            objDataTable = objLookup.GetLookupData(LOOKUP_REPORTED_LOCATION)
            If Not (objDataTable Is Nothing) Then
                ddlReportedLocation.DataSource = objDataTable
                ddlReportedLocation.DataValueField = "Code"
                ddlReportedLocation.DataTextField = "Description"
                ddlReportedLocation.DataBind()
                Common.AddItemToDropDownList(ddlReportedLocation)
            End If

            objDataTable = objLookup.GetLookupData(LOOKUP_SURVEY)
            If Not (objDataTable Is Nothing) Then
                ddlSurvey.DataSource = objDataTable
                ddlSurvey.DataValueField = "Code"
                ddlSurvey.DataTextField = "Description"
                ddlSurvey.DataBind()
                Common.AddItemToDropDownList(ddlSurvey)
            End If

            objDataTable = objLookup.GetLookupData(LOOKUP_VALUATION_AGE)
            If Not (objDataTable Is Nothing) Then
                ddlValuationAge.DataSource = objDataTable
                ddlValuationAge.DataValueField = "Code"
                ddlValuationAge.DataTextField = "Description"
                ddlValuationAge.DataBind()
                Common.AddItemToDropDownList(ddlValuationAge)
            End If

            objDataTable = objLookup.GetLookupData(LOOKUP_CASE_TYPE)
            If Not (objDataTable Is Nothing) Then
                ddlCaseType.DataSource = objDataTable
                ddlCaseType.DataValueField = "Code"
                ddlCaseType.DataTextField = "Description"
                ddlCaseType.DataBind()
                Common.AddItemToDropDownList(ddlCaseType)
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve 'DEFRA Tab' drop down lists.", ex)
        End Try

    End Sub

    Public Function GetTestTypeList() As DataTable
        Try
            Dim objLookup As New BSELib.LookupData()
            Dim dt As DataTable = objLookup.GetLookupData(LOOKUP_TEST_TYPE)

            If dt Is Nothing Then
                Throw New Exception("LookupData.GetLookupData returned Nothing")
            End If

            Return dt

        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve the list of Test Types", ex)
        End Try
    End Function

    Public Function GetTestResultList() As DataTable
        Try
            Dim objLookup As New BSELib.LookupData()
            Dim dt As DataTable = objLookup.GetLookupData(LOOKUP_TEST_RESULT)

            If dt Is Nothing Then
                Throw New Exception("LookupData.GetLookupData returned Nothing")
            End If

            Return dt

        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve the list of Test Results", ex)
        End Try
    End Function

#End Region

#Region "Handle Grids"

    Private Sub BindTestsGrid()
        Dim dtData As DataTable = Session.Item(SessionVars.SV_TestTable)
        Dim dvData As DataView = dtData.DefaultView
        Session.Item(SessionVars.SV_TestView) = dvData

        grdTests.DataSource = dtData
        grdTests.DataKeyField = "ID"
        grdTests.CurrentPageIndex = 0
        grdTests.SelectedIndex = -1
        grdTests.EditItemIndex = -1
        grdTests.DataBind()

        TestsPager.DataTableSessionID = SessionVars.SV_TestTable
        TestsPager.DataViewSessionID = SessionVars.SV_TestView
        TestsPager.PageLinkCount = 10
        TestsPager.Refresh()
    End Sub

    Private Sub DisableTestsGrid()
        TestsPager.AllowAddNew = False
        TestsPager.AllowDelete = False
        TestsPager.AllowEdit = False
    End Sub

    Private Sub EnableTestsGrid()
        TestsPager.AllowAddNew = True
        TestsPager.AllowDelete = True
        TestsPager.AllowEdit = True
    End Sub

    Private Sub grdTests_ItemDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.DataGridItemEventArgs) Handles grdTests.ItemDataBound
        Try
            ' set up the checkbox and drop-down columns
            Dim drv As DataRowView = CType(e.Item.DataItem, DataRowView)
            If Not drv Is Nothing Then
                If e.Item.ItemType = ListItemType.EditItem Then
                    ' populate edit mode controls
                    Dim ddlTestType As DropDownList = CType(e.Item.FindControl("ddlTestType"), DropDownList)
                    If Not ddlTestType Is Nothing Then
                        If Not IsDBNull(drv("TestType")) Then
                            SelectItemInDropDownList(ddlTestType, drv("TestType"))
                        End If
                    End If

                    Dim ddlTestResult As DropDownList = CType(e.Item.FindControl("ddlTestResult"), DropDownList)
                    If Not ddlTestResult Is Nothing Then
                        If Not IsDBNull(drv("TestResult")) Then
                            SelectItemInDropDownList(ddlTestResult, drv("TestResult"))
                        End If
                    End If

                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to bind template columns in the Tests grid", ex)
        End Try
    End Sub

    Private Sub TestsPager_RowSave(ByVal sender As Object, ByVal e As BSESystem.DataGridPagerEventArgs) Handles TestsPager.RowSave
        ' save template column values to the dataset here

        If e.DataTableRow.RowState = DataRowState.Added Then
            e.DataTableRow("RBSE") = Replace(Session(SessionVars.SV_RBSENumber), "/", "")
        End If

        Dim ddlTestType As DropDownList = CType(e.GridRow.FindControl("ddlTestType"), DropDownList)
        e.DataTableRow("TestType") = ddlTestType.SelectedItem.Value
        e.DataTableRow("TestTypeDescription") = ddlTestType.SelectedItem.Text
        Dim ddlTestResult As DropDownList = CType(e.GridRow.FindControl("ddlTestResult"), DropDownList)
        e.DataTableRow("TestResult") = ddlTestResult.SelectedItem.Value
        e.DataTableRow("TestResultDescription") = ddlTestResult.SelectedItem.Text
    End Sub

#End Region

#Region "Event Handlers"

    Private Sub ctlDateOfBirth_DateChanged(ByVal sender As System.Object, ByVal e As System.EventArgs)
        CheckBirthDateSource()
        CheckHerdEntryDate()
    End Sub

    Private Sub ctlFormADate_DateChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ctlFormADate.DateChanged
        If ctlFormADate.DateField = "" Then
            ctlFormBDate.DateField = ""
            ctlFormCDate.DateField = ""
            ctlFormAResubmittedDate.DateField = ""
            SelectItemInDropDownList(ddlFate, "")
            ddlFate.Enabled = False
            lblrfvFate.Visible = False
        Else
            FormADateValid()
        End If
    End Sub

    Private Sub ctlFormBDate_DateChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ctlFormBDate.DateChanged
        FormBDateValid()
        If ctlFormBDate.DateField = "" Then
            SelectItemInDropDownList(ddlFate, "")
            ddlFate.Enabled = False
            lblrfvFate.Visible = False
            ctlFormCDate.DateField = ""
        End If
    End Sub

    Private Sub ctlFormCDate_DateChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ctlFormCDate.DateChanged
        FormCDateValid()
    End Sub

    Private Sub ctlEartag_EartagCountryChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ctlEartag.EartagCountryChanged
        ctlEartag.Validate()
    End Sub

    Private Sub ctlEartag_EartagHerdmarkChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ctlEartag.EartagHerdmarkChanged
        ctlEartag.Validate()
    End Sub

    Private Sub ctlEartag_EartagAnimalChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ctlEartag.EartagAnimalChanged
        ctlEartag.Validate()
    End Sub

    Private Sub ctlPreviousEartag_EartagChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtPreviousEartag.TextChanged
        'txtPreviousEartag.Validate()
    End Sub

    Private Sub btnCaseAuditLog_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCaseAuditLog.Click
        If UpdateSessionWithCaseDetails() Then Response.Redirect("CaseAuditLogReport.aspx?page=CaseEntryDEFRA")
    End Sub

    Private Sub btnSave_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSave.Click, btnSave2.Click
        If (ctlEartag.Validate()) Then  
            If UpdateSessionWithCaseDetails() Then Response.Redirect("CaseEntrySave.aspx")
        End If
    End Sub

    Private Sub btnCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancel.Click, btnCancel2.Click
        CancelCaseEdit()
    End Sub

    Private Sub VLAHeader1_HomeClick(ByVal sender As Object, ByVal e As BSESystem.HomeLinkEventArgs) Handles VLAHeader1.HomeClick

        CancelCaseEdit()
        e.bNavigateHome = False

    End Sub

    Private Sub CancelCaseEdit()
        Dim dsFarm As DataSet = Session(SessionVars.SV_FarmDetails)
        Dim dsCase As DataSet = Session(SessionVars.SV_CaseDetails)
        Dim clsDataCheck As New BSELib.clsDataCheck()
        Dim bFarmChanges As Boolean
        Dim bCaseChanges As Boolean

        UpdateSessionWithCaseDetails()

        If clsDataCheck.DataSetHasChanges(dsCase) Then bCaseChanges = True
        If clsDataCheck.DataSetHasChanges(dsFarm) Then bFarmChanges = True

        If bFarmChanges Or bCaseChanges Then
            ctlExitConfirmation.ShowExitConfirmation()
            'Page.RegisterStartupScript("navigate", PromptBeforeNavigateScript("You have not saved.  Cancel then Save or OK to exit without saving.", "home.aspx"))
        Else
            Response.Redirect("Home.aspx")
        End If
    End Sub

    Private Sub hlbFarm_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlbFarm.Click
        If UpdateSessionWithCaseDetails() Then Response.Redirect("CaseEntryFarm.aspx")
    End Sub

    Private Sub hlbBAB_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlbBAB.Click
        If UpdateSessionWithCaseDetails() Then Response.Redirect("CaseEntryBAB.aspx")
    End Sub

    Private Sub hlbCaseVLA_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlbCaseVLA.Click
        If UpdateSessionWithCaseDetails() Then Response.Redirect("CaseEntryVLA.aspx")
    End Sub

    Private Sub hlbClinical_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlbClinical.Click
        If UpdateSessionWithCaseDetails() Then Response.Redirect("CaseEntryClinical.aspx")
    End Sub

    Private Sub hlbFeeds_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlbFeeds.Click
        If UpdateSessionWithCaseDetails() Then Response.Redirect("CaseEntryFeeds.aspx")
    End Sub

    Private Sub hlbRelations_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlbRelations.Click
        If UpdateSessionWithCaseDetails() Then Response.Redirect("CaseEntryRelations.aspx")
    End Sub

    Private Sub ctlPurchaserBSE1ReceivedDate_DateChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ctlPurchaserBSE1ReceivedDate.DateChanged
        If ctlPurchaserBSE1ReceivedDate.DateField <> "" Then
            chkPurchaserBSE1Received.Checked = True
        End If
    End Sub

    Private Sub ctlBreederBSE1ReceivedDate_DateChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ctlBreederBSE1ReceivedDate.DateChanged
        If ctlBreederBSE1ReceivedDate.DateField <> "" Then
            chkBreederBSE1Received.Checked = True
        End If
    End Sub

    Private Sub ctlVendor1BSE1ReceivedDate_DateChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ctlVendor1BSE1ReceivedDate.DateChanged
        If ctlVendor1BSE1ReceivedDate.DateField <> "" Then
            chkVendor1BSE1Received.Checked = True
        End If
    End Sub

    Private Sub ctlHomebredBSE1ReceivedDate_DateChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ctlHomebredBSE1ReceivedDate.DateChanged
        If ctlHomebredBSE1ReceivedDate.DateField <> "" Then
            chkHomebredBSE1Received.Checked = True
        End If
    End Sub

    Private Sub ctlSummarySheetReceivedDate_DateChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ctlSummarySheetReceivedDate.DateChanged
        If ctlSummarySheetReceivedDate.DateField <> "" Then
            chkSummarySheetReceived.Checked = True
        End If
    End Sub

    Private Sub ctlPaperworkCompleteDate_DateChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ctlPaperworkCompleteDate.DateChanged
        If ctlPaperworkCompleteDate.DateField <> "" Then
            chkPaperworkComplete.Checked = True
        End If
    End Sub

    Protected Sub btnCaseWork_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCaseWork.Click
        If UpdateSessionWithCaseDetails() Then Response.Redirect("CaseEntrySave.aspx?redirect=CaseWorkEntry.aspx")
    End Sub

#End Region

#Region "Enable / Disable Controls"

    Private Sub EnableControls()
        VLAHeader1.GetUserDetails()

        Dim sGroupName As String = Session(SessionVars.SV_HeaderGroupName)

        If sGroupName = "DEFRA Viewer" Then
            DEFRAViewerEnable()
        ElseIf sGroupName = "DEFRA Data Entry" Then
            DEFRADataEntryEnable()
        ElseIf sGroupName = "DEFRA Maintenance" Then
            DEFRAMaintenanceEnable()
        ElseIf sGroupName = "VLA Data Entry" Then
            VLADataEntryEnable()
        ElseIf sGroupName = "VLA Maintenance" Then
            VLAMaintenanceEnable()
        Else
            Response.Redirect("SearchMenu.aspx")
        End If
    End Sub

    Private Sub DEFRAViewerEnable()
        BatchNumberDisplay1.Visible = False
        MakeControlsReadOnly()
        btnSave.Enabled = False
        txtBarcode.Enabled = False
        txtAHFReference.Enabled = False
    End Sub

    Private Sub DEFRADataEntryEnable()
        BatchNumberDisplay1.Visible = False
        MakeControlsWritable()
        btnSave.Enabled = True
        txtBarcode.Enabled = False
        txtAHFReference.Enabled = False
    End Sub

    Private Sub DEFRAMaintenanceEnable()
        BatchNumberDisplay1.Visible = False
        MakeControlsWritable()
        btnSave.Enabled = True
        txtBarcode.Enabled = False
        txtAHFReference.Enabled = False
    End Sub

    Private Sub VLADataEntryEnable()
        BatchNumberDisplay1.Visible = True
        MakeControlsReadOnly()
        btnSave.Enabled = True
        txtBarcode.Enabled = False
        txtAHFReference.Enabled = False
    End Sub

    Private Sub VLAMaintenanceEnable()
        BatchNumberDisplay1.Visible = True
        MakeControlsWritable()
        btnSave.Enabled = True

        Dim dsData As DataSet = Session.Item(SessionVars.SV_CaseDetails)
        If dsData.Tables(BSELib.clsCase.CASEWORK_TABLE).Rows.Count <> 0 AndAlso _
           Not IsDBNull(dsData.Tables(BSELib.clsCase.CASEWORK_TABLE).Rows(0).Item("IsCaseClosed")) AndAlso _
           dsData.Tables(BSELib.clsCase.CASEWORK_TABLE).Rows(0).Item("IsCaseClosed") <> "1" Then
            txtBarcode.Enabled = True
            txtAHFReference.Enabled = True
            ctlPaperworkCompleteDate.Enabled = True
        Else
            txtBarcode.Enabled = False
            txtAHFReference.Enabled = False
            ctlPaperworkCompleteDate.Enabled = False
        End If
    End Sub

    Private Sub MakeControlsReadOnly()
        ctlEartag.Enabled = False
        txtPreviousEartag.Enabled = False
        ctlBSE1DateReceived.Enabled = False
        ctlBSE1DateReceived.Mandatory = False
        ctlFormADate.Enabled = False
        ctlFormADate.Mandatory = False
        ctlFormBDate.Enabled = False
        ctlFormBDate.Mandatory = False
        ctlFormCDate.Enabled = False
        ctlFormCDate.Mandatory = False
        ctlFormAResubmittedDate.Enabled = False
        ctlFormAResubmittedDate.Mandatory = False
        ddlFate.Enabled = False
        lblrfvFate.Visible = False
        ddlReportedLocation.Enabled = False
        ddlSurvey.Enabled = False
        ctlDateOfBirth.Enabled = False
        ctlDateOfBirth.Mandatory = False
        ddlValuationAge.Enabled = False
        chkDateOfBirthEstimated.Enabled = False
        txtNotes.Enabled = False
        chkPurchaserBSE1Received.Enabled = False
        chkBreederBSE1Received.Enabled = False
        chkVendor1BSE1Received.Enabled = False
        chkSummarySheetReceived.Enabled = False
        chkHomebredBSE1Received.Enabled = False
        chkPaperworkComplete.Enabled = False
        ddlBirthDateSource.Enabled = False
        ddlCaseType.Enabled = False
        txtLabComment.Enabled = False
        ctlPurchaserBSE1ReceivedDate.Enabled = False
        ctlBreederBSE1ReceivedDate.Enabled = False
        ctlVendor1BSE1ReceivedDate.Enabled = False
        ctlHomebredBSE1ReceivedDate.Enabled = False
        ctlSummarySheetReceivedDate.Enabled = False
        ctlPaperworkCompleteDate.Enabled = False

        DisableTestsGrid()

    End Sub

    Private Sub MakeControlsWritable()
        ctlEartag.Enabled = True
        ctlEartag.AutoPostBack = True
        txtPreviousEartag.Enabled = True
        txtPreviousEartag.AutoPostBack = True
        ctlBSE1DateReceived.Enabled = True
        ctlBSE1DateReceived.Mandatory = False
        If chkNonGBCase.Checked Then
            ctlFormADate.Enabled = False
            ctlFormADate.Mandatory = False
            ctlFormADate.AutoPostBack = False
        Else
            ctlFormADate.Enabled = True
            ctlFormADate.Mandatory = False
            ctlFormADate.AutoPostBack = True
        End If
        If ctlFormADate.DateField <> "" Then
            ctlFormAResubmittedDate.Enabled = True
            ctlFormAResubmittedDate.Mandatory = False
            ctlFormBDate.Enabled = True
            ctlFormBDate.Mandatory = False
            ctlFormBDate.AutoPostBack = True

        Else
            ctlFormAResubmittedDate.Enabled = False
            ctlFormAResubmittedDate.Mandatory = False
            ctlFormAResubmittedDate.DateField = ""
            ctlFormBDate.Enabled = False
            ctlFormBDate.Mandatory = False
        End If
        If ctlFormBDate.DateField <> "" Then
            ddlFate.Enabled = True
            If GetSelectedItemFromDropDownList(ddlFate) = "-1" Then
                lblrfvFate.Visible = True
            Else
                lblrfvFate.Visible = False
            End If
            ctlFormCDate.Enabled = True
            ctlFormCDate.Mandatory = False
            ctlFormCDate.AutoPostBack = True
        Else
            ddlFate.Enabled = False
            lblrfvFate.Visible = False
            ctlFormCDate.Enabled = False
            ctlFormCDate.Mandatory = False
        End If

        ddlCaseType.Enabled = True
        ddlReportedLocation.Enabled = True
        ddlSurvey.Enabled = True
        ctlDateOfBirth.Enabled = True
        ctlDateOfBirth.Mandatory = False
        ctlDateOfBirth.AutoPostBack = True
        ddlValuationAge.Enabled = True
        If ctlDateOfBirth.DateField = "" Then
            chkDateOfBirthEstimated.Checked = False
            chkDateOfBirthEstimated.Enabled = False
            ddlBirthDateSource.Enabled = False
        Else
            chkDateOfBirthEstimated.Enabled = True
            ddlBirthDateSource.Enabled = True
        End If
        txtNotes.Enabled = True
        chkPurchaserBSE1Received.Enabled = True
        chkBreederBSE1Received.Enabled = True
        chkVendor1BSE1Received.Enabled = True
        chkSummarySheetReceived.Enabled = True
        chkHomebredBSE1Received.Enabled = True
        chkPaperworkComplete.Enabled = True

        txtLabComment.Enabled = True

        Dim dsData As DataSet = Session.Item(SessionVars.SV_CaseDetails)
        If dsData.Tables(BSELib.clsCase.CASEWORK_TABLE).Rows.Count <> 0 Then
            ctlPurchaserBSE1ReceivedDate.Enabled = True
            ctlBreederBSE1ReceivedDate.Enabled = True
            ctlVendor1BSE1ReceivedDate.Enabled = True
            ctlHomebredBSE1ReceivedDate.Enabled = True
            ctlSummarySheetReceivedDate.Enabled = True
            ctlPurchaserBSE1ReceivedDate.AutoPostBack = True
            ctlBreederBSE1ReceivedDate.AutoPostBack = True
            ctlVendor1BSE1ReceivedDate.AutoPostBack = True
            ctlHomebredBSE1ReceivedDate.AutoPostBack = True
            ctlSummarySheetReceivedDate.AutoPostBack = True
            ctlPaperworkCompleteDate.AutoPostBack = True
            btnCaseWork.Enabled = True
        Else
            ctlPurchaserBSE1ReceivedDate.Enabled = False
            ctlBreederBSE1ReceivedDate.Enabled = False
            ctlVendor1BSE1ReceivedDate.Enabled = False
            ctlHomebredBSE1ReceivedDate.Enabled = False
            ctlSummarySheetReceivedDate.Enabled = False
            ctlPurchaserBSE1ReceivedDate.AutoPostBack = True
            ctlBreederBSE1ReceivedDate.AutoPostBack = True
            ctlVendor1BSE1ReceivedDate.AutoPostBack = True
            ctlHomebredBSE1ReceivedDate.AutoPostBack = True
            ctlSummarySheetReceivedDate.AutoPostBack = True
            ctlPaperworkCompleteDate.AutoPostBack = True
            btnCaseWork.Enabled = False
        End If

        ctlPurchaserBSE1ReceivedDate.Mandatory = False
        ctlBreederBSE1ReceivedDate.Mandatory = False
        ctlVendor1BSE1ReceivedDate.Mandatory = False
        ctlHomebredBSE1ReceivedDate.Mandatory = False
        ctlSummarySheetReceivedDate.Mandatory = False
        ctlPaperworkCompleteDate.Mandatory = False

        EnableTestsGrid()
    End Sub

#End Region

#Region "Handle Data"

    Private Function LoadCaseDetails() As Boolean

        Dim dsData As DataSet

        Try

            dsData = Session.Item(SessionVars.SV_CaseDetails)

            If dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows.Count <> 0 Then

                Session.Item(SessionVars.SV_CPHHNumber) = dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows(0)("CPHH").ToString()

                With dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows(0)
                    ctlEartag.EartagCountry = .Item("EartagCountry").ToString()
                    ctlEartag.EartagHerdmark = .Item("EartagHerdmark").ToString()
                    ctlEartag.EartagAnimal = .Item("Eartag").ToString()
                    txtPreviousEartag.Text = .Item("PreviousEartag").ToString()
                    ctlBSE1DateReceived.DateField = FormatDate(.Item("BSE1ReceivedDate").ToString())
                    ctlFormADate.DateField = FormatDate(.Item("FormADate").ToString())
                    ctlFormAResubmittedDate.DateField = FormatDate(.Item("FormAResubmittedDate").ToString())
                    ctlFormBDate.DateField = FormatDate(.Item("FormBDate").ToString())
                    SelectItemInDropDownList(ddlFate, .Item("Fate").ToString())
                    ctlFormCDate.DateField = FormatDate(.Item("FormCDate").ToString())
                    chkPurchaserBSE1Received.Checked = GetRowColumnData(.Item("IsPurchaserBSE1Received"))
                    chkBreederBSE1Received.Checked = GetRowColumnData(.Item("IsBreederBSE1Received"))
                    chkVendor1BSE1Received.Checked = GetRowColumnData(.Item("IsVendor1BSE1Received"))
                    chkHomebredBSE1Received.Checked = GetRowColumnData(.Item("IsHomebredBSE1Received"))
                    chkSummarySheetReceived.Checked = GetRowColumnData(.Item("IsSummarySheetReceived"))
                    lblFinalResultDateValue.Text = FormatDate(.Item("FinalResultDate").ToString())
                    lblFinalResultValue.Text = .Item("FinalResult").ToString()
                    ctlDateOfBirth.DateField = FormatDate(.Item("BirthDate").ToString())
                    SelectItemInDropDownList(ddlBirthDateSource, .Item("BirthDateSource").ToString())
                    chkDateOfBirthEstimated.Checked = GetRowColumnData(.Item("IsBirthDateEst"))
                    lblDBSEValue.Text = FormatDBSE(.Item("DBSE").ToString())
                    SelectItemInDropDownList(ddlReportedLocation, .Item("ReportedLocation").ToString())
                    SelectItemInDropDownList(ddlSurvey, .Item("Survey").ToString())
                    txtNotes.Text = .Item("Notes").ToString()
                    SelectItemInDropDownList(ddlValuationAge, .Item("ValuationAge").ToString())
                    chkPaperworkComplete.Checked = GetRowColumnData(.Item("IsPaperworkComplete"))
                    chkNonGBCase.Checked = GetRowColumnData(.Item("IsNonGBCase"))
                    SelectItemInDropDownList(ddlCaseType, .Item("CaseType").ToString())
                    txtLabComment.Text = .Item("LabComment").ToString()
                End With

                If dsData.Tables(BSELib.clsCase.CASEWORK_TABLE).Rows.Count <> 0 Then
                    With dsData.Tables(BSELib.clsCase.CASEWORK_TABLE).Rows(0)
                        hdnRBSEDate.Value = .Item("RBSEDate").ToString()
                        txtBarcode.Text = .Item("Barcode").ToString()
                        txtAHFReference.Text = .Item("AHFReference").ToString()
                        ctlPurchaserBSE1ReceivedDate.DateField = FormatDate(.Item("PurchaserBSE1ReceivedDate").ToString())
                        ctlBreederBSE1ReceivedDate.DateField = FormatDate(.Item("BreederBSE1ReceivedDate").ToString())
                        ctlVendor1BSE1ReceivedDate.DateField = FormatDate(.Item("Vendor1BSE1ReceivedDate").ToString())
                        ctlHomebredBSE1ReceivedDate.DateField = FormatDate(.Item("HomebredBSE1ReceivedDate").ToString())
                        ctlSummarySheetReceivedDate.DateField = FormatDate(.Item("SummarySheetReceivedDate").ToString())
                        ctlPaperworkCompleteDate.DateField = FormatDate(.Item("PaperworkCompleteDate").ToString())
                    End With
                End If

                Return True
            End If
            Return False

        Catch ex As Exception
            clsAppError.DisplayError("Failed to 'Load DEFRA Case Details'.", ex)
            Return False
        End Try
    End Function

    Private Function UpdateSessionWithCaseDetails() As Boolean

        Dim dsData As DataSet = Session.Item(SessionVars.SV_CaseDetails)
        Dim dDate As Date

        Try
            If Not dsData Is Nothing Then
                If dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows.Count <> 0 Then
                    With dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows(0)

                        .Item("Eartag") = FormatEmptyString(ctlEartag.EartagAnimal)
                        .Item("EartagHerdmark") = FormatEmptyString(ctlEartag.EartagHerdmark)
                        .Item("EartagCountry") = FormatEmptyString(ctlEartag.EartagCountry)

                        .Item("PreviousEartag") = FormatEmptyString(txtPreviousEartag.Text)

                        If Not FormADateValid() Then Return False
                        .Item("FormADate") = FormatEmptyString(ctlFormADate.DateField)

                        If Not FormAResubmittedDateValid() Then Return False
                        .Item("FormAResubmittedDate") = FormatEmptyString(ctlFormAResubmittedDate.DateField)

                        If Not FormBDateValid() Then Return False
                        .Item("FormBDate") = FormatEmptyString(ctlFormBDate.DateField)
                        If IsDBNull(.Item("SlaughterDate")) Then
                            .Item("SlaughterDate") = FormatEmptyString(ctlFormBDate.DateField)
                        ElseIf FormatDate(.Item("SlaughterDate")) = "" Then
                            .Item("SlaughterDate") = FormatEmptyString(ctlFormBDate.DateField)
                        End If

                        If Not FormCDateValid() Then Return False
                        .Item("FormCDate") = FormatEmptyString(ctlFormCDate.DateField)

                        If Not ctlBSE1DateReceived.Validate(dDate) Then Return False
                        .Item("BSE1ReceivedDate") = FormatEmptyString(ctlBSE1DateReceived.DateField)

                        .Item("Notes") = FormatEmptyString(txtNotes.Text)

                        If Not DateOfBirthValid(dsData.Tables(BSELib.clsCase.CASE_TABLE)) Then Return False
                        .Item("BirthDate") = FormatEmptyString(ctlDateOfBirth.DateField)

                        .Item("BirthDateSource") = FormatEmptyString(GetSelectedItemFromDropDownList(ddlBirthDateSource))

                        If ctlDateOfBirth.DateField = "" Then
                            .Item("IsBirthDateEst") = DBNull.Value
                        Else
                            .Item("IsBirthDateEst") = chkDateOfBirthEstimated.Checked()
                        End If
                        .Item("IsPurchaserBSE1Received") = chkPurchaserBSE1Received.Checked()
                        .Item("IsBreederBSE1Received") = chkBreederBSE1Received.Checked()
                        .Item("IsVendor1BSE1Received") = chkVendor1BSE1Received.Checked()
                        .Item("IsSummarySheetReceived") = chkSummarySheetReceived.Checked()

                        If ctlFormBDate.DateField <> "" And GetSelectedItemFromDropDownList(ddlFate) = "-1" Then Return False
                        .Item("Fate") = FormatEmptyString(GetSelectedItemFromDropDownList(ddlFate))

                        .Item("ReportedLocation") = FormatEmptyString(GetSelectedItemFromDropDownList(ddlReportedLocation))
                        .Item("Survey") = FormatEmptyString(GetSelectedItemFromDropDownList(ddlSurvey))
                        .Item("ValuationAge") = FormatEmptyString(GetSelectedItemFromDropDownList(ddlValuationAge))
                        .Item("IsHomebredBSE1Received") = chkHomebredBSE1Received.Checked()
                        .Item("IsPaperworkComplete") = chkPaperworkComplete.Checked()

                        .Item("CaseType") = FormatEmptyString(GetSelectedItemFromDropDownList(ddlCaseType))
                        .Item("LabComment") = FormatEmptyString(txtLabComment.Text)

                    End With

                    If dsData.Tables(BSELib.clsCase.CASEWORK_TABLE).Rows.Count <> 0 Then
                        With dsData.Tables(BSELib.clsCase.CASEWORK_TABLE).Rows(0)
                            .Item("RBSEDate") = FormatDate(hdnRBSEDate.Value)

                            .Item("Barcode") = FormatEmptyString(txtBarcode.Text)
                            .Item("AHFReference") = FormatEmptyString(txtAHFReference.Text)

                            If Not PurchaserBSE1ReceivedDateValid() Then Return False
                            .Item("PurchaserBSE1ReceivedDate") = FormatEmptyString(ctlPurchaserBSE1ReceivedDate.DateField)

                            If Not BreederBSE1ReceivedDateValid() Then Return False
                            .Item("BreederBSE1ReceivedDate") = FormatEmptyString(ctlBreederBSE1ReceivedDate.DateField)

                            If Not Vendor1BSE1ReceivedDateValid() Then Return False
                            .Item("Vendor1BSE1ReceivedDate") = FormatEmptyString(ctlVendor1BSE1ReceivedDate.DateField)

                            If Not HomebredBSE1ReceivedDateValid() Then Return False
                            .Item("HomebredBSE1ReceivedDate") = FormatEmptyString(ctlHomebredBSE1ReceivedDate.DateField)

                            If Not SummarySheetReceivedDateValid() Then Return False
                            .Item("SummarySheetReceivedDate") = FormatEmptyString(ctlSummarySheetReceivedDate.DateField)

                            If Not PaperworkCompleteDateValid() Then Return False
                            .Item("PaperworkCompleteDate") = FormatEmptyString(ctlPaperworkCompleteDate.DateField)
                        End With
                    End If
                    Return True
                End If
                Return False
            End If
            Return True

        Catch ex As Exception
            clsAppError.DisplayError("Failed to Save DEFRA Case Details.", ex)
            Return False
        End Try
    End Function

    Private Function DateOfBirthValid(ByRef dtData As DataTable) As Boolean
        Dim dDate As Date
        Dim dEarliestDate As Date
        Dim dLatestDate As Date

        dEarliestDate = CDate("01 Jan 1970")

        If ctlFormADate.DateField = "" Then
            dLatestDate = Now()
        Else
            dLatestDate = Convert.ToDateTime(ctlFormADate.DateField)
        End If

        If Not ctlDateOfBirth.Validate(dDate, dEarliestDate, dLatestDate, "Date of Birth must be before the Form A Date") Then
            Return False
        End If

        If Not dtData.Rows(0)("PurchaseDate") Is DBNull.Value Then
            dLatestDate = dtData.Rows(0)("PurchaseDate")
            If Not ctlDateOfBirth.Validate(dDate, dEarliestDate, dLatestDate, "Date of Birth must be before the Purchase Date") Then
                Return False
            End If
        End If

        If Not dtData.Rows(0)("OnsetDate") Is DBNull.Value Then
            dLatestDate = dtData.Rows(0)("OnsetDate")
            If Not ctlDateOfBirth.Validate(dDate, dEarliestDate, dLatestDate, "Date of Birth must be before the Onset Date") Then
                Return False
            End If
        End If

        Return True
    End Function

    Private Function FormAResubmittedDateValid() As Boolean
        Dim dDate As Date
        Dim dEarliestDate As Date
        Dim dLatestDate As Date

        dLatestDate = Now()

        If ctlFormAResubmittedDate.Enabled = True And ctlFormAResubmittedDate.DateField <> "" Then
            If ctlFormADate.DateField <> "" Then
                Try
                    dEarliestDate = Convert.ToDateTime(ctlFormADate.DateField)
                Catch
                    Return False
                End Try
            Else
                ' In theory there should be a Form A date for there to be a Form A Resubmitted Date
                Return False
            End If

            If Not ctlFormAResubmittedDate.Validate(dDate, dEarliestDate, dLatestDate) Then
                Return False
            End If
        End If

        Return True
    End Function

    Private Function FormADateValid() As Boolean
        Dim dLatestDate As Date
        Dim sMessage As String
        Dim dsData As DataSet = Session.Item(SessionVars.SV_CaseDetails)

        If Not dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows(0)("SlaughterDate") Is DBNull.Value Then
            dLatestDate = CDate(dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows(0)("SlaughterDate"))
            sMessage = "You must enter a date before the Slaughter Date."
        Else
            dLatestDate = Now()
            sMessage = "You must enter a past date."
        End If

        If Not ctlFormADate.Validate(dLatestDate, ctlFormADate.ValidationType.eValidateLatest, sMessage) Then Return False

        Return True
    End Function

    Private Function FormBDateValid() As Boolean
        Dim dDate As Date
        Dim dEarliestDate As Date
        Dim dLatestDate As Date

        dLatestDate = Now()

        If ctlFormBDate.Enabled = True And ctlFormBDate.DateField <> "" Then
            If ctlFormADate.DateField <> "" Then
                Try
                    dEarliestDate = Convert.ToDateTime(ctlFormADate.DateField)
                Catch
                    Return False
                End Try
            Else
                ' In theory there should be a Form A date for there to be a Form B Date
                Return False
            End If

            If Not ctlFormBDate.Validate(dDate, dEarliestDate, dLatestDate) Then
                Return False
            End If
        End If

        Return True
    End Function

    Private Function FormCDateValid() As Boolean
        Dim dDate As Date
        Dim dEarliestDate As Date
        Dim dLatestDate As Date

        If ctlFormCDate.Enabled = True And ctlFormCDate.DateField <> "" Then
            If ctlFormBDate.DateField <> "" Then
                Try
                    dEarliestDate = Convert.ToDateTime(ctlFormBDate.DateField)
                    dLatestDate = dEarliestDate
                Catch
                    Return False
                End Try
            Else
                ' In theory there should be a Form B date for there to be a Form C Date
                Return False
            End If

            dDate = ctlFormCDate.DateField

            If dDate = dLatestDate Then
                lblFormCDateWarning.Visible = False
            Else
                lblFormCDateWarning.Visible = True
            End If
        End If

        Return True
    End Function

    Private Function PurchaserBSE1ReceivedDateValid() As Boolean
        Dim dEarliestDate As Date
        Dim sMessage As String
        Dim dsData As DataSet = Session.Item(SessionVars.SV_CaseDetails)

        dEarliestDate = FormatDate(dsData.Tables(BSELib.clsCase.CASEWORK_TABLE).Rows(0)("RBSEDate"))
        sMessage = "You must enter a date in the past but after the RBSE Date (" & FormatDate(dEarliestDate.ToString()) & ")"

        If Not ctlPurchaserBSE1ReceivedDate.Validate(dEarliestDate.AddDays(1), Today(), sMessage) Then Return False

        Return True
    End Function

    Private Function BreederBSE1ReceivedDateValid() As Boolean
        Dim dEarliestDate As Date
        Dim sMessage As String
        Dim dsData As DataSet = Session.Item(SessionVars.SV_CaseDetails)

        dEarliestDate = FormatDate(dsData.Tables(BSELib.clsCase.CASEWORK_TABLE).Rows(0)("RBSEDate"))
        sMessage = "You must enter a date in the past but after the RBSE Date (" & FormatDate(dEarliestDate.ToString()) & ")"

        If Not ctlBreederBSE1ReceivedDate.Validate(dEarliestDate.AddDays(1), Today(), sMessage) Then Return False

        Return True
    End Function

    Private Function Vendor1BSE1ReceivedDateValid() As Boolean
        Dim dEarliestDate As Date
        Dim sMessage As String
        Dim dsData As DataSet = Session.Item(SessionVars.SV_CaseDetails)

        dEarliestDate = FormatDate(dsData.Tables(BSELib.clsCase.CASEWORK_TABLE).Rows(0)("RBSEDate"))
        sMessage = "You must enter a date in the past but after the RBSE Date (" & FormatDate(dEarliestDate.ToString()) & ")"

        If Not ctlVendor1BSE1ReceivedDate.Validate(dEarliestDate.AddDays(1), Today(), sMessage) Then Return False

        Return True
    End Function

    Private Function HomebredBSE1ReceivedDateValid() As Boolean
        Dim dEarliestDate As Date
        Dim sMessage As String
        Dim dsData As DataSet = Session.Item(SessionVars.SV_CaseDetails)

        dEarliestDate = FormatDate(dsData.Tables(BSELib.clsCase.CASEWORK_TABLE).Rows(0)("RBSEDate"))
        sMessage = "You must enter a date in the past but after the RBSE Date (" & FormatDate(dEarliestDate.ToString()) & ")"

        If Not ctlHomebredBSE1ReceivedDate.Validate(dEarliestDate.AddDays(1), Today(), sMessage) Then Return False

        Return True
    End Function

    Private Function SummarySheetReceivedDateValid() As Boolean
        Dim dEarliestDate As Date
        Dim sMessage As String
        Dim dsData As DataSet = Session.Item(SessionVars.SV_CaseDetails)

        dEarliestDate = FormatDate(dsData.Tables(BSELib.clsCase.CASEWORK_TABLE).Rows(0)("RBSEDate"))
        sMessage = "You must enter a date in the past but after the RBSE Date (" & FormatDate(dEarliestDate.ToString()) & ")"

        If Not ctlSummarySheetReceivedDate.Validate(dEarliestDate.AddDays(1), Today(), sMessage) Then Return False

        Return True
    End Function

    Private Function PaperworkCompleteDateValid() As Boolean
        Dim dEarliestDate As Date
        Dim sMessage As String
        Dim dsData As DataSet = Session.Item(SessionVars.SV_CaseDetails)

        dEarliestDate = FormatDate(dsData.Tables(BSELib.clsCase.CASEWORK_TABLE).Rows(0)("RBSEDate"))
        sMessage = "You must enter a date in the past but after the RBSE Date (" & FormatDate(dEarliestDate.ToString()) & ")"

        If Not ctlPaperworkCompleteDate.Validate(dEarliestDate.AddDays(1), Today(), sMessage) Then Return False

        Return True
    End Function

#End Region

#Region "Private Methods"

    Private Sub CheckBirthDateSource()
        Dim sMessage As String
        If ctlDateOfBirth.DateField = "" Then
            If ddlBirthDateSource.SelectedItem.Value <> "" Then
                sMessage = "The Date of Birth Source you entered for this date has been cleared."
            End If

            SelectItemInDropDownList(ddlBirthDateSource, "")
            ShowMessage(sMessage)
        End If
    End Sub

    Private Sub CheckHerdEntryDate()
        Dim dDate As Date
        Dim dEarliestDate As Date
        Dim dLatestDate As Date

        dEarliestDate = CDate("01 Jan 1970")

        Dim dsData As DataSet = Session.Item(SessionVars.SV_CaseDetails)

        If Not dsData Is Nothing Then
            If dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows.Count <> 0 Then
                If Not dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows(0)("HerdEntryDate") Is DBNull.Value Then
                    dLatestDate = DateAdd(DateInterval.Month, -18, dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows(0)("HerdEntryDate"))
                    dDate = Convert.ToDateTime(ctlDateOfBirth.DateField)
                    If dDate >= dEarliestDate And dDate <= dLatestDate Then
                        lblHerdEntryDateWarning.Visible = False
                    Else
                        lblHerdEntryDateWarning.Visible = True
                    End If
                End If
            End If
        End If
    End Sub

    Private Sub ShowMessage(ByVal sMessage As String)
        If sMessage <> "" Then
            Dim jScript As System.Text.StringBuilder = New System.Text.StringBuilder()
            With jScript
                .Append("<script language=""JavaScript"">")
                .Append("alert('" & sMessage & "')")
                .Append("</script>")
            End With
            Page.RegisterStartupScript("navigate", jScript.ToString())
        End If
    End Sub

#End Region


End Class
