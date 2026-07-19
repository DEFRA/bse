Partial Class CaseEntrySave
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader

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

        VLAHeader1.PageTitle = "Save Case Details"

        If Not IsPostBack Then
            If IsNothing(Session(SessionVars.SV_RBSENumber)) Then
                Response.Redirect("SessionError.aspx")
            End If

            PerformSave()
        End If

    End Sub

    Private Sub PerformSave()

        Dim sRedirectURL As String = Request.QueryString("redirect")
        Dim bRedirect As Boolean = False
        Dim objErrorlist As New ArrayList()
        Dim objCase As New BSELib.clsCase()
        Dim dsFarm As DataSet = Session(SessionVars.SV_FarmDetails)
        Dim dsCase As DataSet = Session(SessionVars.SV_CaseDetails)
        Dim iBatchID As Integer = 0
        If CStr(Session(SessionVars.SV_BatchID)) <> "" Then
            iBatchID = CInt(Session(SessionVars.SV_BatchID))
        End If
        If Not objCase.CheckMandatoryFields(dsCase, dsFarm, objErrorlist) Then
            lblRBSE.Text = "RBSE: " & Session(SessionVars.SV_RBSENumber)
            ctlDIV.InnerHtml = "<p>The case is missing the following items of data:</p><p>&nbsp;</p><p>" & Join(objErrorlist.ToArray, "</p><p>") & "</p>"
            btnOK.Text = "Return"
        Else
            Dim bSuccess As Boolean = objCase.UpdateCaseDetails(Session(SessionVars.SV_HeaderUserID), iBatchID, dsCase, dsFarm, objErrorlist)

            If bSuccess Then
                If objErrorlist.Count = 0 Then
                    bRedirect = True
                Else
                    lblRBSE.Text = "RBSE: " & Session(SessionVars.SV_RBSENumber)
                    ctlDIV.InnerHtml = "<p>The database has been updated but some errors were encountered:</p><p>&nbsp;</p><p>" & Join(objErrorlist.ToArray, "</p><p>") & "</p>"
                End If
            Else
                lblRBSE.Text = "RBSE: " & Session(SessionVars.SV_RBSENumber)
                ctlDIV.InnerHtml = "<p>The database has not been updated because the following error(s) occurred:</p><p>&nbsp;</p><p>" & Join(objErrorlist.ToArray, "</p><p>") & "</p>"
            End If

            If bRedirect Then
                If sRedirectURL <> Nothing Then
                    Response.Redirect(sRedirectURL)
                Else
                    Common.RemoveCaseFromSession(Session)
                    Response.Redirect("home.aspx")
                End If
            End If
        End If
    End Sub

    
    Private Sub btnOK_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnOK.Click
        If btnOK.Text = "Return" Then
            Response.Redirect("CaseEntryFarm.aspx")
        Else
            Response.Redirect("home.aspx")
        End If
    End Sub
End Class
