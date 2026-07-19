Partial Class PickFarm
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents PickFarmPager As DataGridPager

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

        If IsNothing(Session(SessionVars.SV_CPHHNumber)) Then
            Response.Redirect("SessionError.aspx")
        End If

        VLAHeader1.PageTitle = "Pick A Farm"
        VLAHeader1.BatchNumber = Session.Item(SessionVars.SV_BatchNumber)
        lblCPHH.Text = FormatCPHH(Session.Item(SessionVars.SV_CPHHNumber))
        PickFarmPager.SetGrid(grdPickFarm)
        If Not IsPostBack Then
            PopulateTable()
        End If
        EnableControls()
    End Sub

#Region "Event Handlers"

    Private Sub btnNew_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnNew.Click
        Session.Remove(SessionVars.SV_TempPickFarmList)
        Response.Redirect("NewFarm.aspx")
    End Sub

    Private Sub btnExit_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnExit.Click
        Session.Remove(SessionVars.SV_TempPickFarmList)
        Response.Redirect("CaseEntryFarm.aspx")
    End Sub

    Private Sub grdPickFarm_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles grdPickFarm.SelectedIndexChanged
        btnUseSelected.Enabled = True
    End Sub

    Private Sub btnUseSelected_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnUseSelected.Click
        If grdPickFarm.SelectedIndex >= 0 Then
            Session.Item(SessionVars.SV_CPHHNumber) = grdPickFarm.DataKeys(grdPickFarm.SelectedIndex)
            GetFarmDetailsFromDatabase(Session.Item(SessionVars.SV_CPHHNumber), Session)
            Response.Redirect("CaseEntryFarm.aspx")
        End If
    End Sub

#End Region

#Region "Private Functions"

    Private Sub PopulateTable()

        Try
            Dim sCPH As String = Left$(Replace(Session(SessionVars.SV_CPHHNumber), "/", ""), 9)
            Dim objFarm As New BSELib.clsFarm()
            Dim dtData As DataTable = Nothing

            If Not objFarm.GetFarmsByCPH(sCPH, dtData) Then
                clsAppError.DisplayError("Failed to get similar farms.")
            End If

            Session.Item(SessionVars.SV_TempPickFarmList) = dtData

            grdPickFarm.Enabled = True
            grdPickFarm.DataSource = dtData
            grdPickFarm.DataKeyField = "CPHH"
            grdPickFarm.CurrentPageIndex = 0
            grdPickFarm.SelectedIndex = -1
            grdPickFarm.EditItemIndex = -1
            grdPickFarm.DataBind()

            PickFarmPager.DataTableSessionID = SessionVars.SV_TempPickFarmList
            PickFarmPager.AllowAddNew = False
            PickFarmPager.AllowEdit = False
            PickFarmPager.AllowDelete = False
            PickFarmPager.PageLinkCount = 10
            PickFarmPager.Refresh()

        Catch ex As Exception
            clsAppError.DisplayError("Failed to populate table.", ex)
        End Try
    End Sub

#End Region

#Region "Permissions"

    Private Sub EnableControls()
        VLAHeader1.GetUserDetails()

        Dim sGroupName As String = Session(SessionVars.SV_HeaderGroupName)

        If sGroupName = "DEFRA Viewer" Then
            Response.Redirect("Home.aspx")
        ElseIf sGroupName = "DEFRA Data Entry" Then
            ' Do Nothing
        ElseIf sGroupName = "DEFRA Maintenance" Then
            ' Do Nothing
        ElseIf sGroupName = "VLA Data Entry" Then
            Response.Redirect("Home.aspx")
        ElseIf sGroupName = "VLA Maintenance" Then
            ' Do Nothing
        Else
            Response.Redirect("SearchMenu.aspx")
        End If
    End Sub

#End Region


End Class
