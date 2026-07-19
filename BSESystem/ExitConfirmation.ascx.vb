Partial  Class ExitConfirmation
    Inherits System.Web.UI.UserControl

    Private Shared bMessageDisplayed As Boolean = False

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

    '*****************************************************
    ' N.B. This control must be contained within a DIV
    ' where id="exitConfirmationDIV"
    '*****************************************************

    Private Sub Page_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        SetConfirmationHandler()
        litConfirmation.Text = ""

        'If Not IsPostBack Then
        bMessageDisplayed = False
        'End If
    End Sub

    Public Sub ShowExitConfirmation()
        Dim strCtrlID As String

        If (bMessageDisplayed = False) Then
            strCtrlID = Me.UniqueID().Replace(":", "_")
            litConfirmation.Text = "<iframe id=""fme_" + strCtrlID + """ name=""" + strCtrlID + """ style=""WIDTH: 204px; HEIGHT: 75px; position:absolute; left:0px;top:27px;visibility=""visible"""" src=""ExitConfirmationPopup.aspx"" frameBorder=""yes"" scrolling=""no""></iframe>"
            bMessageDisplayed = True
        Else
            litConfirmation.Text = ""
            bMessageDisplayed = False
        End If
    End Sub

    Private Sub SetConfirmationHandler()
        If Not Page.IsClientScriptBlockRegistered("SetConfirmationHandler") Then

            If HttpContext.Current.Request.Browser.JavaScript Then
                Dim scr As New System.Text.StringBuilder()

                scr.Append("<SCRIPT language=""javascript"">" + vbNewLine)
                scr.Append("function SetConfirmation(strName, bExit)" + vbNewLine + "{" + vbNewLine)
                scr.Append("    var fme = ""fme_"" + strName;" + vbNewLine)
                scr.Append("    if (bExit) {" + vbNewLine)
                scr.Append("    location.href = 'Home.aspx';" + vbNewLine)
                scr.Append("    }" + vbNewLine)
                scr.Append("    document.all[fme].style.visibility = ""hidden"";" + vbNewLine)
                scr.Append("}" + vbNewLine)
                scr.Append("</SCRIPT>" + vbNewLine)

                Page.RegisterClientScriptBlock("SetConfirmationHandler", scr.ToString())
            End If
        End If
    End Sub
End Class
