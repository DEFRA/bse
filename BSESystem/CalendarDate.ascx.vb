Partial  Class CalendarDate
    Inherits System.Web.UI.UserControl

    Private Shared blnCalendarOpen As Boolean
    Private Shared blnAllowUnknown As Boolean = False
    Private Shared strUnknown As String = "unknown"
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

        SetCalendarDateHandler()

        lblError.Visible = False

        litCalendar.Text = ""
        btnCalendar.ImageUrl = "Images/calendarOpenDown.gif"

        ' Reset calendar status
        If Not IsPostBack Then
            blnCalendarOpen = False
        End If

        ' If calendar iframe has been closed by javascript then set appropriately
        If Not (txtClosedCalendar.Text() = "") Then
            blnCalendarOpen = False
            txtClosedCalendar.Text = ""
        End If
    End Sub

#Region "Private Methods"

    Private Sub SetCalendarDateHandler()
        If Not Page.IsClientScriptBlockRegistered("SetCalendarDateHandler") Then

            If HttpContext.Current.Request.Browser.JavaScript Then
                Dim scr As New System.Text.StringBuilder()

                scr.Append("<SCRIPT language=""javascript"">" + vbNewLine)
                scr.Append("function SetDate(strCtrl, strDate)" + vbNewLine + "{" + vbNewLine)
                scr.Append("    var btn = strCtrl + ""_btnCalendar"";" + vbNewLine)
                scr.Append("    var fme = ""fme_"" + strCtrl;" + vbNewLine)
                scr.Append("    var txtDate = strCtrl + ""_txtDate"";" + vbNewLine)
                scr.Append("    var txtClosed = strCtrl + ""_txtClosedCalendar"";" + vbNewLine)
                scr.Append("    var txtClosed = strCtrl + ""_txtClosedCalendar"";" + vbNewLine + vbNewLine)
                scr.Append("    document.all[fme].style.visibility = ""hidden"";" + vbNewLine)
                scr.Append("    document.all[btn].src=""Images/calendarOpenDown.gif"";" + vbNewLine)
                scr.Append("    document.all[txtClosed].value = ""changed"";" + vbNewLine)
                scr.Append("    document.all[txtDate].value = strDate;" + vbNewLine)
                If txtDate.AutoPostBack = True Then
                    scr.Append("    __doPostBack(strCtrl, '');" + vbNewLine)
                End If

                scr.Append("}" + vbNewLine)
                scr.Append("</SCRIPT>" + vbNewLine)

                Page.RegisterClientScriptBlock("SetCalendarDateHandler", scr.ToString())
            End If
        End If
    End Sub

    Private Sub cvDate_ServerValidate(ByVal source As Object, ByVal args As System.Web.UI.WebControls.ServerValidateEventArgs) Handles cvDate.ServerValidate

        If (AllowUnknown = True And txtDate.Text.ToLower = strUnknown) Then
            args.IsValid = True
        Else
            args.IsValid = IsDate(args.Value)
        End If
    End Sub

#End Region

