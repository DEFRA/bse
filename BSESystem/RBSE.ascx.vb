Partial  Class RBSE
    Inherits System.Web.UI.UserControl

    Public Event RBSEChanged(ByVal sender As System.Object, ByVal e As System.EventArgs)

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

        'SeparateRBSE()

    End Sub

    Public Sub SetMandatory(ByVal blnValue As Boolean)

        rfvRBSE.Enabled = blnValue

    End Sub

    Public Sub SetREVTooltip(ByVal strValue As String)

        revRBSE.ToolTip = strValue

    End Sub

    Public Sub SetAutopostback(ByVal blnValue As Boolean)

        txtRBSE.AutoPostBack = blnValue

    End Sub

    ' Enable RBSE textbox
    ' Default is enabled
    Public Sub SetEnabled(ByVal blnValue As Boolean)

        txtRBSE.Enabled = blnValue

    End Sub

    Public Function Validate() As Boolean

        If (txtRBSE.Text.Length() = 0) Then
            ' Validate seems to return True when empty string even though client 
            ' side validator does not allow empty string to be posted so we need
            ' to maually Invalidate an empty string
            Return False
        Else
            revRBSE.Validate()
            Return revRBSE.IsValid()
        End If

    End Function

    Public Property RBSE() As String
        Get
            Return txtRBSE.Text
        End Get
        Set(ByVal sValue As String)
            txtRBSE.Text = FormatRBSE(sValue)
        End Set
    End Property

    Public ReadOnly Property UnformattedRBSE() As String
        Get
            Return Replace(txtRBSE.Text, "/", "")
        End Get
    End Property

    Public ReadOnly Property IsMarkedValid() As Boolean
        Get
            Return Not lblInvalid.Visible
        End Get
    End Property

    Private Sub txtRBSE_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtRBSE.TextChanged
        txtRBSE.Text = FormatRBSE(txtRBSE.Text)
        RaiseEvent RBSEChanged(Me, Nothing)
    End Sub

    Private Function FormatRBSE(ByVal sRBSE As String) As String
        ' RBSE format = NN/NN/NNNNN
        ' User can enter anything up to A/B which becomes 00/0A/0000B
        Dim iFirstDash As Int32
        Dim iSecondDash As Int32
        Dim sPartOne As String
        Dim sPartTwo As String
        Dim sPartThree As String

        If InStr(sRBSE, "/") = 0 And Len(sRBSE) = 9 Then
            sRBSE = Left$(sRBSE, 2) & "/" & Mid$(sRBSE, 3, 2) & "/" & Mid$(sRBSE, 5, 5)
        End If

        Dim objMatch As System.Text.RegularExpressions.Match = System.Text.RegularExpressions.Regex.Match(sRBSE, "[0-9]?[0-9]?(/)?[0-9X]?[0-9X]/[0-9]?[0-9]?[0-9]?[0-9]?[0-9]")
        If objMatch.Success Then
            iFirstDash = InStr(sRBSE, "/")
            iSecondDash = InStr(iFirstDash + 1, sRBSE, "/")

            If iSecondDash = 0 Then ' User only entered one dash
                If iFirstDash <= 3 Then 'User didn't enter the first part
                    sPartOne = "00"
                ElseIf iFirstDash = 4 Then
                    sPartOne = "0" & Left$(sRBSE, 1)
                Else    'User did enter the first part but without the dash
                    sPartOne = Left$(sRBSE, 2)
                End If
                iSecondDash = iFirstDash
            Else
                If iFirstDash >= 3 Then
                    sPartOne = Mid$(sRBSE, iFirstDash - 2, 2)
                Else
                    sPartOne = "0" & Left$(sRBSE, 1)
                End If
            End If

            If iSecondDash >= 3 Then
                If iSecondDash - iFirstDash = 2 Then
                    sPartTwo = "0" & Mid$(sRBSE, iSecondDash - 1, 1)
                Else
                    sPartTwo = Mid$(sRBSE, iSecondDash - 2, 2)
                End If
            Else
                sPartTwo = "0" & Left$(sRBSE, 1)
            End If

            sPartThree = "00000" & Mid$(sRBSE, (iSecondDash + 1), (Len(sRBSE) - (iSecondDash)))
            sPartThree = Right$(sPartThree, 5)

            Return sPartOne & "/" & sPartTwo & "/" & sPartThree
        End If
        Return sRBSE

    End Function

    Public Sub SetValidMark(ByVal vbValid As Boolean, Optional ByVal vsTooltip As String = "")

        If vbValid Then
            lblInvalid.Visible = False
        Else
            lblInvalid.ToolTip = vsTooltip
            lblInvalid.Visible = True
        End If

    End Sub

#Region "enter key javascript"

    Public Sub SetDefaultButton(ByRef ctlButton As Button)

        If HttpContext.Current.Request.Browser.JavaScript Then
            Dim scr As New System.Text.StringBuilder()

            With scr
                .Append("<SCRIPT language=""javascript"">" & vbNewLine)
                .Append("function " & Me.ClientID & "_OnKeyDown(btn)" & vbNewLine + "{" + vbNewLine)
                .Append("   if (event.keyCode == 13) {" & vbNewLine)
                .Append("       event.returnValue=false;" & vbNewLine)
                .Append("       event.cancel = true;" & vbNewLine)
                .Append("       if ((btn)) {" & vbNewLine)
                .Append("           if (!(btn.disabled)) {" & vbNewLine)
                .Append("               btn.click();" & vbNewLine)
                .Append("           }" & vbNewLine)
                .Append("       }" & vbNewLine)
                .Append("   } " & vbNewLine)
                .Append("}" & vbNewLine)
                .Append("</SCRIPT>" & vbNewLine)
            End With

            txtRBSE.Attributes.Add("onkeydown", Me.ClientID & "_OnKeyDown(document.all(""" & ctlButton.UniqueID & """))")
            ctlButton.Page.RegisterStartupScript(Me.ClientID & "DefaultButton", scr.ToString())
        End If
    End Sub

    Public Sub SetControlOnEnter(ByVal sControlClientID As String)

        If HttpContext.Current.Request.Browser.JavaScript Then
            Dim scr As New System.Text.StringBuilder()

            With scr
                .Append("<SCRIPT language=""javascript"">" & vbNewLine)
                .Append("function " & Me.ClientID & "_OnKeyDown()" & vbNewLine & "{" + vbNewLine)
                .Append("   if (event.keyCode == 13) {" & vbNewLine)
                .Append("       event.returnValue=false;" & vbNewLine)
                .Append("       event.cancel = true;" & vbNewLine)
                .Append("       if ((Form1." & sControlClientID & ")) {" & vbNewLine)
                .Append("           if (!(Form1." & sControlClientID & ".disabled)) {" & vbNewLine)
                .Append("               Form1." & sControlClientID & ".focus()" & vbNewLine)
                .Append("           }" & vbNewLine)
                .Append("       }" & vbNewLine)
                .Append("   } " & vbNewLine)
                .Append("}" & vbNewLine)
                .Append("</SCRIPT>" & vbNewLine)
            End With
            txtRBSE.Attributes.Add("onkeydown", Me.ClientID & "_OnKeyDown()")
            Me.Page.RegisterStartupScript(Me.ClientID & "EnterKey", scr.ToString())
        End If
    End Sub

    Public ReadOnly Property FirstClientID() As String
        Get
            Return txtRBSE.ClientID
        End Get
    End Property

#End Region
End Class
