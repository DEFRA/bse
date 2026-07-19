<%@ Page Language="vb" AutoEventWireup="false" Codebehind="FinalResultEntry.aspx.vb" Inherits="BSESystem.FinalResultEntry" smartNavigation="true"%>
<%@ Register TagPrefix="uc1" TagName="CalendarDate" Src="CalendarDate.ascx" %>
<%@ Register TagPrefix="uc1" TagName="RBSE" Src="RBSE.ascx" %>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : Final Result Entry</title>
		<META http-equiv="Content-Type" content="text/html; charset=windows-1252">
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<P><uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader></P>
			<DIV style="WIDTH: 791px; POSITION: relative; HEIGHT: 248px" ms_positioning="GridLayout">
				<HR style="Z-INDEX: 100; LEFT: 16px; WIDTH: 96.14%; POSITION: absolute; TOP: 40px; HEIGHT: 1px" width="96.14%" SIZE="1">
				<asp:label id="lblEartag" style="Z-INDEX: 101; LEFT: 16px; POSITION: absolute; TOP: 56px" runat="server" Width="152px">Eartag</asp:label>
				<asp:label id="lblDateOfBirth" style="Z-INDEX: 102; LEFT: 16px; POSITION: absolute; TOP: 88px" runat="server" Width="152px">Date Of Birth</asp:label>
				<asp:label id="lblCPHH" style="Z-INDEX: 103; LEFT: 16px; POSITION: absolute; TOP: 120px" runat="server" Width="152px">CPHH</asp:label>
				<asp:label id="lblEarTagValue" style="Z-INDEX: 104; LEFT: 184px; POSITION: absolute; TOP: 56px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblDateOfBirthValue" style="Z-INDEX: 105; LEFT: 184px; POSITION: absolute; TOP: 88px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblCPHHValue" style="Z-INDEX: 106; LEFT: 184px; POSITION: absolute; TOP: 120px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblName" style="Z-INDEX: 107; LEFT: 400px; POSITION: absolute; TOP: 56px" runat="server" Width="152px">Name</asp:label>
				<asp:label id="lblNameValue" style="Z-INDEX: 108; LEFT: 568px; POSITION: absolute; TOP: 56px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblAddress" style="Z-INDEX: 109; LEFT: 400px; POSITION: absolute; TOP: 88px" runat="server" Width="152px">Address</asp:label>
				<asp:label id="lblAddressValue" style="Z-INDEX: 110; LEFT: 568px; POSITION: absolute; TOP: 88px" runat="server" Width="152px"></asp:label>
				<HR style="Z-INDEX: 111; LEFT: 16px; WIDTH: 96.14%; POSITION: absolute; TOP: 144px; HEIGHT: 1px" width="96.14%" SIZE="1" id="HR1">
				<asp:label id="lblPurchaserBSE1Received" style="Z-INDEX: 112; LEFT: 16px; POSITION: absolute; TOP: 160px" runat="server" Width="168px">Purchaser BSE1 Received?</asp:label>
				<asp:label id="lblVendor1BSE1Received" style="Z-INDEX: 113; LEFT: 16px; POSITION: absolute; TOP: 192px" runat="server" Width="152px">Vendor1 BSE1 Received?</asp:label>
				<asp:label id="lblBreederBSE1Received" style="Z-INDEX: 114; LEFT: 400px; POSITION: absolute; TOP: 160px" runat="server" Width="152px">Breeder BSE1 Received?</asp:label>
				<asp:label id="lblHomebredBSE1Received" style="Z-INDEX: 115; LEFT: 400px; POSITION: absolute; TOP: 192px" runat="server" Width="160px">Homebred BSE1 Received?</asp:label>
				<asp:label id="lblSummarySheetReceived" style="Z-INDEX: 116; LEFT: 16px; POSITION: absolute; TOP: 224px" runat="server" Width="160px">Summary Sheet Received?</asp:label>
				<asp:label id="lblPaperworkComplete" style="Z-INDEX: 146; LEFT: 400px; POSITION: absolute; TOP: 224px" runat="server" Width="152px">Paperwork Complete?</asp:label>
				<asp:checkbox id="chkPurchaserBSE1Received" style="Z-INDEX: 147; LEFT: 184px; POSITION: absolute; TOP: 160px" runat="server" Width="112px" Text=" " Enabled="False"></asp:checkbox>
				<asp:checkbox id="chkVendor1BSE1Received" style="Z-INDEX: 148; LEFT: 183px; POSITION: absolute; TOP: 192px" runat="server" Width="112px" Text=" " Enabled="False"></asp:checkbox>
				<asp:checkbox id="chkSummarySheetReceived" style="Z-INDEX: 149; LEFT: 184px; POSITION: absolute; TOP: 224px" runat="server" Width="112px" Text=" " Enabled="False"></asp:checkbox>
				<asp:checkbox id="chkBreederBSE1Received" style="Z-INDEX: 150; LEFT: 568px; POSITION: absolute; TOP: 160px" runat="server" Width="112px" Text=" " Enabled="False"></asp:checkbox>
				<asp:checkbox id="chkHomebredBSE1Received" style="Z-INDEX: 151; LEFT: 568px; POSITION: absolute; TOP: 192px" runat="server" Width="112px" Text=" " Enabled="False"></asp:checkbox>
				<asp:checkbox id="chkPaperworkComplete" style="Z-INDEX: 152; LEFT: 568px; POSITION: absolute; TOP: 224px" runat="server" Width="112px" Text=" " Enabled="False"></asp:checkbox>
				<asp:label id="lblRBSE" style="Z-INDEX: 153; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Width="64px">RBSE</asp:label>
				<DIV style="Z-INDEX: 145; LEFT: 96px; WIDTH: 238px; POSITION: absolute; TOP: 8px; HEIGHT: 24px" ms_positioning="GridLayout">
					<uc1:rbse id="ctlRBSE" runat="server"></uc1:rbse>
					<asp:button id="btnLookup" style="Z-INDEX: 128; LEFT: 176px; POSITION: absolute; TOP: 0px" runat="server" Text="Look Up"></asp:button>
				</DIV>
                <asp:Label ID="lblPurchaserBSE1ReceivedDate" style="Z-INDEX: 154; LEFT: 207px; POSITION: absolute; TOP: 160px" runat="server"></asp:Label>
                <asp:Label ID="lblBreederBSE1ReceivedDate" style="Z-INDEX: 155; LEFT: 595px; POSITION: absolute; TOP: 160px" runat="server"></asp:Label>
                <asp:Label ID="lblVendor1BSE1ReceivedDate" style="Z-INDEX: 156; LEFT: 207px; POSITION: absolute; TOP: 192px" runat="server"></asp:Label>
                <asp:Label ID="lblHomebredBSE1ReceivedDate" style="Z-INDEX: 157; LEFT: 595px; POSITION: absolute; TOP: 192px" runat="server"></asp:Label>
                <asp:Label ID="lblSummarySheetReceivedDate" style="Z-INDEX: 158; LEFT: 207px; POSITION: absolute; TOP: 224px" runat="server"></asp:Label>
                <asp:Label ID="lblPaperworkCompleteDate" style="Z-INDEX: 159; LEFT: 595px; POSITION: absolute; TOP: 224px" runat="server"></asp:Label></DIV>
			<asp:datagrid id="grdTests" runat="server" Width="740px" AutoGenerateColumns="False" AllowSorting="True" AllowPaging="True">
				<SelectedItemStyle CssClass="GridSelectedItem"></SelectedItemStyle>
				<EditItemStyle CssClass="GridEditItem"></EditItemStyle>
				<ItemStyle CssClass="GridItem"></ItemStyle>
				<HeaderStyle CssClass="GridHeader"></HeaderStyle>
				<Columns>
					<asp:ButtonColumn Text="&lt;img src=&quot;Images/GridPager/sel.gif&quot;&gt;" CommandName="Select">
						<HeaderStyle Width="25px"></HeaderStyle>
					</asp:ButtonColumn>
					<asp:BoundColumn DataField="TestTypeDescription" SortExpression="TestTypeDescription" ReadOnly="True" HeaderText="Test Type"></asp:BoundColumn>
					<asp:BoundColumn DataField="TestResultDescription" SortExpression="TestResultDescription" ReadOnly="True" HeaderText="Test Result"></asp:BoundColumn>
				</Columns>
				<PagerStyle Visible="False"></PagerStyle>
			</asp:datagrid>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 28px" ms_positioning="GridLayout">
				<P><uc1:datagridpager id="TestsPager" runat="server"></uc1:datagridpager></P>
			</DIV>
			<DIV style="WIDTH: 757px; POSITION: relative; HEIGHT: 317px; left: 0px; top: 0px;" ms_positioning="GridLayout">
				<asp:label id="lblFinalResult" style="Z-INDEX: 160; LEFT: 16px; POSITION: absolute; TOP: 28px" runat="server" Width="158px">Final Result</asp:label>
                <asp:Label ID="lblCheckDNAMatch" runat="server" Font-Bold="True" ForeColor="Red"
                    Style="z-index: 160; left: 185px; position: absolute; top: 5px" Width="158px">Check DNA match</asp:Label>
				<asp:label id="lblFinalResultDate" style="Z-INDEX: 160; LEFT: 400px; POSITION: absolute; TOP: 28px" runat="server" Width="152px">Final Result Date</asp:label>
				<asp:label id="lblFinalResultDateValue" style="Z-INDEX: 160; LEFT: 568px; POSITION: absolute; TOP: 28px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblDBSE" style="Z-INDEX: 160; LEFT: 400px; POSITION: absolute; TOP: 107px" runat="server" Width="152px">DBSE</asp:label>
				<asp:label id="lblDBSEValue" style="Z-INDEX: 160; LEFT: 568px; POSITION: absolute; TOP: 107px" runat="server" Width="152px"></asp:label>
				<HR style="Z-INDEX: 160; LEFT: 15px; WIDTH: 96.14%; POSITION: absolute; TOP: 149px; HEIGHT: 1px" width="96.14%" SIZE="1">
				<asp:label id="lblTestType" style="Z-INDEX: 160; LEFT: 15px; POSITION: absolute; TOP: 165px" runat="server" Width="158px">Retrospective Test Type</asp:label>
				<asp:label id="lblDate" style="Z-INDEX: 160; LEFT: 15px; POSITION: absolute; TOP: 197px" runat="server" Width="158px">Retrospective Date</asp:label>
				<asp:label id="lblComments" style="Z-INDEX: 160; LEFT: 15px; POSITION: absolute; TOP: 229px" runat="server" Width="158px">Retrospective Comments</asp:label>
				<asp:label id="lblResult" style="Z-INDEX: 160; LEFT: 399px; POSITION: absolute; TOP: 165px" runat="server" Width="158px">Retrospective Result</asp:label>
				<asp:Label id="lblPositiveWarning" style="Z-INDEX: 160; LEFT: 352px; POSITION: absolute; TOP: 28px" runat="server" ForeColor="#9CCE00" CssClass="validatortext" ToolTip="You have selected positive but the paperwork is not complete" Visible="False">*</asp:Label>
				<asp:dropdownlist id="ddlFinalResult" style="Z-INDEX: 160; LEFT: 184px; POSITION: absolute; TOP: 28px" runat="server" Width="168px" AutoPostBack="True" Enabled="False"></asp:dropdownlist>
				<asp:Button id="btnDownload" style="Z-INDEX: 160; LEFT: 15px; POSITION: absolute; TOP: 108px" runat="server" Text="Download" Enabled="False"></asp:Button>
				<asp:dropdownlist id="ddlTestType" style="Z-INDEX: 160; LEFT: 183px; POSITION: absolute; TOP: 165px" runat="server" Width="168px"></asp:dropdownlist>
				<asp:dropdownlist id="ddlResult" style="Z-INDEX: 160; LEFT: 567px; POSITION: absolute; TOP: 165px" runat="server" Width="168px"></asp:dropdownlist>
				<asp:textbox id="txtComments" style="Z-INDEX: 160; LEFT: 15px; POSITION: absolute; TOP: 253px" runat="server" Width="723px" Height="54px" TextMode="MultiLine" MaxLength="500"></asp:textbox>
				<DIV style="Z-INDEX: 160; LEFT: 184px; WIDTH: 296px; POSITION: absolute; TOP: 196px; HEIGHT: 8px" ms_positioning="GridLayout">
					<uc1:CalendarDate id="ctlRetrospectiveDate" runat="server"></uc1:CalendarDate></DIV>
				<asp:Button id="btnPrintMemo" style="Z-INDEX: 160; LEFT: 103px; POSITION: absolute; TOP: 108px" runat="server" Text="Print Memo" Enabled="False"></asp:Button>
				<asp:Label id="lblDBSECleared" style="Z-INDEX: 160; LEFT: 720px; POSITION: absolute; TOP: 99px" runat="server" Visible="False" ToolTip="The DBSE has been cleared." CssClass="validatortext" ForeColor="#9CCE00">*</asp:Label>
				<asp:Label id="lblResultsNotMatching" style="Z-INDEX: 159; LEFT: 368px; POSITION: absolute; TOP: 28px" runat="server" Visible="False" ToolTip="You have selected positive but the paperwork is not complete" CssClass="validatortext" ForeColor="#9CCE00">*</asp:Label>
                <asp:Label ID="lblCaseType" runat="server" style="Z-INDEX: 160; LEFT: 16px; POSITION: absolute; TOP: 60px" Text="Case Type"></asp:Label>
                <asp:Label ID="lblLabComment" runat="server" style="Z-INDEX: 160; LEFT: 401px; POSITION: absolute; TOP: 60px" Text="Lab Comment"></asp:Label>
                <asp:label id="lblCaseTypeValue" style="Z-INDEX: 160; LEFT: 184px; POSITION: absolute; TOP: 60px" runat="server" Width="168px"></asp:label>
                <asp:TextBox ID="txtLabComment" runat="server" style="Z-INDEX: 160; LEFT: 566px; POSITION: absolute; TOP: 60px" Width="165px" MaxLength="255" TextMode="MultiLine" AutoPostBack="True"></asp:TextBox></DIV>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 54px" ms_positioning="GridLayout">
				<HR style="Z-INDEX: 101; LEFT: 16px; WIDTH: 96.03%; POSITION: absolute; TOP: 16px; HEIGHT: 1px" width="96.03%" SIZE="1">
				<asp:button id="btnSave" style="Z-INDEX: 102; LEFT: 592px; POSITION: absolute; TOP: 24px" runat="server" Text="  Save  "></asp:button>
				<asp:button id="btnCancel" style="Z-INDEX: 103; LEFT: 664px; POSITION: absolute; TOP: 24px" runat="server" Text=" Cancel " CausesValidation="False"></asp:button>
			</DIV>
			<P><uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></P>
		</form>
	</body>
</HTML>
