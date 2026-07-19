Partial Class ShowCase
    Inherits System.Web.UI.Page

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
        Session.Clear()

        Dim sRBSE As String = Request.QueryString("RBSE")
        Dim sGroupName As String = Session(SessionVars.SV_HeaderGroupName)

        Session(SessionVars.SV_RBSENumber) = sRBSE
        GetCaseDetailsFromDatabase(sRBSE, Session)
        Dim dsData As DataSet = Session.Item(SessionVars.SV_CaseDetails)
        'if the case exists
        If (Not dsData Is Nothing) AndAlso Session.Item(SessionVars.SV_CPHHNumber) <> "" Then

            Dim dtCase As DataTable = CType(dsData.Tables(BSELib.clsCase.CASE_TABLE), DataTable)
            'if the existing case is a non-GB case
            If (dtCase.Rows.Count <> 0) AndAlso CBool((dtCase.Rows(0)("IsNonGBCase")) = True) Then

                'if the user is in one of the VLA groups, they have permission 
                'to edit the non-GB case. Get the rest of the details and
                'redirect to Case Entry
                If sGroupName = "VLA Data Entry" OrElse sGroupName = "VLA Maintenance" Then
                    GetFarmDetailsFromDatabase(Session.Item(SessionVars.SV_CPHHNumber), Session)
                    GetBatchNumbersFromDatabase(sRBSE, Session)
                    Response.Redirect("CaseEntryFarm.aspx")
                Else 'otherwise, user does not have permission to edit non-GB case
                    lblMessage.Text = "This is not a GB case"
                End If

            Else 'the existing case is a GB case

                GetFarmDetailsFromDatabase(Session.Item(SessionVars.SV_CPHHNumber), Session)
                GetBatchNumbersFromDatabase(sRBSE, Session)
                Response.Redirect("CaseEntryFarm.aspx")

            End If '(dtCase.Rows.Count <> 0) AndAlso CBool((dtCase.Rows(0)("IsNonGBCase")) = True)
        Else
            lblMessage.Text = "Case not found"
        End If

    End Sub

End Class
