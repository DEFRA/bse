Partial  Class CPHH
    Inherits System.Web.UI.UserControl

    Private m_strCPHH As String = ""
    Private m_strCounty As String = ""
    Private m_strParish As String = ""
    Private m_strHolding As String = ""
    Private m_strHerd As String = ""

    Public Event CPHHChanged(ByVal sender As System.Object, ByVal e As System.EventArgs)
    
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
        SeparateCPHH()
    End Sub

    Public Sub SetEnableCPH(ByVal bEnableCPH As Boolean)
        If bEnableCPH Then
            revCPHH.ValidationExpression = "\d{2}(/)?\d{3}(/)?\d{4}(/)?(\d{2})?"
        Else
            revCPHH.ValidationExpression = "\d{2}(/)?\d{3}(/)?\d{4}(/)?\d{2}"
        End If
    End Sub

    Public Property Mandatory() As Boolean
        Get
            Return rfvCPHH.Enabled
        End Get
        Set(ByVal bEnable As Boolean)
            rfvCPHH.Enabled = bEnable
        End Set
    End Property

    Public Sub SetREVTooltip(ByVal strValue As String)
        revCPHH.ToolTip = strValue
    End Sub

    Public Sub SetAutopostback(ByVal blnValue As Boolean)
        txtCPHH.AutoPostBack = blnValue
    End Sub

    Public Property Enabled() As Boolean
        Get
            Return txtCPHH.Enabled
        End Get
        Set(ByVal bEnable As Boolean)
            txtCPHH.Enabled = bEnable
            revCPHH.Enabled = bEnable
        End Set
    End Property

    Public Function Validate() As Boolean

        If (txtCPHH.Text.Length() = 0) Then
            ' Validate seems to return True when empty string even though client 
            ' side validator does not allow empty string to be posted so we need
            ' to maually Invalidate an empty string
            Return False
        Else
            revCPHH.Validate()
            Return revCPHH.IsValid()
        End If

    End Function

    Public Function GetCounty() As String
        Return m_strCounty
    End Function

    Public Function GetParish() As String
        Return m_strParish
    End Function

    Public Function GetHolding() As String
        Return m_strHolding
    End Function

    Public Function GetHerd() As String
        Return m_strHerd
    End Function

    Public Property CPHH() As String
        Get
            Return m_strCPHH
        End Get
        Set(ByVal strValue As String)
            txtCPHH.Text = ""

            SeparateCPHH(strValue)
            ConstructCPHH()
        End Set
    End Property

    Public Function GetVetnetCPHH() As String

        SeparateCPHH()
        ConstructCPHH()
        Return txtCPHH.Text()

    End Function

    Private Sub SeparateCPHH(Optional ByVal strTemp As String = "")

        If (strTemp = "") Then
            m_strCPHH = RemoveAlphas(txtCPHH.Text())
        Else
            m_strCPHH = RemoveAlphas(strTemp)
        End If

        If (m_strCPHH.Length() >= 2) Then
            m_strCounty = CStr(Convert.ToInt16(m_strCPHH.Substring(0, 2)))
        Else
            m_strCounty = ""
        End If

        If (m_strCPHH.Length() >= 5) Then
            m_strParish = CStr(Convert.ToInt16(m_strCPHH.Substring(2, 3)))
        Else
            m_strParish = ""
        End If

        If (m_strCPHH.Length() >= 9) Then
            m_strHolding = CStr(Convert.ToInt16(m_strCPHH.Substring(5, 4)))
        Else
            m_strHolding = ""
        End If

        If (m_strCPHH.Length() = 11) Then
            m_strHerd = CStr(Convert.ToInt16(m_strCPHH.Substring(9, 2)))
        Else
            m_strHerd = ""
        End If

    End Sub

    Private Sub ConstructCPHH()

        Dim strTemp As String = ""

        If Not (m_strCounty = "") Then
            strTemp += Right("00" & m_strCounty.ToString(), 2)
        End If

        If Not (m_strParish = "") Then
            strTemp += "/"
            strTemp += Right("000" & m_strParish.ToString(), 3)
        End If

        If Not (m_strHolding = "") Then
            strTemp += "/"
            strTemp += Right("0000" & m_strHolding.ToString(), 4)
        End If

        If Not (m_strHerd = "") Then
            strTemp += "/"
            strTemp += Right("00" & m_strHerd.ToString(), 2)
        End If

        txtCPHH.Text = strTemp

    End Sub

    Private Function RemoveAlphas(Optional ByVal strTemp As String = "") As String

        Dim chrTemp As Char
        Dim strReturn As String = ""

        If (strTemp = "") Then
            strTemp = txtCPHH.Text()
        End If

        For Each chrTemp In strTemp
            If (Char.IsDigit(chrTemp)) Then
                strReturn += chrTemp
            End If
        Next

        Return strReturn

    End Function

    Private Sub txtCPHH_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtCPHH.TextChanged

        RaiseEvent CPHHChanged(Me, Nothing)

    End Sub

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

            txtCPHH.Attributes.Add("onkeydown", Me.ClientID & "_OnKeyDown(document.all(""" & ctlButton.UniqueID & """))")
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
                .Append("       event.returnValue=false;" & vbNewLine)
                .Append("       event.cancel = true;" & vbNewLine)
                .Append("       if ((Form1." & sControlClientID & ")) {" & vbNewLine)
                .Append("           if (!(Form1." & sControlClientID & ".disabled)) {" & vbNewLine)
                .Append("               Form1." & sControlClientID & ".focus()" & vbNewLine)
                .Append("           }" & vbNewLine)
                .Append("       }" & vbNewLine)
                .Append("    } " + vbNewLine)
                .Append("}" + vbNewLine)
                .Append("</SCRIPT>" + vbNewLine)
            End With
            txtCPHH.Attributes.Add("onkeydown", Me.ClientID & "_OnKeyDown()")
            Me.Page.RegisterStartupScript(Me.ClientID & "EnterKey", scr.ToString())
        End If
    End Sub

    Public ReadOnly Property FirstClientID() As String
        Get
            Return txtCPHH.ClientID
        End Get
    End Property

#End Region

End Class
