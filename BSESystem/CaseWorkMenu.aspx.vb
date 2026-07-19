Public Partial Class CaseWorkMenu
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        VLAHeader1.PageTitle = "Casework Menu"
        EnableControls()
    End Sub


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

#Region "Permissions"

    Private Sub EnableControls()

        VLAHeader1.GetUserDetails()
        Dim sGroupName As String = CStr(Session(SessionVars.SV_HeaderGroupName))


        If sGroupName <> "VLA Maintenance" Then
            Response.Redirect("Home.aspx")
        End If
    End Sub

#End Region



End Class