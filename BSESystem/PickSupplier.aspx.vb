Partial Class PickSupplier
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents SupplierPager As DataGridPager

    Private Const SELECTED_INDEX = 0
    Private Const YEAR_FROM = 1
    Private Const YEAR_TO = 2
    Private Const RATION_TYPE = 3
    Private Const RATION_DESCRIPTION = 4
    Private Const RATION_NAME = 5
    Private Const IS_PREPURCHASE = 6
    Private Const SUPPLIER_ID = 7
    Private Const SUPPLIER_NAME = 8

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
        VLAHeader1.PageTitle = "Pick A Supplier"
        SupplierPager.SetGrid(grdSupplier)
        If Not IsPostBack Then
            If Session.Item(SessionVars.SV_TempSupplierDetails) Is Nothing Then
                Response.Redirect("SessionError.aspx")
            End If
            Dim SupplierDetails As Object
            SupplierDetails = Session.Item(SessionVars.SV_TempSupplierDetails)
            lblSupplier.Text = "An exact match for '" & SupplierDetails(SUPPLIER_NAME) & "' was not found."
            lblSupplierName.Text = SupplierDetails(SUPPLIER_NAME)
            If lblSupplierName.Text = "" Then
                btnNew.Enabled = False
                txtDetails.Enabled = False
            Else
                btnNew.Enabled = True
                txtDetails.Enabled = True
            End If
            PopulateSupplierGrid(SupplierDetails(SUPPLIER_NAME))
        End If
            EnableControls()
    End Sub

#Region "Grid Handling"

    Private Sub PopulateSupplierGrid(ByVal sName As String)

        Dim dtData As DataTable
        Dim objCase As New BSELib.clsCase()

        If Not (objCase.GetPossibleSuppliers(sName, dtData)) Then
            Throw New Exception("Case.GetPossibleSuppliers returned False")
        End If

        Session.Item(SessionVars.SV_TempSupplierTable) = dtData
        Dim dvData As DataView = dtData.DefaultView
        Session.Item(SessionVars.SV_TempSupplierView) = dvData

        grdSupplier.DataSource = dtData
        grdSupplier.DataKeyField = "ID"
        grdSupplier.CurrentPageIndex = 0
        grdSupplier.SelectedIndex = -1
        grdSupplier.EditItemIndex = -1
        grdSupplier.DataBind()

        SupplierPager.DataTableSessionID = SessionVars.SV_TempSupplierTable
        SupplierPager.DataViewSessionID = SessionVars.SV_TempSupplierView
        SupplierPager.AllowAddNew = False
        SupplierPager.AllowEdit = False
        SupplierPager.AllowDelete = False
        SupplierPager.PageLinkCount = 10
        SupplierPager.Refresh()
    End Sub

    Private Sub grdSupplier_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles grdSupplier.SelectedIndexChanged
        btnUseSelected.Enabled = True
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
            ' Do Nothing
        ElseIf sGroupName = "VLA Maintenance" Then
            ' Do Nothing
        Else
            Response.Redirect("SearchMenu.aspx")
        End If
    End Sub

#End Region

#Region "Event Handler"

    Private Sub btnNew_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnNew.Click

        If txtDetails.Text <> "" Then
            Dim objLookup As New BSELib.LookupData()
            Dim dtData As DataTable = Session.Item(SessionVars.SV_TempSupplierTable)
            Dim drRow As DataRow = dtData.NewRow
            Dim SupplierDetails As Object

            SupplierDetails = Session.Item(SessionVars.SV_TempSupplierDetails)

            drRow.Item("ID") = -1
            drRow.Item("Name") = SupplierDetails(SUPPLIER_NAME)
            drRow.Item("Details") = txtDetails.Text
            dtData.Rows.Add(drRow)

            objLookup.SaveSupplier(LOOKUP_SUPPLIER, dtData)

            SupplierDetails = Session.Item(SessionVars.SV_TempSupplierDetails)
            SupplierDetails(SUPPLIER_ID) = dtData.Rows(dtData.Rows.Count - 1)("ID")

            Session.Remove(SessionVars.SV_TempSupplierTable)
            Response.Redirect("CaseEntryFeeds.aspx")
        Else
            lblrfvDescription.Visible = True
        End If
    End Sub

    Private Sub btnUseSelected_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnUseSelected.Click
        Dim drData() As DataRow
        Dim dtData As DataTable = Session.Item(SessionVars.SV_TempSupplierTable)
        Dim iID As Int32 = Convert.ToInt32(grdSupplier.DataKeys(grdSupplier.SelectedIndex))
        Dim sSearch As String = "ID = " & CStr(iID)

        drData = dtData.Select(sSearch)

        Dim SupplierDetails As Object
        SupplierDetails = Session.Item(SessionVars.SV_TempSupplierDetails)

        SupplierDetails(SUPPLIER_ID) = drData(0).Item("ID")
        SupplierDetails(SUPPLIER_NAME) = drData(0).Item("Name")
        Session.Remove(SessionVars.SV_TempSupplierTable)
        Response.Redirect("CaseEntryFeeds.aspx")
    End Sub

    Private Sub btnReturn_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnReturn.Click
        Session.Remove(SessionVars.SV_TempSupplierTable)

        Dim SupplierDetails As Object = Session.Item(SessionVars.SV_TempSupplierDetails)
        SupplierDetails(SUPPLIER_ID) = ""
        SupplierDetails(SUPPLIER_NAME) = ""

        Response.Redirect("CaseEntryFeeds.aspx")
    End Sub

#End Region

End Class
