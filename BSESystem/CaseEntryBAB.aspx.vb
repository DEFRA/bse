Partial Class CaseEntryBAB
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents ctlTracedCPHH As CPHH
    Protected WithEvents BatchNumberDisplay1 As BatchNumberDisplay
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
        'SetEnterKeys()
        lblRBSEHeader.Text = RBSE_CAPTION & Session(SessionVars.SV_RBSENumber)
        VLAHeader1.BatchNumber = Session(SessionVars.SV_BatchNumber)
        If Not IsPostBack Then
            LoadLookupLists()
            LoadCaseDetails()
        End If

        litViewDocs.Text = GetSpolSiteButtonCode(Session(SessionVars.SV_RBSENumber))
        litViewDocs2.Text = litViewDocs.Text

        EnableControls()
    End Sub

    Private Sub SetEnterKeys()

        SetTextboxControlOnEnter(txtNotes, ddlOrigin.ClientID)
        ctlTracedCPHH.SetControlOnEnter(txtTracedName.ClientID)
        SetTextboxControlOnEnter(txtTracedName, txtTracedAddress1.ClientID)
        SetTextboxControlOnEnter(txtTracedAddress1, txtTracedAddress2.ClientID)
        SetTextboxControlOnEnter(txtTracedAddress2, txtTracedAddress3.ClientID)
        SetTextboxControlOnEnter(txtTracedAddress3, txtTracedPostcode.ClientID)
        SetTextboxControlOnEnter(txtTracedPostcode, ddlFeedRisk.ClientID)

    End Sub

#Region "Lookup List Population"

    Private Sub LoadLookupLists()

        Dim blnResult As Boolean
        Dim objDataTable As DataTable
        Dim objLookup As New BSELib.LookupData()

        Try
            objDataTable = objLookup.GetLookupData(LOOKUP_ANIMAL_ORIGIN)
            If Not (objDataTable Is Nothing) Then
                ddlOrigin.DataSource = objDataTable
                ddlOrigin.DataValueField = "Code"
                ddlOrigin.DataTextField = "Description"
                ddlOrigin.DataBind()
                Common.AddItemToDropDownList(ddlOrigin)
            End If

            objDataTable = objLookup.GetLookupData(LOOKUP_FEED_RISK)
            If Not (objDataTable Is Nothing) Then
                ddlFeedRisk.DataSource = objDataTable
                ddlFeedRisk.DataValueField = "Code"
                ddlFeedRisk.DataTextField = "Description"
                ddlFeedRisk.DataBind()
                Common.AddItemToDropDownList(ddlFeedRisk)
            End If

            objDataTable = objLookup.GetLookupData(LOOKUP_HORIZONTAL_RISK)
            If Not (objDataTable Is Nothing) Then
                ddlHoizontalRisk.DataSource = objDataTable
                ddlHoizontalRisk.DataValueField = "Code"
                ddlHoizontalRisk.DataTextField = "Description"
                ddlHoizontalRisk.DataBind()
                Common.AddItemToDropDownList(ddlHoizontalRisk)
            End If

            objDataTable = objLookup.GetLookupData(LOOKUP_MATERNAL_RISK)
            If Not (objDataTable Is Nothing) Then
                ddlMaternalRisk.DataSource = objDataTable
                ddlMaternalRisk.DataValueField = "Code"
                ddlMaternalRisk.DataTextField = "Description"
                ddlMaternalRisk.DataBind()
                Common.AddItemToDropDownList(ddlMaternalRisk)
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve 'BAB Tab' drop down lists.", ex)
        End Try

    End Sub

#End Region

#Region "Event Handlers"

    Private Sub ddlOrigin_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ddlOrigin.SelectedIndexChanged
        Dim dsData As DataSet = Session.Item(SessionVars.SV_CaseDetails)
        Dim sMessage As String = ""

        If ddlOrigin.SelectedItem.Value <> "P" Then
            EmptyPurchaseFields(sMessage)
            EmptyTracedFields(sMessage)
            If sMessage <> "" Then
                Dim jScript As System.Text.StringBuilder = New System.Text.StringBuilder()
                With jScript
                    .Append("<script language=""JavaScript"">")
                    .Append("alert('" & sMessage & "')")
                    .Append("</script>")
                End With
                Page.RegisterStartupScript("navigate", jScript.ToString())
            End If
        End If
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

    Private Sub hlbRelations_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlbRelations.Click
        If UpdateSessionWithCaseDetails() Then
            Response.Redirect("CaseEntryRelations.aspx")
        End If
    End Sub

