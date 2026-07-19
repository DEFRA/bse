Partial Class CaseEntryClinical
    Inherits System.Web.UI.Page
    Protected WithEvents ClinicalVisitPager As DataGridPager
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents BatchNumberDisplay1 As BatchNumberDisplay
    Protected WithEvents ctlExitConfirmation As ExitConfirmation


    Private m_bContinueEditingVisitDate As Boolean = False

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
        lblRBSEHeader.Text = RBSE_CAPTION & Session(SessionVars.SV_RBSENumber)
        VLAHeader1.BatchNumber = Session(SessionVars.SV_BatchNumber)
        ClinicalVisitPager.SetGrid(grdClinicalVisit)
        If Not IsPostBack Then
            DisableClinicalVisitGrid()
            LoadCaseDetails()
        End If

        litViewDocs.Text = GetSpolSiteButtonCode(Session(SessionVars.SV_RBSENumber))
        litViewDocs2.Text = litViewDocs.Text

        EnableControls()
    End Sub

#Region "Grid Handling"

    Private Sub DisableClinicalVisitGrid()
        Dim dtData As DataTable = Session.Item(SessionVars.SV_ClinicalVisitTable)
        Dim dvData As DataView = dtData.DefaultView
        Session.Item(SessionVars.SV_ClinicalVisitView) = dvData
        grdClinicalVisit.DataSource = New DataTable()
        grdClinicalVisit.DataBind()

        ClinicalVisitPager.DataTableSessionID = SessionVars.SV_ClinicalVisitTable
        ClinicalVisitPager.DataViewSessionID = SessionVars.SV_ClinicalVisitView
        ClinicalVisitPager.PageLinkCount = 0
        ClinicalVisitPager.Refresh()
    End Sub

    Private Sub EnableClinicalVisitGrid()
        Dim dtData As DataTable = Session.Item(SessionVars.SV_ClinicalVisitTable)
        Dim dvData As DataView = dtData.DefaultView
        Session.Item(SessionVars.SV_ClinicalVisitView) = dvData

        grdClinicalVisit.DataSource = dtData
        grdClinicalVisit.DataKeyField = "ID"
        grdClinicalVisit.CurrentPageIndex = 0
        grdClinicalVisit.SelectedIndex = -1
        grdClinicalVisit.EditItemIndex = -1
        grdClinicalVisit.DataBind()

        ClinicalVisitPager.DataTableSessionID = SessionVars.SV_ClinicalVisitTable
        ClinicalVisitPager.DataViewSessionID = SessionVars.SV_ClinicalVisitView
        ClinicalVisitPager.PageLinkCount = 10
        ClinicalVisitPager.Refresh()
    End Sub

    Private Sub grdClinicalVisit_ItemDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.DataGridItemEventArgs) Handles grdClinicalVisit.ItemDataBound
        ' populate template column values here
        Try
            ' set up the checkbox and drop-down columns
            Dim drv As DataRowView = CType(e.Item.DataItem, DataRowView)
            If Not drv Is Nothing Then
                Dim ctlVisitDate As CalendarDate = Nothing
                Dim lblVisitDate As Label = Nothing
                Dim lblVisitDateError As Label = Nothing

                If e.Item.ItemType = ListItemType.EditItem Then
                    ' populate edit mode controls
                    ctlVisitDate = CType(e.Item.FindControl("ctlVisitDateEdit"), CalendarDate)
                ElseIf e.Item.ItemType = ListItemType.Item _
                OrElse e.Item.ItemType = ListItemType.AlternatingItem _
                OrElse e.Item.ItemType = ListItemType.SelectedItem Then
                    ' populate display mode controls
                    lblVisitDate = CType(e.Item.FindControl("lblVisitDateDisplay"), Label)
                End If

                If Not lblVisitDate Is Nothing Then
                    If Not IsDBNull(drv("VisitDate")) Then
                        lblVisitDate.Text = drv("VisitDate")
                    Else
                        lblVisitDate.Text = ""
                    End If
                End If

                If Not ctlVisitDate Is Nothing Then
                    If Not IsDBNull(drv("VisitDate")) Then
                        ctlVisitDate.DateField = drv("VisitDate")
                    End If
                    lblVisitDateError = CType(e.Item.FindControl("lblVisitDateError"), Label)
                    If Not lblVisitDateError Is Nothing Then
                        If m_bContinueEditingVisitDate Then
                            lblVisitDateError.Visible = True
                        Else
                            lblVisitDateError.Visible = False
                        End If
                    End If
                    lblVisitDateError = Nothing
                End If


            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to bind template columns in the Other Owners grid", ex)
        End Try
    End Sub

    Private Sub ClinicalVisitPager_RowSave(ByVal sender As Object, ByVal e As BSESystem.DataGridPagerEventArgs) Handles ClinicalVisitPager.RowSave
        ' save template column values to the dataset here
        Dim ctlVisitDate As CalendarDate = CType(e.GridRow.FindControl("ctlVisitDateEdit"), CalendarDate)
        Dim dDate As Date
        Dim dBirthDate As Date
        Dim dsData As DataSet = Session.Item(SessionVars.SV_CaseDetails)

        If Not dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows(0)("BirthDate") Is DBNull.Value Then
            dBirthDate = dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows(0)("BirthDate")
        Else
            dBirthDate = CDate("01 jan 1970")
        End If

        m_bContinueEditingVisitDate = False

        e.DataTableRow("RBSE") = Replace(Session.Item(SessionVars.SV_RBSENumber), "/", "")

        Dim dCompareNothing As Date = Nothing

        If ctlVisitDate.Validate(dDate, dBirthDate, Now()) Then
            If dDate = dCompareNothing Then
                e.DataTableRow("VisitDate") = DBNull.Value
            Else
                e.DataTableRow("VisitDate") = dDate
            End If
        End If

        If e.DataTableRow("VisitDate") Is DBNull.Value Then
            m_bContinueEditingVisitDate = True
        Else
            ' Check to ensure that there is only one instance of this date
            Dim iCount As Integer
            Dim dtData As DataTable = Session.Item(SessionVars.SV_ClinicalVisitTable)

            For iCount = 0 To dtData.Rows.Count - 1
                If dtData.Rows(iCount)("VisitDate") = e.DataTableRow("VisitDate") Then
                    If dtData.Rows(iCount)("ID") <> e.DataTableRow("ID") Then
                        m_bContinueEditingVisitDate = True
                        Exit For
                    End If
                End If
            Next
        End If
    End Sub

    Private Sub ClinicalVisitPager_BeforeDataChanged(ByVal sender As Object, ByRef e As BSESystem.DataGridPagerEventArgs) Handles ClinicalVisitPager.BeforeDataChanged
        e.bCarryOnEditing = m_bContinueEditingVisitDate
    End Sub

