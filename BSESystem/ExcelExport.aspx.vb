Partial Class ExcelExport
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

        If IsNothing(Session(SessionVars.SV_ExcelExport)) Then
            Response.Redirect("SessionError.aspx")
        End If

        Convert()
    End Sub

    Private Sub Convert()

        Dim objDatatable As DataTable = Session.Item(SessionVars.SV_ExcelExport)
        Dim objDataView As DataView = Session.Item(SessionVars.SV_ExcelExportView)

        With Response
            .Clear()
            .Charset = ""
            .ContentType = "application/vnd.ms-excel"
            .AddHeader("Content-Disposition", "attachment; filename=""" & objDataTable.TableName & ".xls""")

        End With
        'create a string writer
        Dim objStringWriter As New System.IO.StringWriter()
        'create an htmltextwriter which uses the stringwriter
        Dim objHTMLWriter As New System.Web.UI.HtmlTextWriter(objStringWriter)
        'instantiate a datagrid
        Dim objDataGrid As New DataGrid()
        'set the datagrid datasource to the dataset passed in
        objDataGrid.DataSource = objDataView
        'bind the datagrid
        objDataGrid.DataBind()
        'tell the datagrid to render itself to our htmltextwriter
        objDataGrid.RenderControl(objHTMLWriter)
        'output the HTML
        Response.Write(objStringWriter.ToString)
        Response.End()
    End Sub


End Class
