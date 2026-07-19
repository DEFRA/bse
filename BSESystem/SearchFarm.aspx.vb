Partial Class SearchFarm
    Inherits System.Web.UI.Page
    Protected WithEvents txtCounty As System.Web.UI.WebControls.TextBox
    Protected WithEvents ctlCPHH As CPHH
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents ResultsPager As DataGridPager

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
        VLAHeader1.PageTitle = "Farm Search"
        SetEnterKeys()
        ResultsPager.SetGrid(grdResults)
        ctlCPHH.SetEnableCPH(True)
        If Not IsPostBack Then
            LoadLookupLists()
            grdResults.Visible = False
            ResultsPager.Visible = False
        End If
    End Sub

    Private Sub SetEnterKeys()

        ctlCPHH.SetControlOnEnter(txtOwnerName.ClientID)
        SetTextboxControlOnEnter(txtOwnerName, txtHerdmark.ClientID)
        SetTextboxControlOnEnter(txtHerdmark, txtAddress.ClientID)
        SetTextboxControlOnEnter(txtAddress, txtNumericHerdmark.ClientID)
        SetTextboxControlOnEnter(txtNumericHerdmark, ddlCounty.ClientID)

    End Sub

    Private Sub LoadLookupLists()

        Dim blnResult As Boolean

        Dim objLookup As New BSELib.LookupData()

        Try
            Dim objAHOTable As DataTable = objLookup.GetLookupData(LOOKUP_AHO)
            If Not (objAHOTable Is Nothing) Then
                With ddlAHO
                    .DataSource = objAHOTable
                    .DataValueField = "Code"
                    .DataTextField = "Name"
                    .DataBind()
                End With
                Common.AddItemToDropDownList(ddlAHO)
            End If


            Dim objCountyTable As DataTable = objLookup.GetLookupData(LOOKUP_BSE_COUNTY)
            If Not (objCountyTable Is Nothing) Then
                With ddlCounty
                    .DataSource = objCountyTable
                    .DataValueField = "Code"
                    .DataTextField = "Description"
                    .DataBind()
                End With
                Common.AddItemToDropDownList(ddlCounty)
            End If

            Common.AddItemToDropDownList(ddlDealer)
            Common.AddItemToDropDownList(ddlDealer, "No", "N", 1)
            Common.AddItemToDropDownList(ddlDealer, "Yes", "Y", 2)

        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve 'Farm Search' drop down lists.", ex)
        End Try

    End Sub

    Private Sub btnSearch_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSearch.Click

        Dim objSearch As BSELib.clsSearch
        Dim dtResultsData As DataTable
        Dim dvResultsData As DataView

        lblResultError.Visible = False

        If ctlCPHH.CPHH = "" AndAlso txtOwnerName.Text = "" _
            AndAlso txtAddress.Text = "" AndAlso GetSelectedItemFromDropDownList(ddlCounty) = "" _
            AndAlso txtHerdmark.Text = "" AndAlso txtNumericHerdmark.Text = "" _
            AndAlso GetSelectedItemFromDropDownList(ddlDealer) = "" _
            AndAlso GetSelectedItemFromDropDownList(ddlAHO) = "" Then

            lblError.Visible = True
            grdResults.Visible = False
            ResultsPager.Visible = False
            hlbExcel.Visible = False
            Exit Sub
        End If

        Try
            objSearch.GetFarmSearchResults(ctlCPHH.CPHH, txtOwnerName.Text, txtAddress.Text, GetSelectedItemFromDropDownList(ddlCounty), txtHerdmark.Text, txtNumericHerdmark.Text, GetSelectedItemFromDropDownList(ddlDealer), GetSelectedItemFromDropDownList(ddlAHO), chkIncludeNonGBFarms.Checked, dtResultsData)

            dtResultsData.TableName = "farmsearchresults"
            dvResultsData = dtResultsData.DefaultView

            Session.Item(SessionVars.SV_ExcelExport) = dtResultsData
            Session.Item(SessionVars.SV_ExcelExportView) = dvResultsData

            With grdResults
                .Visible = True
                .DataSource = dtResultsData
                .DataKeyField = "CPHH"
                .CurrentPageIndex = 0
                .SelectedIndex = -1
                .EditItemIndex = -1
                .DataBind()
            End With

            With ResultsPager
                .Visible = True
                .DataTableSessionID = SessionVars.SV_ExcelExport
                .DataViewSessionID = SessionVars.SV_ExcelExportView
                .AllowAddNew = False
                .AllowDelete = False
                .AllowEdit = False
                .PageLinkCount = 10
                .Refresh()
            End With

            hlbExcel.Visible = True
        Catch ex As SqlClient.SqlException
            lblResultError.Visible = True
            lblResultError.Text = ex.Message
        End Try

    End Sub

    Private Sub btnSearchMenu_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSearchMenu.Click
        Response.Redirect("SearchMenu.aspx")
    End Sub
End Class
