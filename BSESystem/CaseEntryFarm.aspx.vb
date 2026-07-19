Partial Class CaseEntryFarm
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents LinkedFarmsPager As DataGridPager
    Protected WithEvents HerdSizePager As DataGridPager
    Protected WithEvents CPHH1 As CPHH
    Protected WithEvents MapReference1 As MapReference
    Protected WithEvents BatchNumberDisplay1 As BatchNumberDisplay
    Protected WithEvents ctlExitConfirmation As ExitConfirmation


    Private m_bContinueEditingLinkedFarms As Boolean = False

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
        LinkedFarmsPager.SetGrid(grdLinkedFarms)
        HerdSizePager.SetGrid(grdHerdSize)
        If Not IsPostBack Then
            LoadLookupLists()
            DisableLinkedFarmGrid()
            DisableHerdSizeGrid()
            LoadCaseDetails()
        End If

        litViewDocs.Text = GetSpolSiteButtonCode(Session(SessionVars.SV_RBSENumber))
        litViewDocs2.Text = litViewDocs.Text

        If Not IsNothing(Session(SessionVars.SV_CPHHNumber)) AndAlso CStr(Session(SessionVars.SV_CPHHNumber)).Length > 0 Then
            litViewDocsForCPHH.Text = GetSpolSiteButtonCode(Session(SessionVars.SV_CPHHNumber))
        End If

        EnableControls()
    End Sub

    Private Sub SetEnterKeys()
        CPHH1.SetDefaultButton(btnLookUp)
    End Sub

#Region "Populate Lookup Lists"

    Private Sub LoadLookupLists()

        Dim objDataTable As DataTable
        Dim objLookup As New BSELib.LookupData()

        Try
            objDataTable = objLookup.GetLookupData(LOOKUP_HERD_TYPE)
            If Not (objDataTable Is Nothing) Then
                ddlHerdType.DataSource = objDataTable
                ddlHerdType.DataValueField = "Code"
                ddlHerdType.DataTextField = "Description"
                ddlHerdType.DataBind()
                Common.AddItemToDropDownList(ddlHerdType)
            End If

            objDataTable = objLookup.GetLookupData(LOOKUP_PEDIGREE)
            If Not (objDataTable Is Nothing) Then
                ddlPedigree.DataSource = objDataTable
                ddlPedigree.DataValueField = "Code"
                ddlPedigree.DataTextField = "Description"
                ddlPedigree.DataBind()
                Common.AddItemToDropDownList(ddlPedigree)
            End If

            objDataTable = objLookup.GetLookupData(LOOKUP_AHO)
            If Not (objDataTable Is Nothing) Then
                ddlAHO.DataSource = objDataTable
                ddlAHO.DataValueField = "Code"
                ddlAHO.DataTextField = "Name"
                ddlAHO.DataBind()
                Common.AddItemToDropDownList(ddlAHO)
            End If

            objDataTable = objLookup.GetLookupData(LOOKUP_BSE_COUNTY)
            If Not (objDataTable Is Nothing) Then
                ddlCounty.DataSource = objDataTable
                ddlCounty.DataValueField = "Code"
                ddlCounty.DataTextField = "Description"
                ddlCounty.DataBind()
                Common.AddItemToDropDownList(ddlCounty)
            End If

            objDataTable = objLookup.GetLookupData(LOOKUP_AHO)
            If Not (objDataTable Is Nothing) Then
                ddlAHO.DataSource = objDataTable
                ddlAHO.DataValueField = "Code"
                ddlAHO.DataTextField = "Name"
                ddlAHO.DataBind()
                Common.AddItemToDropDownList(ddlAHO)
            End If

            objDataTable = objLookup.GetAuthorityCounties()
            If Not (objDataTable Is Nothing) Then
                ddlAuthorityCounty.DataSource = objDataTable
                ddlAuthorityCounty.DataValueField = "ID"
                ddlAuthorityCounty.DataTextField = "County"
                ddlAuthorityCounty.DataBind()
                Common.AddItemToDropDownList(ddlAuthorityCounty)
            End If

            Common.AddItemToDropDownList(ddlLocalAuthority)
            Common.AddItemToDropDownList(ddlADNSRegion)

        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve 'Farm Tab' drop down lists.", ex)
        End Try

    End Sub

    Private Function LoadAuthorityLookup(ByVal vsAuthorityCountyID As String) As String

        Dim objDataTable As DataTable
        Dim objLookup As New BSELib.LookupData()
        Dim sAuthorityID As String = ""

        Try
            ddlLocalAuthority.Items.Clear()

            If vsAuthorityCountyID <> "" Then
                objDataTable = objLookup.GetAuthorities(CInt(vsAuthorityCountyID))
                If Not (objDataTable Is Nothing) Then
                    ddlLocalAuthority.DataSource = objDataTable
                    ddlLocalAuthority.DataValueField = "ID"
                    ddlLocalAuthority.DataTextField = "Name"
                    ddlLocalAuthority.DataBind()
                    If objDataTable.Rows.Count = 1 Then
                        sAuthorityID = objDataTable.Rows(0).Item("ID").ToString()
                    End If
                End If
            End If
            Common.AddItemToDropDownList(ddlLocalAuthority)
            If sAuthorityID <> "" Then
                SelectItemInDropDownList(ddlLocalAuthority, sAuthorityID)
            End If
            Return sAuthorityID

        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve 'Farm Tab' local authority drop down list.", ex)
        End Try
    End Function

    Private Function LoadADNSRegionLookup(ByVal vsAuthorityID As String) As String

        Dim objDataTable As DataTable
        Dim objLookup As New BSELib.LookupData()
        Dim sADNSRegionID As String = ""

        Try
            ddlADNSRegion.Items.Clear()

            If vsAuthorityID <> "" Then
                objDataTable = objLookup.GetADNSRegions(CInt(vsAuthorityID))
                If Not (objDataTable Is Nothing) Then
                    ddlADNSRegion.DataSource = objDataTable
                    ddlADNSRegion.DataValueField = "ID"
                    ddlADNSRegion.DataTextField = "Name"
                    ddlADNSRegion.DataBind()
                    If objDataTable.Rows.Count = 1 Then
                        sADNSRegionID = objDataTable.Rows(0).Item("ID").ToString()
                    End If
                End If
            End If
            AddItemToDropDownList(ddlADNSRegion)
            If sADNSRegionID <> "" Then
                SelectItemInDropDownList(ddlADNSRegion, sADNSRegionID)
            End If
            Return sADNSRegionID
        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve 'Farm Tab' ADNS Region drop down list.", ex)
        End Try
    End Function

