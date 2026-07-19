Public Partial Class ThreePartEartag
    Inherits System.Web.UI.UserControl

    Private mbEnabled As Boolean
    Private mbIsMandatory As Boolean
    Private mbAutoPostback As Boolean
    Private mbValidationEnabled As Boolean
    Private mbIsChanged As Boolean

    Public Event EartagCountryChanged(ByVal sender As System.Object, ByVal e As System.EventArgs)
    Public Event EartagHerdmarkChanged(ByVal sender As System.Object, ByVal e As System.EventArgs)
    Public Event EartagAnimalChanged(ByVal sender As System.Object, ByVal e As System.EventArgs)

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

    End Sub

#Region "Properties"

    Public Property Enabled() As Boolean
        Get
            Return mbEnabled
        End Get
        Set(ByVal bValue As Boolean)
            mbEnabled = bValue
            txtEartagCountry.Enabled = bValue
            txtEartagHerdmark.Enabled = bValue
            txtEartagAnimal.Enabled = bValue
        End Set
    End Property

    Public Property Mandatory() As Boolean
        Get
            Return mbIsMandatory
        End Get
        Set(ByVal bValue As Boolean)
            mbIsMandatory = bValue
        End Set
    End Property

    Public Property AutoPostBack() As Boolean
        Get
            Return mbAutoPostback
        End Get
        Set(ByVal bValue As Boolean)
            mbAutoPostback = bValue
            txtEartagCountry.AutoPostBack = bValue
            txtEartagHerdmark.AutoPostBack = bValue
            txtEartagAnimal.AutoPostBack = bValue
        End Set
    End Property

    Public Property ValidationEnabled() As Boolean
        Get
            Return mbValidationEnabled
        End Get
        Set(ByVal bValue As Boolean)
            mbValidationEnabled = bValue
        End Set
    End Property

    Public Property EartagCountry() As String
        Get
            Return txtEartagCountry.Text
        End Get
        Set(ByVal sValue As String)
            If txtEartagCountry.Text <> sValue Then
                mbIsChanged = True
            End If
            txtEartagCountry.Text = sValue
        End Set
    End Property

    Public Property EartagHerdmark() As String
        Get
            Return txtEartagHerdmark.Text
        End Get
        Set(ByVal sValue As String)
            If txtEartagHerdmark.Text <> sValue Then
                mbIsChanged = True
            End If
            txtEartagHerdmark.Text = sValue
        End Set
    End Property

    Public Property EartagAnimal() As String
        Get
            Return txtEartagAnimal.Text
        End Get
        Set(ByVal sValue As String)
            If txtEartagAnimal.Text <> sValue Then
                mbIsChanged = True
            End If
            txtEartagAnimal.Text = sValue
        End Set
    End Property

    Public ReadOnly Property Changed() As Boolean
        Get
            Return mbIsChanged
        End Get
    End Property

    Public ReadOnly Property FirstClientID() As String
        Get
            Return txtEartagCountry.ClientID
        End Get
    End Property
#End Region

#Region "Validation"

    Public Function Validate() As Boolean
        lblError.Visible = False
        lblType.Visible = False

        If txtEartagCountry.Text = "" AndAlso txtEartagHerdmark.Text = "" AndAlso txtEartagAnimal.Text = "" Then
            If mbIsMandatory Then
                lblError.Visible = True
            End If
            Return Not mbIsMandatory
        End If

        Dim objEartag As BSELib.Eartag = BSELib.Eartag.GetEartag(txtEartagCountry.Text, txtEartagHerdmark.Text, txtEartagAnimal.Text)

        If objEartag.ErrorCode = "" Then
            lblType.ToolTip = objEartag.FormatName
            lblType.Visible = True
            Return True
        Else
            lblError.ToolTip = objEartag.ErrorCode
            lblError.Visible = True
            Return False
        End If

    End Function

#End Region

#Region "Enter key JavaScript"

    Public Sub SetControlOnEnter(ByVal sControlClientID As String)

        If HttpContext.Current.Request.Browser.JavaScript Then
            Dim scr As New System.Text.StringBuilder()

            With scr
                .Append("<SCRIPT language=""javascript"">" & vbNewLine)
                .Append("function " & Me.ClientID & "_OnKeyDown(sOriginControl)" & vbNewLine + "{" + vbNewLine)
                .Append("    if (event.keyCode == 13) {" & vbNewLine)
                .Append("        event.returnValue=false;" & vbNewLine)
                .Append("        event.cancel = true;" & vbNewLine)
                .Append("        if (sOriginControl == ""EartagCountry"") {" & vbNewLine)
                .Append("            if (!(Form1." & txtEartagHerdmark.ClientID & ".disabled)) {" & vbNewLine)
                .Append("                Form1." & txtEartagHerdmark.ClientID & ".focus()" & vbNewLine)
                .Append("            }" & vbNewLine)
                .Append("        } else if (sOriginControl == ""EartagHerdmark"") {" & vbNewLine)
                .Append("            if (!(Form1." & txtEartagAnimal.ClientID & ".disabled)) {" & vbNewLine)
                .Append("                Form1." & txtEartagAnimal.ClientID & ".focus()" & vbNewLine)
                .Append("            }" & vbNewLine)
                .Append("        } else {" & vbNewLine)
                .Append("            if (!(Form1." & sControlClientID & ".disabled)) {" & vbNewLine)
                .Append("                Form1." & sControlClientID & ".focus()" & vbNewLine)
                .Append("            }" & vbNewLine)
                .Append("        }" & vbNewLine)
                .Append("    } " + vbNewLine)
                .Append("}" + vbNewLine)
                .Append("</SCRIPT>" + vbNewLine)
            End With
            txtEartagCountry.Attributes.Add("onkeydown", Me.ClientID & "_OnKeyDown(""EartagCountry"")")
            txtEartagHerdmark.Attributes.Add("onkeydown", Me.ClientID & "_OnKeyDown(""EartagHerdmark"")")
            txtEartagAnimal.Attributes.Add("onkeydown", Me.ClientID & "_OnKeyDown(""EartagAnimal"")")

            Me.Page.RegisterStartupScript(Me.ClientID & "EnterKey", scr.ToString())
        End If
    End Sub

#End Region

#Region "Event Handlers"
    Protected Sub txtEartagCountry_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles txtEartagCountry.TextChanged
        RaiseEvent EartagCountryChanged(Me, Nothing)
    End Sub

    Protected Sub txtEartagHerdmark_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles txtEartagHerdmark.TextChanged
        RaiseEvent EartagHerdmarkChanged(Me, Nothing)
    End Sub

    Protected Sub txtEartagAnimal_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles txtEartagAnimal.TextChanged
        RaiseEvent EartagAnimalChanged(Me, Nothing)
    End Sub
#End Region

    Public Sub HideValidation()
        lblError.Visible = False
        lblType.Visible = False
    End Sub

End Class