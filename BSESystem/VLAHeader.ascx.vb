'C****************************************************************************
'  Class:    VLAHeader
'
'  Summary:  Header User Control
'
'****************************************************************************C

'****************************************************************************
'*      Imports
'****************************************************************************

Public Class HomeLinkEventArgs
    Inherits System.EventArgs

    Public bNavigateHome As Boolean = True

End Class

Partial  Class VLAHeader
    Inherits System.Web.UI.UserControl

    Private Const USER_CAPTION As String = "User: "
    Private Const LAB_CAPTION As String = "Group: "
    Private Const BATCH_CAPTION As String = "Batch No: "

    '****************************************************************************
    '*      Private class members
    '****************************************************************************

    '****************************************************************************
    '*      Protected class members
    '****************************************************************************

    '****************************************************************************
    '*      Public class members
    '****************************************************************************

    Public Event HomeClick(ByVal sender As System.Object, ByVal e As HomeLinkEventArgs)

    Public Property PageTitle() As String
        Get
            Return lblPageTitle.Text
        End Get
        Set(ByVal Value As String)
            lblPageTitle.Text = Value
        End Set
    End Property

    Public Property BatchNumber() As String
        Get
            Return lblBatchNumber.Text
        End Get
        Set(ByVal Value As String)
            If Trim(Value) <> "" Then
                lblBatchNumber.Text = BATCH_CAPTION & Value
            End If
        End Set
    End Property

    Public Property ShowLinks() As Boolean
        Get
            Return hlHelp.Visible And lnkHome.Visible
        End Get
        Set(ByVal Value As Boolean)
            hlHelp.Visible = Value
            lnkHome.Visible = Value
        End Set
    End Property

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
        If Not IsPostBack Then
            Try
                Dim sVersion As String = System.Configuration.ConfigurationSettings.AppSettings("SystemVersion")
                If sVersion <> "" Then
                    sVersion = sVersion & " (" & System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() & ")"
                    lblVersion.Text = sVersion
                End If
                GetUserDetails()
                lblUser.Text = USER_CAPTION & Session(SessionVars.SV_HeaderUserName)
                lblLab.Text = LAB_CAPTION & Session(SessionVars.SV_HeaderGroupName)

            Catch ex As Exception
                Dim sMsg As String = "Header control failed to retrieve user info"
                sMsg &= vbNewLine & vbNewLine & "Exception info:-"
                sMsg &= vbNewLine & ex.Source
                sMsg &= vbNewLine & ex.Message
                sMsg &= vbNewLine & ex.StackTrace

                lblUser.Visible = False
                lblLab.Visible = False
                lblBatchNumber.Visible = False
                lblError.Visible = True
            End Try

            Dim sPageName As String
            Try
                ' set the help link to point to the section in Help.aspx with
                ' the same name as this page (if a section hasn't already
                ' been set)
                If hlHelp.NavigateUrl.IndexOf("#") < 0 Then
                    sPageName = Request.CurrentExecutionFilePath()
                    Dim sPathParts() As String = sPageName.Split(New Char() {"/", "\"})
                    Dim sNameParts() As String = sPathParts(sPathParts.GetUpperBound(0)).Split(".")

                    hlHelp.NavigateUrl = "Help.aspx#" & sNameParts(0)
                End If
            Catch ex As Exception
                clsAppError.DisplayError("Failed to set Help chapter for page " & sPageName, ex)
            End Try
        End If
    End Sub

    Public Function GetUserName() As String
        Return lblUser.Text.Substring(USER_CAPTION.Length)
    End Function

    Public Sub GetUserDetails()

        Dim sName As String = Session(SessionVars.SV_HeaderUserName)
        Dim sGroupName As String = Session(SessionVars.SV_HeaderGroupName)

        If sName Is Nothing OrElse sName = "" OrElse sGroupName Is Nothing OrElse sGroupName = "" Then
            Dim sNTLogin As String = GetLoggedOnUser()
            Dim iUserID As Integer
            Dim sEmail As String
            Dim objUser As New BSELib.clsUser()
            Dim sGroupCode As Long
            If Not objUser.GetUserByNTLogin(sNTLogin, iUserID, sName, sGroupCode, sGroupName, sEmail) Then
                Response.Redirect("unauthorized.htm")
            End If

            Session(SessionVars.SV_HeaderUserName) = sName
            Session(SessionVars.SV_HeaderGroupName) = sGroupName
            Session(SessionVars.SV_HeaderUserID) = iUserID
            Session(SessionVars.SV_HeaderUserEmail) = sEmail
        End If

    End Sub


    Private Sub lnkHome_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lnkHome.Click

        Dim arg As New HomeLinkEventArgs()
        RaiseEvent HomeClick(sender, arg)
        If arg.bNavigateHome Then
            Response.Redirect("home.aspx")
        End If

    End Sub

End Class
