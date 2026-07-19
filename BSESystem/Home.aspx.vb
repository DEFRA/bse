Partial Class Home
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents RBSE1 As RBSE
    Protected WithEvents BatchNumber1 As BatchNumber

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

        If Not IsPostBack Then
            If CStr(Session(SessionVars.SV_BatchNumber)) <> "" Then
                BatchNumber1.BatchNumber = CStr(Session(SessionVars.SV_BatchNumber))
            End If

            Session.Clear()
        End If
        LoadLatestBatchNumbers()
        RBSE1.SetDefaultButton(btnGo)
        BatchNumber1.SetControlOnEnter(RBSE1.FirstClientID)

        VLAHeader1.PageTitle = "Home"
        GetLatestRBSENumber()
        EnableControls()
    End Sub

    Private Sub LoadLatestBatchNumbers()

        Dim dtData As DataTable

        dtData = BSELib.clsBatch.GetLatestBatchNumbers

        rptBatch.DataSource = dtData
        rptBatch.DataBind()
    End Sub
#Region "Event Handlers"

    Private Sub btnCreateNew_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCreateNew.Click

        Dim iBatchID As Integer
        Dim iBatchYear As Integer
        Dim iBatchNumber As Integer
        Dim objBatch As BSELib.clsBatch

        ' Make label invisible.  This is the only control which doesn't redirect.
        lblRBSEEmpty.Visible = False

        objBatch.CreateBatchNumber(iBatchID, iBatchYear, iBatchNumber)
        BatchNumber1.BatchNumber = CStr(iBatchYear) & "/" & CStr(iBatchNumber)
        Session(SessionVars.SV_BatchNumber) = BatchNumber1.BatchNumber
        Session(SessionVars.SV_BatchID) = iBatchID

        LoadLatestBatchNumbers()

    End Sub

    Private Sub btnGo_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnGo.Click
        Dim sGroupName As String = Session(SessionVars.SV_HeaderGroupName)
        RBSE1.SetValidMark(True)

        Dim sRBSE As String = RBSE1.RBSE()

        Dim iBatchID As Integer
        If Not BatchNumber1.Validate(iBatchID) Then
            Exit Sub
        End If

        Dim sBatchNumber As String = BatchNumber1.BatchNumber

        If sBatchNumber <> "" Then
            Session(SessionVars.SV_BatchNumber) = sBatchNumber
        End If

        If iBatchID <> 0 Then
            Session(SessionVars.SV_BatchID) = iBatchID
        Else
            Session.Remove(SessionVars.SV_BatchID)
        End If

        If sRBSE <> "" Then
            lblRBSEEmpty.Visible = False

            Session(SessionVars.SV_RBSENumber) = sRBSE
            GetCaseDetailsFromDatabase(sRBSE, Session)
            Dim dsData As DataSet = Session.Item(SessionVars.SV_CaseDetails)
            'if the case exists
            If (Not dsData Is Nothing) AndAlso Session.Item(SessionVars.SV_CPHHNumber) <> "" Then

                Dim dtCase As DataTable = CType(dsData.Tables(BSELib.clsCase.CASE_TABLE), DataTable)
                'if the existing case is a non-GB case
                If (dtCase.Rows.Count <> 0) AndAlso CBool((dtCase.Rows(0)("IsNonGBCase")) = True) Then

                    'if the user is in one of the VLA groups, they have permission 
                    'to edit the non-GB case. Get the rest of the details and
                    'redirect to Case Entry
                    If sGroupName = "VLA Data Entry" OrElse sGroupName = "VLA Maintenance" Then
                        GetFarmDetailsFromDatabase(Session.Item(SessionVars.SV_CPHHNumber), Session)
                        GetBatchNumbersFromDatabase(sRBSE, Session)
                        Response.Redirect("CaseEntryFarm.aspx")
                    Else 'otherwise, user does not have permission to edit non-GB case
                        RBSE1.SetValidMark(False, "This is not a GB case")
                        Exit Sub
                    End If

                Else 'the existing case is a GB case

                    GetFarmDetailsFromDatabase(Session.Item(SessionVars.SV_CPHHNumber), Session)
                    GetBatchNumbersFromDatabase(sRBSE, Session)
                    Response.Redirect("CaseEntryFarm.aspx")

                End If '(dtCase.Rows.Count <> 0) AndAlso CBool((dtCase.Rows(0)("IsNonGBCase")) = True)

            Else 'otherwise, the case doesn't exist

                'if the new case is a non-GB case
                If Left$(sRBSE, 5) = "63/00" Or Left$(sRBSE, 5) = "23/00" Then

                    'see if the user has permission to create a non-GB case
                    Select Case sGroupName
                        Case "VLA Maintenance"
                            Response.Redirect("NonGBCaseCreation.aspx")
                        Case "VLA Data Entry"
                            RBSE1.SetValidMark(False, "This case does not exist")
                            Exit Sub
                        Case Else
                            RBSE1.SetValidMark(False, "This is not a GB case")
                            Exit Sub
                    End Select

                Else 'otherwise, the new case is a GB case

                    'DEFRA Viewers and VLA Data Entry cannot create new GB cases
                    If sGroupName = "DEFRA Viewer" OrElse sGroupName = "VLA Data Entry" Then
                        RBSE1.SetValidMark(False, "This case does not exist")
                        Exit Sub
                    Else 'other groups can create new GB cases, but only if the
                        'RBSE doesn't have an 'X' in it
                        If Mid$(sRBSE, 4, 1) = "X" OrElse Mid$(sRBSE, 5, 1) = "X" Then
                            RBSE1.SetValidMark(False, "This RBSE is not valid for a new case")
                            Exit Sub
                        Else
                            GetFarmDetailsFromDatabase(Session.Item(SessionVars.SV_CPHHNumber), Session)
                            GetBatchNumbersFromDatabase(sRBSE, Session)
                            Response.Redirect("CaseEntryFarm.aspx")
                        End If 'Mid$(sRBSE, 4, 1) = "X" OrElse Mid$(sRBSE, 5, 1) = "X"

                    End If 'sGroupName = "DEFRA Viewer" OrElse sGroupName = "VLA Data Entry"

                End If 'Left$(sRBSE, 5) = "63/00" Or Left$(sRBSE, 5) = "23/00" 

            End If '(Not dsData Is Nothing) AndAlso Session.Item(SessionVars.SV_CPHHNumber) <> ""
        Else
            lblRBSEEmpty.Visible = True
        End If 'sRBSE <> ""
    End Sub

    Private Sub hlPrintBatch_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlPrintBatch.Click
        Dim sBatchNumber As String

        sBatchNumber = BatchNumber1.BatchNumber

        If sBatchNumber <> "" Then
            Session(SessionVars.SV_BatchNumber) = sBatchNumber
        End If

        Response.Redirect("PrintBatch.aspx")
    End Sub