#End Region

#Region "Handle Data"

    Private Function LoadCaseDetails() As Boolean

        Dim dsData As DataSet
        Dim bError As Boolean = True

        Try

            dsData = Session.Item(SessionVars.SV_CaseDetails)

            If dsData.Tables(BSELib.clsCase.BAB_TABLE).Rows.Count <> 0 Then
                With dsData.Tables(BSELib.clsCase.BAB_TABLE).Rows(0)
                    txtNotes.Text = .Item("Notes").ToString()
                    txtTracedAddress1.Text = .Item("TracedAddress1").ToString()
                    txtTracedAddress2.Text = .Item("TracedAddress2").ToString()
                    txtTracedAddress3.Text = .Item("TracedAddress3").ToString()
                    ctlTracedCPHH.CPHH = .Item("NatalCPHH").ToString()
                    txtTracedName.Text = .Item("TracedName").ToString()
                    txtTracedPostcode.Text = .Item("TracedPostcode").ToString()
                    SelectItemInDropDownList(ddlMaternalRisk, .Item("MaternalRisk").ToString())
                    SelectItemInDropDownList(ddlHoizontalRisk, .Item("HorizontalRisk").ToString())
                    SelectItemInDropDownList(ddlFeedRisk, .Item("FeedRisk").ToString())
                End With
                bError = False
            End If
            If dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows.Count <> 0 Then
                SelectItemInDropDownList(ddlOrigin, dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows(0)("Origin").ToString())
                bError = False
            End If

            Return Not bError

        Catch ex As Exception
            clsAppError.DisplayError("Failed to 'Load BAB Details'.", ex)
            Return False
        End Try
    End Function

    Private Function UpdateSessionWithCaseDetails() As Boolean

        Dim dsData As DataSet = Session.Item(SessionVars.SV_CaseDetails)

        Try
            If txtNotes.Enabled = True Then ' If the controls are not enabled, no need to save
                If dsData.Tables(BSELib.clsCase.BAB_TABLE).Rows.Count = 0 Then
                    AddEmptyRow(dsData.Tables(BSELib.clsCase.BAB_TABLE), dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows(0)("RBSE"))
                End If

                With dsData.Tables(BSELib.clsCase.BAB_TABLE).Rows(0)
                    .Item("Notes") = FormatEmptyString(txtNotes.Text)
                    .Item("TracedAddress1") = FormatEmptyString(txtTracedAddress1.Text)
                    .Item("TracedAddress2") = FormatEmptyString(txtTracedAddress2.Text)
                    .Item("TracedAddress3") = FormatEmptyString(txtTracedAddress3.Text)
                    .Item("NatalCPHH") = FormatEmptyString(ctlTracedCPHH.CPHH)
                    .Item("TracedName") = FormatEmptyString(txtTracedName.Text)
                    .Item("TracedPostcode") = FormatEmptyString(txtTracedPostcode.Text)
                    .Item("MaternalRisk") = FormatEmptyString(GetSelectedItemFromDropDownList(ddlMaternalRisk))
                    .Item("HorizontalRisk") = FormatEmptyString(GetSelectedItemFromDropDownList(ddlHoizontalRisk))
                    .Item("FeedRisk") = FormatEmptyString(GetSelectedItemFromDropDownList(ddlFeedRisk))
                End With
            End If

            If ddlOrigin.Enabled Then
                dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows(0)("Origin") = FormatEmptyString(GetSelectedItemFromDropDownList(ddlOrigin))
            End If
            Return True

        Catch ex As Exception
            clsAppError.DisplayError("Failed to 'Save BAB Details'.", ex)
            Return False
        End Try
    End Function

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
    End Sub

    Private Sub DEFRADataEntryEnable()
        BatchNumberDisplay1.Visible = False
        MakeControlsWritable()
        btnSave.Enabled = True
    End Sub

    Private Sub DEFRAMaintenanceEnable()
        BatchNumberDisplay1.Visible = False
        MakeControlsWritable()
        btnSave.Enabled = True
    End Sub

    Private Sub VLADataEntryEnable()
        BatchNumberDisplay1.Visible = True
        MakeControlsReadOnly()
        btnSave.Enabled = True
    End Sub

    Private Sub VLAMaintenanceEnable()
        BatchNumberDisplay1.Visible = True
        MakeControlsWritable()
        btnSave.Enabled = True
    End Sub

    Private Sub MakeControlsReadOnly()
        txtNotes.Enabled = False
        ddlOrigin.Enabled = False
        txtTracedAddress1.Enabled = False
        txtTracedAddress2.Enabled = False
        txtTracedAddress3.Enabled = False
        txtTracedPostcode.Enabled = False
        txtTracedName.Enabled = False
        ctlTracedCPHH.Enabled = False
        ddlFeedRisk.Enabled = False
        ddlMaternalRisk.Enabled = False
        ddlHoizontalRisk.Enabled = False
    End Sub

    Private Sub MakeControlsWritable()
        Dim dsData As DataSet = Session.Item(SessionVars.SV_CaseDetails)
        If dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows.Count <> 0 Then
            If Not (dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows(0)("birthdate") Is DBNull.Value) Then
                If (dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows(0)("birthdate") >= CDate("18/07/1988")) Or (dsData.Tables(BSELib.clsCase.BAB_TABLE).Rows.Count <> 0) Then
                    txtNotes.Enabled = True
                    ddlOrigin.Enabled = True
                    If ddlOrigin.SelectedItem.Value = "P" Then
                        txtTracedAddress1.Enabled = True
                        txtTracedAddress2.Enabled = True
                        txtTracedAddress3.Enabled = True
                        txtTracedPostcode.Enabled = True
                        txtTracedName.Enabled = True
                        ctlTracedCPHH.Enabled = True
                    Else
                        txtTracedAddress1.Enabled = False
                        txtTracedAddress2.Enabled = False
                        txtTracedAddress3.Enabled = False
                        txtTracedPostcode.Enabled = False
                        txtTracedName.Enabled = False
                        ctlTracedCPHH.Enabled = False
                    End If
                    ddlFeedRisk.Enabled = True
                    ddlMaternalRisk.Enabled = True
                    ddlHoizontalRisk.Enabled = True
                    Exit Sub
                End If
            End If
        End If
        MakeControlsReadOnly()

    End Sub

