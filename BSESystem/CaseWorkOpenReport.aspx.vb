Public Partial Class CaseWorkOpenReport
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Session(SessionVars.SV_HeaderGroupName) <> "VLA Maintenance" Then
            Response.Redirect("Home.aspx")
        End If
        VLAHeader1.PageTitle = "Open cases"
        Pager.SetGrid(grdCases)
        If Not IsPostBack Then
            RefreshGrid()
        End If
    End Sub

#Region "Load Grid Contents"

    Private Sub RefreshGrid()
        Dim iTableID As Integer

        Try
            Dim dtOpenCaseReportData As DataTable
            Dim iCount As Integer
            Dim objCase As New BSELib.clsCase()
            Dim sSelectedRBSE As String = Request.QueryString("rbse")

            dtOpenCaseReportData = objCase.GetOpenCaseReportData()
            If dtOpenCaseReportData Is Nothing Then Throw New Exception()

            Dim keys() As DataColumn = {dtOpenCaseReportData.Columns("RBSE")}
            dtOpenCaseReportData.PrimaryKey = keys

            Session.Item(SessionVars.SV_CaseWorkOpenReportTable) = dtOpenCaseReportData

            ' create a dataview for filtering and sorting
            Dim dv As DataView = dtOpenCaseReportData.DefaultView
            Session.Item(SessionVars.SV_LookupDataView) = dv

            grdCases.DataSource = dtOpenCaseReportData
            grdCases.DataKeyField = "RBSE"
            grdCases.CurrentPageIndex = 0
            grdCases.SelectedIndex = -1
            grdCases.EditItemIndex = -1
            grdCases.DataBind()

            Pager.DataTableSessionID = SessionVars.SV_CaseWorkOpenReportTable
            Pager.DataViewSessionID = SessionVars.SV_CaseWorkOpenReportView
            Pager.PageLinkCount = 10
            Pager.AllowAddNew = False
            Pager.AllowEdit = False
            Pager.AllowDelete = False
            Pager.ConfirmDelete = False

            If sSelectedRBSE <> Nothing Then
                If dtOpenCaseReportData.Rows.Contains(sSelectedRBSE) Then
                    Dim drSelectedRow As DataRow = dtOpenCaseReportData.Rows.Find(sSelectedRBSE)
                    Pager.SelectGridRowForDataRow(drSelectedRow)
                End If
            End If

            Pager.Refresh()

        Catch ex As Exception
            Dim sMsg As String
            sMsg = "Failed to retrieve the Casework Report data in table '" & iTableID & "'"
            clsAppError.DisplayError(sMsg, ex)
        End Try
    End Sub

#End Region

