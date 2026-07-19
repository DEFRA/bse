<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="BatchNumber" Src="BatchNumber.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="PrintBatch.aspx.vb" Inherits="BSESystem.PrintBatch"%>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : Print Batch</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<P><uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader></P>
			<DIV style="WIDTH: 828px; POSITION: relative; HEIGHT: 211px" ms_positioning="GridLayout">
				<P></P>
				<HR style="Z-INDEX: 101; LEFT: 16px; WIDTH: 96.14%; POSITION: absolute; TOP: 40px; HEIGHT: 1px" width="96.14%" SIZE="1">
				<P></P>
				<HR style="Z-INDEX: 102; LEFT: 16px; WIDTH: 96.14%; POSITION: absolute; TOP: 88px; HEIGHT: 1px" width="96.14%" SIZE="1">
				<asp:button id="btnDownload" style="Z-INDEX: 103; LEFT: 16px; POSITION: absolute; TOP: 104px" runat="server" Text="Download"></asp:button>
				<DIV style="Z-INDEX: 104; LEFT: 165px; WIDTH: 268px; POSITION: absolute; TOP: 56px; HEIGHT: 30px"><uc1:batchnumber id="BatchNumber1" runat="server"></uc1:batchnumber></DIV>
				<asp:dropdownlist id="ddlReportType" style="Z-INDEX: 105; LEFT: 379px; POSITION: absolute; TOP: 56px" runat="server" Width="141px"></asp:dropdownlist><asp:label id="Label1" style="Z-INDEX: 106; LEFT: 291px; POSITION: absolute; TOP: 56px" runat="server" Width="94px">Report type:</asp:label><asp:label id="lblEnterBatchNumber" style="Z-INDEX: 107; LEFT: 15px; POSITION: absolute; TOP: 56px" runat="server" Width="152px">Enter Batch Number</asp:label><asp:requiredfieldvalidator id="revReport" style="Z-INDEX: 108; LEFT: 521px; POSITION: absolute; TOP: 58px" runat="server" Width="3px" InitialValue="0" Height="5px" CssClass="validatortext" ToolTip="Select the report type" ControlToValidate="ddlReportType" ErrorMessage="*"></asp:requiredfieldvalidator></DIV>
			<P><uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter>
			<P></P>
		</form>
	</body>
</HTML>
