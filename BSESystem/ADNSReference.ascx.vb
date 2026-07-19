Partial  Class ADNSReference
    Inherits System.Web.UI.UserControl


    Public Event ADNSReferenceChanged(ByVal sender As System.Object, ByVal e As System.EventArgs)

    Private mbIsMandatory As Boolean

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

    Public Property Mandatory() As Boolean
        Get
            Return mbIsMandatory
        End Get
        Set(ByVal bValue As Boolean)
            mbIsMandatory = bValue
        End Set
    End Property

    Public Property ADNSReference() As String
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
            If sValue = "" Then
                txtYear.Text = ""
                txtNumber.Text = ""
            Else
                sValue = Replace(sValue, "/", "")
                txtYear.Text = Left$(sValue, 4)
                txtNumber.Text = Right$("00000" & Mid$(sValue, 5), 5)
            End If
        End Set
    End Property

    Public Property ADNSYear() As Integer
        Get
            If IsNumeric(txtYear.Text) Then
                Return CInt(txtYear.Text)
            End If
        End Get
        Set(ByVal iValue As Integer)
            txtYear.Text = CStr(iValue)
        End Set
    End Property

    Public Property ADNSNumber() As Integer
        Get
            If IsNumeric(txtNumber.Text) Then
                Return CInt(txtNumber.Text)
            End If
        End Get
        Set(ByVal iValue As Integer)
            txtNumber.Text = Right$("00000" & CStr(iValue), 5)
        End Set
    End Property

    Public Function Validate() As Boolean
        lblError.Visible = False

        If txtYear.Text = "" AndAlso txtNumber.Text = "" Then
            If mbIsMandatory Then
                lblError.Visible = True
            End If
            Return Not mbIsMandatory
        End If

        revYear.Validate()
        revNumber.Validate()
        If revNumber.IsValid() Then
            txtNumber.Text = Right$("00000" & txtNumber.Text, 5)
        End If
        If revYear.IsValid() And revNumber.IsValid() Then
            Return True
        Else
            Return False
        End If

    End Function

    Private Sub txtNumber_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs)
        RaiseEvent ADNSReferenceChanged(Me, Nothing)
    End Sub

    Private Sub txtYear_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs)
        RaiseEvent ADNSReferenceChanged(Me, Nothing)
    End Sub

    Public Sub SetValidMark(ByVal vbValid As Boolean, Optional ByVal vsTooltip As String = "Please enter an ADNS reference")

        If vbValid Then
            lblError.Visible = False
        Else
            lblError.ToolTip = vsTooltip
            lblError.Visible = True
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
