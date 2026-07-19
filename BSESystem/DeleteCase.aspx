<%@ Register TagPrefix="uc1" TagName="RBSE" Src="RBSE.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="DeleteCase.aspx.vb" Inherits="BSESystem.DeleteCase" smartNavigation="false"%>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : Delete Case</title>
		<meta name="GENERATOR" content="Microsoft Visual Studio.NET 7.0">
		<meta name="CODE_LANGUAGE" content="Visual Basic 7.0">
		<meta name="vs_defaultClientScript" content="JavaScript">
		<meta name="vs_targetSchema" content="http://schemas.microsoft.com/intellisense/ie5">
		<link href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<P>
				<uc1:VLAHeader id="VLAHeader1" runat="server"></uc1:VLAHeader></P>
			<DIV style="WIDTH: 753px; POSITION: relative; HEIGHT: 248px" ms_positioning="GridLayout">
				<DIV style="Z-INDEX: 100; LEFT: 184px; WIDTH: 176px; POSITION: absolute; TOP: 16px; HEIGHT: 27px" ms_positioning="GridLayout">
					<uc1:RBSE id="ctlRBSE" runat="server"></uc1:RBSE></DIV>
				<asp:label id="lblRBSE" style="Z-INDEX: 101; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Width="152px">RBSE</asp:label>
				<HR style="Z-INDEX: 102; LEFT: 16px; WIDTH: 96.14%; POSITION: absolute; TOP: 56px; HEIGHT: 1px" width="96.14%" SIZE="1">
				<asp:label id="lblCPHH" style="Z-INDEX: 103; LEFT: 392px; POSITION: absolute; TOP: 72px" runat="server" Width="152px">CPHH</asp:label>
				<asp:label id="lblCPHHValue" style="Z-INDEX: 104; LEFT: 584px; POSITION: absolute; TOP: 72px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblOwnerNameValue" style="Z-INDEX: 105; LEFT: 184px; POSITION: absolute; TOP: 72px" runat="server" Width="145px"></asp:label>
				<asp:label id="lblOwnerName" style="Z-INDEX: 106; LEFT: 16px; POSITION: absolute; TOP: 72px" runat="server" Width="152px">Owner Name</asp:label>
				<asp:label id="lblAddress" style="Z-INDEX: 107; LEFT: 16px; POSITION: absolute; TOP: 104px" runat="server" Width="152px">Address</asp:label>
				<asp:label id="lblAddress1Value" style="Z-INDEX: 108; LEFT: 184px; POSITION: absolute; TOP: 104px" runat="server" Width="152px"></asp:label>
				<asp:label id="Label1" style="Z-INDEX: 109; LEFT: 392px; POSITION: absolute; TOP: 104px" runat="server" Width="177px">No. of cases with same CPHH</asp:label>
				<asp:label id="lblNumberOfCasesValue" style="Z-INDEX: 110; LEFT: 584px; POSITION: absolute; TOP: 104px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblVLAMessage" style="Z-INDEX: 112; LEFT: 16px; POSITION: absolute; TOP: 216px" runat="server" Width="334px" Font-Bold="True" Visible="False">Cannot delete – VLA have entered data on this case</asp:label>
				<HR style="Z-INDEX: 113; LEFT: 16px; WIDTH: 96.14%; POSITION: absolute; TOP: 200px; HEIGHT: 1px" width="96.14%" SIZE="1">
				<asp:button id="btnLookUp" style="Z-INDEX: 114; LEFT: 376px; POSITION: absolute; TOP: 16px" runat="server" Width="104px" Text="Look Up"></asp:button>
				<asp:label id="lblAddress2Value" style="Z-INDEX: 115; LEFT: 184px; POSITION: absolute; TOP: 128px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblAddress3Value" style="Z-INDEX: 116; LEFT: 184px; POSITION: absolute; TOP: 152px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblPostcodeValue" style="Z-INDEX: 117; LEFT: 184px; POSITION: absolute; TOP: 176px" runat="server" Width="152px"></asp:label>
				<asp:button id="btnOK" style="Z-INDEX: 119; LEFT: 608px; POSITION: absolute; TOP: 216px" runat="server" Text="  OK  " Enabled="False"></asp:button>
				<asp:button id="btnCancel" style="Z-INDEX: 120; LEFT: 672px; POSITION: absolute; TOP: 216px" runat="server" Text=" Cancel " CausesValidation="False"></asp:button>
				<asp:label id="lblFarmMessage" style="Z-INDEX: 121; LEFT: 392px; POSITION: absolute; TOP: 136px" runat="server" Width="344px" Font-Bold="True" Visible="False">Farm will be deleted along with case</asp:label></DIV>
			<P>
				<uc1:VLAFooter id="VLAFooter1" runat="server"></uc1:VLAFooter></P>
		</form>
	</body>
</HTML>
