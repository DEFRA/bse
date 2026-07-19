Partial Class CaseEntryRelations
    Inherits System.Web.UI.Page
    Protected WithEvents ctlDamRBSE As RBSE
    Protected WithEvents ctlDamBirthDate As PartialDate
    Protected WithEvents ctlSireRBSE As RBSE
    Protected WithEvents ctlSireBirthDate As PartialDate
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents RelationsPager As DataGridPager
    Protected WithEvents BatchNumberDisplay1 As BatchNumberDisplay
    Protected WithEvents ctlRelationEartag As ThreePartEartag
    Protected WithEvents ctlRelationRBSE As RBSE
    Protected WithEvents ctlRelationLeftDate As CalendarDate
    Protected WithEvents ctlRelationBirthDate As CalendarDate
    Protected WithEvents ctlRemoveDamDiv As System.Web.UI.HtmlControls.HtmlGenericControl
    Protected WithEvents ctlRemoveSireDiv As System.Web.UI.HtmlControls.HtmlGenericControl
    Private mbCanEditRelations As Boolean
    Protected WithEvents ctlExitConfirmation As ExitConfirmation


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
        RelationsPager.SetGrid(grdRelations)
        ctlRelationRBSE.SetAutopostback(True)
        If Not IsPostBack Then
            DisableRelationsGrid()
            LoadLookupLists()
            LoadCaseDetails()
        End If

        litViewDocs.Text = GetSpolSiteButtonCode(Session(SessionVars.SV_RBSENumber))
        litViewDocs2.Text = litViewDocs.Text

        btnRemoveSire.Attributes.Add("onClick", "javascript:return confirm('This will disassociate the sire from the case.  Do you wish to continue?');")
        btnRemoveDam.Attributes.Add("onClick", "javascript:return confirm('This will disassociate the dam from the case.  Do you wish to continue?');")

        EnableControls()
    End Sub

    Private Sub SetEnterKeys()
        SetTextboxControlOnEnter(txtCaseHerdbook, txtDamEartag.ClientID)
        SetTextboxControlOnEnter(txtDamEartag, ctlDamRBSE.FirstClientID)
        ctlDamRBSE.SetControlOnEnter(txtDamName.ClientID)
        SetTextboxControlOnEnter(txtDamName, txtDamHerdbook.ClientID)
        SetTextboxControlOnEnter(txtDamHerdbook, btnDamLookUp.ClientID)
        ctlDamBirthDate.SetControlOnEnter(txtSireEartag.ClientID)
        SetTextboxControlOnEnter(txtSireEartag, ctlSireRBSE.FirstClientID)
        ctlSireRBSE.SetControlOnEnter(txtSireName.ClientID)
        SetTextboxControlOnEnter(txtSireName, txtSireHerdbook.ClientID)
        SetTextboxControlOnEnter(txtSireHerdbook, btnSireLookUp.ClientID)
        ctlSireBirthDate.SetControlOnEnter(ddlRelationType.ClientID)
        ctlRelationEartag.SetControlOnEnter(ctlRelationBirthDate.FirstClientID)
        ctlRelationBirthDate.SetControlOnEnter(ctlRelationLeftDate.FirstClientID)
        ctlRelationLeftDate.SetControlOnEnter(txtRelationSire.ClientID)
        SetTextboxControlOnEnter(txtRelationSire, btnAddAsNew.ClientID)
    End Sub

#Region "Grid Handling"

    Private Sub DisableRelationsGrid()
        With grdRelations
            .DataSource = New DataTable()
            .DataBind()
            .Enabled = True
        End With
        With RelationsPager
            .AllowAddNew = False
            .AllowEdit = False
            .AllowDelete = False
            .PageLinkCount = 0
            .Refresh()
        End With
    End Sub

    Private Sub EnableRelationsGrid()
        Dim dtData As DataTable = Session.Item(SessionVars.SV_RelationsTable)
        Dim dvData As DataView = dtData.DefaultView
        Session.Item(SessionVars.SV_RelationsView) = dvData

        With grdRelations
            .DataSource = dtData
            .DataKeyField = "ID"
            .CurrentPageIndex = 0
            .SelectedIndex = -1
            .EditItemIndex = -1
            .DataBind()
        End With

        With RelationsPager
            .DataTableSessionID = SessionVars.SV_RelationsTable
            .DataViewSessionID = SessionVars.SV_RelationsView
            .PageLinkCount = 10
            .Refresh()
        End With
    End Sub

    Private Sub RelationsPager_RowSave(ByVal sender As Object, ByVal e As BSESystem.DataGridPagerEventArgs) Handles RelationsPager.RowSave

        Dim objRelationTypeList As DropDownList = CType(e.GridRow.FindControl("ddlRelationType"), DropDownList)
        e.DataTableRow("RelationType") = objRelationTypeList.SelectedItem.Value
        e.DataTableRow("RelationTypeDesc") = objRelationTypeList.SelectedItem.Text

        Dim objRelationFateList As DropDownList = CType(e.GridRow.FindControl("ddlRelationFate"), DropDownList)
        e.DataTableRow("RelationFate") = objRelationFateList.SelectedItem.Value
        e.DataTableRow("RelationFateDesc") = objRelationFateList.SelectedItem.Text

        Dim objSexList As DropDownList = CType(e.GridRow.FindControl("ddlSex"), DropDownList)
        e.DataTableRow("Sex") = objSexList.SelectedItem.Value
        e.DataTableRow("SexDesc") = objSexList.SelectedItem.Text

    End Sub

#End Region

