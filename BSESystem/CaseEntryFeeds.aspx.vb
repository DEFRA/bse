Partial Class CaseEntryFeeds
    Inherits System.Web.UI.Page
    Protected WithEvents FeedsPager As DataGridPager
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents BatchNumberDisplay1 As BatchNumberDisplay
    Protected WithEvents ctlExitConfirmation As ExitConfirmation

    Private Const SELECTED_INDEX = 0
    Private Const YEAR_FROM = 1
    Private Const YEAR_TO = 2
    Private Const RATION_TYPE = 3
    Private Const RATION_DESCRIPTION = 4
    Private Const RATION_NAME = 5
    Private Const IS_PREPURCHASE = 6
    Private Const SUPPLIER_ID = 7
    Private Const SUPPLIER_NAME = 8

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
        'SetEnterKeys()
        lblRBSEHeader.Text = RBSE_CAPTION & Session(SessionVars.SV_RBSENumber)
        VLAHeader1.BatchNumber = Session(SessionVars.SV_BatchNumber)
        FeedsPager.SetGrid(grdFeeds)
        If Not IsPostBack Then
            LoadLookupLists()
            DisableFeedsGrid()
            LoadCaseDetails()
        End If

        litViewDocs.Text = GetSpolSiteButtonCode(Session(SessionVars.SV_RBSENumber))
        litViewDocs2.Text = litViewDocs.Text

        EnableControls()
    End Sub

    Private Sub SetEnterKeys()

        SetTextboxControlOnEnter(txtYearFrom, txtYearTo.ClientID)
        SetTextboxControlOnEnter(txtYearTo, txtSupplier.ClientID)
        SetTextboxDefaultButton(txtSupplier, btnLookupSupplier)
        SetTextboxControlOnEnter(txtRationName, chkIsPrePurchase.ClientID)

    End Sub


#Region "Load Lookup Lists"

    Private Sub LoadLookupLists()

        Dim blnResult As Boolean
        Dim objDataTable As DataTable
        Dim objLookup As New BSELib.LookupData()

        Try
            objDataTable = objLookup.GetLookupData(LOOKUP_RATION_TYPE)
            If Not (objDataTable Is Nothing) Then
                ddlRationType.DataSource = objDataTable
                ddlRationType.DataValueField = "Code"
                ddlRationType.DataTextField = "Description"
                ddlRationType.DataBind()
                Common.AddItemToDropDownList(ddlRationType)
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve 'DEFRA Tab' drop down lists.", ex)
        End Try

    End Sub

#End Region

#Region "Handle Grids"

    Private Sub DisableFeedsGrid()
        grdFeeds.DataSource = New DataTable()
        grdFeeds.DataBind()
        'grdFeeds.Enabled = False
        'FeedsPager.AllowAddNew = False
        'FeedsPager.AllowEdit = False
        'FeedsPager.AllowDelete = False
        FeedsPager.PageLinkCount = 0
        FeedsPager.Refresh()
    End Sub

    Private Sub EnableFeedsGrid()
        Dim dtData As DataTable = Session.Item(SessionVars.SV_FeedTable)
        Dim dvData As DataView = dtData.DefaultView
        Session.Item(SessionVars.SV_FeedView) = dvData

        grdFeeds.DataSource = dtData
        grdFeeds.DataKeyField = "ID"
        grdFeeds.CurrentPageIndex = 0
        grdFeeds.SelectedIndex = -1
        grdFeeds.EditItemIndex = -1
        grdFeeds.DataBind()

        FeedsPager.DataTableSessionID = SessionVars.SV_FeedTable
        FeedsPager.DataViewSessionID = SessionVars.SV_FeedView
        FeedsPager.PageLinkCount = 10
        FeedsPager.Refresh()
    End Sub

#End Region