#End Region

#Region "Other Event Handlers"

    Private Sub btnSave_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSave.Click, btnSave2.Click
        HerdSizePager.CleanUpBlankRows()
        LinkedFarmsPager.CleanUpBlankRows()
        If UpdateSessionWithCaseDetails() Then
            Response.Redirect("CaseEntrySave.aspx")
        End If
    End Sub

    Private Sub btnFarmAuditLog_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnFarmAuditLog.Click
        HerdSizePager.CleanUpBlankRows()
        LinkedFarmsPager.CleanUpBlankRows()
        If UpdateSessionWithCaseDetails() Then Response.Redirect("FarmAuditLogReport.aspx")
    End Sub

    Private Sub btnCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancel.Click, btnCancel2.Click
        CancelCaseEdit()
    End Sub

    Private Sub VLAHeader1_HomeClick(ByVal sender As Object, ByVal e As BSESystem.HomeLinkEventArgs) Handles VLAHeader1.HomeClick
        CancelCaseEdit()
        e.bNavigateHome = False
    End Sub

    Private Sub CancelCaseEdit()
        Dim sMessage As System.Text.StringBuilder = New System.Text.StringBuilder()
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

    Private Sub btnLookUp_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnLookUp.Click
        If Left$(CPHH1.CPHH, 2) = "00" Then
            lblNonGBCPHH.Visible = True
        Else
            lblNonGBCPHH.Visible = False
            Session(SessionVars.SV_CPHHNumber) = CPHH1.CPHH
            If Not LoadFarmDetails() Then
                Response.Redirect("PickFarm.aspx")
            End If
            EnableControls()
            LinkedFarmsPager.Refresh()
            HerdSizePager.Refresh()
        End If
    End Sub

    Private Sub hlbCaseDEFRA_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlbCaseDEFRA.Click
        HerdSizePager.CleanUpBlankRows()
        LinkedFarmsPager.CleanUpBlankRows()
        If UpdateSessionWithCaseDetails() Then Response.Redirect("CaseEntryDEFRA.aspx")
    End Sub

    Private Sub hlbBAB_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlbBAB.Click
        HerdSizePager.CleanUpBlankRows()
        LinkedFarmsPager.CleanUpBlankRows()
        If UpdateSessionWithCaseDetails() Then Response.Redirect("CaseEntryBAB.aspx")
    End Sub

    Private Sub hlbCaseVLA_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlbCaseVLA.Click
        HerdSizePager.CleanUpBlankRows()
        LinkedFarmsPager.CleanUpBlankRows()
        If UpdateSessionWithCaseDetails() Then Response.Redirect("CaseEntryVLA.aspx")
    End Sub

    Private Sub hlbClinical_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlbClinical.Click
        HerdSizePager.CleanUpBlankRows()
        LinkedFarmsPager.CleanUpBlankRows()
        If UpdateSessionWithCaseDetails() Then Response.Redirect("CaseEntryClinical.aspx")
    End Sub

    Private Sub hlbFeeds_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlbFeeds.Click
        HerdSizePager.CleanUpBlankRows()
        LinkedFarmsPager.CleanUpBlankRows()
        If UpdateSessionWithCaseDetails() Then Response.Redirect("CaseEntryFeeds.aspx")
    End Sub

    Private Sub hlbRelations_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlbRelations.Click
        HerdSizePager.CleanUpBlankRows()
        LinkedFarmsPager.CleanUpBlankRows()
        If UpdateSessionWithCaseDetails() Then Response.Redirect("CaseEntryRelations.aspx")
    End Sub

    Private Sub btnUseAboveAddress_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnUseAboveAddress.Click
        txtCorrespondenceAddress1.Text = txtAddress1.Text
        txtCorrespondenceAddress2.Text = txtAddress2.Text
        txtCorrespondenceAddress3.Text = txtAddress3.Text
        txtCorrespondencePostcode.Text = txtPostcode.Text
    End Sub

#End Region

