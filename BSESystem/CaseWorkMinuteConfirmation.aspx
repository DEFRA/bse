<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="CaseWorkMinuteConfirmation.aspx.vb" Inherits="BSESystem.CaseWorkMinuteConfirmation" %>

<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : Casework Minute Confirmation</title>
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:VLAHeader id="VLAHeader1" runat="server"></uc1:VLAHeader>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 233px" ms_positioning="GridLayout">
				<asp:label id="lblRBSE" style="Z-INDEX: 101; LEFT: 32px; POSITION: absolute; TOP: 16px" runat="server" Font-Bold="True" Width="416px">RBSE: </asp:label>
				<asp:label id="lblMinute" style="Z-INDEX: 102; LEFT: 32px; POSITION: absolute; TOP: 48px" runat="server" Font-Bold="True" Width="416px">Minute: </asp:label>
                <asp:Label ID="lblDate" runat="server" Font-Bold="True" Style="z-index: 102; left: 32px; position: absolute; top: 84px" Width="416px">Date: </asp:Label>
				<asp:Button id="btnDownload" style="Z-INDEX: 103; LEFT: 40px; POSITION: absolute; TOP: 193px" runat="server" Text="Download"></asp:Button>
				<asp:Button id="btnHome" style="Z-INDEX: 104; LEFT: 664px; POSITION: absolute; TOP: 193px" runat="server" Text="Home"></asp:Button>
                &nbsp;
				<asp:Button id="btnPrintMemo" style="Z-INDEX: 106; LEFT: 136px; POSITION: absolute; TOP: 193px" runat="server" Text="Print Memo"></asp:Button>
				<asp:Button id="btnBack" style="Z-INDEX: 106; LEFT: 519px; POSITION: absolute; TOP: 193px" runat="server" Text="Casework Entry"></asp:Button>
                <asp:Label ID="lblOutstandingForms" style="Z-INDEX: 106; LEFT: 32px; POSITION: absolute; TOP: 120px" runat="server" Text="Outstanding Forms:" Visible="False"></asp:Label>
                <asp:CheckBox ID="chkHomebred" style="Z-INDEX: 106; LEFT: 186px; POSITION: absolute; TOP: 117px" runat="server" Text="Homebred" Visible="False" Width="128px" AutoPostBack="True" />
                <asp:CheckBox ID="chkBreeder" style="Z-INDEX: 106; LEFT: 314px; POSITION: absolute; TOP: 117px" runat="server" Text="Breeder" Visible="False" Width="128px" AutoPostBack="True" />
                <asp:CheckBox ID="chkPurchaser" style="Z-INDEX: 106; LEFT: 442px; POSITION: absolute; TOP: 117px" runat="server" Text="Purchaser" Visible="False" Width="128px" AutoPostBack="True" />
                <asp:CheckBox ID="chkVendor" style="Z-INDEX: 106; LEFT: 186px; POSITION: absolute; TOP: 147px" runat="server" Text="Vendor" Visible="False" Width="128px" AutoPostBack="True" />
                <asp:CheckBox ID="chkSummarySheet" style="Z-INDEX: 106; LEFT: 314px; POSITION: absolute; TOP: 147px" runat="server" Text="Summary Sheet" Visible="False" Width="128px" AutoPostBack="True" />
                <asp:CheckBox ID="chkAllPaperwork" style="Z-INDEX: 106; LEFT: 442px; POSITION: absolute; TOP: 147px" runat="server" Text="All Paperwork" Visible="False" Width="128px" AutoPostBack="True" />
                <asp:Label ID="lblError" runat="server" CssClass="ValidatorText" EnableViewState="False"
                    ForeColor="Red" Style="z-index: 100; left: 148px; position: absolute; top: 115px"
                    ToolTip="At least one box must be checked" Visible="False">*</asp:Label>
             </DIV>   
			<uc1:VLAFooter id="VLAFooter1" runat="server"></uc1:VLAFooter>
		</form>
	</body>
</HTML>