#Region "Event Handlers"

    Protected Sub hlRBSEDisplay_Command(ByVal sender As Object, ByVal e As CommandEventArgs)
        Dim sRBSE As String
        sRBSE = CType(sender, LinkButton).Text

        Session.Item(SessionVars.SV_RBSENumber) = sRBSE
        Response.Redirect("CaseWorkEntry.aspx")
    End Sub


    Protected Sub grdCases_ItemDataBound(ByVal sender As System.Object, ByVal e As System.Web.UI.WebControls.DataGridItemEventArgs) Handles grdCases.ItemDataBound
        ' populate template column values here
        Try
            Dim drv As DataRowView = CType(e.Item.DataItem, DataRowView)
            If Not drv Is Nothing Then
                Dim hlRBSE As LinkButton = Nothing
                Dim lblFormADate As Label = Nothing
                Dim lblRBSEDate As Label = Nothing
                Dim lblSlaughterDate As Label = Nothing
                Dim lblActiveMemoDueDate As Label = Nothing
                Dim lblActiveMemoDate As Label = Nothing
                Dim lblAnnexADueDate As Label = Nothing
                Dim lblAnnexADate As Label = Nothing
                Dim lblAnnexBDueDate As Label = Nothing
                Dim lblAnnexBDate As Label = Nothing
                Dim lblPaperworkCompleteDate As Label = Nothing
                Dim lblAnnexCDueDate As Label = Nothing
                Dim lblAnnexCDate As Label = Nothing
                Dim lblAnnexDDueDate As Label = Nothing
                Dim lblAnnexDDate As Label = Nothing
                Dim lblReceivedByRegionalLabDate As Label = Nothing
                Dim lblInitialReceivedDate As Label = Nothing
                Dim lblFinalReceivedDate As Label = Nothing
                Dim lblFinalSentDate As Label = Nothing
                Dim lblLabChaseDueDate As Label = Nothing
                Dim lblLabChasedDate As Label = Nothing
                Dim lblFinalResultDate As Label = Nothing
                Dim lblBirthDate As Label = Nothing
                Dim lblPost2000SentDate As Label = Nothing
                Dim chkBarbMemoDue As CheckBox = Nothing
                Dim lblBarbMinuteSentDate As Label = Nothing
                Dim lblDataCompleteDate As Label = Nothing

                If e.Item.ItemType = ListItemType.Item _
                OrElse e.Item.ItemType = ListItemType.AlternatingItem _
                OrElse e.Item.ItemType = ListItemType.SelectedItem Then
                    ' populate display mode controls
                    hlRBSE = CType(e.Item.FindControl("hlRBSEDisplay"), LinkButton)
                    lblFormADate = CType(e.Item.FindControl("lblFormADateDisplay"), Label)
                    lblRBSEDate = CType(e.Item.FindControl("lblRBSEDateDisplay"), Label)
                    lblSlaughterDate = CType(e.Item.FindControl("lblSlaughterDateDisplay"), Label)
                    lblActiveMemoDueDate = CType(e.Item.FindControl("lblActiveMemoDueDateDisplay"), Label)
                    lblActiveMemoDate = CType(e.Item.FindControl("lblActiveMemoDateDisplay"), Label)
                    lblAnnexADueDate = CType(e.Item.FindControl("lblAnnexADueDateDisplay"), Label)
                    lblAnnexADate = CType(e.Item.FindControl("lblAnnexADateDisplay"), Label)
                    lblAnnexBDueDate = CType(e.Item.FindControl("lblAnnexBDueDateDisplay"), Label)
                    lblAnnexBDate = CType(e.Item.FindControl("lblAnnexBDateDisplay"), Label)
                    lblPaperworkCompleteDate = CType(e.Item.FindControl("lblPaperworkCompleteDateDisplay"), Label)
                    lblAnnexCDueDate = CType(e.Item.FindControl("lblAnnexCDueDateDisplay"), Label)
                    lblAnnexCDate = CType(e.Item.FindControl("lblAnnexCDateDisplay"), Label)
                    lblAnnexDDueDate = CType(e.Item.FindControl("lblAnnexDDueDateDisplay"), Label)
                    lblAnnexDDate = CType(e.Item.FindControl("lblAnnexDDateDisplay"), Label)
                    lblReceivedByRegionalLabDate = CType(e.Item.FindControl("lblReceivedByRegionalLabDateDisplay"), Label)
                    lblInitialReceivedDate = CType(e.Item.FindControl("lblInitialReceivedDateDisplay"), Label)
                    lblFinalReceivedDate = CType(e.Item.FindControl("lblFinalReceivedDateDisplay"), Label)
                    lblFinalSentDate = CType(e.Item.FindControl("lblFinalSentDateDisplay"), Label)
                    lblLabChaseDueDate = CType(e.Item.FindControl("lblLabChaseDueDateDisplay"), Label)
                    lblLabChasedDate = CType(e.Item.FindControl("lblLabChasedDateDisplay"), Label)
                    lblFinalResultDate = CType(e.Item.FindControl("lblFinalResultDateDisplay"), Label)
                    lblBirthDate = CType(e.Item.FindControl("lblBirthDateDisplay"), Label)
                    lblPost2000SentDate = CType(e.Item.FindControl("lblPost2000SentDateDisplay"), Label)
                    chkBarbMemoDue = CType(e.Item.FindControl("chkBarbMemoDueDisplay"), CheckBox)
                    lblBarbMinuteSentDate = CType(e.Item.FindControl("lblBarbMinuteSentDateDisplay"), Label)
                    lblDataCompleteDate = CType(e.Item.FindControl("lblDataCompleteDateDisplay"), Label)
                End If

                If Not IsDBNull(drv("RBSE")) Then
                    hlRBSE.Text = FormatRBSE(drv("RBSE"))
                Else
                    hlRBSE.Text = ""
                End If

                If Not IsDBNull(drv("FormADate")) Then
                    lblFormADate.Text = FormatDate(drv("FormADate"))
                Else
                    lblFormADate.Text = ""
                End If

                If Not IsDBNull(drv("RBSEDate")) Then
                    lblRBSEDate.Text = FormatDate(drv("RBSEDate"))
                Else
                    lblRBSEDate.Text = ""
                End If

                If Not IsDBNull(drv("SlaughterDate")) Then
                    lblSlaughterDate.Text = FormatDate(drv("SlaughterDate"))
                Else
                    lblSlaughterDate.Text = ""
                End If


                If Not IsDBNull(drv("ActiveMemoDueDate")) Then
                    lblActiveMemoDueDate.Text = FormatDate(drv("ActiveMemoDueDate"))

                    If Not IsDBNull(drv("ActiveMemoDate")) Then
                        lblActiveMemoDueDate.Visible = False
                    Else
                        If Date.Compare(drv("ActiveMemoDueDate"), Today) <= 0 Then
                            lblActiveMemoDueDate.ForeColor = Color.Red
                            lblActiveMemoDueDate.Font.Italic = True
                        End If
                    End If
                Else
                    lblActiveMemoDueDate.Text = ""
                End If

                If Not IsDBNull(drv("ActiveMemoDate")) Then
                    lblActiveMemoDate.Text = FormatDate(drv("ActiveMemoDate"))
                Else
                    lblActiveMemoDate.Text = ""
                End If


                If Not IsDBNull(drv("AnnexADueDate")) Then
                    lblAnnexADueDate.Text = FormatDate(drv("AnnexADueDate"))

                    If Not IsDBNull(drv("AnnexADate")) Then
                        lblAnnexADueDate.Visible = False
                    Else
                        If Date.Compare(drv("AnnexADueDate"), Today) <= 0 Then
                            lblAnnexADueDate.ForeColor = Color.Red
                            lblAnnexADueDate.Font.Italic = True
                        End If
                    End If
                Else
                    lblAnnexADueDate.Text = ""
                End If

                If Not IsDBNull(drv("AnnexADate")) Then
                    lblAnnexADate.Text = FormatDate(drv("AnnexADate"))
                Else
                    lblAnnexADate.Text = ""
                End If


                If Not IsDBNull(drv("AnnexBDueDate")) Then
                    lblAnnexBDueDate.Text = FormatDate(drv("AnnexBDueDate"))

                    If Not IsDBNull(drv("AnnexBDate")) Then
                        lblAnnexBDueDate.Visible = False
                    Else
                        If Date.Compare(drv("AnnexBDueDate"), Today) <= 0 Then
                            lblAnnexBDueDate.ForeColor = Color.Red
                            lblAnnexBDueDate.Font.Italic = True
                        End If
                    End If
                Else
                    lblAnnexBDueDate.Text = ""
                End If

                If Not IsDBNull(drv("AnnexBDate")) Then
                    lblAnnexBDate.Text = FormatDate(drv("AnnexBDate"))
                Else
                    lblAnnexBDate.Text = ""
                End If


                If Not IsDBNull(drv("PaperworkCompleteDate")) Then
                    lblPaperworkCompleteDate.Text = FormatDate(drv("PaperworkCompleteDate"))
                Else
                    lblPaperworkCompleteDate.Text = ""
                End If


                If Not IsDBNull(drv("AnnexCDueDate")) Then
                    lblAnnexCDueDate.Text = FormatDate(drv("AnnexCDueDate"))

                    If Not IsDBNull(drv("AnnexCDate")) Then
                        lblAnnexCDueDate.Visible = False
                    Else
                        If Date.Compare(drv("AnnexCDueDate"), Today) <= 0 Then
                            lblAnnexCDueDate.ForeColor = Color.Red
                            lblAnnexCDueDate.Font.Italic = True
                        End If
                    End If
                Else
                    lblAnnexCDueDate.Text = ""
                End If

                If Not IsDBNull(drv("AnnexCDate")) Then
                    lblAnnexCDate.Text = FormatDate(drv("AnnexCDate"))
                Else
                    lblAnnexCDate.Text = ""
                End If


                If Not IsDBNull(drv("AnnexDDueDate")) Then
                    lblAnnexDDueDate.Text = FormatDate(drv("AnnexDDueDate"))

                    If Not IsDBNull(drv("AnnexDDate")) Then
                        lblAnnexDDueDate.Visible = False
                    Else
                        If Date.Compare(drv("AnnexDDueDate"), Today) <= 0 Then
                            lblAnnexDDueDate.ForeColor = Color.Red
                            lblAnnexDDueDate.Font.Italic = True
                        End If
                    End If
                Else
                    lblAnnexDDueDate.Text = ""
                End If
                    If Not IsDBNull(drv("AnnexDDate")) Then
                        lblAnnexDDate.Text = FormatDate(drv("AnnexDDate"))
                    Else
                        lblAnnexDDate.Text = ""
                    End If


                If Not IsDBNull(drv("ReceivedByRegionalLabDate")) Then
                    lblReceivedByRegionalLabDate.Text = FormatDate(drv("ReceivedByRegionalLabDate"))
                Else
                    lblReceivedByRegionalLabDate.Text = ""
                End If

                If Not IsDBNull(drv("InitialReceivedDate")) Then
                    lblInitialReceivedDate.Text = FormatDate(drv("InitialReceivedDate"))
                Else
                    lblInitialReceivedDate.Text = ""
                End If

                If Not IsDBNull(drv("FinalReceivedDate")) Then
                    lblFinalReceivedDate.Text = FormatDate(drv("FinalReceivedDate"))
                Else
                    lblFinalReceivedDate.Text = ""
                End If

                If Not IsDBNull(drv("FinalSentDate")) Then
                    lblFinalSentDate.Text = FormatDate(drv("FinalSentDate"))
                Else
                    lblFinalSentDate.Text = ""
                End If

                If Not IsDBNull(drv("LabChaseDueDate")) Then
                    lblLabChaseDueDate.Text = FormatDate(drv("LabChaseDueDate"))

                    If Date.Compare(drv("LabChaseDueDate"), Today) <= 0 Then
                        lblLabChaseDueDate.ForeColor = Color.Red
                        lblLabChaseDueDate.Font.Italic = True
                    Else
                        If Not IsDBNull(drv("LabChasedDate")) Then
                            lblLabChaseDueDate.ForeColor = Color.Blue
                        End If
                    End If
                Else
                    lblLabChaseDueDate.Text = ""
                End If

                If Not IsDBNull(drv("LabChasedDate")) Then
                    lblLabChasedDate.Text = FormatDate(drv("LabChasedDate"))
                Else
                    lblLabChasedDate.Text = ""
                End If

                If Not IsDBNull(drv("FinalResultDate")) Then
                    lblFinalResultDate.Text = FormatDate(drv("FinalResultDate"))
                Else
                    lblFinalResultDate.Text = ""
                End If

                If Not IsDBNull(drv("BirthDate")) Then
                    lblBirthDate.Text = FormatDate(drv("BirthDate"))

                    If Date.Compare(drv("BirthDate"), New System.DateTime(2000, 12, 31)) > 0 Then
                        lblBirthDate.ForeColor = Color.Red
                        lblBirthDate.Font.Bold = True
                    End If
                Else
                    lblBirthDate.Text = ""
                End If

                If Not IsDBNull(drv("Post2000SentDate")) Then
                    lblPost2000SentDate.Text = FormatDate(drv("Post2000SentDate"))
                Else
                    lblPost2000SentDate.Text = ""
                End If

                If Not IsDBNull(drv("BarbMemoDue")) Then
                    If CStr(drv("BarbMemoDue")) = "Yes" Then
                        chkBarbMemoDue.Checked = True
                    Else
                        chkBarbMemoDue.Checked = False
                    End If
                Else
                    chkBarbMemoDue.Checked = False
                End If

                If Not IsDBNull(drv("BarbMinuteSentDate")) Then
                    lblBarbMinuteSentDate.Text = FormatDate(drv("BarbMinuteSentDate"))
                Else
                    lblBarbMinuteSentDate.Text = ""
                End If

                If Not IsDBNull(drv("DataCompleteDate")) Then
                    lblDataCompleteDate.Text = FormatDate(drv("DataCompleteDate"))
                Else
                    lblDataCompleteDate.Text = ""
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to bind template columns in the cases grid", ex)
        End Try
    End Sub

#End Region




End Class