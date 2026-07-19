<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="CaseWorkClosedReport.aspx.vb" Inherits="BSESystem.CaseWorkClosedReport" %>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>BSE System : Closed Cases</title>
    <meta content="JavaScript" name="vs_defaultClientScript">
    <link href="Style/vla-ie.css" type="text/css" rel="stylesheet">
</head>
<body>
    <form id="form1" runat="server">
    <P><uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader></P>
    <asp:HyperLink id="hlCaseworkOpenReportLink" runat="server" Text="See open cases" NavigateUrl="CaseWorkOpenReport.aspx"></asp:HyperLink>
    <asp:datagrid id="grdCases" runat="server"  AutoGenerateColumns="False" AllowSorting="True" AllowPaging="True">
		<SelectedItemStyle CssClass="GridSelectedItem"></SelectedItemStyle>
		<ItemStyle CssClass="GridItem"></ItemStyle>
		<HeaderStyle CssClass="GridHeader"></HeaderStyle>
		<Columns>
		    <asp:ButtonColumn Text="&lt;img src=&quot;Images/GridPager/sel.gif&quot;&gt;" CommandName="Select">
		        <HeaderStyle Width="25px"></HeaderStyle>
		    </asp:ButtonColumn>
            <asp:TemplateColumn SortExpression="RBSE" HeaderText="RBSE">
			    <ItemTemplate>
				    <asp:LinkButton id="hlRBSEDisplay" runat="server" OnCommand="hlRBSEDisplay_Command"></asp:LinkButton>
				</ItemTemplate>
			</asp:TemplateColumn>
            <asp:BoundColumn DataField="Survey" SortExpression="Survey" HeaderText="Survey" ReadOnly="True"></asp:BoundColumn>
			<asp:BoundColumn DataField="Barcode" SortExpression="Barcode" HeaderText="Barcode" ReadOnly="True"></asp:BoundColumn>
			<asp:BoundColumn DataField="AHFReference" SortExpression="AHFReference" HeaderText="AHF Reference" ReadOnly="True"></asp:BoundColumn>
			<asp:TemplateColumn SortExpression="FormADate" HeaderText="Form A Date">
			    <ItemTemplate>
							<asp:Label id="lblFormADateDisplay" runat="server"></asp:Label>
				</ItemTemplate>
			</asp:TemplateColumn>
			<asp:TemplateColumn SortExpression="RBSEDate" HeaderText="RBSE Date">
			    <ItemTemplate>
							<asp:Label id="lblRBSEDateDisplay" runat="server"></asp:Label>
				</ItemTemplate>
			</asp:TemplateColumn>
			<asp:TemplateColumn SortExpression="SlaughterDate" HeaderText="Slaughter Date">
			    <ItemTemplate>
							<asp:Label id="lblSlaughterDateDisplay" runat="server"></asp:Label>
				</ItemTemplate>
			</asp:TemplateColumn>
			
			<asp:BoundColumn DataField="Fate" SortExpression="Fate" HeaderText="Fate" ReadOnly="True"></asp:BoundColumn>
			
			<asp:TemplateColumn SortExpression="ActiveMemoDate" HeaderText="Active Memo Date">
			    <ItemTemplate>
							<asp:Label id="lblActiveMemoDateDisplay" runat="server"></asp:Label>
				</ItemTemplate>
			</asp:TemplateColumn>
			<asp:TemplateColumn SortExpression="AnnexADate" HeaderText="Annex A Date">
			    <ItemTemplate>
							<asp:Label id="lblAnnexADateDisplay" runat="server"></asp:Label>
				</ItemTemplate>
			</asp:TemplateColumn>
			<asp:TemplateColumn SortExpression="AnnexBDate" HeaderText="Annex B Date">
			    <ItemTemplate>
							<asp:Label id="lblAnnexBDateDisplay" runat="server"></asp:Label>
				</ItemTemplate>
			</asp:TemplateColumn>
			<asp:TemplateColumn SortExpression="PaperworkCompleteDate" HeaderText="Paperwork Complete Date">
			    <ItemTemplate>
							<asp:Label id="lblPaperworkCompleteDateDisplay" runat="server"></asp:Label>
				</ItemTemplate>
			</asp:TemplateColumn>
			<asp:TemplateColumn SortExpression="AnnexCDate" HeaderText="Annex C Date">
			    <ItemTemplate>
							<asp:Label id="lblAnnexCDateDisplay" runat="server"></asp:Label>
				</ItemTemplate>
			</asp:TemplateColumn>
			<asp:TemplateColumn SortExpression="AnnexDDate" HeaderText="Annex D Date">
			    <ItemTemplate>
							<asp:Label id="lblAnnexDDateDisplay" runat="server"></asp:Label>
				</ItemTemplate>
			</asp:TemplateColumn>
			
			<asp:BoundColumn DataField="RegionalLab" SortExpression="RegionalLab" HeaderText="Regional Lab" ReadOnly="True"></asp:BoundColumn>
			
			<asp:TemplateColumn SortExpression="ReceivedByRegionalLabDate" HeaderText="Regional Lab Received Date">
			    <ItemTemplate>
							<asp:Label id="lblReceivedByRegionalLabDateDisplay" runat="server"></asp:Label>
				</ItemTemplate>
			</asp:TemplateColumn>
			<asp:TemplateColumn SortExpression="InitialReceivedDate" HeaderText="Initial Received Date">
			    <ItemTemplate>
							<asp:Label id="lblInitialReceivedDateDisplay" runat="server"></asp:Label>
				</ItemTemplate>
			</asp:TemplateColumn>
			<asp:TemplateColumn SortExpression="FinalReceivedDate" HeaderText="Final Received Date">
			    <ItemTemplate>
							<asp:Label id="lblFinalReceivedDateDisplay" runat="server"></asp:Label>
				</ItemTemplate>
			</asp:TemplateColumn>
			<asp:TemplateColumn SortExpression="FinalSentDate" HeaderText="Final Sent Date">
			    <ItemTemplate>
							<asp:Label id="lblFinalSentDateDisplay" runat="server"></asp:Label>
				</ItemTemplate>
			</asp:TemplateColumn>
			<asp:TemplateColumn SortExpression="LabChasedDate" HeaderText="Lab Chased Date">
			    <ItemTemplate>
							<asp:Label id="lblLabChasedDateDisplay" runat="server"></asp:Label>
				</ItemTemplate>
			</asp:TemplateColumn>
			
			<asp:BoundColumn DataField="FinalResult" SortExpression="FinalResult" HeaderText="Final Result" ReadOnly="True"></asp:BoundColumn>
			
			<asp:TemplateColumn SortExpression="FinalResultDate" HeaderText="Final Result Date">
			    <ItemTemplate>
							<asp:Label id="lblFinalResultDateDisplay" runat="server"></asp:Label>
				</ItemTemplate>
			</asp:TemplateColumn>
			<asp:TemplateColumn SortExpression="BirthDate" HeaderText="Birth Date">
			    <ItemTemplate>
							<asp:Label id="lblBirthDateDisplay" runat="server"></asp:Label>
				</ItemTemplate>
			</asp:TemplateColumn>
			<asp:TemplateColumn SortExpression="Post2000SentDate" HeaderText="Post 2000 Sent Date">
			    <ItemTemplate>
							<asp:Label id="lblPost2000SentDateDisplay" runat="server"></asp:Label>
				</ItemTemplate>
			</asp:TemplateColumn>
			<asp:TemplateColumn SortExpression="BarbMinuteSentDate" HeaderText="Barb Minute Sent Date">
			    <ItemTemplate>
							<asp:Label id="lblBarbMinuteSentDateDisplay" runat="server"></asp:Label>
				</ItemTemplate>
			</asp:TemplateColumn>
			<asp:TemplateColumn SortExpression="DataCompleteDate" HeaderText="Data Complete Date">
			    <ItemTemplate>
							<asp:Label id="lblDataCompleteDateDisplay" runat="server"></asp:Label>
				</ItemTemplate>
			</asp:TemplateColumn>
			
			<asp:BoundColumn DataField="CaseWorkNotes" SortExpression="CaseWorkNotes" HeaderText="CaseWorkNotes" ReadOnly="True"></asp:BoundColumn>
		
		    <asp:ButtonColumn Text="&lt;img src=&quot;Images/GridPager/sel_reverse.gif&quot;&gt;" CommandName="Select">
		        <HeaderStyle Width="25px"></HeaderStyle>
		    </asp:ButtonColumn>
		</Columns>
		<PagerStyle Visible="False"></PagerStyle>
	</asp:datagrid>
	<div style="WIDTH: 750px; POSITION: relative; HEIGHT: 28px" ms_positioning="GridLayout">
		<P><uc1:datagridpager id="Pager" runat="server"></uc1:datagridpager></P>
	</div>
    <P><uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></P>
    </form>
</body>
</html>