#Region "Lookup list population"

    Private Sub LoadLookupLists()

        Dim objLookup As New BSELib.LookupData()

        Try
            'dam status
            Dim objAnimalStatusTable As DataTable = objLookup.GetLookupData(LOOKUP_ANIMAL_STATUS)
            If Not (objAnimalStatusTable Is Nothing) Then
                With ddlDamStatus
                    .DataSource = objAnimalStatusTable
                    .DataValueField = "Code"
                    .DataTextField = "Description"
                    .DataBind()
                End With
                Common.AddItemToDropDownList(ddlDamStatus)
            End If

            'relation type dropdown
            Dim objRelationTypeTable As DataTable = objLookup.GetLookupData(LOOKUP_RELATION_TYPE)
            If Not (objRelationTypeTable Is Nothing) Then
                With ddlRelationType
                    .DataSource = objRelationTypeTable
                    .DataValueField = "Code"
                    .DataTextField = "Description"
                    .DataBind()
                End With
                Common.AddItemToDropDownList(ddlRelationType)
            End If

            'relation fate dropdown
            Dim objRelationFateTable As DataTable = objLookup.GetLookupData(LOOKUP_RELATION_FATE)
            If Not (objRelationFateTable Is Nothing) Then
                With ddlRelationFate
                    .DataSource = objRelationFateTable
                    .DataValueField = "Code"
                    .DataTextField = "Description"
                    .DataBind()
                End With
                Common.AddItemToDropDownList(ddlRelationFate)
            End If

            'sex dropdown
            Dim objSexTable As DataTable = objLookup.GetLookupData(LOOKUP_SEX)
            If Not (objSexTable Is Nothing) Then
                With ddlRelationSex
                    .DataSource = objSexTable
                    .DataValueField = "Code"
                    .DataTextField = "Description"
                    .DataBind()
                End With
                Common.AddItemToDropDownList(ddlRelationSex)
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve 'Relations Tab' drop down lists.", ex)
        End Try

    End Sub

#End Region