#Region "Control Enabling - Disabling"

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
        CPHH1.Enabled = False
        EnableAuditLogButton()
        MakeDEFRAControlsReadOnly()
        MakeJointControlsReadOnly()
        MakeVLAControlsReadOnly()
        lblConfidential.Visible = True
        btnSave.Enabled = False
    End Sub

    Private Sub DEFRADataEntryEnable()
        BatchNumberDisplay1.Visible = False
        CPHH1.Enabled = True
        EnableAuditLogButton()
        MakeDEFRAControlsWritable()
        MakeJointControlsWritable()
        MakeVLAControlsReadOnly()
        lblConfidential.Visible = False
        btnSave.Enabled = True
    End Sub

    Private Sub DEFRAMaintenanceEnable()
        BatchNumberDisplay1.Visible = False
        CPHH1.Enabled = True
        EnableAuditLogButton()
        MakeDEFRAControlsWritable()
        MakeJointControlsWritable()
        MakeVLAControlsReadOnly()
        lblConfidential.Visible = False
        btnSave.Enabled = True
    End Sub

    Private Sub VLADataEntryEnable()
        BatchNumberDisplay1.Visible = True
        CPHH1.Enabled = False
        EnableAuditLogButton()
        MakeDEFRAControlsReadOnly()
        If IsVLAAllowedMainCaseEdit(Session) Then
            MakeJointControlsWritable()
            MakeVLAControlsWritable()
        Else
            MakeJointControlsReadOnly()
            MakeVLAControlsReadOnly()
        End If
        lblConfidential.Visible = False
        btnSave.Enabled = True
    End Sub

    Private Sub VLAMaintenanceEnable()
        BatchNumberDisplay1.Visible = True
        CPHH1.Enabled = True
        EnableAuditLogButton()
        MakeDEFRAControlsWritable()
        If IsVLAAllowedMainCaseEdit(Session) Then
            MakeJointControlsWritable()
            MakeVLAControlsWritable()
        Else
            MakeJointControlsReadOnly()
            MakeVLAControlsReadOnly()
        End If
        lblConfidential.Visible = False
        btnSave.Enabled = True
    End Sub

    Private Sub MakeDEFRAControlsReadOnly()
        txtOwnerName.Enabled = False
        txtAddress1.Enabled = False
        txtAddress2.Enabled = False
        txtAddress3.Enabled = False
        txtPostcode.Enabled = False
        txtParish.Enabled = False
        txtDistrict.Enabled = False
        ddlCounty.Enabled = False
        ddlAHO.Enabled = False
        ddlADNSRegion.Enabled = False
        ddlLocalAuthority.Enabled = False
        ddlAuthorityCounty.Enabled = False
        btnEstimate.Enabled = False
        MapReference1.Enabled = False
        btnUseAboveAddress.Enabled = False
        txtCorrespondenceAddress1.Enabled = False
        txtCorrespondenceAddress2.Enabled = False
        txtCorrespondenceAddress3.Enabled = False
        txtCorrespondencePostcode.Enabled = False
        LinkedFarmsPager.AllowAddNew = False
        LinkedFarmsPager.AllowEdit = False
        LinkedFarmsPager.AllowDelete = False
    End Sub

    Private Sub MakeJointControlsReadOnly()
        txtHerdmark1.Enabled = False
        txtHerdmark2.Enabled = False
        txtHerdmark3.Enabled = False
        txtNumericHerdmark1.Enabled = False
        txtNumericHerdmark2.Enabled = False
    End Sub

    Private Sub MakeVLAControlsReadOnly()
        ddlHerdType.Enabled = False
        ddlPedigree.Enabled = False
        chkIsDealer.Enabled = False
        HerdSizePager.AllowAddNew = False
        HerdSizePager.AllowEdit = False
        HerdSizePager.AllowDelete = False
    End Sub

    Private Sub EnableAuditLogButton()
        If Session(SessionVars.SV_CPHHNumber) <> "" Then
            btnFarmAuditLog.Enabled = True
        Else
            btnFarmAuditLog.Enabled = False
        End If
    End Sub

    Private Sub MakeDEFRAControlsWritable()
        If Session(SessionVars.SV_CPHHNumber) <> "" Then
            CPHH1.Enabled = btnLookUp.Visible
            txtOwnerName.Enabled = True
            txtAddress1.Enabled = True
            txtAddress2.Enabled = True
            txtAddress3.Enabled = True
            txtPostcode.Enabled = True
            txtDistrict.Enabled = True
            If chkNonGBFarm.Checked Then
                txtParish.Enabled = False
                ddlAHO.Enabled = False
            Else
                txtParish.Enabled = True
                ddlAHO.Enabled = True
            End If
            ddlCounty.Enabled = True
            ddlADNSRegion.Enabled = Not chkNonGBFarm.Checked
            ddlLocalAuthority.Enabled = Not chkNonGBFarm.Checked
            ddlAuthorityCounty.Enabled = Not chkNonGBFarm.Checked
            btnEstimate.Enabled = True
            MapReference1.Enabled = True
            btnUseAboveAddress.Enabled = True
            txtCorrespondenceAddress1.Enabled = True
            txtCorrespondenceAddress2.Enabled = True
            txtCorrespondenceAddress3.Enabled = True
            txtCorrespondencePostcode.Enabled = True
            LinkedFarmsPager.AllowAddNew = True
            LinkedFarmsPager.AllowEdit = True
            LinkedFarmsPager.AllowDelete = True
        Else
            MakeDEFRAControlsReadOnly()
        End If
    End Sub

    Private Sub MakeJointControlsWritable()
        If Session(SessionVars.SV_CPHHNumber) <> "" Then
            txtHerdmark1.Enabled = True
            txtHerdmark2.Enabled = True
            txtHerdmark3.Enabled = True
            txtNumericHerdmark1.Enabled = True
            txtNumericHerdmark2.Enabled = True
        Else
            MakeJointControlsReadOnly()
        End If
    End Sub

    Private Sub MakeVLAControlsWritable()
        If Session(SessionVars.SV_CPHHNumber) <> "" Then
            ddlHerdType.Enabled = True
            ddlPedigree.Enabled = True
            chkIsDealer.Enabled = True
            HerdSizePager.AllowAddNew = True
            HerdSizePager.AllowEdit = True
            HerdSizePager.AllowDelete = True
        Else
            MakeVLAControlsReadOnly()
        End If
    End Sub

#End Region