#Region "Event Handlers"

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
        FeedsPager.CleanUpBlankRows()
        If UpdateSessionWithCaseDetails() Then
            Response.Redirect("CaseEntryFarm.aspx")
        End If
    End Sub

    Private Sub hlbCaseDEFRA_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlbCaseDEFRA.Click
        FeedsPager.CleanUpBlankRows()
        If UpdateSessionWithCaseDetails() Then
            Response.Redirect("CaseEntryDEFRA.aspx")
        End If
    End Sub

    Private Sub hlbBAB_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlbBAB.Click
        FeedsPager.CleanUpBlankRows()
        If UpdateSessionWithCaseDetails() Then
            Response.Redirect("CaseEntryBAB.aspx")
        End If
    End Sub

    Private Sub hlbClinical_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlbClinical.Click
        FeedsPager.CleanUpBlankRows()
        If UpdateSessionWithCaseDetails() Then
            Response.Redirect("CaseEntryClinical.aspx")
        End If
    End Sub

    Private Sub hlbCaseVLA_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlbCaseVLA.Click
        FeedsPager.CleanUpBlankRows()
        If UpdateSessionWithCaseDetails() Then
            Response.Redirect("CaseEntryVLA.aspx")
        End If
    End Sub

    Private Sub hlbRelations_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlbRelations.Click
        FeedsPager.CleanUpBlankRows()
        If UpdateSessionWithCaseDetails() Then
            Response.Redirect("CaseEntryRelations.aspx")
        End If
    End Sub

    Private Sub btnSave_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSave.Click, btnSave2.Click
        FeedsPager.CleanUpBlankRows()
        If UpdateSessionWithCaseDetails() Then
            Response.Redirect("CaseEntrySave.aspx")
        End If
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
        lblConfidential.Visible = True
        btnSave.Enabled = False
    End Sub

    Private Sub DEFRADataEntryEnable()
        BatchNumberDisplay1.Visible = False
        MakeControlsReadOnly()
        lblConfidential.Visible = False
        btnSave.Enabled = True
    End Sub

    Private Sub DEFRAMaintenanceEnable()
        BatchNumberDisplay1.Visible = False
        MakeControlsReadOnly()
        lblConfidential.Visible = False
        btnSave.Enabled = True
    End Sub

    Private Sub VLADataEntryEnable()
        BatchNumberDisplay1.Visible = True
        If IsVLAAllowedAdditionalCaseEdit(Session) Then
            MakeControlsWritable()
        Else
            MakeControlsReadOnly()
        End If
        lblConfidential.Visible = False
        btnSave.Enabled = True
    End Sub

    Private Sub VLAMaintenanceEnable()
        BatchNumberDisplay1.Visible = True
        If IsVLAAllowedAdditionalCaseEdit(Session) Then
            MakeControlsWritable()
        Else
            MakeControlsReadOnly()
        End If
        lblConfidential.Visible = False
        btnSave.Enabled = True
    End Sub

    Private Sub MakeControlsReadOnly()
        FeedsPager.AllowAddNew = False
        FeedsPager.AllowEdit = False
        FeedsPager.AllowDelete = False
        txtYearFrom.Enabled = False
        txtYearTo.Enabled = False
        ddlRationType.Enabled = False
        txtRationName.Enabled = False
        chkIsPrePurchase.Enabled = False
        txtSupplier.Enabled = False
        btnLookupSupplier.Enabled = False
        btnAddAsNew.Enabled = False
        btnUpdateSelected.Enabled = False
        btnDeleteSelected.Enabled = False
    End Sub

    Private Sub MakeControlsWritable()

        FeedsPager.AllowAddNew = False
        FeedsPager.AllowEdit = False
        FeedsPager.AllowDelete = False
        txtYearFrom.Enabled = True
        txtYearTo.Enabled = True
        ddlRationType.Enabled = True
        txtRationName.Enabled = True
        chkIsPrePurchase.Enabled = True
        txtSupplier.Enabled = True
        btnLookupSupplier.Enabled = True
        btnAddAsNew.Enabled = True
        If grdFeeds.SelectedIndex > 0 Then
            btnUpdateSelected.Enabled = True
            btnDeleteSelected.Enabled = True
        Else
            btnUpdateSelected.Enabled = False
            btnDeleteSelected.Enabled = False
        End If
    End Sub

#End Region

#Region "Handle Data"

    Private Function LoadCaseDetails() As Boolean
        Dim dsData As DataSet
        Dim SupplierDetails(8) As Object

        Try

            dsData = Session.Item(SessionVars.SV_CaseDetails)

            'If dsData.Tables(BSELib.clsCase.FEED_TABLE).Rows.Count <> 0 Then
            EnableFeedsGrid()
            If Not Session.Item(SessionVars.SV_TempSupplierDetails) Is Nothing Then
                ' We are coming back to the page from PickSupplier.aspx
                SupplierDetails = Session.Item(SessionVars.SV_TempSupplierDetails)

                txtID.Text = SupplierDetails(SELECTED_INDEX)
                txtYearFrom.Text = SupplierDetails(YEAR_FROM)
                txtYearTo.Text = SupplierDetails(YEAR_TO)
                SelectItemInDropDownList(ddlRationType, SupplierDetails(RATION_TYPE))
                txtRationName.Text = SupplierDetails(RATION_NAME)
                chkIsPrePurchase.Checked = SupplierDetails(IS_PREPURCHASE)
                txtSupplierID.Text = SupplierDetails(SUPPLIER_ID)
                txtSupplier.Text = SupplierDetails(SUPPLIER_NAME)
                lblSupplierValue.Text = txtSupplier.Text
                If txtID.Text <> "" Then 'We have the results from a selected row
                    grdFeeds.SelectedIndex = txtID.Text
                    btnUpdateSelected.Enabled = True
                    btnDeleteSelected.Enabled = True
                End If
                Session.Remove(SessionVars.SV_TempSupplierDetails)
            End If
            Return True
            'End If

            Return False

        Catch ex As Exception
            clsAppError.DisplayError("Failed to 'Load Case Details'.", ex)
            Return False
        End Try
    End Function

    Private Function UpdateSessionWithCaseDetails() As Boolean
        ' Perform any writing to the datasets in the session object.
        ' Databinding of tables means this might not be used.

        ' Also perform any Validation that is required.
        Return True
    End Function

