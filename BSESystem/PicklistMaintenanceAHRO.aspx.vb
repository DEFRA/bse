Public Partial Class PicklistMaintenanceAHRO
    Inherits System.Web.UI.Page
    Private m_bContinueEditing As Boolean = Nothing

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        VLAHeader1.PageTitle = "Pick List Maintenance"
        Pager.SetGrid(grdLookup)
        If Not IsPostBack Then
            LoadLookupLists()
            SelectItemInDropDownList(ddlEditableLookups, LOOKUP_AHRO.ToString())
            RefreshLookupGrid()
        End If
        EnableControls()
    End Sub

#Region "Load Dropdown list"
    Private Sub ddlEditableLookups_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ddlEditableLookups.SelectedIndexChanged

        Select Case CType(ddlEditableLookups.SelectedItem.Value, Integer)
            Case LOOKUP_AHO
                Response.Redirect("PickListMaintenanceAHO.aspx")
            Case LOOKUP_BREED
                Response.Redirect("PickListMaintenanceBreed.aspx")
            Case LOOKUP_BSE_COUNTY
                Response.Redirect("PickListMaintenanceBSECounty.aspx")
            Case LOOKUP_RELATION_FATE
                Response.Redirect("PickListMaintenanceRelationFate.aspx")
            Case LOOKUP_SUPPLIER
                Response.Redirect("PickListMaintenanceSupplier.aspx")
            Case LOOKUP_TEST_TYPE
                Response.Redirect("PickListMaintenanceTestType.aspx")
            Case LOOKUP_TSE_TESTING_SITE
                Response.Redirect("PickListMaintenanceTSETestingSite.aspx")
            Case Else
                Response.Redirect("PickListMaintenance.aspx?TableID=" & ddlEditableLookups.SelectedItem.Value)
        End Select

        RefreshLookupGrid()
    End Sub

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

#Region "Event handlers"

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

    Private Sub Pager_BeforeDataChanged(ByVal sender As Object, ByRef e As BSESystem.DataGridPagerEventArgs) Handles Pager.BeforeDataChanged
        e.bCarryOnEditing = m_bContinueEditing
    End Sub

#End Region

End Class