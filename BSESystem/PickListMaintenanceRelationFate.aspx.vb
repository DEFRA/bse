Partial Class PickListMaintenanceRelationFate
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents Pager As DataGridPager
    Private m_bContinueEditing As Boolean = Nothing

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
        VLAHeader1.PageTitle = "Pick List Maintenance"
        Pager.SetGrid(grdLookup)
        If Not IsPostBack Then
            LoadLookupLists()
            SelectItemInDropDownList(ddlEditableLookups, LOOKUP_RELATION_FATE)
            RefreshLookupGrid()
        End If
        EnableControls()
    End Sub

#Region "Load Dropdown List"

    Private Sub LoadLookupLists()
        Dim blnResult As Boolean
        Dim objDataTable As DataTable
        Dim objLookup As New BSELib.LookupData()

        Try
            objDataTable = objLookup.ListEditableLookups()
            If Not (objDataTable Is Nothing) Then
                ddlEditableLookups.DataSource = objDataTable
                ddlEditableLookups.DataValueField = "ID"
                ddlEditableLookups.DataTextField = "Description"
                ddlEditableLookups.DataBind()
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve drop down lists.", ex)
        End Try
    End Sub

#End Region

#Region "Load Grid Contents"

    Private Sub RefreshLookupGrid()
        Dim iTableID As Integer

        Try
            iTableID = CType(ddlEditableLookups.SelectedItem.Value, Integer)
            Dim dtLookupData As DataTable
            Dim Lookup As New BSELib.LookupData()
            Dim iCount As Integer

            dtLookupData = Lookup.GetLookupData(iTableID)

            If dtLookupData Is Nothing Then Throw New Exception()

            Session.Item(SessionVars.SV_LookupDataTable) = dtLookupData

            ' create a dataview for filtering and sorting
            Dim dv As DataView = dtLookupData.DefaultView
            Session.Item(SessionVars.SV_LookupDataView) = dv

            grdLookup.DataSource = dtLookupData
            grdLookup.DataKeyField = "ID"
            grdLookup.CurrentPageIndex = 0
            grdLookup.SelectedIndex = -1
            grdLookup.EditItemIndex = -1
            grdLookup.DataBind()

            Pager.DataTableSessionID = SessionVars.SV_LookupDataTable
            Pager.DataViewSessionID = SessionVars.SV_LookupDataView
            Pager.PageLinkCount = 10
            If Session(SessionVars.SV_HeaderGroupName) = "VLA Maintenance" Then
                Pager.AllowAddNew = True
                Pager.AllowEdit = True
                Pager.AllowDelete = True
                Pager.ConfirmDelete = True
            Else
                Pager.AllowAddNew = False
                Pager.AllowEdit = False
                Pager.AllowDelete = False
                Pager.ConfirmDelete = False
            End If
            Pager.Refresh()

        Catch ex As Exception
            Dim sMsg As String
            sMsg = "Failed to retrieve the lookup data in table '" & iTableID & "'"
            clsAppError.DisplayError(sMsg, ex)
        End Try
    End Sub

#End Region

#Region "Event handlers"

    Private Sub ddlEditableLookups_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ddlEditableLookups.SelectedIndexChanged
        Select Case CType(ddlEditableLookups.SelectedItem.Value, Integer)
            Case LOOKUP_AHO
                Response.Redirect("PickListMaintenanceAHO.aspx")
            Case LOOKUP_BREED
                Response.Redirect("PickListMaintenanceBreed.aspx")
            Case LOOKUP_BSE_COUNTY
                Response.Redirect("PickListMaintenanceBSECounty.aspx")
            Case LOOKUP_SUPPLIER
                Response.Redirect("PickListMaintenanceSupplier.aspx")
            Case LOOKUP_TEST_TYPE
                Response.Redirect("PickListMaintenanceTestType.aspx")
            Case LOOKUP_TSE_TESTING_SITE
                Response.Redirect("PickListMaintenanceTSETestingSite.aspx")
            Case LOOKUP_AHRO
                Response.Redirect("PickListMaintenanceAHRO.aspx")
            Case Else
                Response.Redirect("PickListMaintenance.aspx?TableID=" & ddlEditableLookups.SelectedItem.Value)
        End Select

        RefreshLookupGrid()
    End Sub

    Private Sub Pager_DataChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles Pager.DataChanged
        ' save the data in the DataTable to the database
        Try
            If Not m_bContinueEditing Then
                Dim dt As DataTable = CType(Session.Item(SessionVars.SV_LookupDataTable), DataTable)

                If dt Is Nothing Then
                    Throw New Exception("DataTable not found with session ID " & SessionVars.SV_LookupDataTable)
                End If

                Dim iTableID As Integer = CType(ddlEditableLookups.SelectedItem.Value, Integer)

                Dim Lookup As New BSELib.LookupData()

                If Lookup.SaveLookupData(iTableID, dt) Then
                    dt.AcceptChanges()
                Else
                    If dt.HasErrors Then
                        ' we have row update errors - tell the data grid to display them
                        Pager.DisplayRowError()
                    Else
                        Throw New Exception("Lookup.SaveLookupData returned False")
                    End If
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to save look-up data to the database", ex)
        End Try
    End Sub

    Private Sub Pager_RowSave(ByVal sender As Object, ByVal e As BSESystem.DataGridPagerEventArgs) Handles Pager.RowSave
        Dim iCount As Integer
        Dim dtData As DataTable = Session.Item(SessionVars.SV_LookupDataTable)

        Dim cb As CheckBox = CType(e.GridRow.FindControl("cbActiveEdit"), CheckBox)
        e.DataTableRow("IsActive") = cb.Checked

        m_bContinueEditing = False
        For iCount = 0 To dtData.Rows.Count - 1
            If dtData.Rows(iCount)("Code") = e.DataTableRow("Code") Then
                If dtData.Rows(iCount)("ID") <> e.DataTableRow("ID") Then
                    m_bContinueEditing = True
                    Exit For
                End If
            End If
        Next
    End Sub

    Private Sub Pager_BeforeDataChanged(ByVal sender As Object, ByRef e As BSESystem.DataGridPagerEventArgs) Handles Pager.BeforeDataChanged
        e.bCarryOnEditing = m_bContinueEditing
    End Sub

    Private Sub grdLookup_ItemDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.DataGridItemEventArgs) Handles grdLookup.ItemDataBound
        ' populate template column values here
        Try
            ' set up the checkbox column
            Dim drv As DataRowView = CType(e.Item.DataItem, DataRowView)
            Dim cb As CheckBox

            If e.Item.ItemType = ListItemType.EditItem Then
                cb = CType(e.Item.FindControl("cbActiveEdit"), CheckBox)
            ElseIf e.Item.ItemType = ListItemType.Item _
            OrElse e.Item.ItemType = ListItemType.AlternatingItem _
            OrElse e.Item.ItemType = ListItemType.SelectedItem Then
                cb = CType(e.Item.FindControl("cbActiveDisplay"), CheckBox)
            End If

            If Not cb Is Nothing AndAlso Not drv Is Nothing Then
                If IsDBNull(drv("IsActive")) Then
                    cb.Checked = True
                Else
                    cb.Checked = drv("IsActive")
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to bind the check box column in the look-up data grid", ex)
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
            ' Make Read only
        ElseIf sGroupName = "VLA Data Entry" Then
            ' Make Read only
        ElseIf sGroupName = "VLA Maintenance" Then
            ' Do Nothing
        Else
            Response.Redirect("SearchMenu.aspx")
        End If
    End Sub

#End Region

End Class