#Region "Event Handlers"

    Private Sub btnRemoveDam_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnRemoveDam.Click
        RemoveDam()
    End Sub

    Private Sub btnRemoveSire_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnRemoveSire.Click
        RemoveSire()
    End Sub

    Private Sub btnViewRelations_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnViewRelations.Click
        OpenRelationsPopup(ctlRelationRBSE.UnformattedRBSE)
    End Sub

    Private Sub btnSave_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSave.Click, btnSave2.Click
        If UpdateSessionWithCaseDetails() Then
            Response.Redirect("CaseEntrySave.aspx")
        End If
    End Sub

    Private Sub btnCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancel.Click, btnCancel2.Click
        CancelCaseEdit()
    End Sub

    Private Sub VLAHeader1_HomeClick(ByVal sender As Object, ByVal e As BSESystem.HomeLinkEventArgs) Handles VLAHeader1.HomeClick
        CancelCaseEdit()
        e.bNavigateHome = False
    End Sub

    Private Sub hlbFarm_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlbFarm.Click
        If UpdateSessionWithCaseDetails() Then
            Response.Redirect("CaseEntryFarm.aspx")
        End If
    End Sub

    Private Sub hlbCaseDEFRA_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlbCaseDEFRA.Click
        If UpdateSessionWithCaseDetails() Then
            Response.Redirect("CaseEntryDEFRA.aspx")
        End If
    End Sub

    Private Sub hlbBAB_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlbBAB.Click
        If UpdateSessionWithCaseDetails() Then
            Response.Redirect("CaseEntryBAB.aspx")
        End If
    End Sub

    Private Sub hlbCaseVLA_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlbCaseVLA.Click
        If UpdateSessionWithCaseDetails() Then
            Response.Redirect("CaseEntryVLA.aspx")
        End If
    End Sub

    Private Sub hlbClinical_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlbClinical.Click
        If UpdateSessionWithCaseDetails() Then
            Response.Redirect("CaseEntryClinical.aspx")
        End If
    End Sub

    Private Sub hlbFeeds_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlbFeeds.Click
        If UpdateSessionWithCaseDetails() Then
            Response.Redirect("CaseEntryFeeds.aspx")
        End If
    End Sub

    Private Sub btnDamLookUp_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnDamLookUp.Click

        ctlDamRBSE.SetValidMark(True)

        'check the RBSE provided is not the same as the case
        If ctlDamRBSE.RBSE = Session(SessionVars.SV_RBSENumber) Then
            ctlDamRBSE.SetValidMark(False, "This RBSE is the same as the case RBSE")
            Exit Sub
        End If

        'check the RBSE provided isn't already a relation
        If RBSEIsRelation(ctlDamRBSE.UnformattedRBSE) Then
            ctlDamRBSE.SetValidMark(False, "This RBSE is already a twin, sister or offspring")
            Exit Sub
        End If

        Dim objRelations As New BSELib.clsRelations()
        Dim dtDamMatches As DataTable

        If objRelations.GetDamSireDetailsMatches(txtDamEartag.Text, txtDamName.Text, ctlDamRBSE.UnformattedRBSE, txtDamHerdbook.Text, True, dtDamMatches) Then
            If dtDamMatches.Rows.Count = 1 Then
                Dim dsData As DataSet = Session.Item(SessionVars.SV_CaseDetails)
                Dim drTargetRow As DataRow
                drTargetRow = dsData.Tables(BSELib.clsCase.DAM_DETAILS_TABLE).Rows(0)
                With drTargetRow
                    If dtDamMatches.Rows(0).Item("ID") Is DBNull.Value Then
                        .Item("ID") = 0
                    Else
                        .Item("ID") = dtDamMatches.Rows(0).Item("ID")
                    End If
                    .Item("Eartag") = dtDamMatches.Rows(0).Item("Eartag")
                    .Item("Name") = dtDamMatches.Rows(0).Item("Name")
                    .Item("RBSE") = dtDamMatches.Rows(0).Item("RBSE")
                    .Item("Herdbook") = dtDamMatches.Rows(0).Item("Herdbook")
                    .Item("BirthDay") = dtDamMatches.Rows(0).Item("BirthDay")
                    .Item("BirthMonth") = dtDamMatches.Rows(0).Item("BirthMonth")
                    .Item("BirthYear") = dtDamMatches.Rows(0).Item("BirthYear")
                    .Item("Fate") = dtDamMatches.Rows(0).Item("Fate")
                    .Item("FinalResult") = dtDamMatches.Rows(0).Item("FinalResult")
                    .Item("ChildCount") = dtDamMatches.Rows(0).Item("ChildCount")
                    .Item("RowStamp") = dtDamMatches.Rows(0).Item("RowStamp")
                End With
                LoadDamDetails()
                EnableControls()
            ElseIf dtDamMatches.Rows.Count > 1 OrElse ctlDamRBSE.RBSE = "" Then
                Session(SessionVars.SV_SireDamMatchesTable) = dtDamMatches
                Session(SessionVars.SV_SireDamMatchesView) = dtDamMatches.DefaultView
                Response.Redirect("PickSireDam.aspx?eartag=" & txtDamEartag.Text & "&name=" & txtDamName.Text & "&herdbook=" & txtDamHerdbook.Text & "&sex=F")
            Else
                ctlDamRBSE.SetValidMark(False, "This RBSE does not exist or is not a female animal")
            End If
        End If
    End Sub

    Private Sub btnSireLookUp_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSireLookUp.Click
        ctlSireRBSE.SetValidMark(True)

        'check the RBSE isn't the same as the case
        If ctlSireRBSE.RBSE = Session(SessionVars.SV_RBSENumber) Then
            ctlSireRBSE.SetValidMark(False, "This RBSE is the same as the case RBSE")
            Exit Sub
        End If

        'check the RBSE provided isn't already a relation
        If RBSEIsRelation(ctlSireRBSE.UnformattedRBSE) Then
            ctlSireRBSE.SetValidMark(False, "This RBSE is already a twin, sister or offspring")
            Exit Sub
        End If

        Dim objRelations As New BSELib.clsRelations()
        Dim dtSireMatches As DataTable

        If objRelations.GetDamSireDetailsMatches(txtSireEartag.Text, txtSireName.Text, ctlSireRBSE.UnformattedRBSE, txtSireHerdbook.Text, False, dtSireMatches) Then
            If dtSireMatches.Rows.Count = 1 Then
                Dim dsData As DataSet = Session.Item(SessionVars.SV_CaseDetails)
                Dim drTargetRow As DataRow
                drTargetRow = dsData.Tables(BSELib.clsCase.SIRE_DETAILS_TABLE).Rows(0)
                With drTargetRow
                    If dtSireMatches.Rows(0).Item("ID") Is DBNull.Value Then
                        .Item("ID") = 0
                    Else
                        .Item("ID") = dtSireMatches.Rows(0).Item("ID")
                    End If
                    .Item("Eartag") = dtSireMatches.Rows(0).Item("Eartag")
                    .Item("Name") = dtSireMatches.Rows(0).Item("Name")
                    .Item("RBSE") = dtSireMatches.Rows(0).Item("RBSE")
                    .Item("Herdbook") = dtSireMatches.Rows(0).Item("Herdbook")
                    .Item("BirthDay") = dtSireMatches.Rows(0).Item("BirthDay")
                    .Item("BirthMonth") = dtSireMatches.Rows(0).Item("BirthMonth")
                    .Item("BirthYear") = dtSireMatches.Rows(0).Item("BirthYear")
                    .Item("Fate") = dtSireMatches.Rows(0).Item("Fate")
                    .Item("ChildCount") = dtSireMatches.Rows(0).Item("ChildCount")
                    .Item("RowStamp") = dtSireMatches.Rows(0).Item("RowStamp")
                End With
                LoadSireDetails()
                EnableControls()
            ElseIf dtSireMatches.Rows.Count > 1 OrElse ctlSireRBSE.RBSE = "" Then
                Session(SessionVars.SV_SireDamMatchesTable) = dtSireMatches
                Session(SessionVars.SV_SireDamMatchesView) = dtSireMatches.DefaultView
                Response.Redirect("PickSireDam.aspx?eartag=" & txtSireEartag.Text & "&name=" & txtSireName.Text & "&herdbook=" & txtSireHerdbook.Text & "&sex=M")
            Else
                ctlSireRBSE.SetValidMark(False, "This RBSE does not exist or is not a male animal")
            End If
        End If
    End Sub

    Private Sub btnDamRelations_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnDamRelations.Click
        OpenRelationsPopup(ctlDamRBSE.UnformattedRBSE)
    End Sub

    Private Sub btnSireRelations_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSireRelations.Click
        OpenRelationsPopup(ctlSireRBSE.UnformattedRBSE)
    End Sub

    Private Sub OpenRelationsPopup(ByVal sRBSE As String)

        Dim sJScript As New System.Text.StringBuilder()

        sJScript.Append("<script language=""JavaScript"">")
        sJScript.Append("<!-- " + vbCrLf)
        sJScript.Append("window.showModalDialog(""RelationsPopup.aspx?rbse=")
        sJScript.Append(sRBSE)
        sJScript.Append(""", """",""dialogTop:0px; dialogLeft:0px; dialogWidth:630px; dialogHeight:500px; resizable:yes; status:no;"");" + vbCrLf)
        sJScript.Append("// -->")
        sJScript.Append("</script>")

        Page.RegisterClientScriptBlock("PopUp", sJScript.ToString())

    End Sub

    Private Sub grdRelations_SortCommand(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.DataGridSortCommandEventArgs) Handles grdRelations.SortCommand

        'After sorting... disable the update, delete buttons and clear the relation input fields.
        btnUpdateSelected.Enabled = False
        btnDeleteSelected.Enabled = False
        'If grdRelations.SelectedIndex = -1 Then
        'ClearRelationInputFields(True)
        'End If

    End Sub

    Private Sub ctlRelationRBSE_RBSEChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ctlRelationRBSE.RBSEChanged

        'set state of controls
        ctlRelationRBSE.SetValidMark(True)

        EnableRelationsControlsForRBSE()

        If txtRelationID.Text <> "" Then
            Dim iSelectedRow As Integer = RelationsPager.GetRowIndexFromID(txtRelationID.Text)
            If iSelectedRow <> -1 Then
                grdRelations.SelectedIndex = iSelectedRow
            End If
        End If

        'check if the user cleared the RBSE box
        If ctlRelationRBSE.RBSE = "" Then
            ClearRelationInputFields(False)
        End If
        'check if the user entered an invalid RBSE
        If Not ctlRelationRBSE.Validate() Then
            ClearRelationInputFields(False)
            Exit Sub
        End If

        'check if the user entered the same RBSE as the case RBSE
        If ctlRelationRBSE.RBSE = Session(SessionVars.SV_RBSENumber).ToString() Then
            ClearRelationInputFields(False)
            ctlRelationRBSE.SetValidMark(False, "This RBSE is the same as the case RBSE")
            Exit Sub
        End If

        'check if the user entered the same RBSE as the dam's RBSE
        If lblDamID.Text <> "" AndAlso ctlDamRBSE.RBSE = ctlRelationRBSE.RBSE Then
            ClearRelationInputFields(False)
            ctlRelationRBSE.SetValidMark(False, "This RBSE is the same as the dam's RBSE")
            Exit Sub
        End If

        'check if the user entered the same RBSE as the sire's RBSE
        If lblSireID.Text <> "" AndAlso ctlSireRBSE.RBSE = ctlRelationRBSE.RBSE Then
            ClearRelationInputFields(False)
            ctlRelationRBSE.SetValidMark(False, "This RBSE is the same as the sire's RBSE")
            Exit Sub
        End If

        'check if the user entered an RBSE that is already a relation
        If RBSEIsRelation(ctlRelationRBSE.UnformattedRBSE, txtRelationID.Text) Then
            ClearRelationInputFields(False)
            ctlRelationRBSE.SetValidMark(False, "This RBSE is already a relation")
            Exit Sub
        End If

        'otherwise, look up the RBSE in the database
        Dim objRelations As New BSELib.clsRelations()
        Dim bSuccess As Boolean
        Dim sSex As String
        Dim sSexDesc As String
        Dim iBirthDay As Integer
        Dim iBirthMonth As Integer
        Dim iBirthYear As Integer
        Dim sFate As String
        Dim sFateDesc As String
        Dim sLeftDate As String
        Dim sEartagCountry As String
        Dim sEartagHerdmark As String
        Dim sEartag As String
        Dim sSire As String

        bSuccess = objRelations.GetRelationDetailsOfRelatedCase(ctlRelationRBSE.UnformattedRBSE, sSex, sSexDesc, iBirthDay, iBirthMonth, iBirthYear, sFate, sFateDesc, sLeftDate, sEartagCountry, sEartagHerdmark, sEartag, sSire)
        If bSuccess Then

            SelectItemInDropDownList(ddlRelationSex, sSex)
            lblRelationFateValue.Text = sFateDesc
            SelectItemInDropDownList(ddlRelationFate, sFate)
            ctlRelationEartag.EartagCountry = sEartagCountry
            ctlRelationEartag.EartagHerdmark = sEartagHerdmark
            ctlRelationEartag.EartagAnimal = sEartag
            If iBirthDay = 0 Or iBirthMonth = 0 Or iBirthYear = 0 Then
                ctlRelationBirthDate.DateField = ""
            Else
                ctlRelationBirthDate.DateField = iBirthDay & "/" & MonthName(iBirthMonth) & "/" & iBirthYear
            End If
            ctlRelationLeftDate.DateField = sLeftDate
            txtRelationSire.Text = sSire
        Else
            ClearRelationInputFields(False)
            ctlRelationRBSE.SetValidMark(False, "RBSE number not found")
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
        SetDamDetailsControls(False)
        SetSireDetailsControls(False)
        SetRelationsControls(False)
        btnSave.Enabled = False
    End Sub

    Private Sub DEFRADataEntryEnable()
        BatchNumberDisplay1.Visible = False
        SetDamDetailsControls(True)
        SetSireDetailsControls(False)
        SetRelationsControls(False)
        btnSave.Enabled = True
    End Sub

    Private Sub DEFRAMaintenanceEnable()
        BatchNumberDisplay1.Visible = False
        SetDamDetailsControls(True)
        SetSireDetailsControls(False)
        SetRelationsControls(False)
        btnSave.Enabled = True
    End Sub

    Private Sub VLADataEntryEnable()
        BatchNumberDisplay1.Visible = True
        If IsVLAAllowedAdditionalCaseEdit(Session) Then
            SetDamDetailsControls(True)
            SetSireDetailsControls(True)
            SetRelationsControls(True)
        Else
            SetDamDetailsControls(False)
            SetSireDetailsControls(False)
            SetRelationsControls(False)
        End If
        btnSave.Enabled = True
    End Sub

    Private Sub VLAMaintenanceEnable()
        BatchNumberDisplay1.Visible = True
        If IsVLAAllowedAdditionalCaseEdit(Session) Then
            SetDamDetailsControls(True)
            SetSireDetailsControls(True)
            SetRelationsControls(True)
        Else
            SetDamDetailsControls(False)
            SetSireDetailsControls(False)
            SetRelationsControls(False)
        End If
        btnSave.Enabled = True
    End Sub

    Private Sub SetDamDetailsControls(ByVal vbEnable As Boolean)

        Dim sRBSE As String = ctlDamRBSE.RBSE
        Dim sDamID As String = lblDamID.Text

        txtDamEartag.Enabled = vbEnable AndAlso (sRBSE = "" OrElse sDamID = "")
        'txtDamEartag.ValidationEnabled = (sDamID <> "")
        'If sDamID <> "" Then
        '    txtDamEartag.Validate()
        'End If
        txtDamName.Enabled = vbEnable
        txtDamHerdbook.Enabled = vbEnable AndAlso (sRBSE = "" OrElse sDamID = "")

        ctlDamRBSE.SetEnabled(vbEnable AndAlso sDamID = "")
        btnDamLookUp.Enabled = vbEnable AndAlso sDamID = ""
        ddlDamStatus.Enabled = vbEnable
        ctlDamBirthDate.Enabled = vbEnable AndAlso sDamID <> "" AndAlso sRBSE = ""
        EnableDisableRemoveDam(vbEnable AndAlso sDamID <> "")
        btnDamRelations.Enabled = vbEnable AndAlso sRBSE <> "" AndAlso sDamID <> ""

    End Sub

    Private Sub SetSireDetailsControls(ByVal vbEnable As Boolean)

        Dim sRBSE As String = ctlSireRBSE.RBSE
        Dim sSireID As String = lblSireID.Text

        txtSireEartag.Enabled = vbEnable AndAlso (sRBSE = "" OrElse sSireID = "")
        'txtSireEartag.ValidationEnabled = (sSireID <> "")
        'If sSireID <> "" Then
        '    txtSireEartag.Validate()
        'End If
        txtSireName.Enabled = vbEnable
        txtSireHerdbook.Enabled = vbEnable AndAlso (sRBSE = "" OrElse sSireID = "")

        ctlSireRBSE.SetEnabled(vbEnable AndAlso sSireID = "")
        btnSireLookUp.Enabled = vbEnable AndAlso sSireID = ""
        ctlSireBirthDate.Enabled = vbEnable AndAlso sSireID <> "" AndAlso sRBSE = ""
        EnableDisableRemoveSire(vbEnable AndAlso sSireID <> "")
        btnSireRelations.Enabled = vbEnable AndAlso sRBSE <> "" AndAlso sSireID <> ""

    End Sub

    Private Sub EnableDisableRemoveDam(ByVal bEnable As Boolean)
        btnRemoveDam.Enabled = bEnable
    End Sub

    Private Sub EnableDisableRemoveSire(ByVal bEnable As Boolean)
        btnRemoveSire.Enabled = bEnable
    End Sub

    Private Sub SetRelationsControls(ByVal vbEnable As Boolean)

        Dim sRBSE As String = ctlRelationRBSE.RBSE

        ddlRelationType.Enabled = vbEnable
        ddlRelationSex.Enabled = vbEnable AndAlso sRBSE = ""
        ddlRelationFate.Enabled = vbEnable AndAlso sRBSE = ""
        ctlRelationEartag.Enabled = vbEnable AndAlso sRBSE = ""
        ctlRelationRBSE.SetEnabled(vbEnable)
        ctlRelationBirthDate.Enabled = vbEnable AndAlso sRBSE = ""
        ctlRelationLeftDate.Enabled = vbEnable AndAlso sRBSE = ""
        txtRelationSire.Enabled = vbEnable AndAlso sRBSE = ""
        btnViewRelations.Enabled = vbEnable AndAlso sRBSE <> ""
        btnAddAsNew.Enabled = vbEnable AndAlso Not (grdRelations.SelectedIndex <> -1)
        btnUpdateSelected.Enabled = vbEnable AndAlso (grdRelations.SelectedIndex <> -1)
        btnDeleteSelected.Enabled = vbEnable AndAlso (grdRelations.SelectedIndex <> -1)

        mbCanEditRelations = vbEnable

    End Sub