#Region "Handle Data"

    Private Function LoadCaseDetails() As Boolean

        Try
            Dim dsFarmData As DataSet = Session.Item(SessionVars.SV_FarmDetails)
            Dim sCPHH As String

            If Not (dsFarmData Is Nothing) Then
                If dsFarmData.Tables(BSELib.clsFarm.FARM_TABLE).Rows.Count <> 0 Then
                    With dsFarmData.Tables(BSELib.clsFarm.FARM_TABLE).Rows(0)
                        sCPHH = .Item("CPHH").ToString()
                        CPHH1.CPHH = sCPHH

                        txtOwnerName.Text = .Item("OwnerName").ToString()
                        txtAddress1.Text = .Item("Address1").ToString()
                        txtAddress2.Text = .Item("Address2").ToString()
                        txtAddress3.Text = .Item("Address3").ToString()
                        txtPostcode.Text = .Item("Postcode").ToString()
                        txtParish.Text = .Item("Parish").ToString()
                        txtDistrict.Text = .Item("District").ToString()
                        SelectItemInDropDownList(ddlCounty, .Item("County").ToString())
                        txtCorrespondenceAddress1.Text = .Item("CorrespondenceAddress1").ToString()
                        txtCorrespondenceAddress2.Text = .Item("CorrespondenceAddress2").ToString()
                        txtCorrespondenceAddress3.Text = .Item("CorrespondenceAddress3").ToString()
                        txtCorrespondencePostcode.Text = .Item("CorrespondencePostcode").ToString()

                        SelectItemInDropDownList(ddlAuthorityCounty, .Item("AuthorityCountyID").ToString())
                        LoadAuthorityLookup(.Item("AuthorityCountyID").ToString())
                        SelectItemInDropDownList(ddlLocalAuthority, .Item("AuthorityID").ToString())
                        LoadADNSRegionLookup(.Item("AuthorityID").ToString())
                        SelectItemInDropDownList(ddlADNSRegion, .Item("ADNSRegionID").ToString())

                        MapReference1.MapReference = .Item("MapReference").ToString()
                        txtHerdmark1.Text = .Item("Herdmark1").ToString()
                        txtHerdmark2.Text = .Item("Herdmark2").ToString()
                        txtHerdmark3.Text = .Item("Herdmark3").ToString()
                        txtNumericHerdmark1.Text = .Item("NumericHerdmark1").ToString()
                        txtNumericHerdmark2.Text = .Item("NumericHerdmark2").ToString()
                        SelectItemInDropDownList(ddlAHO, .Item("AHO").ToString())
                        SelectItemInDropDownList(ddlHerdType, .Item("HerdType").ToString())
                        SelectItemInDropDownList(ddlPedigree, .Item("PedigreeType").ToString())
                        chkIsDealer.Checked = GetRowColumnData(.Item("IsDealer"))
                        chkNonGBFarm.Checked = GetRowColumnData(.Item("IsNonGBFarm"))
                    End With

                    EnableLinkedFarmGrid()
                    EnableHerdSizeGrid()
                    DisplayNumberOfConfirmedCases(sCPHH)
                    CheckLookupCPHH()

                    Return True
                End If
            End If

            Return False

        Catch ex As Exception
            clsAppError.DisplayError("Failed to Load Case Details.", ex)
            Return False
        End Try
    End Function

    Private Sub CheckLookupCPHH()
        Dim dsData As DataSet = Session.Item(SessionVars.SV_CaseDetails)

        If dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows(0).RowState = DataRowState.Added Then
            btnLookUp.Visible = True
        Else
            btnLookUp.Visible = False
        End If
    End Sub

    Private Sub DisplayNumberOfConfirmedCases(ByVal sCPHH As String)
        Dim objFarm As New BSELib.clsFarm()
        Dim sConfirmedCases As String

        If Not objFarm.GetNumberOfConfirmedCases(sCPHH, sConfirmedCases) Then
            Throw New Exception("Farm.GetNumberOfConfirmedCases returned False")
        End If
        lblConfirmedCases.Text = "Number of Confirmed Cases: " & sConfirmedCases
    End Sub

    Private Function LoadFarmDetails() As Boolean

        Dim sCPHH As String
        Dim dsData As DataSet

        Try
            sCPHH = CPHH1.CPHH
            If Trim(sCPHH) <> "" Then
                Session.Item(SessionVars.SV_CPHHNumber) = sCPHH
                dsData = Session.Item(SessionVars.SV_CaseDetails)
                dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows(0)("CPHH") = sCPHH
                GetFarmDetailsFromDatabase(sCPHH, Session)

                If LoadCaseDetails() Then
                    Return True
                End If
            End If
            Return False

        Catch ex As Exception
            clsAppError.DisplayError("Failed to Load Farm Details.", ex)
            Return False
        End Try
    End Function

    Private Function UpdateSessionWithCaseDetails() As Boolean

        Dim dsData As DataSet

        Try
            dsData = Session.Item(SessionVars.SV_FarmDetails)

            If Not dsData Is Nothing Then
                If dsData.Tables(BSELib.clsFarm.FARM_TABLE).Rows.Count <> 0 Then
                    Dim sCPHH As String = CPHH1.CPHH
                    With dsData.Tables(BSELib.clsFarm.FARM_TABLE).Rows(0)
                        .Item("CPHH") = sCPHH
                        Session.Item(SessionVars.SV_CPHHNumber) = sCPHH
                        .Item("OwnerName") = FormatEmptyString(txtOwnerName.Text)
                        .Item("Address1") = FormatEmptyString(txtAddress1.Text)
                        .Item("Address2") = FormatEmptyString(txtAddress2.Text)
                        .Item("Address3") = FormatEmptyString(txtAddress3.Text)
                        .Item("Postcode") = FormatEmptyString(txtPostcode.Text)
                        .Item("Parish") = FormatEmptyString(txtParish.Text)
                        .Item("District") = FormatEmptyString(txtDistrict.Text)
                        .Item("County") = FormatEmptyString(GetSelectedItemFromDropDownList(ddlCounty))
                        .Item("CorrespondenceAddress1") = FormatEmptyString(txtCorrespondenceAddress1.Text)
                        .Item("CorrespondenceAddress2") = FormatEmptyString(txtCorrespondenceAddress2.Text)
                        .Item("CorrespondenceAddress3") = FormatEmptyString(txtCorrespondenceAddress3.Text)
                        .Item("CorrespondencePostcode") = FormatEmptyString(txtCorrespondencePostcode.Text)
                        .Item("MapReference") = FormatEmptyString(MapReference1.MapReference)
                        .Item("Herdmark1") = FormatEmptyString(txtHerdmark1.Text)
                        .Item("Herdmark2") = FormatEmptyString(txtHerdmark2.Text)
                        .Item("Herdmark3") = FormatEmptyString(txtHerdmark3.Text)
                        .Item("NumericHerdmark1") = FormatEmptyString(txtNumericHerdmark1.Text)
                        .Item("NumericHerdmark2") = FormatEmptyString(txtNumericHerdmark2.Text)
                        .Item("AHO") = FormatEmptyString(GetSelectedItemFromDropDownList(ddlAHO))
                        .Item("HerdType") = FormatEmptyString(GetSelectedItemFromDropDownList(ddlHerdType))
                        .Item("PedigreeType") = FormatEmptyString(GetSelectedItemFromDropDownList(ddlPedigree))
                        .Item("IsDealer") = chkIsDealer.Checked
                        .Item("ADNSRegionID") = FormatEmptyString(GetSelectedItemFromDropDownList(ddlADNSRegion))
                    End With

                    If HerdSizePager.InEditMode Then Return False
                    If LinkedFarmsPager.InEditMode Then Return False

                    Return True
                End If

                Return False
            End If
            Return True

        Catch ex As Exception
            clsAppError.DisplayError("Failed to Save Case Details.", ex)
            Return False
        End Try
    End Function

    Private Function GetRelatedFarmDetails(ByVal sCPHH As String) As String

        Dim bIsNonGBFarm As Boolean
        Dim strOwnerName As String
        Dim strAddress1 As String
        Dim strAddress2 As String
        Dim strAddress3 As String
        Dim strPostcode As String
        Dim strParish As String
        Dim strDistrict As String
        Dim strCounty As String
        Dim strCorrespondenceAddress1 As String
        Dim strCorrespondenceAddress2 As String
        Dim strCorrespondenceAddress3 As String
        Dim strCorrespondencePostcode As String
        Dim strMapReference As String
        Dim strHerdmark1 As String
        Dim strHerdmark2 As String
        Dim strHerdmark3 As String
        Dim strNumericHerdmark1 As String
        Dim strNumericHerdmark2 As String
        Dim strAHO As String
        Dim strHerdType As String
        Dim strPedigree As String
        Dim bIsDealer As Boolean
        Dim lRowStamp As Long
        Dim objFarm As New BSELib.clsFarm()

        Dim dtData As DataTable

        Try
            If (sCPHH <> "") Then
                If Not (objFarm.GetByCPHH(sCPHH, _
                bIsNonGBFarm, _
                strOwnerName, _
                strAddress1, _
                strAddress2, _
                strAddress3, _
                strPostcode, _
                strParish, _
                strDistrict, _
                strCounty, _
                strCorrespondenceAddress1, _
                strCorrespondenceAddress2, _
                strCorrespondenceAddress3, _
                strCorrespondencePostcode, _
                strMapReference, _
                strHerdmark1, _
                strHerdmark2, _
                strHerdmark3, _
                strNumericHerdmark1, _
                strNumericHerdmark2, _
                strAHO, _
                strHerdType, _
                strPedigree, _
                bIsDealer, _
                lRowStamp)) Then
                    Return ""
                End If

                If Not (strOwnerName Is Nothing) And Not (strAddress1 Is Nothing) Then
                    Return strOwnerName & ", " & strAddress1
                Else
                    Return ""
                End If
            End If

            Return ""

        Catch ex As Exception
            clsAppError.DisplayError("Failed to Get Related Farm Details.", ex)
            Return False
        End Try
    End Function