#End Region

#Region "Private Functions"

    Private Sub GetLatestRBSENumber()
        Dim objCase As New BSELib.clsCase()
        Dim sYear As String = Right(CStr(Year(Now())), 2)
        Dim sLatestValue As String

        objCase.GetLatestRBSE(sYear, sLatestValue)
        lblCurrentRBSE.Text = FormatRBSE(sLatestValue)

        objCase.GetLatestDBSE(sYear, sLatestValue)
        lblCurrentDBSE.Text = FormatDBSE(sLatestValue)

        sYear = Right(CStr(Year(Now()) - 1), 2)
        objCase.GetLatestRBSE(sYear, sLatestValue)
        lblPreviousRBSE.Text = FormatRBSE(sLatestValue)

        objCase.GetLatestDBSE(sYear, sLatestValue)
        lblPreviousDBSE.Text = FormatDBSE(sLatestValue)
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
        hlExportADNS.Enabled = False
        hlExportADNS.Visible = False
        hlExportOSS.Enabled = False
        hlExportOSS.Visible = False
        hlPrintBatch.Enabled = False
        hlPrintBatch.Visible = False
        hlFinalResultEntry.Enabled = False
        hlFinalResultEntry.Visible = False
        hlCPHHChange.Enabled = False
        hlCPHHChange.Visible = False
        hlRBSEChange.Enabled = False
        hlRBSEChange.Visible = False
        hlMoveCase.Enabled = False
        hlMoveCase.Visible = False
        hlDeleteCase.Enabled = False
        hlDeleteCase.Visible = False
        hlPickListMaintenance.Enabled = False
        hlPickListMaintenance.Visible = False
        hlUserMaintenance.Enabled = False
        hlUserMaintenance.Visible = False
        hlCasework.Enabled = False
        hlCasework.Visible = False
        Panel1.Enabled = False
        Panel1.Visible = False
        Panel2.Enabled = False
        Panel2.Visible = False
    End Sub

    Private Sub DEFRADataEntryEnable()
        hlExportADNS.Enabled = False
        hlExportADNS.Visible = False
        hlExportOSS.Enabled = False
        hlExportOSS.Visible = False
        hlPrintBatch.Enabled = False
        hlPrintBatch.Visible = False
        hlFinalResultEntry.Enabled = False
        hlFinalResultEntry.Visible = False
        hlCPHHChange.Enabled = False
        hlCPHHChange.Visible = False
        hlRBSEChange.Enabled = False
        hlRBSEChange.Visible = False
        hlMoveCase.Enabled = False
        hlMoveCase.Visible = False
        hlDeleteCase.Enabled = False
        hlDeleteCase.Visible = False
        hlPickListMaintenance.Enabled = False
        hlPickListMaintenance.Visible = False
        hlUserMaintenance.Enabled = False
        hlUserMaintenance.Visible = False
        hlCasework.Enabled = False
        hlCasework.Visible = False
        Panel1.Enabled = False
        Panel1.Visible = False
        Panel2.Enabled = False
        Panel2.Visible = False
    End Sub

    Private Sub DEFRAMaintenanceEnable()
        hlExportADNS.Enabled = True
        hlExportADNS.Visible = True
        hlExportOSS.Enabled = False
        hlExportOSS.Visible = False
        hlPrintBatch.Enabled = False
        hlPrintBatch.Visible = False
        hlFinalResultEntry.Enabled = True
        hlFinalResultEntry.Visible = True
        hlCPHHChange.Enabled = True
        hlCPHHChange.Visible = True
        hlRBSEChange.Enabled = True
        hlRBSEChange.Visible = True
        hlMoveCase.Enabled = True
        hlMoveCase.Visible = True
        hlDeleteCase.Enabled = True
        hlDeleteCase.Visible = True
        hlPickListMaintenance.Enabled = True
        hlPickListMaintenance.Visible = True
        hlUserMaintenance.Enabled = False
        hlUserMaintenance.Visible = False
        hlCasework.Enabled = False
        hlCasework.Visible = False
        Panel1.Enabled = True
        Panel1.Visible = True
        Panel2.Enabled = False
        Panel2.Visible = False
    End Sub

    Private Sub VLADataEntryEnable()
        hlExportADNS.Enabled = False
        hlExportADNS.Visible = False
        hlExportOSS.Enabled = True
        hlExportOSS.Visible = True
        hlPrintBatch.Enabled = True
        hlPrintBatch.Visible = True
        hlFinalResultEntry.Enabled = False
        hlFinalResultEntry.Visible = False
        hlCPHHChange.Enabled = False
        hlCPHHChange.Visible = False
        hlRBSEChange.Enabled = False
        hlRBSEChange.Visible = False
        hlMoveCase.Enabled = False
        hlMoveCase.Visible = False
        hlDeleteCase.Enabled = False
        hlDeleteCase.Visible = False
        hlPickListMaintenance.Enabled = True
        hlPickListMaintenance.Visible = True
        hlUserMaintenance.Enabled = False
        hlUserMaintenance.Visible = False
        hlCasework.Enabled = False
        hlCasework.Visible = False
        Panel1.Enabled = True
        Panel1.Visible = True
        Panel2.Enabled = True
        Panel2.Visible = True
    End Sub

    Private Sub VLAMaintenanceEnable()
        hlExportADNS.Enabled = True
        hlExportADNS.Visible = True
        hlExportOSS.Enabled = True
        hlExportOSS.Visible = True
        hlPrintBatch.Enabled = True
        hlPrintBatch.Visible = True
        hlFinalResultEntry.Enabled = True
        hlFinalResultEntry.Visible = True
        hlCPHHChange.Enabled = True
        hlCPHHChange.Visible = True
        hlRBSEChange.Enabled = True
        hlRBSEChange.Visible = True
        hlMoveCase.Enabled = True
        hlMoveCase.Visible = True
        hlDeleteCase.Enabled = True
        hlDeleteCase.Visible = True
        hlPickListMaintenance.Enabled = True
        hlPickListMaintenance.Visible = True
        hlUserMaintenance.Enabled = True
        hlUserMaintenance.Visible = True
        hlCasework.Enabled = True
        hlCasework.Visible = True
        Panel1.Enabled = True
        Panel1.Visible = True
        Panel2.Enabled = True
        Panel2.Visible = True
    End Sub

#End Region


End Class
