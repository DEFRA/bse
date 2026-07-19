Imports System.Text.RegularExpressions

Partial Class Help
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

    Private Const HELP_FILE As String = "help.htm"

    Private Sub Page_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

        If Not IsPostBack Then
            Try
                VLAHeader1.PageTitle = "Help"
                VLAHeader1.ShowLinks = False

                Dim sHelpPath As String = MapPath(HELP_FILE)
                Dim fs As New IO.FileStream(sHelpPath, _
                                            IO.FileMode.Open, _
                                            IO.FileAccess.Read, _
                                            IO.FileShare.Read)
                Dim reader As New IO.StreamReader(fs)

                Dim sHelp As String = reader.ReadToEnd()
                reader.Close()

                ' find the body section
                Dim rxBody As New Regex("<\s*body\s*>", RegexOptions.IgnoreCase)
                Dim rxBodyEnd As New Regex("<\s*/body\s*>", RegexOptions.IgnoreCase)

                Dim m As Match = rxBody.Match(sHelp)
                If Not m.Success Then
                    Throw New Exception("<body> tag not found")
                End If

                sHelp = sHelp.Substring(m.Groups(0).Index + m.Groups(0).Length)

                m = rxBodyEnd.Match(sHelp)
                If Not m.Success Then
                    Throw New Exception("</body> tag not found")
                End If

                sHelp = sHelp.Substring(0, m.Groups(0).Index)

                litHelpText.Text = sHelp

            Catch ex As Exception
                clsAppError.DisplayError("Error loading help text from file " & HELP_FILE, ex)
            End Try
        End If

    End Sub

End Class