#End Region

#Region "Linked Farms Grid"

    Private Sub DisableLinkedFarmGrid()
        grdLinkedFarms.DataSource = New DataTable()
        grdLinkedFarms.DataBind()
        LinkedFarmsPager.DataTableSessionID = SessionVars.SV_RelatedFarmsTable
        LinkedFarmsPager.PageLinkCount = 0
        LinkedFarmsPager.Refresh()
    End Sub

    Private Sub EnableLinkedFarmGrid()
        Dim dtData As DataTable = Session.Item(SessionVars.SV_RelatedFarmsTable)
        Dim dvData As DataView = dtData.DefaultView
        Session.Item(SessionVars.SV_RelatedFarmsView) = dvData

        grdLinkedFarms.DataSource = dtData
        grdLinkedFarms.DataKeyField = "ID"
        grdLinkedFarms.CurrentPageIndex = 0
        grdLinkedFarms.SelectedIndex = -1
        grdLinkedFarms.EditItemIndex = -1
        grdLinkedFarms.DataBind()

        LinkedFarmsPager.DataTableSessionID = SessionVars.SV_RelatedFarmsTable
        LinkedFarmsPager.DataViewSessionID = SessionVars.SV_RelatedFarmsView
        LinkedFarmsPager.PageLinkCount = 10
        LinkedFarmsPager.Refresh()
    End Sub

    Private Sub grdLinkedFarms_ItemDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.DataGridItemEventArgs) Handles grdLinkedFarms.ItemDataBound
        ' populate template column values here
        Try
            ' set up the checkbox and drop-down columns
            Dim drv As DataRowView = CType(e.Item.DataItem, DataRowView)
            If Not drv Is Nothing Then
                Dim ctlCPHH As CPHH = Nothing
                Dim lblCPHHLabel As Label = Nothing
                Dim lblError As Label = Nothing

                If e.Item.ItemType = ListItemType.EditItem Then
                    ' populate edit mode controls
                    ctlCPHH = CType(e.Item.FindControl("ctlLinkedCPHH"), CPHH)
                ElseIf e.Item.ItemType = ListItemType.Item _
                OrElse e.Item.ItemType = ListItemType.AlternatingItem _
                OrElse e.Item.ItemType = ListItemType.SelectedItem Then
                    ' populate display mode controls
                    lblCPHHLabel = CType(e.Item.FindControl("lblLinkedCPHH"), Label)
                End If

                If Not lblCPHHLabel Is Nothing Then
                    If Not IsDBNull(drv("RelatedCPHH")) Then
                        Dim sCPHH As String
                        sCPHH = drv("RelatedCPHH")
                        lblCPHHLabel.Text = FormatCPHH(sCPHH)
                    Else
                        lblCPHHLabel.Text = ""
                    End If
                End If

                If Not ctlCPHH Is Nothing Then
                    If Not IsDBNull(drv("RelatedCPHH")) Then
                        ctlCPHH.CPHH = drv("RelatedCPHH")
                    End If

                    lblError = CType(e.Item.FindControl("lblRepeatedCPHH"), Label)
                    If Not lblError Is Nothing Then
                        If m_bContinueEditingLinkedFarms = True Then
                            lblError.Visible = True
                        Else
                            lblError.Visible = False
                        End If
                    End If
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to bind template columns in the Linked Farms grid", ex)
        End Try
    End Sub

    Private Sub LinkedFarmsPager_RowSave(ByVal sender As Object, ByVal e As BSESystem.DataGridPagerEventArgs) Handles LinkedFarmsPager.RowSave
        ' save template column values to the dataset here
        Dim ctlCPHH As CPHH = CType(e.GridRow.FindControl("ctlLinkedCPHH"), CPHH)

        e.DataTableRow("RelatedCPHH") = Replace(ctlCPHH.CPHH, "/", "")
        With e.DataTableRow
            If .RowState = DataRowState.Added Or .RowState = DataRowState.Modified Then
                'look up the status in the database for the given related CPHH
                Dim sStatus As String = GetRelatedFarmDetails(.Item("RelatedCPHH").ToString())
                If sStatus = "" Then
                    .Item("Status") = "BSE Free"
                Else
                    .Item("Status") = sStatus
                End If
            End If

            If .RowState = DataRowState.Added Then
                'associate the new row with the farm we are working with
                .Item("CPHH") = Session(SessionVars.SV_CPHHNumber)
            End If
        End With

        ' Check to ensure that the CPHH hasn't already been entered 
        ' or is the CPHH of the current farm
        Dim iCount As Integer
        Dim dtData As DataTable = Session.Item(SessionVars.SV_RelatedFarmsTable)

        m_bContinueEditingLinkedFarms = False
        For iCount = 0 To dtData.Rows.Count - 1
            If dtData.Rows(iCount)("RelatedCPHH") = e.DataTableRow("RelatedCPHH") Then
                If dtData.Rows(iCount)("ID") <> e.DataTableRow("ID") Then
                    m_bContinueEditingLinkedFarms = True
                    Exit For
                End If
            End If
            If e.DataTableRow("RelatedCPHH") = Session.Item(SessionVars.SV_CPHHNumber) Then
                m_bContinueEditingLinkedFarms = True
                Exit For
            End If
        Next
    End Sub

    Private Sub LinkedFarmsPager_BeforeDataChanged(ByVal sender As Object, ByRef e As BSESystem.DataGridPagerEventArgs) Handles LinkedFarmsPager.BeforeDataChanged
        ' If the user has entered the same CPHH twice or the 
        ' CPHH of the currently selected Farm then we carry on editing.
        e.bCarryOnEditing = m_bContinueEditingLinkedFarms
    End Sub

