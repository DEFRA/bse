Partial Class UserMaintenance
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents UserPager As DataGridPager

    'Private m_bContinueEditing As Boolean = Nothing

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
        VLAHeader1.PageTitle = "User Maintenance"
        UserPager.SetGrid(grdUser)
        If Not IsPostBack Then
            LoadUsers()
        End If
        EnableControls()
    End Sub

#Region "Grid Handling"

    Private Sub LoadUsers()
        Try
            Dim dtData As DataTable
            Dim objUser As New BSELib.clsUser()

            dtData = objUser.GetUsers()

            If dtData Is Nothing Then Throw New Exception()

            Session.Item(SessionVars.SV_UserTable) = dtData

            ' create a dataview for filtering and sorting
            Dim dv As DataView = dtData.DefaultView
            Session.Item(SessionVars.SV_UserView) = dv

            grdUser.DataSource = dtData
            grdUser.DataKeyField = "ID"
            grdUser.CurrentPageIndex = 0
            grdUser.SelectedIndex = -1
            grdUser.EditItemIndex = -1
            grdUser.DataBind()

            UserPager.DataTableSessionID = SessionVars.SV_UserTable
            UserPager.DataViewSessionID = SessionVars.SV_UserView
            UserPager.PageLinkCount = 10
            UserPager.AllowAddNew = True
            UserPager.AllowEdit = True
            UserPager.AllowDelete = False
            UserPager.Refresh()

        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve the User data", ex)
        End Try
    End Sub

    Private Sub grdUser_ItemDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.DataGridItemEventArgs) Handles grdUser.ItemDataBound
        ' populate template column values here
        Try
            ' set up the checkbox and drop-down columns
            Dim drv As DataRowView = CType(e.Item.DataItem, DataRowView)
            If Not drv Is Nothing Then
                Dim lblUserGroup As Label = Nothing
                Dim ddlUserGroup As DropDownList = Nothing
                Dim chkIsActive As CheckBox = Nothing

                If e.Item.ItemType = ListItemType.EditItem Then
                    ' populate edit mode controls
                    ddlUserGroup = CType(e.Item.FindControl("ddlUserGroupEdit"), DropDownList)
                    chkIsActive = CType(e.Item.FindControl("chkIsActiveEdit"), CheckBox)
                ElseIf e.Item.ItemType = ListItemType.Item _
                OrElse e.Item.ItemType = ListItemType.AlternatingItem _
                OrElse e.Item.ItemType = ListItemType.SelectedItem Then
                    ' populate display mode controls
                    lblUserGroup = CType(e.Item.FindControl("lblUserGroupDisplay"), Label)
                    chkIsActive = CType(e.Item.FindControl("chkIsActiveDisplay"), CheckBox)
                End If

                If Not lblUserGroup Is Nothing Then
                    If Not IsDBNull(drv("UserGroup")) Then
                        lblUserGroup.Text = GetUserGroupType(CInt(drv("UserGroup")))
                    Else
                        lblUserGroup.Text = ""
                    End If
                End If

                If Not chkIsActive Is Nothing AndAlso Not drv Is Nothing Then
                    If IsDBNull(drv("IsActive")) Then
                        chkIsActive.Checked = True
                    Else
                        chkIsActive.Checked = drv("IsActive")
                    End If
                End If

                If Not ddlUserGroup Is Nothing Then
                    If Not IsDBNull(drv("UserGroup")) Then
                        SelectItemInDropDownList(ddlUserGroup, drv("UserGroup"))
                    End If
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to bind template columns in the Users grid", ex)
        End Try
    End Sub

    Private Sub UserPager_RowSave(ByVal sender As Object, ByVal e As BSESystem.DataGridPagerEventArgs) Handles UserPager.RowSave
        Dim ddlUserGroup As DropDownList = CType(e.GridRow.FindControl("ddlUserGroupEdit"), DropDownList)
        e.DataTableRow("UserGroup") = ddlUserGroup.SelectedItem.Value

        Dim chkIsActiveEdit As CheckBox = CType(e.GridRow.FindControl("chkIsActiveEdit"), CheckBox)
        e.DataTableRow("IsActive") = chkIsActiveEdit.Checked
    End Sub

    Private Sub UserPager_DataChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles UserPager.DataChanged
        ' save the data in the DataTable to the database
        Try
            Dim objUser As New BSELib.clsUser()
            Dim dtData As DataTable = Session.Item(SessionVars.SV_UserTable)

            If dtData Is Nothing Then
                Throw New Exception("DataTable not found with session ID " & SessionVars.SV_UserTable)
            End If

            If objUser.SaveUsers(dtData) Then
                dtData.AcceptChanges()
            Else
                If dtData.HasErrors Then
                    ' we have row update errors - tell the data grid to display them
                    UserPager.DisplayRowError(blnShowFullerror:=False)
                    UserPager.AllowAddNew = False
                    UserPager.AllowDelete = False
                    UserPager.AllowEdit = False
                Else
                    Throw New Exception("clsUser.SaveUser returned False")
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to save User data to the database", ex)
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
            Response.Redirect("Home.aspx")
        ElseIf sGroupName = "DEFRA Maintenance" Then
            Response.Redirect("Home.aspx")
        ElseIf sGroupName = "VLA Data Entry" Then
            Response.Redirect("Home.aspx")
        ElseIf sGroupName = "VLA Maintenance" Then
            ' Do Nothing
        Else
            Response.Redirect("SearchMenu.aspx")
        End If
    End Sub

#End Region

#Region "Methods"

    Public Function GetUserGroups() As DataTable
        Dim objUser As New BSELib.clsUser()

        Try
            Return objUser.GetUserGroups()

        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve drop down list", ex)
        End Try
    End Function

    Private Function GetUserGroupType(ByRef sID As Integer) As String

        Try
            Dim dt As DataTable = GetUserGroups()

            If Not dt Is Nothing Then
                Dim dv As New DataView(dt, "", "ID", DataViewRowState.CurrentRows)
                Dim iRow As Integer = dv.Find(sID)
                If iRow >= 0 Then
                    Return dv(iRow).Item("Name").ToString()
                Else
                    Return ""
                End If
            Else
                Return ""
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve the list of User Types", ex)
        End Try

    End Function

#End Region

End Class