#Region "Event Handlers"

    Private Sub btnCalendar_Click(ByVal sender As System.Object, ByVal e As System.Web.UI.ImageClickEventArgs) Handles btnCalendar.Click

        Dim strCtrlID As String

        If txtDate.Enabled Then
            If (blnCalendarOpen = False) Then
                Dim dDate As Date
                Try
                    If (AllowUnknown = True And txtDate.Text.ToLower = strUnknown) Then
                        dDate = Now()
                    Else
                        dDate = CDate(txtDate.Text)
                    End If
                Catch
                    dDate = Now()
                End Try
                strCtrlID = Me.UniqueID().Replace(":", "_")
                litCalendar.Text = "<iframe id=""fme_" + strCtrlID + """ name=""" + strCtrlID + """ style=""WIDTH: 207px; HEIGHT: 194px; position:absolute; left:0px;top:27px;visibility=""visible"""" src=""CalendarPopup.aspx?date=" & dDate & """ frameBorder=""no"" scrolling=""no""></iframe>"
                btnCalendar.ImageUrl = "Images/calendarCloseUp.gif"
                blnCalendarOpen = True
            Else
                litCalendar.Text = ""
                btnCalendar.ImageUrl = "Images/calendarOpenDown.gif"
                blnCalendarOpen = False
            End If
        End If

    End Sub

    Private Sub txtDate_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtDate.TextChanged
        RaiseEvent DateChanged(Me, Nothing)
    End Sub

#End Region

#Region "Public Properties"

    Public Property DateField() As String
        Get
            If (AllowUnknown = True And txtDate.Text.ToLower = strUnknown) Then
                Return txtDate.Text
            Else
                Return FormattedDate(txtDate.Text)
            End If
        End Get
        Set(ByVal sValue As String)
            If (AllowUnknown = True And txtDate.Text.ToLower = strUnknown) Then
                txtDate.Text = sValue
            Else
                txtDate.Text = FormattedDate(sValue)
            End If
        End Set
    End Property

    Public Property AllowUnknown() As Boolean
        Get
            If ViewState("AllowUnknown") Is Nothing Then
                Return False
            Else
                Return CBool(ViewState("AllowUnknown"))
            End If
        End Get
        Set(ByVal value As Boolean)
            'blnAllowUnknown = value
            ViewState("AllowUnknown") = value
        End Set
    End Property

    Private Function FormattedDate(ByVal sDateText As String) As String

        If Not IsDate(sDateText) Then
            Return sDateText
        End If
        Dim dDate As Date = CDate(sDateText)
        Dim sDate As New System.Text.StringBuilder()
        Dim sDay As String = CStr(Day(dDate))
        Dim sMonth As String = CStr(Month(dDate))
        Dim sYear As String = CStr(Year(dDate))

        If Len(sDay) = 1 Then
            sDate.Append("0")
        End If

        sDate.Append(sDay)
        sDate.Append("/")

        If Len(sMonth) = 1 Then
            sDate.Append("0")
        End If

        sDate.Append(sMonth)
        sDate.Append("/")
        sDate.Append(sYear)

        Return sDate.ToString()

    End Function

    Public Property Enabled() As Boolean
        Get
            Return txtDate.Enabled
        End Get
        Set(ByVal bValue As Boolean)
            If bValue Then
                btnCalendar.ImageUrl = "Images/calendarOpenDown.gif"
            Else
                btnCalendar.ImageUrl = "Images/calendarDisabled.gif"
            End If
            txtDate.Enabled = bValue
            'rfvDate.Enabled = bValue
            cvDate.Enabled = bValue
            btnCalendar.Enabled = bValue
        End Set
    End Property

    Public Property Mandatory() As Boolean
        Get
            Return rfvDate.Enabled
        End Get
        Set(ByVal bValue As Boolean)
            rfvDate.Enabled = bValue
        End Set
    End Property

    Public Property AutoPostBack() As Boolean
        Get
            Return txtDate.AutoPostBack
        End Get
        Set(ByVal bValue As Boolean)
            txtDate.AutoPostBack = bValue
        End Set
    End Property

#End Region

#Region "Public Functions"

    Public Function Validate(ByRef dDate As Date) As Boolean
        Try
            If (txtDate.Text.Length() = 0) Then
                dDate = Nothing
                Return True
            End If

            txtDate.Text = FormattedDate(txtDate.Text)
            dDate = Convert.ToDateTime(txtDate.Text())
            Return True
        Catch
            lblError.Visible = True
            Return False
        End Try
    End Function

    Public Enum ValidationType As Integer
        eValidateEarliest = 1
        eValidateLatest = 2
    End Enum

    Public Function Validate(ByRef dDate As Date, ByVal dCheckDate As Date, ByVal iValidationType As ValidationType, Optional ByVal sErrorText As String = "Invalid Date") As Boolean
        Try
            If (txtDate.Text.Length() = 0) Then
                dDate = Nothing
                Return True
            End If

            txtDate.Text = FormattedDate(txtDate.Text)
            If iValidationType = ValidationType.eValidateEarliest Then
                dDate = Convert.ToDateTime(txtDate.Text())
                If dDate >= dCheckDate Then
                    Return True
                End If
            Else
                dDate = Convert.ToDateTime(txtDate.Text())
                If dDate <= dCheckDate Then
                    Return True
                End If
            End If
            lblError.ToolTip = sErrorText
            lblError.Visible = True
            Return False
        Catch
            lblError.ToolTip = "Please enter a valid date"
            lblError.Visible = True
            Return False
        End Try
    End Function

    Public Function Validate(ByRef dDate As Date, ByVal dEarliestDate As Date, ByVal dLatestDate As Date, Optional ByVal sErrorText As String = "Invalid Date") As Boolean
        Try
            If (txtDate.Text.Length() = 0) Then
                dDate = Nothing
                Return True
            End If

            dDate = Convert.ToDateTime(txtDate.Text())
            If dDate >= dEarliestDate And dDate <= dLatestDate Then
                Return True
            End If
            lblError.ToolTip = sErrorText
            lblError.Visible = True
            Return False
        Catch
            lblError.ToolTip = "Please enter a valid date"
            lblError.Visible = True
            Return False
        End Try
    End Function

    Public Function Validate(ByVal dEarliestDate As Date, ByVal dLatestDate As Date, Optional ByVal sErrorText As String = "Invalid Date") As Boolean

        Dim dDate As Date
        Return Validate(dDate, dEarliestDate, dLatestDate, sErrorText)

    End Function

    Public Function Validate(ByVal dCheckDate As Date, ByVal iValidationType As ValidationType, Optional ByVal sErrorText As String = "Invalid Date") As Boolean

        Dim dDate As Date
        Return Validate(dDate, dCheckDate, iValidationType, sErrorText)

    End Function
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

            txtDate.Attributes.Add("onkeydown", Me.ClientID & "_OnKeyDown(document.all(""" & ctlButton.UniqueID & """))")
            ctlButton.Page.RegisterStartupScript(Me.ClientID & "DefaultButton", scr.ToString())
        End If

    End Sub

    Public Sub SetControlOnEnter(ByVal sControlClientID As String)

        'this does tab as well
        If HttpContext.Current.Request.Browser.JavaScript Then
            Dim scr As New System.Text.StringBuilder()

            With scr
                .Append("<SCRIPT language=""javascript"">" & vbNewLine)
                .Append("function " & Me.ClientID & "_OnKeyDown()" & vbNewLine + "{" + vbNewLine)
                .Append("    if (event.keyCode == 13 || event.keyCode == 9) {" & vbNewLine)
                .Append("       event.returnValue=false;" & vbNewLine)
                .Append("       event.cancel = true;" & vbNewLine)
                .Append("       if ((Form1." & sControlClientID & ")) {" & vbNewLine)
                .Append("           if (!(Form1." & sControlClientID & ".disabled)) {" & vbNewLine)
                .Append("               Form1." & sControlClientID & ".focus();" & vbNewLine)
                .Append("           }" & vbNewLine)
                .Append("       }" & vbNewLine)
                .Append("    } " + vbNewLine)
                .Append("}" + vbNewLine)
                .Append("</SCRIPT>" + vbNewLine)
            End With
            txtDate.Attributes.Add("onkeydown", Me.ClientID & "_OnKeyDown()")
            Me.Page.RegisterStartupScript(Me.ClientID & "EnterKey", scr.ToString())
        End If
    End Sub

    Public ReadOnly Property FirstClientID() As String
        Get
            Return txtDate.ClientID
        End Get
    End Property

#End Region

End Class
