<%@ Register TagPrefix="uc1" TagName="MapReference" Src="MapReference.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="Home.aspx.vb" Inherits="BSESystem.Home"%>
<%@ Register TagPrefix="uc1" TagName="BatchNumber" Src="BatchNumber.ascx" %>
<%@ Register TagPrefix="uc1" TagName="RBSE" Src="RBSE.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : Home</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
		<style type="text/css">
		 .ReportTable TH { BORDER-RIGHT: medium none; BORDER-TOP: medium none; FONT-WEIGHT: bold; FONT-SIZE: 90%; VERTICAL-ALIGN: baseline; BORDER-LEFT: medium none; BORDER-BOTTOM: medium none; TEXT-ALIGN: center }
		 .ReportTable TD { BORDER-RIGHT: medium none; BORDER-TOP: medium none; FONT-WEIGHT: normal; FONT-SIZE: 90%; MARGIN: 0px; BORDER-LEFT: medium none; BORDER-BOTTOM: medium none; TEXT-ALIGN: center }
		</style>
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<P><uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader></P>
			<asp:Panel id="Panel2" runat="server" Width="288px" Height="104px">
				<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 112px" ms_positioning="GridLayout">
					<asp:label id="lblBatchNumber" style="Z-INDEX: 101; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Width="344px" Font-Bold="True">Batch Number</asp:label>
					<asp:label id="lblInstruction" style="Z-INDEX: 102; LEFT: 16px; POSITION: absolute; TOP: 40px" runat="server" Width="402px">Only enter a batch number if you are entering a batch of case details</asp:label>
					<asp:label id="lblEnterBatchNumber" style="Z-INDEX: 103; LEFT: 16px; POSITION: absolute; TOP: 72px" runat="server" Width="152px">Enter Batch Number</asp:label>
					<asp:label id="lblOr" style="Z-INDEX: 104; LEFT: 304px; POSITION: absolute; TOP: 72px" runat="server" Width="24px">Or</asp:label>
					<HR style="Z-INDEX: 106; LEFT: 16px; WIDTH: 96.14%; POSITION: absolute; TOP: 104px; HEIGHT: 1px" width="96.14%" SIZE="1">
					<DIV style="Z-INDEX: 107; LEFT: 168px; WIDTH: 271px; POSITION: absolute; TOP: 72px; HEIGHT: 16px" ms_positioning="GridLayout">
						<uc1:batchnumber id="Batchnumber1" runat="server"></uc1:batchnumber></DIV>
					<asp:button id="btnCreateNew" style="Z-INDEX: 105; LEFT: 336px; POSITION: absolute; TOP: 72px" runat="server" Width="96px" Text="Create New"></asp:button>
					<DIV style="Z-INDEX: 108; LEFT: 456px; WIDTH: 282px; POSITION: absolute; TOP: 16px; HEIGHT: 72px" ms_positioning="GridLayout">
						<TABLE class="ReportTable">
							<asp:Repeater id="rptBatch" runat="server">
								<HeaderTemplate>
									<tr>
										<th>
											Batch Number&nbsp;</th>
										<th>
											Cases</th>
									</tr>
								</HeaderTemplate>
								<ItemTemplate>
									<tr>
										<td align="center"><%# DataBinder.Eval(Container, "DataItem.Batch")%></td>
										<td align="center"><%# DataBinder.Eval(Container, "DataItem.CaseCount")%></td>
									</tr>
								</ItemTemplate>
							</asp:Repeater></TABLE>
					</DIV>
				</DIV>
			</asp:Panel>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 378px" ms_positioning="GridLayout">
				<DIV style="Z-INDEX: 117; LEFT: 184px; WIDTH: 229px; POSITION: absolute; TOP: 40px; HEIGHT: 24px" ms_positioning="GridLayout">
					<uc1:rbse id="RBSE1" runat="server"></uc1:rbse>
					<asp:button id="btnGo" style="Z-INDEX: 128; LEFT: 176px; POSITION: absolute; TOP: 0px" runat="server" Text=" Go "></asp:button>
				</DIV>
				<asp:label id="lblRBSENumber" style="Z-INDEX: 118; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Font-Bold="True" Width="344px">RBSE Number</asp:label>
				<asp:label id="lblDBSETitle" style="Z-INDEX: 119; LEFT: 496px; POSITION: absolute; TOP: 80px" runat="server" Width="216px">DBSE</asp:label>
				<asp:label id="lblCurrentDBSE" style="Z-INDEX: 120; LEFT: 496px; POSITION: absolute; TOP: 112px" runat="server" Width="216px">1234567</asp:label>
				<asp:label id="lblPreviousRBSE" style="Z-INDEX: 121; LEFT: 248px; POSITION: absolute; TOP: 144px" runat="server" Width="216px">1234567</asp:label>
				<asp:label id="lblPreviousDBSE" style="Z-INDEX: 122; LEFT: 496px; POSITION: absolute; TOP: 144px" runat="server" Width="216px">1234567</asp:label>
				<asp:label id="lblRBSE" style="Z-INDEX: 100; LEFT: 16px; POSITION: absolute; TOP: 40px" runat="server" Width="152px">RBSE</asp:label>
				<asp:label id="lblLatestInPreviousYear" style="Z-INDEX: 101; LEFT: 16px; POSITION: absolute; TOP: 144px" runat="server" Width="200px">Latest in Previous Year</asp:label>
				<asp:label id="lblLatestInCurrentYear" style="Z-INDEX: 102; LEFT: 16px; POSITION: absolute; TOP: 112px" runat="server" Width="200px">Latest in Current Year:</asp:label>
				<asp:label id="lblRBSETitle" style="Z-INDEX: 103; LEFT: 248px; POSITION: absolute; TOP: 80px" runat="server" Width="216px">RBSE</asp:label>
				<asp:label id="lblCurrentRBSE" style="Z-INDEX: 104; LEFT: 248px; POSITION: absolute; TOP: 112px" runat="server" Width="216px"> 1234567</asp:label>
				<HR style="Z-INDEX: 105; LEFT: 16px; WIDTH: 96.14%; POSITION: absolute; TOP: 168px; HEIGHT: 1px" width="96.14%" SIZE="1">
				<asp:hyperlink id="hlAuditLog" style="Z-INDEX: 106; LEFT: 16px; POSITION: absolute; TOP: 184px" runat="server" Width="162px" NavigateUrl="AuditLogMenu.aspx">Audit Log</asp:hyperlink>
				<asp:hyperlink id="hlSearch" style="Z-INDEX: 107; LEFT: 280px; POSITION: absolute; TOP: 184px" runat="server" Width="160px" NavigateUrl="SearchMenu.aspx">Search</asp:hyperlink>
				<asp:hyperlink id="hlBSESS" style="Z-INDEX: 109; LEFT: 544px; POSITION: absolute; TOP: 184px" runat="server" Width="160px" NavigateUrl="BSESSMenu.aspx">BSESS Check</asp:hyperlink>
				<asp:hyperlink id="hlExportADNS" style="Z-INDEX: 108; LEFT: 16px; POSITION: absolute; TOP: 224px" runat="server" Width="160px" NavigateUrl="ADNSExportMenu.aspx">Export To ADNS</asp:hyperlink>
				<asp:LinkButton id="hlPrintBatch" style="Z-INDEX: 125; LEFT: 280px; POSITION: absolute; TOP: 224px" runat="server">Print Batch</asp:LinkButton>
				<asp:hyperlink id="hlFinalResultEntry" style="Z-INDEX: 110; LEFT: 544px; POSITION: absolute; TOP: 224px" runat="server" Width="160px" NavigateUrl="FinalResultEntry.aspx">Final Result Entry</asp:hyperlink>
				<asp:hyperlink id="hlExportOSS" style="Z-INDEX: 123; LEFT: 16px; POSITION: absolute; TOP: 264px" runat="server" Width="160px" NavigateUrl="OSSExportMenu.aspx"> Export To OSS</asp:hyperlink>
				<asp:hyperlink id="hlCPHHChange" style="Z-INDEX: 111; LEFT: 16px; POSITION: absolute; TOP: 312px" runat="server" Width="162px" NavigateUrl="CPHHChange.aspx">CPHH Change</asp:hyperlink>
				<asp:hyperlink id="hlRBSEChange" style="Z-INDEX: 112; LEFT: 280px; POSITION: absolute; TOP: 312px" runat="server" Width="162px" NavigateUrl="RBSEChange.aspx">RBSE Change</asp:hyperlink>
				<asp:hyperlink id="hlPickListMaintenance" style="Z-INDEX: 116; LEFT: 544px; POSITION: absolute; TOP: 312px" runat="server" Width="162px" NavigateUrl="PickListMaintenance.aspx">Pick List Maintenance</asp:hyperlink>
				<asp:hyperlink id="hlMoveCase" style="Z-INDEX: 113; LEFT: 16px; POSITION: absolute; TOP: 352px" runat="server" Width="162px" NavigateUrl="MoveCase.aspx">Move Case</asp:hyperlink>
				<asp:hyperlink id="hlDeleteCase" style="Z-INDEX: 115; LEFT: 280px; POSITION: absolute; TOP: 352px" runat="server" Width="162px" NavigateUrl="DeleteCase.aspx">Delete Case</asp:hyperlink>
				<asp:panel id="Panel1" style="Z-INDEX: 124; LEFT: 0px; POSITION: absolute; TOP: 288px" runat="server" Width="740px" Height="14px">
					<HR id="hrMaintenanceLine" style="WIDTH: 96.14%; HEIGHT: 1px" width="96.14%" SIZE="1">
				</asp:panel>
				<asp:Label id="lblRBSEEmpty" style="Z-INDEX: 126; LEFT: 344px; POSITION: absolute; TOP: 40px" runat="server" ToolTip="You must enter a RBSE number" ForeColor="Red" CssClass="validatortext" Visible="False">*</asp:Label>
				<asp:hyperlink id="hlUserMaintenance" style="Z-INDEX: 127; LEFT: 544px; POSITION: absolute; TOP: 352px" runat="server" Width="162px" NavigateUrl="UserMaintenance.aspx">User Maintenance</asp:hyperlink>
                <asp:hyperlink ID="hlCasework" style="Z-INDEX: 125; LEFT: 280px; POSITION: absolute; TOP: 264px" runat="server" NavigateUrl="CaseWorkMenu.aspx" Width="160px">Casework</asp:hyperlink></DIV>
			<p>
				<uc1:vlafooter id="VLAFooter1" runat="server" DESIGNTIMEDRAGDROP="868"></uc1:vlafooter>
			</p>
		</form>
	</body>
</HTML>
