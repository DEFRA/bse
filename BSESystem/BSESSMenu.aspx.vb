Partial Class BSESSMenu
    Inherits System.Web.UI.Page
    Protected WithEvents hlNewFarms As System.Web.UI.WebControls.HyperLink
    Protected WithEvents hlCPHHChanges As System.Web.UI.WebControls.HyperLink
    Protected WithEvents hlRBSEChanges As System.Web.UI.WebControls.HyperLink
    Protected WithEvents hlCaseMovesList As System.Web.UI.WebControls.HyperLink
    Protected WithEvents hlAuditLogByUser As System.Web.UI.WebControls.HyperLink
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
        VLAHeader1.PageTitle = "BSESS Check Menu"
        EnableControls()
    End Sub

#Region "Permissions"

    Private Sub EnableControls()

        VLAHeader1.GetUserDetails()
        Dim sGroupName As String = Session(SessionVars.SV_HeaderGroupName)


        If sGroupName <> "DEFRA Viewer" And sGroupName <> "DEFRA Data Entry" And sGroupName <> "DEFRA Maintenance" And sGroupName <> "VLA Data Entry" And sGroupName <> "VLA Maintenance" Then
            Response.Redirect("SearchMenu.aspx")
        End If
    End Sub

#End Region

End Class
