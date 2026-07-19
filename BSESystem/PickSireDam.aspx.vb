Partial Class PickSireDam
    Inherits System.Web.UI.Page
    Protected WithEvents PickSireDamPager As DataGridPager
    Protected WithEvents VLAHeader1 As VLAHeader

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

        If Request.QueryString("sex") = "" Then
            Response.Redirect("SessionError.aspx")
        End If

        If Request.QueryString("sex") = "F" Then
            VLAHeader1.PageTitle = "Pick A Dam"
        Else
            VLAHeader1.PageTitle = "Pick A Sire"
        End If
        VLAHeader1.BatchNumber = Session.Item(SessionVars.SV_BatchNumber)
        lblEartag.Text = "Eartag: " & Request.QueryString("eartag")
        lblName.Text = "Name: " & Request.QueryString("name")
        lblHerdbook.Text = "Herdbook: " & Request.QueryString("herdbook")
        PickSireDamPager.SetGrid(grdPickSireDam)
        If Not IsPostBack Then
            PopulateTable()
        End If
        EnableControls()
    End Sub

#Region "Event Handlers"

    Private Sub btnNew_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnNew.Click

        Dim iAnimalTableID As Integer

        If Request.QueryString("sex") = "F" Then
            iAnimalTableID = BSELib.clsCase.DAM_DETAILS_TABLE
        Else
            iAnimalTableID = BSELib.clsCase.SIRE_DETAILS_TABLE
        End If

        Dim dsData As DataSet = Session.Item(SessionVars.SV_CaseDetails)
        Try
            If Not dsData Is Nothing Then
                Dim drTargetRow As DataRow
                drTargetRow = dsData.Tables(iAnimalTableID).Rows(0)
                    
                With drTargetRow
                    .Item("ID") = 0
                    .Item("Eartag") = Request.QueryString("eartag")
                    .Item("Name") = Request.QueryString("name")
                    .Item("Herdbook") = Request.QueryString("herdbook")
                End With
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to store the details for the new dam or sire.", ex)
        End Try
        Session.Remove(SessionVars.SV_SireDamMatchesTable)
        Response.Redirect("CaseEntryRelations.aspx")
    End Sub

    Private Sub btnExit_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnExit.Click
        Session.Remove(SessionVars.SV_SireDamMatchesTable)
        Response.Redirect("CaseEntryRelations.aspx")
    End Sub

    Private Sub grdPickSireDam_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles grdPickSireDam.SelectedIndexChanged
        btnUseSelected.Enabled = True
    End Sub

    Private Sub btnUseSelected_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnUseSelected.Click
        If grdPickSireDam.SelectedIndex >= 0 Then

            Dim iAnimalTableID As Integer

            'work out whether we are updating the dam or the sire
            If Request.QueryString("sex") = "F" Then
                iAnimalTableID = BSELib.clsCase.DAM_DETAILS_TABLE
            Else
                iAnimalTableID = BSELib.clsCase.SIRE_DETAILS_TABLE
            End If

            Dim dsData As DataSet = Session.Item(SessionVars.SV_CaseDetails)

            'find the row in the damsire matches table from which we will get the data 
            Dim drData() As DataRow
            Dim dtData As DataTable = Session.Item(SessionVars.SV_SireDamMatchesTable)
            Dim iID As Int32 = Convert.ToInt32(grdPickSireDam.DataKeys(grdPickSireDam.SelectedIndex))
            Dim sSearch As String = "ID = " & CStr(iID)

            drData = dtData.Select(sSearch)

            Try
                If Not dsData Is Nothing Then
                    Dim drTargetRow As DataRow
                    drTargetRow = dsData.Tables(iAnimalTableID).Rows(0)
                    With drTargetRow
                        .Item("ID") = drData(0).Item("ID")
                        .Item("Eartag") = drData(0).Item("Eartag")
                        .Item("Name") = drData(0).Item("Name")
                        .Item("RBSE") = drData(0).Item("RBSE")
                        .Item("Herdbook") = drData(0).Item("Herdbook")
                        .Item("BirthDay") = drData(0).Item("BirthDay")
                        .Item("BirthMonth") = drData(0).Item("BirthMonth")
                        .Item("BirthYear") = drData(0).Item("BirthYear")
                        .Item("Fate") = drData(0).Item("Fate")
                        .Item("ChildCount") = drData(0).Item("ChildCount")
                        If iAnimalTableID = BSELib.clsCase.DAM_DETAILS_TABLE Then
                            .Item("FinalResult") = drData(0).Item("FinalResult")
                        End If
                        .Item("RowStamp") = drData(0).Item("RowStamp")

                    End With
                End If
            Catch ex As Exception
                clsAppError.DisplayError("Failed to store the details for the chosen dam or sire.", ex)
            End Try
            Session.Remove(SessionVars.SV_SireDamMatchesTable)
            Response.Redirect("CaseEntryRelations.aspx")
        End If
    End Sub

#End Region

#Region "Private Functions"

    Private Sub PopulateTable()

        Try
            With grdPickSireDam
                .Enabled = True
                .DataSource = Session(SessionVars.SV_SireDamMatchesTable)
                .DataKeyField = "ID"
                .CurrentPageIndex = 0
                .SelectedIndex = -1
                .EditItemIndex = -1
                .DataBind()
            End With
            With PickSireDamPager
                .DataTableSessionID = SessionVars.SV_SireDamMatchesTable
                .DataViewSessionID = SessionVars.SV_SireDamMatchesView
                .AllowAddNew = False
                .AllowEdit = False
                .AllowDelete = False
                .PageLinkCount = 10
                .Refresh()
            End With
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
            'Response.Redirect("Home.aspx")
        ElseIf sGroupName = "DEFRA Data Entry" Then
            ' Do Nothing
        ElseIf sGroupName = "DEFRA Maintenance" Then
            ' Do Nothing
        ElseIf sGroupName = "VLA Data Entry" Then
            'Response.Redirect("Home.aspx")
        ElseIf sGroupName = "VLA Maintenance" Then
            ' Do Nothing
        Else
            Response.Redirect("SearchMenu.aspx")
        End If
    End Sub

#End Region

    Public Function FormatRBSE(ByVal sRBSE As String) As String

        If sRBSE <> "" Then
            sRBSE = Left$(sRBSE, 2) & "/" & Mid$(sRBSE, 3, 2) & "/" & Mid$(sRBSE, 5, 5)
        End If

        Return sRBSE

    End Function

   
End Class
