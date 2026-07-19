Partial  Class Eartag
    Inherits System.Web.UI.UserControl

    Public Event EartagChanged(ByVal sender As System.Object, ByVal e As System.EventArgs)

    Private m_Herd As String = ""
    Private m_AnimalComponent As String = ""
    Private m_Country As String = ""

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
        If IsPostBack Then
            chkChanged.Checked = True
        Else
            chkChanged.Checked = False
        End If
        If chkChanged.Checked = True Then
            SeparateEartag()
        End If
    End Sub

#Region "Public Properties"

    Public Property AutoPostBack() As Boolean
        Get
            Return txtEartag.AutoPostBack
        End Get
        Set(ByVal bValue As Boolean)
            txtEartag.AutoPostBack = bValue
        End Set
    End Property

    Public Property Mandatory() As Boolean
        Get
            Return rfvEartag.Enabled
        End Get
        Set(ByVal bValue As Boolean)
            rfvEartag.Enabled = bValue
        End Set
    End Property

    Public Property ValidationEnabled() As Boolean
        Get
            Return cvEartag.Enabled
        End Get
        Set(ByVal bValue As Boolean)
            cvEartag.Enabled = bValue
        End Set
    End Property

    Public Property Enabled() As Boolean
        Get
            Return txtEartag.Enabled
        End Get
        Set(ByVal bValue As Boolean)
            txtEartag.Enabled = bValue
        End Set
    End Property

    Public Property Eartag() As String
        Get
            Return txtEartag.Text
        End Get
        Set(ByVal sValue As String)
            txtEartag.Text = sValue
            SeparateEartag()
        End Set
    End Property

    Public ReadOnly Property Herd() As String
        Get
            Return m_Herd
        End Get
    End Property

    Public ReadOnly Property AnimalComponent() As String
        Get
            Return m_AnimalComponent
        End Get
    End Property

    Public ReadOnly Property Country() As String
        Get
            Return m_Country
        End Get
    End Property

    Public ReadOnly Property Changed() As Boolean
        Get
            Return chkChanged.Checked
        End Get
    End Property

#End Region

#Region "Public Functions"

    Private Sub SeparateEartag()
        Dim objEartagValidation As New EartagValidation()
        Dim sEartagType As String
        Dim sString As String = objEartagValidation.ValidateEarTag(txtEartag.Text.ToUpper(), sEartagType)

        If sEartagType <> "" And txtEartag.Text <> "" Then
            lblType.Visible = True
            lblType.ToolTip = "This eartag is of type " & sEartagType
        Else
            lblType.Visible = False
        End If

        m_AnimalComponent = objEartagValidation.GetAnimalComponent()
        m_Herd = objEartagValidation.GetHerd
        m_Country = objEartagValidation.GetCountry

        If m_AnimalComponent = "" AndAlso m_Herd = "" AndAlso m_Country = "" Then
            m_AnimalComponent = txtEartag.Text.ToUpper()
        End If

    End Sub

    Private Function ValidateEartag(ByRef sEartag As String) As String
        Dim objEartagValidation As New EartagValidation()
        Dim sEartagType As String
        Dim sString As String = objEartagValidation.ValidateEarTag(txtEartag.Text.ToUpper(), sEartagType)

        If sEartagType <> "" And txtEartag.Text <> "" Then
            lblType.Visible = True
            lblType.ToolTip = "This eartag is of type " & sEartagType
        Else
            lblType.Visible = False
        End If

        Return sString
    End Function

    Public Function Validate() As Boolean

        If (txtEartag.Text.Length() = 0) Then
            ' Validate seems to return True when empty string even though client 
            ' side validator does not allow empty string to be posted so we need
            ' to maually Invalidate an empty string
            Return False
        Else
            Dim objEartagValidation As New EartagValidation()
            Dim sEartagType As String
            Dim sString As String = objEartagValidation.ValidateEarTag(txtEartag.Text.ToUpper(), sEartagType)

            If sEartagType <> "" And txtEartag.Text <> "" Then
                lblType.Visible = True
                lblType.ToolTip = "This eartag is of type " & sEartagType
            Else
                lblType.Visible = False
            End If

            If objEartagValidation.GetErrorCode = "" Then
                cvEartag.IsValid = True
                Return True
            Else
                cvEartag.IsValid = False
                Return False
            End If
        End If

    End Function

#End Region

#Region "Event Handlers"

    Private Sub txtEartag_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtEartag.TextChanged
        chkChanged.Checked = True
        RaiseEvent EartagChanged(Me, Nothing)
    End Sub

    Private Sub cvEartag_ServerValidate(ByVal source As Object, ByVal args As System.Web.UI.WebControls.ServerValidateEventArgs) Handles cvEartag.ServerValidate
        Dim objEartagValidation As New EartagValidation()
        Dim sEartagType As String
        Dim sError As String = objEartagValidation.ValidateEarTag(args.Value, sEartagType)

        If objEartagValidation.GetErrorCode = "" Then
            args.IsValid = True
        Else
            args.IsValid = False
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

            txtEartag.Attributes.Add("onkeydown", Me.ClientID & "_OnKeyDown(document.all(""" & ctlButton.UniqueID & """))")
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
            txtEartag.Attributes.Add("onkeydown", Me.ClientID & "_OnKeyDown()")
            Me.Page.RegisterStartupScript(Me.ClientID & "EnterKey", scr.ToString())
        End If
    End Sub

    Public ReadOnly Property FirstClientID() As String
        Get
            Return txtEartag.ClientID
        End Get
    End Property

#End Region
End Class