#End Region

#Region "Event Handlers"

    Private Sub btnSave_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSave.Click, btnSave2.Click
        ClinicalVisitPager.CleanUpBlankRows()
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
        ClinicalVisitPager.CleanUpBlankRows()
        If UpdateSessionWithCaseDetails() Then
            Response.Redirect("CaseEntryFarm.aspx")
        End If
    End Sub

    Private Sub hlbCaseDEFRA_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlbCaseDEFRA.Click
        ClinicalVisitPager.CleanUpBlankRows()
        If UpdateSessionWithCaseDetails() Then
            Response.Redirect("CaseEntryDEFRA.aspx")
        End If
    End Sub

    Private Sub hlbBAB_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlbBAB.Click
        ClinicalVisitPager.CleanUpBlankRows()
        If UpdateSessionWithCaseDetails() Then
            Response.Redirect("CaseEntryBAB.aspx")
        End If
    End Sub

    Private Sub hlbCaseVLA_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlbCaseVLA.Click
        ClinicalVisitPager.CleanUpBlankRows()
        If UpdateSessionWithCaseDetails() Then
            Response.Redirect("CaseEntryVLA.aspx")
        End If
    End Sub

    Private Sub hlbFeeds_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlbFeeds.Click
        ClinicalVisitPager.CleanUpBlankRows()
        If UpdateSessionWithCaseDetails() Then
            Response.Redirect("CaseEntryFeeds.aspx")
        End If
    End Sub

    Private Sub hlbRelations_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlbRelations.Click
        ClinicalVisitPager.CleanUpBlankRows()
        If UpdateSessionWithCaseDetails() Then
            Response.Redirect("CaseEntryRelations.aspx")
        End If
    End Sub

#End Region

