Partial  Class PartialDate
    Inherits System.Web.UI.UserControl


    Public Event DateChanged(ByVal sender As System.Object, ByVal e As System.EventArgs)

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
        lblError.Visible = False
    End Sub

#Region "Public Properties"

    Public Property Mandatory() As Boolean
        Get
            Return rfvYear.Enabled
        End Get
        Set(ByVal bEnable As Boolean)
            rfvYear.Enabled = bEnable
        End Set
    End Property

    Public Property Enabled() As Boolean
        Get
            Return txtYear.Enabled
        End Get
        Set(ByVal bEnable As Boolean)
            txtDay.Enabled = bEnable
            txtMonth.Enabled = bEnable
            txtYear.Enabled = bEnable
        End Set
    End Property

    Public Property Value() As Date
        Get
            Return FormatToDate()
        End Get
        Set(ByVal dValue As Date)
            txtDay.Text = Day(dValue)
            txtMonth.Text = Month(dValue)
            txtYear.Text = Year(dValue)
        End Set
    End Property

    Public Property DayValue() As String
        Get
            Return txtDay.Text
        End Get
        Set(ByVal sValue As String)
            txtDay.Text = sValue
        End Set
    End Property

    Public Property MonthValue() As String
        Get
            Return txtMonth.Text
        End Get
        Set(ByVal sValue As String)
            txtMonth.Text = sValue
        End Set
    End Property

    Public Property YearValue() As String
        Get
            Return txtYear.Text
        End Get
        Set(ByVal sValue As String)
            txtYear.Text = sValue
        End Set
    End Property

#End Region

#Region "Public Functions"

    Public Function Validate() As Boolean

        Dim dDate As Date

        Try
            If (txtYear.Text.Length() = 0) Then
                Return True
            End If

            dDate = FormatToDate()
            If dDate = Nothing Then
                lblError.Visible = True
                Return False
            End If
            Return True
        Catch

        End Try
    End Function

    Public Function Validate(ByRef dDate As Date) As Boolean
        Try
            If (txtYear.Text.Length() = 0) Then
                dDate = Nothing
                Return True
            End If

            dDate = FormatToDate()
            If dDate = Nothing Then
                lblError.Visible = True
                Return False
            End If
            Return True
        Catch

        End Try
    End Function

    Public Function Validate(ByRef dDate As Date, ByVal dEarliestDate As Date, ByVal dLatestDate As Date) As Boolean
        If (txtYear.Text.Length() = 0) Then
            dDate = Nothing
            Return True
        End If

        dDate = FormatToDate()
        If dDate >= dEarliestDate And dDate <= dLatestDate Then
            Return True
        End If
        lblError.Visible = True
        Return False
    End Function

#End Region

#Region "Private Functions"

    Private Function FormatToDate() As Date
        Try
            Dim sDate As String

            If txtDay.Text = "" Then
                sDate = "01"
            Else
                sDate = txtDay.Text
            End If
            sDate = sDate & "/"

            If txtMonth.Text = "" Then
                If txtDay.Text <> "" Then
                    lblError.ToolTip = "Please enter a month, or remove the day"
                    Return Nothing
                End If
                sDate = sDate & "01"
            Else
                sDate = sDate & txtMonth.Text
            End If
            sDate = sDate & "/"

            If txtYear.Text = "" Then
                Return Nothing
            Else
                sDate = sDate & txtYear.Text
            End If

            Return Convert.ToDateTime(sDate)
        Catch
            Return Nothing
        End Try
    End Function

#End Region

#Region "Event Handlers"

    Private Sub txtDay_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtDay.TextChanged
        RaiseEvent DateChanged(Me, Nothing)
    End Sub

    Private Sub txtMonth_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtMonth.TextChanged
        RaiseEvent DateChanged(Me, Nothing)
    End Sub

    Private Sub txtYear_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtYear.TextChanged
        RaiseEvent DateChanged(Me, Nothing)
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
                .Append("       if (!(btn.disabled)) {" & vbNewLine)
                .Append("           btn.click();" & vbNewLine)
                .Append("       }" & vbNewLine)
                .Append("   } " & vbNewLine)
                .Append("}" & vbNewLine)
                .Append("</SCRIPT>" & vbNewLine)
            End With

            txtDay.Attributes.Add("onkeydown", Me.ClientID & "_OnKeyDown(document.all(""" & ctlButton.UniqueID & """))")
            txtMonth.Attributes.Add("onkeydown", Me.ClientID & "_OnKeyDown(document.all(""" & ctlButton.UniqueID & """))")
            txtYear.Attributes.Add("onkeydown", Me.ClientID & "_OnKeyDown(document.all(""" & ctlButton.UniqueID & """))")
            ctlButton.Page.RegisterStartupScript(Me.ClientID & "DefaultButton", scr.ToString())
        End If

    End Sub

    Public Sub SetControlOnEnter(ByVal sControlClientID As String)

        If HttpContext.Current.Request.Browser.JavaScript Then
            Dim scr As New System.Text.StringBuilder()

            With scr
                .Append("<SCRIPT language=""javascript"">" & vbNewLine)
                .Append("function " & Me.ClientID & "_OnKeyDown(iDateBox)" & vbNewLine + "{" + vbNewLine)
                .Append("    if (event.keyCode == 13) {" & vbNewLine)
                .Append("        event.returnValue=false;" & vbNewLine)
                .Append("        event.cancel = true;" & vbNewLine)
                .Append("        switch (iDateBox) {" & vbNewLine)
                .Append("           case 1: " & vbNewLine)
                .Append("               if ((Form1." & txtMonth.ClientID & ")) {" & vbNewLine)
                .Append("                   if (!(Form1." & txtMonth.ClientID & ".disabled)) {" & vbNewLine)
                .Append("                       Form1." & txtMonth.ClientID & ".focus()" & vbNewLine)
                .Append("                   }" & vbNewLine)
                .Append("               }" & vbNewLine)
                .Append("               break;" & vbNewLine)
                .Append("           case 2: " & vbNewLine)
                .Append("               if ((Form1." & txtYear.ClientID & ")) {" & vbNewLine)
                .Append("                   if (!(Form1." & txtYear.ClientID & ".disabled)) {" & vbNewLine)
                .Append("                       Form1." & txtYear.ClientID & ".focus()" & vbNewLine)
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
            txtDay.Attributes.Add("onkeydown", Me.ClientID & "_OnKeyDown(1)")
            txtMonth.Attributes.Add("onkeydown", Me.ClientID & "_OnKeyDown(2)")
            txtYear.Attributes.Add("onkeydown", Me.ClientID & "_OnKeyDown(3)")
            Me.Page.RegisterStartupScript(Me.ClientID & "EnterKey", scr.ToString())
        End If
    End Sub

    Public ReadOnly Property FirstClientID() As String
        Get
            Return txtDay.ClientID
        End Get
    End Property

#End Region
End Class