#End Region

#Region "Handle Data"

    Private Function LoadCaseDetails() As Boolean

        Dim dsData As DataSet
        Try

            dsData = Session.Item(SessionVars.SV_CaseDetails)

            'case herdbook
            If dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows.Count > 0 Then
                txtCaseHerdbook.Text = dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows(0).Item("Herdbook").ToString()
            End If

            'dam and sire details
            LoadDamDetails()
            LoadSireDetails()

            'offspring etc. data
            If dsData.Tables(BSELib.clsCase.RELATION_TABLE).Rows.Count <> 0 Then
                EnableRelationsGrid()
            End If
            Return True

        Catch ex As Exception
            clsAppError.DisplayError("Failed to 'Load Case Details'.", ex)
            Return False
        End Try
    End Function

    Private Sub LoadDamDetails()

        Dim dsData As DataSet = Session.Item(SessionVars.SV_CaseDetails)

        If dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows.Count > 0 Then
            SelectItemInDropDownList(ddlDamStatus, dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows(0).Item("DamStatus").ToString())
        End If

        If dsData.Tables(BSELib.clsCase.DAM_DETAILS_TABLE).Rows.Count > 0 Then
            With dsData.Tables(BSELib.clsCase.DAM_DETAILS_TABLE).Rows(0)
                If .Item("ID").ToString() <> "0" Then
                    lblDamID.Text = .Item("ID").ToString()
                Else
                    lblDamID.Text = "(New)"
                End If
                txtDamEartag.Text = .Item("Eartag").ToString()
                txtDamName.Text = .Item("Name").ToString()
                ctlDamRBSE.RBSE = .Item("RBSE").ToString()
                txtDamHerdbook.Text = .Item("Herdbook").ToString()
                ctlDamBirthDate.DayValue = .Item("BirthDay").ToString()
                ctlDamBirthDate.MonthValue = .Item("BirthMonth").ToString()
                ctlDamBirthDate.YearValue = .Item("BirthYear").ToString()
                lblDamFateValue.Text = .Item("Fate").ToString()
                lblDamFinalResultValue.Text = .Item("FinalResult").ToString()
                lblDamOffspringValue.Text = .Item("ChildCount").ToString()
            End With
        Else
            lblDamID.Text = ""
            txtDamEartag.Text = ""
            txtDamName.Text = ""
            ctlDamRBSE.RBSE = ""
            txtDamHerdbook.Text = ""
            ctlDamBirthDate.DayValue = ""
            ctlDamBirthDate.MonthValue = ""
            ctlDamBirthDate.YearValue = ""
            lblDamFateValue.Text = ""
            lblDamFinalResultValue.Text = ""
            lblDamOffspringValue.Text = ""
        End If
    End Sub

    Private Sub LoadSireDetails()

        Dim dsData As DataSet = Session.Item(SessionVars.SV_CaseDetails)
        If dsData.Tables(BSELib.clsCase.SIRE_DETAILS_TABLE).Rows.Count <> 0 Then
            With dsData.Tables(BSELib.clsCase.SIRE_DETAILS_TABLE).Rows(0)
                If .Item("ID").ToString() <> "0" Then
                    lblSireID.Text = .Item("ID").ToString()
                Else
                    lblSireID.Text = "(New)"
                End If
                txtSireEartag.Text = .Item("Eartag").ToString()
                txtSireName.Text = .Item("Name").ToString()
                ctlSireRBSE.RBSE = .Item("RBSE").ToString()
                txtSireHerdbook.Text = .Item("Herdbook").ToString()
                ctlSireBirthDate.DayValue = .Item("BirthDay").ToString()
                ctlSireBirthDate.MonthValue = .Item("BirthMonth").ToString()
                ctlSireBirthDate.YearValue = .Item("BirthYear").ToString()
                lblSireFateValue.Text = .Item("Fate").ToString()
                lblSireOffspringValue.Text = .Item("ChildCount").ToString()
            End With
        Else
            lblSireID.Text = ""
            txtSireEartag.Text = ""
            txtSireName.Text = ""
            ctlSireRBSE.RBSE = ""
            txtSireHerdbook.Text = ""
            ctlSireBirthDate.DayValue = ""
            ctlSireBirthDate.MonthValue = ""
            ctlSireBirthDate.YearValue = ""
            lblSireFateValue.Text = ""
            lblSireOffspringValue.Text = ""
        End If
    End Sub
    Private Function UpdateSessionWithCaseDetails() As Boolean
        ' Perform any writing to the datasets in the session object.

        'If (txtDamEartag.Text <> "" And Not txtDamEartag.Validate()) Or (txtSireEartag.Text <> "" And Not txtSireEartag.Validate()) Or (Not ctlDamBirthDate.Validate()) Or (Not ctlSireBirthDate.Validate()) Then
        '    Return False
        'End If

        If (Not ctlDamBirthDate.Validate()) Or (Not ctlSireBirthDate.Validate()) Then
            Return False
        End If

        Dim dsData As DataSet = Session.Item(SessionVars.SV_CaseDetails)

        Try
            If Not dsData Is Nothing Then

                If dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows.Count <> 0 Then
                    dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows(0)("Herdbook") = FormatEmptyString(txtCaseHerdbook.Text)
                    dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows(0)("DamStatus") = FormatEmptyString(GetSelectedItemFromDropDownList(ddlDamStatus))
                End If

                If dsData.Tables(BSELib.clsCase.DAM_DETAILS_TABLE).Rows.Count > 0 Then
                    With dsData.Tables(BSELib.clsCase.DAM_DETAILS_TABLE).Rows(0)
                        .Item("Eartag") = FormatEmptyString(txtDamEartag.Text)
                        .Item("Name") = FormatEmptyString(txtDamName.Text)
                        .Item("RBSE") = FormatEmptyString(ctlDamRBSE.UnformattedRBSE)
                        .Item("Herdbook") = FormatEmptyString(txtDamHerdbook.Text)
                        .Item("BirthDay") = FormatEmptyString(ctlDamBirthDate.DayValue)
                        .Item("BirthMonth") = FormatEmptyString(ctlDamBirthDate.MonthValue)
                        .Item("BirthYear") = FormatEmptyString(ctlDamBirthDate.YearValue)
                    End With
                End If

                If dsData.Tables(BSELib.clsCase.SIRE_DETAILS_TABLE).Rows.Count > 0 Then
                    With dsData.Tables(BSELib.clsCase.SIRE_DETAILS_TABLE).Rows(0)
                        .Item("Eartag") = FormatEmptyString(txtSireEartag.Text)
                        .Item("Name") = FormatEmptyString(txtSireName.Text)
                        .Item("RBSE") = FormatEmptyString(ctlSireRBSE.UnformattedRBSE)
                        .Item("Herdbook") = FormatEmptyString(txtSireHerdbook.Text)
                        .Item("BirthDay") = FormatEmptyString(ctlSireBirthDate.DayValue)
                        .Item("BirthMonth") = FormatEmptyString(ctlSireBirthDate.MonthValue)
                        .Item("BirthYear") = FormatEmptyString(ctlSireBirthDate.YearValue)
                    End With
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to Save Relations Details.", ex)
            Return False
        End Try

        Return True
    End Function

