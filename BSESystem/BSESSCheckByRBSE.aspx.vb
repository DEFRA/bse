Partial Class BSECheckByRBSE
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents ctlRBSE As RBSE

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
        VLAHeader1.PageTitle = "BSESS Check : Compare Details By RBSE"
        ctlRBSE.SetDefaultButton(btnLookUp)
        EnableControls()
    End Sub

    Private Sub EnableControls()

        VLAHeader1.GetUserDetails()
        Dim sGroupName As String = Session(SessionVars.SV_HeaderGroupName)


        If sGroupName <> "DEFRA Viewer" And sGroupName <> "DEFRA Data Entry" And sGroupName <> "DEFRA Maintenance" And sGroupName <> "VLA Data Entry" And sGroupName <> "VLA Maintenance" Then
            Response.Redirect("SearchMenu.aspx")
        End If
    End Sub

    Private Sub btnLookUp_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnLookUp.Click

        If ctlRBSE.Validate() Then
            Dim objBSESS As BSELib.clsBSESS

            objBSESS.GetBSESSCheckByRBSE(ctlRBSE.UnformattedRBSE, _
                                            lblNotificationDateValue.Text, _
                                            lblBSESSEartagValue.Text, _
                                            lblBSESSBirthDateValue.Text, _
                                            lblTestGroupValue.Text, _
                                            lblBSESSFinalResultValue.Text, _
                                            lblBarcodeValue.Text, _
                                            lblFormADateValue.Text, _
                                            lblBSEEartagValue.Text, _
                                            lblBSEBirthDateValue.Text, _
                                            lblSurveyValue.Text, _
                                            lblBSEFinalResultValue.Text, _
                                            lblAHFReferenceValue.Text)

        End If
    End Sub

End Class