#End Region

#Region "Herd Size Grid"

    Private Sub DisableHerdSizeGrid()
        grdHerdSize.DataSource = New DataTable()
        grdHerdSize.DataBind()
        HerdSizePager.DataTableSessionID = SessionVars.SV_HerdSizeTable
        HerdSizePager.PageLinkCount = 0
        HerdSizePager.Refresh()
    End Sub

    Private Sub EnableHerdSizeGrid()
        Dim dtData As DataTable = Session.Item(SessionVars.SV_HerdSizeTable)
        Dim dvData As DataView = dtData.DefaultView
        Session.Item(SessionVars.SV_HerdSizeView) = dvData

        grdHerdSize.DataSource = dtData
        grdHerdSize.DataKeyField = "ID"
        grdHerdSize.CurrentPageIndex = 0
        grdHerdSize.SelectedIndex = -1
        grdHerdSize.EditItemIndex = -1
        grdHerdSize.DataBind()

        HerdSizePager.DataTableSessionID = SessionVars.SV_HerdSizeTable
        HerdSizePager.DataViewSessionID = SessionVars.SV_HerdSizeView
        HerdSizePager.PageLinkCount = 10
        HerdSizePager.Refresh()
    End Sub

    Private Function HerdSizeYearRepeated(ByVal sHerdYear As String) As Boolean
        Dim dtData As DataTable
        Dim iCount As Integer = 0
        Dim iTimesRepeated As Integer = 0

        dtData = Session.Item(SessionVars.SV_HerdSizeTable)

        For iCount = 0 To dtData.Rows.Count - 1
            If Not dtData.Rows(iCount).RowState = DataRowState.Deleted Then
                If dtData.Rows(iCount)("HerdYear").ToString = sHerdYear Then
                    iTimesRepeated += 1
                End If
            End If
        Next

        If iTimesRepeated > 1 Then
            Return True
        Else
            Return False
        End If

    End Function

    Private Sub grdHerdSize_ItemDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.DataGridItemEventArgs) Handles grdHerdSize.ItemDataBound
        ' populate template column values here
        Try
            ' set up the checkbox and drop-down columns
            Dim lblYear As Label = Nothing
            Dim txtYear As TextBox = Nothing
            Dim lblSize As Label = Nothing
            Dim txtSize As TextBox = Nothing
            Dim lblLactation(11) As Label
            Dim txtLactation(11) As TextBox
            Dim i As Int32
            Dim iSum As Int32
            Dim lblHerdSize As Label = Nothing
            Dim lblIncorrect As Label = Nothing

            Dim drv As DataRowView = CType(e.Item.DataItem, DataRowView)
            If Not drv Is Nothing Then
                For i = 1 To 11
                    lblLactation(i) = Nothing
                    txtLactation(i) = Nothing
                Next

                If e.Item.ItemType = ListItemType.EditItem Then
                    ' populate edit mode controls
                    txtYear = CType(e.Item.FindControl("txtGridYear"), TextBox)
                    txtSize = CType(e.Item.FindControl("txtGridSize"), TextBox)
                    For i = 1 To 10
                        txtLactation(i) = CType(e.Item.FindControl("txtLactation" & CStr(i)), TextBox)
                    Next
                    txtLactation(11) = CType(e.Item.FindControl("txtLactation10Plus"), TextBox)
                ElseIf e.Item.ItemType = ListItemType.Item _
                OrElse e.Item.ItemType = ListItemType.AlternatingItem _
                OrElse e.Item.ItemType = ListItemType.SelectedItem Then
                    ' populate display mode controls
                    lblYear = CType(e.Item.FindControl("lblGridYear"), Label)
                    lblSize = CType(e.Item.FindControl("lblGridSize"), Label)
                    For i = 1 To 10
                        lblLactation(i) = CType(e.Item.FindControl("lblLactation" & CStr(i)), Label)
                    Next
                    lblLactation(11) = CType(e.Item.FindControl("lblLactation10Plus"), Label)
                    lblHerdSize = CType(e.Item.FindControl("lblGridSize"), Label)
                End If

                If Not lblYear Is Nothing Then
                    If Not IsDBNull(drv("HerdYear")) Then
                        lblIncorrect = CType(e.Item.FindControl("lblGridYearRepeated"), Label)

                        If Not lblIncorrect Is Nothing And HerdSizeYearRepeated(drv("HerdYear")) Then
                            lblIncorrect.Visible = True
                        Else
                            lblIncorrect.Visible = False
                        End If
                        lblYear.Text = drv("HerdYear")
                        lblIncorrect = Nothing
                    Else
                        lblYear.Text = ""
                    End If
                End If

                If Not lblSize Is Nothing Then
                    If Not IsDBNull(drv("TotalSize")) Then
                        lblSize.Text = drv("TotalSize")
                    Else
                        lblSize.Text = ""
                    End If
                End If

                For i = 1 To 10
                    If Not lblLactation(i) Is Nothing Then
                        If Not IsDBNull(drv("Lactation" & CStr(i) & "Size")) Then
                            lblLactation(i).Text = drv("Lactation" & CStr(i) & "Size")
                        Else
                            lblLactation(i).Text = ""
                        End If
                    End If
                Next

                If Not lblLactation(11) Is Nothing Then
                    If Not IsDBNull(drv("Lactation10PlusSize")) Then
                        lblLactation(11).Text = drv("Lactation10PlusSize")
                    Else
                        lblLactation(11).Text = ""
                    End If
                End If

                If Not txtYear Is Nothing Then
                    If Not IsDBNull(drv("HerdYear")) Then
                        txtYear.Text = drv("HerdYear")
                    Else
                        txtYear.Text = ""
                    End If
                End If

                If Not txtSize Is Nothing Then
                    If Not IsDBNull(drv("TotalSize")) Then
                        txtSize.Text = drv("TotalSize")
                    Else
                        txtSize.Text = ""
                    End If
                End If

                For i = 1 To 10
                    If Not txtLactation(i) Is Nothing Then
                        If Not IsDBNull(drv("Lactation" & CStr(i) & "Size")) Then
                            txtLactation(i).Text = drv("Lactation" & CStr(i) & "Size")
                        Else
                            txtLactation(i).Text = ""
                        End If
                    End If
                Next

                If Not txtLactation(11) Is Nothing Then
                    If Not IsDBNull(drv("Lactation10PlusSize")) Then
                        txtLactation(11).Text = drv("Lactation10PlusSize")
                    Else
                        txtLactation(11).Text = ""
                    End If
                End If

                If Not lblHerdSize Is Nothing Then
                    ' Cycle through all the lactation values (in the non-editable rows) and sum them
                    For i = 1 To 11
                        If Not lblLactation(i) Is Nothing Then
                            If lblLactation(i).Text <> "" Then
                                iSum = iSum + Convert.ToInt32(lblLactation(i).Text)
                            End If
                        End If
                    Next

                    lblIncorrect = CType(e.Item.FindControl("lblGridSizeIncorrect"), Label)
                    If Not lblIncorrect Is Nothing And lblHerdSize.Text <> "" Then

                        If Convert.ToInt32(lblHerdSize.Text) <> iSum And iSum <> 0 Then
                            lblIncorrect.ToolTip = "The values in the lactation fields sum to " & CStr(iSum) & " not the Herd Size."
                            lblIncorrect.Visible = True
                        Else
                            lblIncorrect.Visible = False
                        End If
                        lblIncorrect = Nothing
                    End If
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to bind template columns in the Other Owners grid", ex)
        End Try
    End Sub

    Private Sub HerdSizePager_RowSave(ByVal sender As Object, ByVal e As BSESystem.DataGridPagerEventArgs) Handles HerdSizePager.RowSave

        Dim txtLactation(11) As TextBox
        Dim i As Int32
        Dim bEnteredValue As Boolean = False
        Dim bBlankValue As Boolean = False

        'if the row is new, add a reference to the farm it belongs to
        If e.DataTableRow.RowState = DataRowState.Added Then
            e.DataTableRow("CPHH") = Session(SessionVars.SV_CPHHNumber)
        End If

        ' Save the HerdYear and Total Size values
        Dim txtYear As TextBox = CType(e.GridRow.FindControl("txtGridYear"), TextBox)
        e.DataTableRow("HerdYear") = txtYear.Text
        Dim txtSize As TextBox = CType(e.GridRow.FindControl("txtGridSize"), TextBox)
        e.DataTableRow("TotalSize") = txtSize.Text

        ' Cycle through the lactation text boxes and save their values
        ' Keep a track of whether a blank value has been entered and whether 
        ' any values have been entered.  This is because as soon as the user
        ' has entered one lactation value they have to enter them all.
        For i = 1 To 10
            txtLactation(i) = CType(e.GridRow.FindControl("txtLactation" & CStr(i)), TextBox)
            If txtLactation(i).Text = "" Then
                e.DataTableRow("Lactation" & CStr(i) & "Size") = DBNull.Value
                bBlankValue = True
            Else
                e.DataTableRow("Lactation" & CStr(i) & "Size") = txtLactation(i).Text
                bEnteredValue = True
            End If
        Next
        ' As above only for the 10plus column
        txtLactation(11) = CType(e.GridRow.FindControl("txtLactation10Plus"), TextBox)
        If txtLactation(i).Text = "" Then
            e.DataTableRow("Lactation10PlusSize") = DBNull.Value
            bBlankValue = True
        Else
            e.DataTableRow("Lactation10PlusSize") = txtLactation(11).Text
            bEnteredValue = True
        End If

        ' If the user has entered a value in any of the lactation fields then they have to complete them all.
        ' Auto-populate the blank fields with 0
        If bEnteredValue And bBlankValue Then
            For i = 1 To 10
                If txtLactation(i).Text = "" Then
                    e.DataTableRow("Lactation" & CStr(i) & "Size") = 0
                End If
            Next
            If txtLactation(11).Text = "" Then
                e.DataTableRow("Lactation10PlusSize") = 0
            End If
        End If

    End Sub