#End Region

#Region "Button Click Handlers"

    Private Sub grdRelations_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles grdRelations.SelectedIndexChanged

        Dim drData() As DataRow
        Dim dtData As DataTable = Session.Item(SessionVars.SV_RelationsTable)
        Dim iID As Int32 = Convert.ToInt32(grdRelations.DataKeys(grdRelations.SelectedIndex))
        Dim sSearch As String = "ID = " & CStr(iID)

        drData = dtData.Select(sSearch)
        With drData(0)
            txtRelationID.Text = .Item("ID").ToString()
            ctlRelationRBSE.RBSE = .Item("RelationRBSE").ToString()
            SelectItemInDropDownList(ddlRelationType, .Item("RelationType").ToString())
            SelectItemInDropDownList(ddlRelationSex, .Item("Sex").ToString())
            lblRelationFateValue.Text = .Item("RelationFateDesc").ToString()
            SelectItemInDropDownList(ddlRelationFate, .Item("RelationFate").ToString())
            ctlRelationEartag.EartagCountry = .Item("EartagCountry").ToString()
            ctlRelationEartag.EartagHerdmark = .Item("EartagHerdmark").ToString()
            ctlRelationEartag.EartagAnimal = .Item("Eartag").ToString()
            If .Item("BirthDay").ToString <> "" AndAlso .Item("BirthMonth").ToString <> "" AndAlso .Item("BirthYear").ToString <> "" Then
                ctlRelationBirthDate.DateField = .Item("BirthDay").ToString() & "/" & MonthName(.Item("BirthMonth").ToString()) & "/" & .Item("BirthYear").ToString()
            Else
                ctlRelationBirthDate.DateField = ""
            End If
            ctlRelationLeftDate.DateField = .Item("LeftDate").ToString()
            txtRelationSire.Text = .Item("Sire").ToString()
        End With

        ctlRelationEartag.Validate()
        ctlRelationRBSE.SetValidMark(True)
        EnableRelationsControlsForRBSE()

        lblrfvRelationType.Visible = False
        lblrfvSex.Visible = False

        If mbCanEditRelations Then
            btnUpdateSelected.Enabled = True
            btnDeleteSelected.Enabled = True
        End If
        btnAddAsNew.Enabled = False

    End Sub

    Private Sub EnableRelationsControlsForRBSE()

        Dim bRBSEPresent As Boolean = ctlRelationRBSE.RBSE <> ""

        ddlRelationSex.Enabled = Not bRBSEPresent
        lblRelationFateValue.Visible = bRBSEPresent
        ddlRelationFate.Visible = Not bRBSEPresent
        ddlRelationFate.Enabled = Not bRBSEPresent      'Enable the fate combo box if it is visible
        ctlRelationEartag.Enabled = Not bRBSEPresent
        ctlRelationBirthDate.Enabled = Not bRBSEPresent
        ctlRelationLeftDate.Enabled = Not bRBSEPresent
        txtRelationSire.Enabled = Not bRBSEPresent

        btnViewRelations.Enabled = bRBSEPresent AndAlso ctlRelationRBSE.IsMarkedValid

    End Sub

    Private Sub btnUpdateSelected_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnUpdateSelected.Click

        'if no relation type is selected, disallow update of record
        If GetSelectedTextFromDropDownList(ddlRelationType) = "" Then
            lblrfvRelationType.Visible = True
            EnableRelationsControlsForRBSE()
            Exit Sub
        End If

        'if no sex is selected, disallow update of record
        If GetSelectedTextFromDropDownList(ddlRelationSex) = "" AndAlso ddlRelationSex.Enabled Then
            lblrfvSex.Visible = True
            EnableRelationsControlsForRBSE()
            Exit Sub
        End If

        'disallow left dates after today
        If Not ctlRelationLeftDate.Validate(Now(), BSESystem.CalendarDate.ValidationType.eValidateLatest, "Must be today or earlier") Then
            EnableRelationsControlsForRBSE()
            Exit Sub
        End If

        'disallow birth dates before 1970 or after today / left date
        Dim dStartDate As Date = CDate("1 January 1970")
        Dim dEndDate As Date = Now()
        Dim dDate As Date

        If IsDate(ctlRelationLeftDate.DateField) Then
            dEndDate = CDate(ctlRelationLeftDate.DateField)
        End If

        If Not ctlRelationBirthDate.Validate(dDate, dStartDate, dEndDate, "Please enter a birth date between " & dStartDate.ToShortDateString & " and " & dEndDate.ToShortDateString) Then
            EnableRelationsControlsForRBSE()
            Exit Sub
        End If

        'if RBSE is not valid, disallow update of record
        If Not ctlRelationRBSE.IsMarkedValid Then
            EnableRelationsControlsForRBSE()
            Exit Sub
        End If

        ctlRelationEartag.HideValidation()

        Dim drData() As DataRow
        Dim dtData As DataTable = Session.Item(SessionVars.SV_RelationsTable)
        Dim iID As Int32 = Convert.ToInt32(grdRelations.DataKeys(grdRelations.SelectedIndex))
        Dim sSearch As String = "ID = " & CStr(iID)

        drData = dtData.Select(sSearch)
        With drData(0)
            .Item("RelationRBSE") = FormatEmptyString(ctlRelationRBSE.UnformattedRBSE)
            .Item("RelationType") = GetSelectedItemFromDropDownList(ddlRelationType)
            .Item("RelationTypeDesc") = GetSelectedTextFromDropDownList(ddlRelationType)
            .Item("Sex") = FormatEmptyString(GetSelectedItemFromDropDownList(ddlRelationSex))
            .Item("SexDesc") = GetSelectedTextFromDropDownList(ddlRelationSex)
            .Item("RelationFate") = FormatEmptyString(GetSelectedItemFromDropDownList(ddlRelationFate))
            If ctlRelationRBSE.RBSE <> "" Then
                .Item("RelationFateDesc") = lblRelationFateValue.Text
            Else
                .Item("RelationFateDesc") = GetSelectedTextFromDropDownList(ddlRelationFate)
            End If
            .Item("EartagCountry") = FormatEmptyString(ctlRelationEartag.EartagCountry)
            .Item("EartagHerdmark") = FormatEmptyString(ctlRelationEartag.EartagHerdmark)
            .Item("Eartag") = FormatEmptyString(ctlRelationEartag.EartagAnimal)
            If IsDate(ctlRelationBirthDate.DateField) Then
                .Item("BirthDay") = Day(ctlRelationBirthDate.DateField)
                .Item("BirthMonth") = Month(ctlRelationBirthDate.DateField)
                .Item("BirthYear") = Year(ctlRelationBirthDate.DateField)
            Else
                .Item("BirthDay") = DBNull.Value
                .Item("BirthMonth") = DBNull.Value
                .Item("BirthYear") = DBNull.Value
            End If
            .Item("LeftDate") = FormatEmptyString(ctlRelationLeftDate.DateField)
            .Item("Sire") = FormatEmptyString(txtRelationSire.Text)
        End With

        ClearRelationInputFields(True)

        btnUpdateSelected.Enabled = False
        btnDeleteSelected.Enabled = False
        If mbCanEditRelations Then
            btnAddAsNew.Enabled = True
        End If

        'after updating a record, re-enable the relationship controls
        SetRelationsControls(True)

    End Sub

    Private Sub btnDeleteSelected_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnDeleteSelected.Click
        Dim drData() As DataRow
        Dim dtData As DataTable = Session.Item(SessionVars.SV_RelationsTable)
        Dim iID As Int32 = Convert.ToInt32(grdRelations.DataKeys(grdRelations.SelectedIndex))
        Dim sSearch As String = "ID = '" & CStr(iID) & "'"

        drData = dtData.Select(sSearch)
        With drData(0)
            '.BeginEdit()
            .Delete()
            '.AcceptChanges()
        End With

        ClearRelationInputFields(True)

        btnUpdateSelected.Enabled = False
        btnDeleteSelected.Enabled = False
        If mbCanEditRelations Then
            btnAddAsNew.Enabled = True
        End If

        'after deleting a record, re-enable the relationship controls
        SetRelationsControls(True)


    End Sub

    Private Sub btnAddAsNew_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnAddAsNew.Click

        'if no relation type is selected, disallow addition of record
        If GetSelectedTextFromDropDownList(ddlRelationType) = "" Then
            lblrfvRelationType.Visible = True
            EnableRelationsControlsForRBSE()
            Exit Sub
        End If

        'if no sex is selected, disallow update of record
        If GetSelectedTextFromDropDownList(ddlRelationSex) = "" AndAlso ddlRelationSex.Enabled Then
            lblrfvSex.Visible = True
            EnableRelationsControlsForRBSE()
            Exit Sub
        End If

        'disallow left dates after today
        If Not ctlRelationLeftDate.Validate(Now(), BSESystem.CalendarDate.ValidationType.eValidateLatest, "Must be today or earlier") Then
            EnableRelationsControlsForRBSE()
            Exit Sub
        End If

        'disallow birth dates before 1970 or after today / left date
        Dim dStartDate As Date = CDate("1 January 1970")
        Dim dEndDate As Date = Now()
        Dim dDate As Date

        If IsDate(ctlRelationLeftDate.DateField) Then
            dEndDate = CDate(ctlRelationLeftDate.DateField)
        End If

        If Not ctlRelationBirthDate.Validate(dDate, dStartDate, dEndDate, "Please enter a birth date between " & dStartDate.ToShortDateString & " and " & dEndDate.ToShortDateString) Then
            EnableRelationsControlsForRBSE()
            Exit Sub
        End If

        'if RBSE is not valid, disallow addition of record
        If Not ctlRelationRBSE.IsMarkedValid Then
            EnableRelationsControlsForRBSE()
            Exit Sub
        End If

        'otherwise, update datatable with data
        Dim dtData As DataTable = Session.Item(SessionVars.SV_RelationsTable)
        Dim drData As DataRow

        drData = dtData.NewRow()
        With drData
            .Item("RBSE") = Replace(Session(SessionVars.SV_RBSENumber), "/", "")
            .Item("RelationRBSE") = FormatEmptyString(ctlRelationRBSE.UnformattedRBSE)
            .Item("RelationType") = GetSelectedItemFromDropDownList(ddlRelationType)
            .Item("RelationTypeDesc") = GetSelectedTextFromDropDownList(ddlRelationType)
            .Item("Sex") = FormatEmptyString(GetSelectedItemFromDropDownList(ddlRelationSex))
            .Item("SexDesc") = GetSelectedTextFromDropDownList(ddlRelationSex)
            .Item("RelationFate") = FormatEmptyString(GetSelectedItemFromDropDownList(ddlRelationFate))
            If ctlRelationRBSE.RBSE <> "" Then
                .Item("RelationFateDesc") = lblRelationFateValue.Text
            Else
                .Item("RelationFateDesc") = GetSelectedTextFromDropDownList(ddlRelationFate)
            End If
            .Item("EartagCountry") = FormatEmptyString(ctlRelationEartag.EartagCountry)
            .Item("EartagHerdmark") = FormatEmptyString(ctlRelationEartag.EartagHerdmark)
            .Item("Eartag") = FormatEmptyString(ctlRelationEartag.EartagAnimal)
            If IsDate(ctlRelationBirthDate.DateField) Then
                .Item("BirthDay") = Day(ctlRelationBirthDate.DateField)
                .Item("BirthMonth") = Month(ctlRelationBirthDate.DateField)
                .Item("BirthYear") = Year(ctlRelationBirthDate.DateField)
            Else
                .Item("BirthDay") = DBNull.Value
                .Item("BirthMonth") = DBNull.Value
                .Item("BirthYear") = DBNull.Value
            End If
            .Item("LeftDate") = FormatEmptyString(ctlRelationLeftDate.DateField)
            .Item("Sire") = FormatEmptyString(txtRelationSire.Text)
        End With

        dtData.Rows.Add(drData)
        'dtData.AcceptChanges()

        'and reset the input form for a new record
        ClearRelationInputFields(True)

        'after adding a new record using RBSE lookup, re-enable the relationship controls
        SetRelationsControls(True)

    End Sub

    Private Sub ClearRelationInputFields(ByVal bClearRBSE As Boolean)
        'bClearRBSE is true if we are clearing data from RBSE-related fields
        'as well as the other fields.  This happens when a row is added, updated
        'or deleted, but not when the user changes the data in the RBSE box

        If bClearRBSE Then
            txtRelationID.Text = ""
        End If

        If bClearRBSE Then
            ddlRelationType.SelectedIndex = -1
        End If
        ddlRelationSex.SelectedIndex = -1
        ddlRelationFate.SelectedIndex = -1
        lblRelationFateValue.Text = ""
        ctlRelationEartag.EartagAnimal = ""
        ctlRelationEartag.EartagHerdmark = ""
        ctlRelationEartag.EartagCountry = ""
        If bClearRBSE Then
            ctlRelationRBSE.RBSE = ""
            're-display the Fate combo box. It is hidden when an RBSE is entered
            ddlRelationFate.Visible = True
        End If
        btnViewRelations.Enabled = False
        ctlRelationBirthDate.DateField = ""
        ctlRelationLeftDate.DateField = ""
        txtRelationSire.Text = ""

        If bClearRBSE Then
            EnableRelationsGrid()
        End If

        lblrfvRelationType.Visible = False
        lblrfvSex.Visible = False

    End Sub
