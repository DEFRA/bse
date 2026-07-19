Partial Class AppError
    Inherits System.Web.UI.Page
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
        'Put user code to initialize the page here
        If Not IsPostBack Then
            VLAHeader1.PageTitle = "Application Error"

            Dim sMsg As String = Session("ErrorMessage")
            If sMsg = "" Then
                sMsg = "An unknown error occurred"
            End If
            lblMessage.Text = sMsg

            Try
                Dim ex As Exception = CType(Session.Item("ErrorException"), Exception)
                If Not ex Is Nothing Then
                    lblAdditionalCaption.Visible = True
                    lblAdditional.Visible = True
                    lblAdditional.Text = ex.Message & "<br>Source: " _
                                        & ex.Source & "<br>Stack trace:" _
                                        & "<br>" & ex.StackTrace
                End If
            Catch
            End Try

        Else
            If txtUserInfo.Text <> "" Then
                clsAppError.LogMessage("User-entered information:" & vbNewLine & txtUserInfo.Text, BSELib.clsLog.LogType.ltInformation)
            End If
        End If

    End Sub

End Class