#End Region

#Region "CancelCaseEdit"

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

#End Region

#Region "Private Methods"

    Private Sub EmptyPurchaseFields(ByRef sMessage As String)
        Dim dsData As DataSet = Session.Item(SessionVars.SV_CaseDetails)

        If dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows.Count <> 0 Then
            With dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows(0)
                If .Item("PurchaseDate").ToString() <> "" Or .Item("PurchasedCounty").ToString() <> "" Or .Item("PurchaseAgeInMonths").ToString() <> "" Then
                    sMessage = "Purchase data has been entered on this Case.  If you do not Cancel then this data will be lost."
                End If

                .Item("PurchaseDate") = FormatEmptyString("")
                .Item("PurchasedCounty") = FormatEmptyString("")
                .Item("PurchaseAgeInMonths") = FormatEmptyString("")
            End With
        End If
    End Sub

    Private Sub EmptyTracedFields(ByRef sMessage As String)
        Dim dsData As DataSet = Session.Item(SessionVars.SV_CaseDetails)

        If txtTracedAddress1.Text <> "" Or txtTracedAddress2.Text <> "" Or txtTracedAddress3.Text <> "" Or ctlTracedCPHH.CPHH <> "" Or txtTracedName.Text <> "" Or txtTracedPostcode.Text <> "" Then
            If sMessage = "" Then
                sMessage = "Trace data has been entered on this Case.  If you do not Cancel then this data will be lost."
            Else
                sMessage = "Purchase And Trace data has been entered on this Case.  If you do not Cancel then this data will be lost."
            End If
        End If

        txtTracedAddress1.Text = ""
        txtTracedAddress2.Text = ""
        txtTracedAddress3.Text = ""
        ctlTracedCPHH.CPHH = ""
        txtTracedName.Text = ""
        txtTracedPostcode.Text = ""
    End Sub

#End Region

End Class