#Region "Handle Data"

    Private Function LoadCaseDetails() As Boolean

        Dim dsData As DataSet

        Try

            dsData = Session.Item(SessionVars.SV_CaseDetails)

            If dsData.Tables(BSELib.clsCase.CLINICAL_TABLE).Rows.Count <> 0 Then
                With dsData.Tables(BSELib.clsCase.CLINICAL_TABLE).Rows(0)
                    chkApprehension.Checked = GetRowColumnData(.Item("Apprehension"))
                    chkHypersensitiveTouch.Checked = GetRowColumnData(.Item("HypersensitiveTouch"))
                    chkHypersensitiveSound.Checked = GetRowColumnData(.Item("HypersensitiveSound"))
                    chkManiacal.Checked = GetRowColumnData(.Item("Maniacal"))
                    chkPanicStricken.Checked = GetRowColumnData(.Item("PanicStricken"))
                    chkTemperamentChange.Checked = GetRowColumnData(.Item("TemperamentChange"))
                    chkAbnormalHeadCarriage.Checked = GetRowColumnData(.Item("AbnormalHeadCarriage"))
                    chkEarTwitching.Checked = GetRowColumnData(.Item("EarTwitching"))
                    chkEarsOddAngle.Checked = GetRowColumnData(.Item("EarsOddAngle"))
                    chkAbnormalBehaviour.Checked = GetRowColumnData(.Item("AbnormalBehaviour"))
                    chkHeadShyness.Checked = GetRowColumnData(.Item("HeadShyness"))
                    chkLickingFlank.Checked = GetRowColumnData(.Item("LickingFlank"))
                    chkLickingNose.Checked = GetRowColumnData(.Item("LickingNose"))
                    chkKicking.Checked = GetRowColumnData(.Item("Kicking"))
                    chkReluctantToEnterDoorways.Checked = GetRowColumnData(.Item("ReluctantDoorways"))
                    chkHeadPressing.Checked = GetRowColumnData(.Item("HeadPressing"))
                    chkHeadRubbing.Checked = GetRowColumnData(.Item("HeadRubbing"))
                    chkTeethGrinding.Checked = GetRowColumnData(.Item("TeethGrinding"))
                    chkBlindness.Checked = GetRowColumnData(.Item("Blindness"))
                    chkCircling.Checked = GetRowColumnData(.Item("Circling"))
                    chkHindAtaxia.Checked = GetRowColumnData(.Item("HindAtaxia"))
                    chkFalling.Checked = GetRowColumnData(.Item("Falling"))
                    chkParesis.Checked = GetRowColumnData(.Item("Paresis"))
                    chkForeAtaxia.Checked = GetRowColumnData(.Item("ForeAtaxia"))
                    chkRecumbent.Checked = GetRowColumnData(.Item("Recumbent"))
                    chkTremor.Checked = GetRowColumnData(.Item("Tremor"))
                    chkKnucklingFetlock.Checked = GetRowColumnData(.Item("KnucklingFetlock"))
                    chkWeightLoss.Checked = GetRowColumnData(.Item("WeightLoss"))
                    chkConditionLoss.Checked = GetRowColumnData(.Item("ConditionLoss"))
                    chkMilkYield.Checked = GetRowColumnData(.Item("MilkYield"))
                End With
                'If dsData.Tables(BSELib.clsCase.CLINICAL_VISIT_TABLE).Rows.Count <> 0 Then
                EnableClinicalVisitGrid()
                'End If

            Return True
            End If
            EnableClinicalVisitGrid()
            Return False

        Catch ex As Exception
            clsAppError.DisplayError("Failed to 'Load Case Details'.", ex)
            Return False
        End Try
    End Function

    Private Function UpdateSessionWithCaseDetails() As Boolean

        Dim dsData As DataSet = Session.Item(SessionVars.SV_CaseDetails)

        Try
            If Not dsData Is Nothing Then
                If dsData.Tables(BSELib.clsCase.CLINICAL_TABLE).Rows.Count = 0 And CheckCheckboxesChecker() Then
                    AddEmptyRow(dsData.Tables(BSELib.clsCase.CLINICAL_TABLE), Replace(Session.Item(SessionVars.SV_RBSENumber), "/", ""))
                End If
                If dsData.Tables(BSELib.clsCase.CLINICAL_TABLE).Rows.Count <> 0 Then
                    With dsData.Tables(BSELib.clsCase.CLINICAL_TABLE).Rows(0)

                        .Item("Apprehension") = chkApprehension.Checked
                        .Item("HypersensitiveTouch") = chkHypersensitiveTouch.Checked
                        .Item("HypersensitiveSound") = chkHypersensitiveSound.Checked
                        .Item("Maniacal") = chkManiacal.Checked
                        .Item("PanicStricken") = chkPanicStricken.Checked
                        .Item("AbnormalHeadCarriage") = chkAbnormalHeadCarriage.Checked
                        .Item("EarTwitching") = chkEarTwitching.Checked
                        .Item("EarsOddAngle") = chkEarsOddAngle.Checked
                        .Item("HeadShyness") = chkHeadShyness.Checked
                        .Item("LickingFlank") = chkLickingFlank.Checked
                        .Item("AbnormalBehaviour") = chkAbnormalBehaviour.Checked
                        .Item("LickingNose") = chkLickingNose.Checked
                        .Item("Kicking") = chkKicking.Checked
                        .Item("ReluctantDoorways") = chkReluctantToEnterDoorways.Checked
                        .Item("HeadPressing") = chkHeadPressing.Checked
                        .Item("HeadRubbing") = chkHeadRubbing.Checked
                        .Item("TeethGrinding") = chkTeethGrinding.Checked
                        .Item("Blindness") = chkBlindness.Checked
                        .Item("Falling") = chkFalling.Checked
                        .Item("Recumbent") = chkRecumbent.Checked
                        .Item("Circling") = chkCircling.Checked
                        .Item("HindAtaxia") = chkHindAtaxia.Checked
                        .Item("Paresis") = chkParesis.Checked
                        .Item("ForeAtaxia") = chkForeAtaxia.Checked
                        .Item("Tremor") = chkTremor.Checked
                        .Item("KnucklingFetlock") = chkKnucklingFetlock.Checked
                        .Item("WeightLoss") = chkWeightLoss.Checked
                        .Item("MilkYield") = chkMilkYield.Checked
                        .Item("TemperamentChange") = chkTemperamentChange.Checked
                        .Item("ConditionLoss") = chkConditionLoss.Checked
                    End With

                    If ClinicalVisitPager.InEditMode Then Return False

                    Return True
                End If
            End If
            Return True

        Catch ex As Exception
            clsAppError.DisplayError("Failed to 'Save VLA Case Details'.", ex)
            Return False
        End Try
    End Function

    Private Function CheckCheckboxesChecker() As Boolean
        If chkApprehension.Checked() Then Return True
        If chkHypersensitiveTouch.Checked() Then Return True
        If chkHypersensitiveSound.Checked() Then Return True
        If chkManiacal.Checked() Then Return True
        If chkPanicStricken.Checked() Then Return True
        If chkAbnormalHeadCarriage.Checked() Then Return True
        If chkEarTwitching.Checked() Then Return True
        If chkEarsOddAngle.Checked() Then Return True
        If chkHeadShyness.Checked() Then Return True
        If chkLickingFlank.Checked() Then Return True
        If chkAbnormalBehaviour.Checked() Then Return True
        If chkLickingNose.Checked() Then Return True
        If chkKicking.Checked() Then Return True
        If chkReluctantToEnterDoorways.Checked() Then Return True
        If chkHeadPressing.Checked() Then Return True
        If chkHeadRubbing.Checked() Then Return True
        If chkTeethGrinding.Checked() Then Return True
        If chkBlindness.Checked() Then Return True
        If chkFalling.Checked() Then Return True
        If chkRecumbent.Checked() Then Return True
        If chkCircling.Checked() Then Return True
        If chkHindAtaxia.Checked() Then Return True
        If chkParesis.Checked() Then Return True
        If chkForeAtaxia.Checked() Then Return True
        If chkTremor.Checked() Then Return True
        If chkKnucklingFetlock.Checked() Then Return True
        If chkWeightLoss.Checked() Then Return True
        If chkMilkYield.Checked() Then Return True
        If chkTemperamentChange.Checked() Then Return True
        If chkConditionLoss.Checked Then Return True
        Return False
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
        MakeControlsReadOnly()
        btnSave.Enabled = True
    End Sub

    Private Sub DEFRAMaintenanceEnable()
        BatchNumberDisplay1.Visible = False
        MakeControlsReadOnly()
        btnSave.Enabled = True
    End Sub

    Private Sub VLADataEntryEnable()
        BatchNumberDisplay1.Visible = True
        If IsVLAAllowedAdditionalCaseEdit(Session) Then
            MakeControlsWritable()
        Else
            MakeControlsReadOnly()
        End If
        btnSave.Enabled = True
    End Sub

    Private Sub VLAMaintenanceEnable()
        BatchNumberDisplay1.Visible = True
        If IsVLAAllowedAdditionalCaseEdit(Session) Then
            MakeControlsWritable()
        Else
            MakeControlsReadOnly()
        End If
        btnSave.Enabled = True
    End Sub

    Private Sub MakeControlsReadOnly()
        chkApprehension.Enabled = False
        chkHypersensitiveTouch.Enabled = False
        chkHypersensitiveSound.Enabled = False
        chkManiacal.Enabled = False
        chkPanicStricken.Enabled = False
        chkTemperamentChange.Enabled = False
        chkAbnormalHeadCarriage.Enabled = False
        chkEarTwitching.Enabled = False
        chkHeadShyness.Enabled = False
        chkLickingFlank.Enabled = False
        chkAbnormalBehaviour.Enabled = False
        chkLickingNose.Enabled = False
        chkKicking.Enabled = False
        chkReluctantToEnterDoorways.Enabled = False
        chkHeadPressing.Enabled = False
        chkHeadRubbing.Enabled = False
        chkEarsOddAngle.Enabled = False
        chkTeethGrinding.Enabled = False
        chkBlindness.Enabled = False
        chkFalling.Enabled = False
        chkRecumbent.Enabled = False
        chkCircling.Enabled = False
        chkHindAtaxia.Enabled = False
        chkParesis.Enabled = False
        chkForeAtaxia.Enabled = False
        chkKnucklingFetlock.Enabled = False
        chkTremor.Enabled = False
        chkWeightLoss.Enabled = False
        chkConditionLoss.Enabled = False
        chkMilkYield.Enabled = False
        ClinicalVisitPager.AllowAddNew = False
        ClinicalVisitPager.AllowEdit = False
        ClinicalVisitPager.AllowDelete = False
    End Sub

    Private Sub MakeControlsWritable()
        chkApprehension.Enabled = True
        chkHypersensitiveTouch.Enabled = True
        chkHypersensitiveSound.Enabled = True
        chkManiacal.Enabled = True
        chkPanicStricken.Enabled = True
        chkTemperamentChange.Enabled = True
        chkAbnormalHeadCarriage.Enabled = True
        chkEarTwitching.Enabled = True
        chkHeadShyness.Enabled = True
        chkLickingFlank.Enabled = True
        chkAbnormalBehaviour.Enabled = True
        chkLickingNose.Enabled = True
        chkKicking.Enabled = True
        chkReluctantToEnterDoorways.Enabled = True
        chkHeadPressing.Enabled = True
        chkHeadRubbing.Enabled = True
        chkEarsOddAngle.Enabled = True
        chkTeethGrinding.Enabled = True
        chkBlindness.Enabled = True
        chkFalling.Enabled = True
        chkRecumbent.Enabled = True
        chkCircling.Enabled = True
        chkHindAtaxia.Enabled = True
        chkParesis.Enabled = True
        chkForeAtaxia.Enabled = True
        chkKnucklingFetlock.Enabled = True
        chkTremor.Enabled = True
        chkWeightLoss.Enabled = True
        chkConditionLoss.Enabled = True
        chkMilkYield.Enabled = True
        ClinicalVisitPager.AllowAddNew = True
        ClinicalVisitPager.AllowEdit = True
        ClinicalVisitPager.AllowDelete = True
    End Sub