#End Region

#Region "Private Methods"

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

    Private Sub RemoveDam()

        Dim dsData As DataSet = Session.Item(SessionVars.SV_CaseDetails)

        With dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows(0)
            .Item("DamStatus") = DBNull.Value
        End With

        With dsData.Tables(BSELib.clsCase.DAM_DETAILS_TABLE).Rows(0)
            .Item("ID") = DBNull.Value
            .Item("Eartag") = DBNull.Value
            .Item("Name") = DBNull.Value
            .Item("RBSE") = DBNull.Value
            .Item("Herdbook") = DBNull.Value
            .Item("BirthDay") = DBNull.Value
            .Item("BirthMonth") = DBNull.Value
            .Item("BirthYear") = DBNull.Value
            .Item("Fate") = DBNull.Value
            .Item("FinalResult") = DBNull.Value
            .Item("ChildCount") = DBNull.Value
            .Item("RowStamp") = DBNull.Value
        End With

        LoadCaseDetails()
        EnableControls()
    End Sub

    Private Sub RemoveSire()

        Dim dsData As DataSet = Session.Item(SessionVars.SV_CaseDetails)

        With dsData.Tables(BSELib.clsCase.SIRE_DETAILS_TABLE).Rows(0)
            .Item("ID") = DBNull.Value
            .Item("Eartag") = DBNull.Value
            .Item("Name") = DBNull.Value
            .Item("RBSE") = DBNull.Value
            .Item("Herdbook") = DBNull.Value
            .Item("BirthDay") = DBNull.Value
            .Item("BirthMonth") = DBNull.Value
            .Item("BirthYear") = DBNull.Value
            .Item("Fate") = DBNull.Value
            .Item("ChildCount") = DBNull.Value
            .Item("RowStamp") = DBNull.Value
        End With

        LoadCaseDetails()
        EnableControls()
    End Sub

    Public Function FormatRBSE(ByVal sRBSE As String) As String
        If sRBSE <> "" Then
            sRBSE = Left$(sRBSE, 2) & "/" & Mid$(sRBSE, 3, 2) & "/" & Mid$(sRBSE, 5, 5)
        End If

        Return sRBSE
    End Function

    Public Function FormatPartialDate(ByVal sDay As String, ByVal sMonth As String, ByVal sYear As String) As String

        Dim sPartialDate As New System.Text.StringBuilder()

        If sDay <> "" Then
            sPartialDate.Append(sDay)
            sPartialDate.Append("/")
        End If

        If sMonth <> "" Then
            sPartialDate.Append(sMonth)
            sPartialDate.Append("/")
        End If

        sPartialDate.Append(sYear)

        Return sPartialDate.ToString()

    End Function

    Private Function RBSEIsRelation(ByRef sRBSE As String, Optional ByVal sID As String = "") As Boolean
        Dim dtData As DataTable = Session.Item(SessionVars.SV_RelationsTable)
        Dim drData() As DataRow
        Dim sSearch As String
        If sID <> "" Then
            sSearch = "RelationRBSE = '" & sRBSE & "' AND ID <> " & sID
        Else
            sSearch = "RelationRBSE = '" & sRBSE & "'"
        End If
        drData = dtData.Select(sSearch)
        If drData.Length > 0 Then
            Return True
        Else
            Return False
        End If
    End Function

#End Region

End Class