#End Region

#Region "Map Reference Handling"

    Private Sub MapReference1_MapReferenceChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles MapReference1.MapReferenceChanged
        ' Validate Map Reference to ensure that it is within the Parish.
        Dim sCounty As String
        Dim sParish As String
        Dim sCPHH As String
        Dim sMapReference As String = MapReference1.MapReference
        Dim objCase As New BSELib.clsCase()

        sCPHH = CPHH1.CPHH
        sCounty = Left$(sCPHH, 2)
        sParish = Mid$(sCPHH, 3, 3)
        If sMapReference <> "" Then
            If sCounty <> "" And sParish <> "" Then
                If objCase.ValidateMapReference(sCounty, sParish, sMapReference) Then
                    lblInvalidMapReference.Visible = False
                Else
                    lblInvalidMapReference.Visible = True
                End If
            End If
        End If
    End Sub

    Private Sub btnEstimate_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnEstimate.Click
        ' Estimate Map Reference based on the CPHH
        Dim sCounty As String
        Dim sParish As String
        Dim sCPHH As String
        Dim objCase As New BSELib.clsCase()

        sCPHH = CPHH1.CPHH
        sCounty = Left$(sCPHH, 2)
        sParish = Mid$(sCPHH, 3, 3)
        If sCounty <> "" And sParish <> "" Then
            MapReference1.MapReference = objCase.EstimateMapReference(sCounty, sParish)
        End If
    End Sub

