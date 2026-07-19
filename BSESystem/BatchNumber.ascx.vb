Partial  Class BatchNumber
    Inherits System.Web.UI.UserControl

    Private m_strHerd As String = ""

    Public Event BatchNumberChanged(ByVal sender As System.Object, ByVal e As System.EventArgs)

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

    End Sub

    Public Property BatchNumber() As String
        Get
            If txtYear.Text = "" Then
                If txtNumber.Text = "" Then
                    Return ""
                Else
                    Return Year(Now()) & "/" & txtNumber.Text
                End If
            Else
                Return txtYear.Text & "/" & txtNumber.Text
            End If
        End Get
        Set(ByVal sValue As String)
            sValue = Replace(sValue, "/", "")
            txtYear.Text = Left$(sValue, 4)
            txtNumber.Text = Mid$(sValue, 5, (Len(sValue) - 4))
        End Set
    End Property

    Public Property Enabled() As Boolean
        Get
            Return txtYear.Enabled
        End Get
        Set(ByVal Value As Boolean)
            txtYear.Enabled = Value
            txtNumber.Enabled = Value
        End Set
    End Property

    Public Function Validate(ByRef iBatchID As Integer) As Boolean
        lblError.Visible = False

        If txtYear.Text = "" And txtNumber.Text = "" Then
            Return True
        End If

        revYear.Validate()
        revNumber.Validate()
        If revYear.IsValid() And revNumber.IsValid() Then
            Dim objBatch As BSELib.clsBatch
            If txtNumber.Text <> "" Then
                If objBatch.GetBatchIDForBatch(txtYear.Text, CInt(txtNumber.Text), iBatchID) Then
                    Return True
                Else
                    lblError.Visible = True
                    Return False
                End If
            End If
        Else
            Return False
        End If

    End Function

    Private Sub txtNumber_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs)
        RaiseEvent BatchNumberChanged(Me, Nothing)
    End Sub

    Private Sub txtYear_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs)
        RaiseEvent BatchNumberChanged(Me, Nothing)
    End Sub

    Public Function ValidateBlank() As Boolean
        lblError.Visible = False

        If txtYear.Text = "" AndAlso txtNumber.Text = "" Then
            lblError.Visible = True
            Return False
        Else
            Return True
        End If
    End Function

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
                .Append("       if (!(btn.disabled)) {" & vbNewLine)
                .Append("           btn.click();" & vbNewLine)
                .Append("       }" & vbNewLine)
                .Append("   } " & vbNewLine)
                .Append("}" & vbNewLine)
                .Append("</SCRIPT>" & vbNewLine)
            End With
            txtYear.Attributes.Add("onkeydown", Me.ClientID & "_OnKeyDown(document.all(""" & ctlButton.UniqueID & """))")
            txtNumber.Attributes.Add("onkeydown", Me.ClientID & "_OnKeyDown(document.all(""" & ctlButton.UniqueID & """))")
            ctlButton.Page.RegisterStartupScript(Me.ClientID & "DefaultButton", scr.ToString())
        End If

    End Sub

    Public Sub SetControlOnEnter(ByVal sControlClientID As String)

        If HttpContext.Current.Request.Browser.JavaScript Then
            Dim scr As New System.Text.StringBuilder()

            With scr
                .Append("<SCRIPT language=""javascript"">" & vbNewLine)
                .Append("function " & Me.ClientID & "_OnKeyDown(bIsYear)" & vbNewLine + "{" + vbNewLine)
                .Append("    if (event.keyCode == 13) {" & vbNewLine)
                .Append("        event.returnValue=false;" & vbNewLine)
                .Append("        event.cancel = true;" & vbNewLine)
                .Append("        if (bIsYear) {" & vbNewLine)
                .Append("           if (!(Form1." & txtNumber.ClientID & ".disabled)) {" & vbNewLine)
                .Append("               Form1." & txtNumber.ClientID & ".focus()" & vbNewLine)
                .Append("           }" & vbNewLine)
                .Append("       } else {" & vbNewLine)
                .Append("           if (!(Form1." & sControlClientID & ".disabled)) {" & vbNewLine)
                .Append("               Form1." & sControlClientID & ".focus()" & vbNewLine)
                .Append("           }" & vbNewLine)
                .Append("       }" & vbNewLine)
                .Append("    } " + vbNewLine)
                .Append("}" + vbNewLine)
                .Append("</SCRIPT>" + vbNewLine)
            End With
            txtYear.Attributes.Add("onkeydown", Me.ClientID & "_OnKeyDown(true)")
            txtNumber.Attributes.Add("onkeydown", Me.ClientID & "_OnKeyDown(false)")
            Me.Page.RegisterStartupScript(Me.ClientID & "EnterKey", scr.ToString())
        End If
    End Sub

    Public ReadOnly Property FirstClientID() As String
        Get
            Return txtYear.ClientID
        End Get
    End Property

#End Region
End Class