#End Region

#Region "Unused Population Functions"

    'Private Function LoadCaseDetails() As Boolean

    '    Dim sRBSE As String
    '    Dim bApprehension As Boolean
    '    Dim bHypersensitiveTouch As Boolean
    '    Dim bHypersensitiveSound As Boolean
    '    Dim bManiacal As Boolean
    '    Dim bPanicStricken As Boolean
    '    Dim bTempChange As Boolean
    '    Dim bAbnormalHeadCarriage As Boolean
    '    Dim bEarTwitching As Boolean
    '    Dim bEarsOddAngle As Boolean
    '    Dim bAbnormalBehaviour As Boolean
    '    Dim bHeadShyness As Boolean
    '    Dim bLickingFlank As Boolean
    '    Dim bLickingNose As Boolean
    '    Dim bKicking As Boolean
    '    Dim bReluctantDoorways As Boolean
    '    Dim bHeadPressing As Boolean
    '    Dim bHeadRubbing As Boolean
    '    Dim bTeethGrinding As Boolean
    '    Dim bBlindness As Boolean
    '    Dim bCircling As Boolean
    '    Dim bHindAtaxia As Boolean
    '    Dim bFalling As Boolean
    '    Dim bParesis As Boolean
    '    Dim bForeAtaxia As Boolean
    '    Dim bRecumbent As Boolean
    '    Dim bTremor As Boolean
    '    Dim bKnucklingFetlock As Boolean
    '    Dim bWeightLoss As Boolean
    '    Dim bCondition As Boolean
    '    Dim bMilkYield As Boolean
    '    Dim objCase As New BSELib.clsCase()

    '    Dim dtData As DataTable

    '    Try

    '        sRBSE = Replace(CStr(Session.Item(SessionVars.SV_RBSENumber)), "/", "")

    '        If (sRBSE <> "") Then
    '            If Not (objCase.GetClinicalByRBSE(sRBSE, _
    '                        bApprehension, _
    '                        bHypersensitiveTouch, _
    '                        bHypersensitiveSound, _
    '                        bManiacal, _
    '                        bPanicStricken, _
    '                        bTempChange, _
    '                        bAbnormalHeadCarriage, _
    '                        bEarTwitching, _
    '                        bEarsOddAngle, _
    '                        bAbnormalBehaviour, _
    '                        bHeadShyness, _
    '                        bLickingFlank, _
    '                        bLickingNose, _
    '                        bKicking, _
    '                        bReluctantDoorways, _
    '                        bHeadPressing, _
    '                        bHeadRubbing, _
    '                        bTeethGrinding, _
    '                        bBlindness, _
    '                        bCircling, _
    '                        bHindAtaxia, _
    '                        bFalling, _
    '                        bParesis, _
    '                        bForeAtaxia, _
    '                        bRecumbent, _
    '                        bTremor, _
    '                        bKnucklingFetlock, _
    '                        bWeightLoss, _
    '                        bCondition, _
    '                        bMilkYield)) Then
    '                Throw New Exception("Farm.GetByCPHH returned False")
    '                Return False
    '            End If

    '            Session(SessionVars.SV_RBSENumber) = sRBSE
    '            chkApprehension.Checked = bApprehension
    '            chkHypersensitiveTouch.Checked = bHypersensitiveTouch
    '            chkHypersensitiveSound.Checked = bHypersensitiveSound
    '            chkManiacal.Checked = bManiacal
    '            chkPanicStricken.Checked = bPanicStricken
    '            chkTempChange.Checked = bTempChange
    '            chkAbnormalHeadCarriage.Checked = bAbnormalHeadCarriage
    '            chkEarTwitching.Checked = bEarTwitching
    '            chkEarsOddAngle.Checked = bEarsOddAngle
    '            chkAbnormalBehaviour.Checked = bAbnormalBehaviour
    '            chkHeadShyness.Checked = bHeadShyness
    '            chkLickingFlank.Checked = bLickingFlank
    '            chkLickingNose.Checked = bLickingNose
    '            chkKicking.Checked = bKicking
    '            chkReluctantToEnterDoorways.Checked = bReluctantDoorways
    '            chkHeadPressing.Checked = bHeadPressing
    '            chkHeadRubbing.Checked = bHeadRubbing
    '            chkTeethGrinding.Checked = bTeethGrinding
    '            chkBlindness.Checked = bBlindness
    '            chkCircling.Checked = bCircling
    '            chkHindAtaxia.Checked = bHindAtaxia
    '            chkFalling.Checked = bFalling
    '            chkParesis.Checked = bParesis
    '            chkForeAtaxia.Checked = bForeAtaxia
    '            chkRecumbent.Checked = bRecumbent
    '            chkTremor.Checked = bTremor
    '            chkKnucklingFetlock.Checked = bKnucklingFetlock
    '            chkWeightLoss.Checked = bWeightLoss
    '            chkCondition.Checked = bCondition
    '            chkMilkYield.Checked = bMilkYield

    '            Return True
    '        End If

    '        Return False

    '    Catch ex As Exception
    '        clsAppError.DisplayError("Failed to 'Get Clinical Case Details'.", ex)
    '        Return False
    '    End Try
    'End Function

#End Region

End Class
