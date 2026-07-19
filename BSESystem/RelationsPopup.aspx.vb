Partial Class RelationsPopup
    Inherits System.Web.UI.Page
    Protected WithEvents RelationsPager As DataGridPager

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

        If IsNothing(Session(SessionVars.SV_RBSENumber)) Then
            Response.Redirect("SessionError.aspx")
        End If

        If Request.QueryString("rbse") = "" Then
            Response.Redirect("SessionError.aspx")
        End If

        lblRBSEHeader.Text = RBSE_CAPTION & FormatRBSE(Request.QueryString("rbse"))
        LoadCaseDetails()
    End Sub

#Region "Handle Data"

    Private Function LoadCaseDetails() As Boolean

        Dim objRelations As New BSELib.clsRelations()

        Dim dsData As DataSet

        If objRelations.GetRelationsDetails(Request.QueryString("rbse"), dsData) Then
            With dsData.Tables(0)
                If .Rows.Count = 1 Then
                    lblDamID.Text = .Rows(0).Item("ID").ToString()
                    lblDamOffspringCountValue.Text = .Rows(0).Item("ChildCount").ToString()
                    lblDamEartagValue.Text = .Rows(0).Item("Eartag").ToString()
                    lblDamNameValue.Text = .Rows(0).Item("Name").ToString()
                    lblDamRBSEValue.Text = FormatRBSE(.Rows(0).Item("RBSE").ToString())
                    lblDamHerdbookValue.Text = .Rows(0).Item("Herdbook").ToString()
                    lblDamBirthDateValue.Text = FormatPartialDate(.Rows(0).Item("BirthDay").ToString(), .Rows(0).Item("BirthMonth").ToString(), .Rows(0).Item("BirthYear").ToString())
                    lblDamFateValue.Text = .Rows(0).Item("Fate").ToString()
                    lblDamFinalResultValue.Text = .Rows(0).Item("FinalResult").ToString()
                End If
            End With

            With (dsData.Tables(1))
                If .Rows.Count = 1 Then
                    lblSireID.Text = .Rows(0).Item("ID").ToString()
                    lblSireOffspringCountValue.Text = .Rows(0).Item("ChildCount").ToString()
                    lblSireEartagValue.Text = .Rows(0).Item("Eartag").ToString()
                    lblSireNameValue.Text = .Rows(0).Item("Name").ToString()
                    lblSireRBSEValue.Text = FormatRBSE(.Rows(0).Item("RBSE").ToString())
                    lblSireHerdbookValue.Text = .Rows(0).Item("Herdbook").ToString()
                    lblSireBirthDateValue.Text = FormatPartialDate(.Rows(0).Item("BirthDay").ToString(), .Rows(0).Item("BirthMonth").ToString(), .Rows(0).Item("BirthYear").ToString())
                    lblSireFateValue.Text = .Rows(0).Item("Fate").ToString()
                End If
            End With

            EnableRelationsGrid(dsData.Tables(2))

        End If

    End Function

    Private Sub EnableRelationsGrid(ByRef dtData As DataTable)

        With grdRelations
            .DataSource = dtData
            .DataKeyField = "ID"
            .CurrentPageIndex = 0
            .SelectedIndex = -1
            .EditItemIndex = -1
            .DataBind()
        End With

        With RelationsPager
            .SetGrid(grdRelations)
            .AllowAddNew = False
            .AllowDelete = False
            .AllowEdit = False
            .PageLinkCount = 10
            .Refresh()
        End With
    End Sub

#End Region

    Public Function FormatRBSE(ByVal sRBSE As String) As String

        If sRBSE <> "" Then
            sRBSE = Left$(sRBSE, 2) & "/" & Mid$(sRBSE, 3, 2) & "/" & Mid$(sRBSE, 5, 5)
        End If

        Return sRBSE

    End Function

    Public Function FormatPartialDate(ByVal sDay As String, ByVal sMonth As String, ByVal sYear As String) As String

        Dim sPartialDate As New System.Text.StringBuilder()

        If sDay <> "" Then
            sPartialDate.Append(sDay)
            sPartialDate.Append("/")
        End If

        If sMonth <> "" Then
            sPartialDate.Append(sMonth)
            sPartialDate.Append("/")
        End If

        sPartialDate.Append(sYear)

        Return sPartialDate.ToString()

    End Function
End Class
