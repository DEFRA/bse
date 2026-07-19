Partial Class NewFarm
    Inherits System.Web.UI.Page
    Protected WithEvents txtAHOCode As System.Web.UI.WebControls.TextBox

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

        If IsNothing(Session(SessionVars.SV_CPHHNumber)) Then
            Response.Redirect("SessionError.aspx")
        End If

        VLAHeader1.PageTitle = "New Farm"
        VLAHeader1.BatchNumber = Session.Item(SessionVars.SV_BatchNumber)
        If Not IsPostBack Then
            lblCPHH.Text = "CPHH: " & FormatCPHH(Session.Item(SessionVars.SV_CPHHNumber))
            LoadDetails()
        End If
        EnableControls()
    End Sub

#Region "Event Handlers"

    Private Sub btnCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancel.Click
        Session.Item(SessionVars.SV_CPHHNumber) = ""
        Response.Redirect("CaseEntryFarm.aspx")
    End Sub

    Private Sub btnCreateFarm_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCreateFarm.Click
        Dim dsData As DataSet = Session.Item(SessionVars.SV_FarmDetails)
        Dim drData As DataRow = dsData.Tables(BSELib.clsFarm.FARM_TABLE).NewRow()

        With drData
            .Item("CPHH") = Session.Item(SessionVars.SV_CPHHNumber)
            .Item("IsDealer") = False

            If chkAcceptVetnetDetails.Checked Then
                .Item("Herdmark1") = lblHerdmarkValue.Text
                .Item("NumericHerdmark1") = lblNumericHerdmarkValue.Text
            End If
            .Item("Parish") = FormatEmptyString(txtParishName.Text)
            .Item("County") = FormatEmptyString(txtCounty.Text)
            .Item("ADNSRegionID") = FormatEmptyString(txtADNSRegionID.Text)
            .Item("AuthorityID") = FormatEmptyString(txtAuthorityID.Text)
            .Item("AuthorityCountyID") = FormatEmptyString(txtAuthorityCountyID.Text)
        End With

        dsData.Tables(BSELib.clsFarm.FARM_TABLE).Rows.Add(drData)

        Response.Redirect("CaseEntryFarm.aspx")
    End Sub

#End Region

#Region "Handle Data"

    Private Sub LoadDetails()
        Dim objFarm As New BSELib.clsFarm()
        Dim sCPHH As String = Session.Item(SessionVars.SV_CPHHNumber)
        Dim sHerdmark As String
        Dim sNumericHerdmark As String

        If Not objFarm.GetVetnetDetails(sCPHH, sHerdmark, sNumericHerdmark) Then
            Throw New Exception("Farm.GetVetnetDetails returned False")
        End If

        If sHerdmark <> "" And sNumericHerdmark <> "" Then
            VetnetPanel.Visible = True
            NotVetnetPanel.Visible = False
            lblHerdmarkValue.Text = sHerdmark
            lblNumericHerdmarkValue.Text = sNumericHerdmark
        Else
            VetnetPanel.Visible = False
            NotVetnetPanel.Visible = True
        End If

        If Not objFarm.GetCountyParishForAuthority(sCPHH, txtParishName.Text, txtCounty.Text, txtADNSRegionID.Text, txtAuthorityID.Text, txtAuthorityCountyID.Text) Then
            Throw New Exception("Farm.GetCountyParishForAuthority returned False")
        End If

        LocalAuthorityPanel.Visible = (txtADNSRegionID.Text = "")

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