#End Region

#Region "Button Click Handlers"

    Private Sub grdFeeds_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles grdFeeds.SelectedIndexChanged
        Dim drData() As DataRow
        Dim dtData As DataTable = Session.Item(SessionVars.SV_FeedTable)
        Dim iID As Int32 = Convert.ToInt32(grdFeeds.DataKeys(grdFeeds.SelectedIndex))
        Dim sSearch As String = "ID = " & CStr(iID)

        drData = dtData.Select(sSearch)
        txtID.Text = grdFeeds.SelectedIndex
        txtYearFrom.Text = Convert.ToString(drData(0).Item("YearFrom"))
        txtYearTo.Text = Convert.ToString(drData(0).Item("YearTo"))
        SelectItemInDropDownList(ddlRationType, drData(0).Item("RationType"))
        txtRationName.Text = drData(0).Item("RationName").ToString()
        lblSupplierValue.Text = drData(0).Item("SupplierName")
        txtSupplierID.Text = drData(0).Item("SupplierID").ToString()
        txtSupplier.Text = drData(0).Item("SupplierName")
        chkIsPrePurchase.Checked = GetRowColumnData(drData(0).Item("IsPrePurchase"))

        btnUpdateSelected.Enabled = True
        btnDeleteSelected.Enabled = True
    End Sub

    Private Sub btnUpdateSelected_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnUpdateSelected.Click
        Dim drData() As DataRow
        Dim dtData As DataTable = Session.Item(SessionVars.SV_FeedTable)
        Dim iID As Int32 = Convert.ToInt32(grdFeeds.DataKeys(grdFeeds.SelectedIndex))
        Dim sSearch As String = "ID = " & CStr(iID)

        If ControlsHaveValues() Then
            drData = dtData.Select(sSearch)

            drData(0).Item("YearFrom") = FormatEmptyString(txtYearFrom.Text)
            drData(0).Item("YearTo") = FormatEmptyString(txtYearTo.Text)
            drData(0).Item("RationType") = GetSelectedItemFromDropDownList(ddlRationType)
            If ddlRationType.SelectedItem Is Nothing Then
                drData(0).Item("RationType") = ""
                drData(0).Item("RationDescription") = ""
            Else
                drData(0).Item("RationType") = ddlRationType.SelectedItem.Value
                drData(0).Item("RationDescription") = ddlRationType.SelectedItem.Text
            End If
            drData(0).Item("RationName") = FormatEmptyString(txtRationName.Text)
            drData(0).Item("SupplierID") = txtSupplierID.Text
            drData(0).Item("SupplierName") = lblSupplierValue.Text
            drData(0).Item("IsPrePurchase") = chkIsPrePurchase.Checked

            txtYearFrom.Text = ""
            txtYearTo.Text = ""
            ddlRationType.SelectedIndex = -1
            txtRationName.Text = ""
            txtSupplier.Text = ""
            btnUpdateSelected.Enabled = False
            btnDeleteSelected.Enabled = False
            chkIsPrePurchase.Checked = False
            EnableFeedsGrid()
        End If
    End Sub

    Private Sub btnDeleteSelected_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnDeleteSelected.Click
        Dim drData() As DataRow
        Dim dtData As DataTable = Session.Item(SessionVars.SV_FeedTable)
        Dim iID As Int32 = Convert.ToInt32(grdFeeds.DataKeys(grdFeeds.SelectedIndex))
        Dim sSearch As String = "ID = '" & CStr(iID) & "'"

        drData = dtData.Select(sSearch)

        drData(0).Delete()

        txtYearFrom.Text = ""
        txtYearTo.Text = ""
        ddlRationType.SelectedIndex = -1
        txtRationName.Text = ""
        txtSupplier.Text = ""
        btnUpdateSelected.Enabled = False
        btnDeleteSelected.Enabled = False
        chkIsPrePurchase.Checked = False
        EnableFeedsGrid()
    End Sub

    Private Function ControlsHaveValues() As Boolean
        If lblSupplierValue.Text = "" Then
            lblLookupError.Visible = True
        Else
            lblLookupError.Visible = False
        End If

        If txtYearFrom.Text = "" Then
            lblYearFromEmpty.Visible = True
        Else
            Dim dsData As DataSet = Session.Item(SessionVars.SV_CaseDetails)
            Dim dBirthDate As Date
            If Not dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows(0)("BirthDate") Is DBNull.Value Then
                dBirthDate = dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows(0)("BirthDate")

                If CInt(txtYearFrom.Text) >= Year(dBirthDate) Then
                    lblYearFromInvalid.Visible = False
                Else
                    lblYearFromInvalid.Visible = True
                End If
                lblYearFromEmpty.Visible = False
            End If
        End If

        If txtYearTo.Text = "" Then
            lblYearToEmpty.Visible = True
        Else
            If txtYearFrom.Text = "" Then
                lblYearToInvalid.Visible = False
            Else
                If CInt(txtYearFrom.Text) <= CInt(txtYearTo.Text) Then
                    lblYearToInvalid.Visible = False
                Else
                    lblYearToInvalid.Visible = True
                End If
            End If
            lblYearToEmpty.Visible = False
        End If

        If ddlRationType.SelectedIndex = 0 Then
            lblRationTypeEmpty.Visible = True
        Else
            lblRationTypeEmpty.Visible = False
        End If

        Return Not (lblLookupError.Visible Or lblYearFromEmpty.Visible Or lblYearToEmpty.Visible Or lblRationTypeEmpty.Visible Or lblYearFromInvalid.Visible Or lblYearToInvalid.Visible)
    End Function

    Private Sub btnAddAsNew_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnAddAsNew.Click
        Dim dtData As DataTable = Session.Item(SessionVars.SV_FeedTable)
        Dim drData As DataRow

        If ControlsHaveValues() Then
            drData = dtData.NewRow()
            drData.Item("RBSE") = Replace(Session.Item(SessionVars.SV_RBSENumber), "/", "")

            drData.Item("YearFrom") = FormatEmptyString(txtYearFrom.Text)
            drData.Item("YearTo") = FormatEmptyString(txtYearTo.Text)
            drData.Item("RationType") = GetSelectedItemFromDropDownList(ddlRationType)
            If ddlRationType.SelectedItem Is Nothing Then
                drData.Item("RationType") = ""
                drData.Item("RationDescription") = ""
            Else
                drData.Item("RationType") = ddlRationType.SelectedItem.Value
                drData.Item("RationDescription") = ddlRationType.SelectedItem.Text
            End If
            drData.Item("RationName") = txtRationName.Text
            drData.Item("SupplierID") = txtSupplierID.Text
            drData.Item("SupplierName") = lblSupplierValue.Text
            drData.Item("IsPrePurchase") = chkIsPrePurchase.Checked

            dtData.Rows.Add(drData)

            txtID.Text = ""
            txtYearFrom.Text = ""
            txtYearTo.Text = ""
            ddlRationType.SelectedIndex = -1
            txtRationName.Text = ""
            txtSupplier.Text = ""
            btnUpdateSelected.Enabled = False
            btnDeleteSelected.Enabled = False
            chkIsPrePurchase.Checked = False
            lblSupplierValue.Text = ""
            EnableFeedsGrid()
        End If
    End Sub

    Private Sub btnLookupSupplier_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnLookupSupplier.Click
        Dim SupplierDetails(8) As Object
        Dim objCase As New BSELib.clsCase()
        Dim sName As String
        Dim iID As Int32
        Dim sDetails As String

        lblLookupError.Visible = False
        sName = UCase(txtSupplier.Text)

        If Not (objCase.GetSupplierByName(sName, iID, sDetails)) Then
            Throw New Exception("Case.GetSupplierByName returned False")
        End If

        If sName <> "" Then
            txtSupplierID.Text = CStr(iID)
            lblSupplierValue.Text = sName
        Else
            ' There was no match so we need to go to the pick supplier page
            SupplierDetails(SELECTED_INDEX) = txtID.Text
            SupplierDetails(YEAR_FROM) = txtYearFrom.Text
            SupplierDetails(YEAR_TO) = txtYearTo.Text
            If ddlRationType.SelectedItem Is Nothing Then
                SupplierDetails(RATION_TYPE) = ""
                SupplierDetails(RATION_DESCRIPTION) = ""
            Else
                SupplierDetails(RATION_TYPE) = ddlRationType.SelectedItem.Value
                SupplierDetails(RATION_DESCRIPTION) = ddlRationType.SelectedItem.Text
            End If
            SupplierDetails(RATION_NAME) = txtRationName.Text
            SupplierDetails(IS_PREPURCHASE) = chkIsPrePurchase.Checked
            SupplierDetails(SUPPLIER_NAME) = txtSupplier.Text

            Session.Item(SessionVars.SV_TempSupplierDetails) = SupplierDetails

            Response.Redirect("PickSupplier.aspx")
        End If
    End Sub
#End Region

End Class
