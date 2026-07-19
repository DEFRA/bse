Partial  Class BatchNumberDisplay
    Inherits System.Web.UI.UserControl

    Private Shared bTableOpen As Boolean

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
        litTable.Text = ""
        btnView.ImageUrl = "Images/btnDown.gif"

        ' Reset Table status
        If Not IsPostBack Then
            bTableOpen = False
        End If
    End Sub

    Private Sub btnView_Click(ByVal sender As System.Object, ByVal e As System.Web.UI.ImageClickEventArgs) Handles btnView.Click

        Dim strCtrlID As String

        If (bTableOpen = False) Then
            strCtrlID = Me.UniqueID().Replace(":", "_")
            litTable.Text = "<iframe id=""fme_" + strCtrlID + """ name=""" + strCtrlID + """ style=""WIDTH: 272px; HEIGHT: 100px; position:absolute; left:0px;top:27px;visibility=""visible"""" src=""BatchNumberDisplayPopup.aspx"" frameBorder=""yes"" scrolling=""yes""></iframe>"
            btnView.ImageUrl = "Images/btnUp.gif"
            bTableOpen = True
        Else
            litTable.Text = ""
            btnView.ImageUrl = "Images/btnDown.gif"
            bTableOpen = False
        End If

    End Sub

End Class