#End Region

#Region "Presentation Functions"

    Public Function GetDateText() As String
        Return "Year must be between 1975 and " & Year(Now())
    End Function

#End Region

    Private Sub ddlAuthorityCounty_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ddlAuthorityCounty.SelectedIndexChanged

        Dim dsData As DataSet

        Try
            dsData = Session.Item(SessionVars.SV_FarmDetails)

            If Not dsData Is Nothing Then
                If dsData.Tables(BSELib.clsFarm.FARM_TABLE).Rows.Count <> 0 Then

                    With dsData.Tables(BSELib.clsFarm.FARM_TABLE).Rows(0)
                        .Item("AuthorityCountyID") = FormatEmptyString(GetSelectedItemFromDropDownList(ddlAuthorityCounty))
                        .Item("AuthorityID") = FormatEmptyString(LoadAuthorityLookup(.Item("AuthorityCountyID").ToString()))
                        .Item("ADNSRegionID") = FormatEmptyString(LoadADNSRegionLookup(.Item("AuthorityID").ToString()))
                    End With
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to change authority county value.", ex)
        End Try
    End Sub

    Private Sub ddlLocalAuthority_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ddlLocalAuthority.SelectedIndexChanged

        Dim dsData As DataSet

        Try
            dsData = Session.Item(SessionVars.SV_FarmDetails)

            If Not dsData Is Nothing Then
                If dsData.Tables(BSELib.clsFarm.FARM_TABLE).Rows.Count <> 0 Then

                    With dsData.Tables(BSELib.clsFarm.FARM_TABLE).Rows(0)
                        .Item("AuthorityID") = FormatEmptyString(GetSelectedItemFromDropDownList(ddlLocalAuthority))
                        .Item("ADNSRegionID") = FormatEmptyString(LoadADNSRegionLookup(.Item("AuthorityID").ToString()))
                    End With
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to change authority county value.", ex)
        End Try
    End Sub
End Class
