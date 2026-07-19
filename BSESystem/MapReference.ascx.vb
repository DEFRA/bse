Partial  Class MapReference
    Inherits System.Web.UI.UserControl

    Private m_intChanges As Integer = 0

    Public Event MapReferenceChanged(ByVal sender As System.Object, ByVal e As System.EventArgs)

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
        txtMapReference1.Attributes.Add("onkeyup", "checkLength(this, document.all('" & txtMapReference2.ClientID & "'), 2)")
        txtMapReference2.Attributes.Add("onkeyup", "checkLength(this, document.all('" & txtMapReference3.ClientID & "'), 3)")
        txtMapReference1.Attributes.Add("onkeypress", "onKeyPressUpperCase()")
    End Sub

#Region "Public Functions"

    Public Function Validate() As Boolean
        If (txtMapReference1.Text.Length() = 0) Then
            ' Validate seems to return True when empty string even though client 
            ' side validator does not allow empty string to be posted so we need
            ' to maually Invalidate an empty string
            Return False
        Else
            revMapReference1.Validate()
            revMapReference2.Validate()
            revMapReference3.Validate()
            If revMapReference1.IsValid() And revMapReference2.IsValid() And revMapReference3.IsValid() Then
                Return True
            Else
                Return False
            End If
        End If
    End Function

#End Region

#Region "Public Properties"

    Public Property MapReference() As String
        Get
            If txtMapReference1.Text <> "" And txtMapReference2.Text <> "" And txtMapReference3.Text <> "" Then
                Return txtMapReference1.Text & txtMapReference2.Text & txtMapReference3.Text
            Else
                Return ""
            End If
        End Get
        Set(ByVal strValue As String)
            txtMapReference1.Text = Left$(strValue, 2)
            txtMapReference2.Text = Mid$(strValue, 3, 3)
            txtMapReference3.Text = Mid$(strValue, 6, 3)
        End Set
    End Property

    Public Property Enabled() As Boolean
        Get
            Return txtMapReference1.Enabled
        End Get
        Set(ByVal bEnable As Boolean)
            txtMapReference1.Enabled = bEnable
            txtMapReference2.Enabled = bEnable
            txtMapReference3.Enabled = bEnable
            revMapReference1.Enabled = bEnable
            revMapReference2.Enabled = bEnable
            revMapReference3.Enabled = bEnable
        End Set
    End Property

#End Region

#Region "Private Event Handlers"
    Private Sub txtMapReference1_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs)
        ' Only raise event if the whole map reference has been entered or 
        ' the user is clearing the control
        If txtMapReference2.Text <> "" And txtMapReference3.Text <> "" Then
            RaiseEvent MapReferenceChanged(Me, Nothing)
        ElseIf txtMapReference2.Text = "" And txtMapReference3.Text = "" Then
            RaiseEvent MapReferenceChanged(Me, Nothing)
        End If
    End Sub

    Private Sub txtMapReference2_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs)
        ' Only raise event if the whole map reference has been entered or 
        ' the user is clearing the control
        If txtMapReference1.Text <> "" And txtMapReference3.Text <> "" Then
            RaiseEvent MapReferenceChanged(Me, Nothing)
        ElseIf txtMapReference1.Text = "" And txtMapReference3.Text = "" Then
            RaiseEvent MapReferenceChanged(Me, Nothing)
        End If
    End Sub

    Private Sub txtMapReference3_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles txtMapReference3.TextChanged
        ' Only raise event if the whole map reference has been entered or 
        ' the user is clearing the control
        If txtMapReference1.Text <> "" And txtMapReference2.Text <> "" Then
            RaiseEvent MapReferenceChanged(Me, Nothing)
        ElseIf txtMapReference1.Text = "" And txtMapReference2.Text = "" Then
            RaiseEvent MapReferenceChanged(Me, Nothing)
        End If
    End Sub
#End Region

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

            txtMapReference1.Attributes.Add("onkeydown", Me.ClientID & "_OnKeyDown(document.all(""" & ctlButton.UniqueID & """))")
            txtMapReference2.Attributes.Add("onkeydown", Me.ClientID & "_OnKeyDown(document.all(""" & ctlButton.UniqueID & """))")
            txtMapReference3.Attributes.Add("onkeydown", Me.ClientID & "_OnKeyDown(document.all(""" & ctlButton.UniqueID & """))")
            ctlButton.Page.RegisterStartupScript(Me.ClientID & "DefaultButton", scr.ToString())
        End If

    End Sub

    Public Sub SetControlOnEnter(ByVal sControlClientID As String)

        If HttpContext.Current.Request.Browser.JavaScript Then
            Dim scr As New System.Text.StringBuilder()

            With scr
                .Append("<SCRIPT language=""javascript"">" & vbNewLine)
                .Append("function " & Me.ClientID & "_OnKeyDown(iMapRefBox)" & vbNewLine + "{" + vbNewLine)
                .Append("    if (event.keyCode == 13) {" & vbNewLine)
                .Append("        event.returnValue=false;" & vbNewLine)
                .Append("        event.cancel = true;" & vbNewLine)
                .Append("        switch (iMapRefBox) {" & vbNewLine)
                .Append("           case 1: " & vbNewLine)
                .Append("               if ((Form1." & txtMapReference2.ClientID & ")) {" & vbNewLine)
                .Append("                   if (!(Form1." & txtMapReference2.ClientID & ".disabled)) {" & vbNewLine)
                .Append("                       Form1." & txtMapReference2.ClientID & ".focus()" & vbNewLine)
                .Append("                   }" & vbNewLine)
                .Append("               }" & vbNewLine)
                .Append("               break;" & vbNewLine)
                .Append("           case 2: " & vbNewLine)
                .Append("               if ((Form1." & txtMapReference3.ClientID & ")) {" & vbNewLine)
                .Append("                   if (!(Form1." & txtMapReference3.ClientID & ".disabled)) {" & vbNewLine)
                .Append("                       Form1." & txtMapReference3.ClientID & ".focus()" & vbNewLine)
                .Append("                   }" & vbNewLine)
                .Append("               }" & vbNewLine)
                .Append("               break;" & vbNewLine)
                .Append("           case 3: " & vbNewLine)
                .Append("               if ((Form1." & sControlClientID & ")) {" & vbNewLine)
                .Append("                   if (!(Form1." & sControlClientID & ".disabled)) {" & vbNewLine)
                .Append("                       Form1." & sControlClientID & ".focus()" & vbNewLine)
                .Append("                   }" & vbNewLine)
                .Append("               }" & vbNewLine)
                .Append("               break;" & vbNewLine)
                .Append("        }" & vbNewLine)
                .Append("    } " + vbNewLine)
                .Append("}" + vbNewLine)
                .Append("</SCRIPT>" + vbNewLine)
            End With
            txtMapReference1.Attributes.Add("onkeydown", Me.ClientID & "_OnKeyDown(1)")
            txtMapReference2.Attributes.Add("onkeydown", Me.ClientID & "_OnKeyDown(2)")
            txtMapReference3.Attributes.Add("onkeydown", Me.ClientID & "_OnKeyDown(3)")
            Me.Page.RegisterStartupScript(Me.ClientID & "EnterKey", scr.ToString())
        End If
    End Sub

    Public ReadOnly Property FirstClientID() As String
        Get
            Return txtMapReference1.ClientID
        End Get
    End Property

#End Region

End Class
