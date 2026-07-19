<%@ Register TagPrefix="uc1" TagName="RBSE" Src="RBSE.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="SearchRelatedAnimal.aspx.vb" Inherits="BSESystem.SearchRelatedAnimal"%>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : Related Animal Search</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<P><uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader></P>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 165px" ms_positioning="GridLayout">
				<asp:label id="lblRBSEOfCase" style="Z-INDEX: 100; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Width="152px">RBSE Of Case</asp:label>
				<asp:label id="lblRelatedRBSE" style="Z-INDEX: 103; LEFT: 16px; POSITION: absolute; TOP: 64px" runat="server" Width="152px">RBSE of Related Animal</asp:label>
				<asp:label id="lblEartag" style="Z-INDEX: 104; LEFT: 400px; POSITION: absolute; TOP: 16px" runat="server" Width="152px">Eartag of Related Animal</asp:label>
				<asp:label id="lblRelationType" style="Z-INDEX: 107; LEFT: 400px; POSITION: absolute; TOP: 64px" runat="server" Width="152px">Relation Type</asp:label>
				<HR style="Z-INDEX: 109; LEFT: 16px; WIDTH: 96.13%; POSITION: absolute; TOP: 152px; HEIGHT: 1px" width="96.14%" SIZE="1">
				<DIV style="Z-INDEX: 101; LEFT: 184px; WIDTH: 184px; POSITION: absolute; TOP: 16px; HEIGHT: 24px" ms_positioning="GridLayout">
					<uc1:rbse id="ctlCaseRBSE" runat="server"></uc1:rbse>
				</DIV>
				<asp:textbox id="txtEartag" style="Z-INDEX: 106; LEFT: 568px; POSITION: absolute; TOP: 16px" runat="server" Width="168px"></asp:textbox>
				<DIV style="Z-INDEX: 102; LEFT: 184px; WIDTH: 208px; POSITION: absolute; TOP: 64px; HEIGHT: 24px" ms_positioning="GridLayout">
					<uc1:rbse id="ctlRelationRBSE" runat="server"></uc1:rbse>
				</DIV>
				<asp:dropdownlist id="ddlRelationType" style="Z-INDEX: 108; LEFT: 568px; POSITION: absolute; TOP: 64px" runat="server" Width="168px"></asp:dropdownlist>
				<asp:button id="btnSearch" style="Z-INDEX: 110; LEFT: 520px; POSITION: absolute; TOP: 120px" runat="server" Width="84px" Text="Search"></asp:button>
				<asp:Button id="btnSearchMenu" style="Z-INDEX: 111; LEFT: 624px; POSITION: absolute; TOP: 120px" runat="server" Text="Search Menu"></asp:Button>
				<asp:label id="Label1" style="Z-INDEX: 112; LEFT: 400px; POSITION: absolute; TOP: 32px" runat="server" Width="152px">(starting with...)</asp:label>
				<asp:label id="lblError" style="Z-INDEX: 113; LEFT: 608px; POSITION: absolute; TOP: 120px" runat="server" CssClass="ValidatorText" Visible="False" EnableViewState="False" ToolTip="Please provide one or more search criteria">*</asp:label>
				<asp:label id="lblName" style="Z-INDEX: 114; LEFT: 16px; POSITION: absolute; TOP: 112px" runat="server" Width="152px">Name of Related Animal</asp:label>
				<asp:label id="Label2" style="Z-INDEX: 115; LEFT: 16px; POSITION: absolute; TOP: 128px" runat="server" Width="152px">(starting with...)</asp:label>
				<asp:textbox id="txtName" style="Z-INDEX: 116; LEFT: 184px; POSITION: absolute; TOP: 112px" runat="server" Width="168px"></asp:textbox>
			</DIV>
			<P><asp:datagrid id="grdResults" runat="server" Width="740px" AllowPaging="True" AllowSorting="True" AutoGenerateColumns="False">
					<SelectedItemStyle CssClass="GridSelectedItem"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItem"></EditItemStyle>
					<ItemStyle CssClass="GridItem"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:BoundColumn DataField="RBSE" SortExpression="RBSE" HeaderText="RBSE"></asp:BoundColumn>
						<asp:BoundColumn DataField="CPHH" SortExpression="CPHH" HeaderText="CPHH"></asp:BoundColumn>
						<asp:BoundColumn DataField="RelationType" SortExpression="RelationType" HeaderText="Relation"></asp:BoundColumn>
						<asp:BoundColumn DataField="RelSex" SortExpression="RelSex" HeaderText="Sex"></asp:BoundColumn>
						<asp:BoundColumn DataField="Eartag" SortExpression="Eartag" HeaderText="Eartag"></asp:BoundColumn>
						<asp:BoundColumn DataField="RelBirthDate" SortExpression="RelBirthDate" HeaderText="Rel Birth Date"></asp:BoundColumn>
						<asp:BoundColumn DataField="RelFate" SortExpression="RelFate" HeaderText="Rel Fate"></asp:BoundColumn>
						<asp:BoundColumn DataField="LeftDate" SortExpression="LeftDate" HeaderText="Date Left" DataFormatString="{0:d}"></asp:BoundColumn>
						<asp:BoundColumn DataField="RelName" SortExpression="RelName" HeaderText="Rel Name"></asp:BoundColumn>
						<asp:BoundColumn DataField="RelEartag" SortExpression="RelEartag" HeaderText="Rel Eartag"></asp:BoundColumn>
						<asp:BoundColumn DataField="RelationRBSE" SortExpression="RelationRBSE" HeaderText="Rel RBSE"></asp:BoundColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid></P>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 56px" ms_positioning="GridLayout"><uc1:datagridpager id="ResultsPager" runat="server"></uc1:datagridpager>
				<asp:HyperLink id="hlbExcel" style="Z-INDEX: 101; LEFT: 640px; POSITION: absolute; TOP: 32px" runat="server" Width="102px" Visible="False" NavigateUrl="ExcelExport.aspx" Target="_blank">Export to Excel</asp:HyperLink>
				<asp:label id="lblResultError" style="Z-INDEX: 102; LEFT: 0px; POSITION: absolute; TOP: 32px" Visible="False" CssClass="GridPagerErrorText" Runat="server"></asp:label></DIV>
			<P><uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></P>
		</form>
	</body>
</HTML>
