<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="ExitConfirmation" Src="ExitConfirmation.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="CaseEntryFeeds.aspx.vb" Inherits="BSESystem.CaseEntryFeeds"%>
<%@ Register TagPrefix="uc1" TagName="BatchNumberDisplay" Src="BatchNumberDisplay.ascx" %>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Register TagPrefix="uc1" TagName="RBSE" Src="RBSE.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : Case Details</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<P><uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader></P>
			<DIV style="Z-INDEX: 400; WIDTH: 750px; POSITION: relative; HEIGHT: 40px" ms_positioning="GridLayout"><asp:label id="lblRBSEHeader" style="Z-INDEX: 101; LEFT: 8px; POSITION: absolute; TOP: 2px" runat="server" Width="208px" Font-Bold="True">RBSE Number:  12/12/12345</asp:label>
				<DIV style="Z-INDEX: 102; LEFT: 320px; WIDTH: 242px; POSITION: absolute; TOP: 0px; HEIGHT: 27px" ms_positioning="GridLayout"><uc1:batchnumberdisplay id="BatchNumberDisplay1" runat="server"></uc1:batchnumberdisplay></DIV>
				<asp:button id="btnSave2" style="Z-INDEX: 103; LEFT: 592px; POSITION: absolute; TOP: 0px" runat="server" Text="  Save  "></asp:button>
				<asp:button id="btnCancel2" style="Z-INDEX: 104; LEFT: 664px; POSITION: absolute; TOP: 0px" runat="server" Text=" Cancel " CausesValidation="False"></asp:button>
				<DIV style="Z-INDEX: 104; LEFT: 496px; WIDTH: 88px; POSITION: absolute; TOP: 0px; HEIGHT: 32px" ms_positioning="GridLayout">
					<asp:Literal id="litViewDocs2" runat="server"></asp:Literal></DIV>
				<asp:Label id="lblConfidential" style="Z-INDEX: 105; LEFT: 184px; POSITION: absolute; TOP: 8px" runat="server" Font-Bold="True" Width="138px" ForeColor="Red">CONFIDENTIAL DATA</asp:Label>
			</DIV>
			<div style="WIDTH: 754px; POSITION: relative; HEIGHT: 35px" ms_positioning="GridLayout">&nbsp;
				<div class="TabEnd" style="Z-INDEX: 100; LEFT: 0px; WIDTH: 8px; POSITION: absolute; TOP: 0px; HEIGHT: 27px"></div>
				<DIV class="UnselectedTabTitle" style="Z-INDEX: 103; LEFT: 8px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbFarm" runat="server" CssClass="tablink">Farm</asp:linkbutton></DIV>
				<div class="UnselectedTabTitle" style="Z-INDEX: 105; LEFT: 108px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbCaseDEFRA" runat="server" CssClass="TABLINK">Case&nbsp;(DEFRA)</asp:linkbutton></div>
				<div class="UnselectedTabTitle" style="Z-INDEX: 106; LEFT: 208px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbBAB" runat="server" CssClass="tablink">BAB</asp:linkbutton></div>
				<DIV class="UnselectedTabTitle" style="Z-INDEX: 107; LEFT: 308px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbCaseVLA" runat="server" CssClass="tablink">Case (VLA)</asp:linkbutton></DIV>
				<DIV class="UnselectedTabTitle" style="Z-INDEX: 104; LEFT: 408px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbClinical" runat="server" CssClass="tablink">Clinical</asp:linkbutton></DIV>
				<div class="SelectedTabTitle" style="Z-INDEX: 102; LEFT: 508px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px">Feeds</div>
				<DIV class="UnselectedTabTitle" style="Z-INDEX: 109; LEFT: 608px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbRelations" runat="server" CssClass="tablink">Relations</asp:linkbutton></DIV>
				<DIV class="TabEnd" style="Z-INDEX: 100; LEFT: 708px; WIDTH: 42px; POSITION: absolute; TOP: 0px; HEIGHT: 27px"></DIV>
				<DIV id="exitConfirmationDIV" style="Z-INDEX: 167; LEFT: 296px; WIDTH: 316px; POSITION: absolute; TOP: 8px; HEIGHT: 8px" ms_positioning="GridLayout">
					<uc1:ExitConfirmation id="ctlExitConfirmation" runat="server"></uc1:ExitConfirmation></DIV>
			</div>
			<P></P>
			<P><asp:datagrid id="grdFeeds" runat="server" Width="740px" AllowPaging="True" AllowSorting="True" AutoGenerateColumns="False">
					<SelectedItemStyle CssClass="GridSelectedItem"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItem"></EditItemStyle>
					<ItemStyle CssClass="GridItem"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:ButtonColumn Text="&lt;img src=&quot;Images/GridPager/sel.gif&quot;&gt;" CommandName="Select"></asp:ButtonColumn>
						<asp:BoundColumn DataField="YearFrom" SortExpression="YearFrom" HeaderText="Year From"></asp:BoundColumn>
						<asp:BoundColumn DataField="YearTo" SortExpression="YearTo" HeaderText="Year To"></asp:BoundColumn>
						<asp:BoundColumn DataField="RationDescription" SortExpression="RationDescription" HeaderText="Ration Type"></asp:BoundColumn>
						<asp:BoundColumn DataField="SupplierName" SortExpression="SupplierName" HeaderText="Supplier"></asp:BoundColumn>
						<asp:BoundColumn DataField="RationName" SortExpression="RationName" HeaderText="Ration Name"></asp:BoundColumn>
						<asp:BoundColumn DataField="IsPrePurchase" SortExpression="IsPrePurchase" HeaderText="Is Pre-purchase?"></asp:BoundColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid></P>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 28px" ms_positioning="GridLayout">
				<P><uc1:datagridpager id="FeedsPager" runat="server"></uc1:datagridpager></P>
			</DIV>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 172px" ms_positioning="GridLayout">
				<asp:label id="lblYearFrom" style="Z-INDEX: 100; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Width="152px">Year From</asp:label>
				<asp:label id="lblYearTo" style="Z-INDEX: 102; LEFT: 16px; POSITION: absolute; TOP: 48px" runat="server" Width="152px">Year To</asp:label>
				<asp:label id="lblRationType" style="Z-INDEX: 104; LEFT: 16px; POSITION: absolute; TOP: 80px" runat="server" Width="152px">Ration Type</asp:label>
				<asp:label id="lblRationName" style="Z-INDEX: 107; LEFT: 16px; POSITION: absolute; TOP: 112px" runat="server" Width="152px">Ration Name</asp:label>
				<asp:label id="lblIsPrePurchase" style="Z-INDEX: 108; LEFT: 16px; POSITION: absolute; TOP: 144px" runat="server" Width="152px">Is Pre-Purchase?</asp:label>
				<asp:label id="lblSupplier" style="Z-INDEX: 110; LEFT: 400px; POSITION: absolute; TOP: 16px" runat="server" Width="152px">Supplier</asp:label>
				<asp:label id="lblSupplierValue" style="Z-INDEX: 118; LEFT: 584px; POSITION: absolute; TOP: 16px" runat="server" Width="152px"></asp:label>
				<asp:Label id="lblLookupError" style="Z-INDEX: 121; LEFT: 400px; POSITION: absolute; TOP: 80px" runat="server" Width="338px" Visible="False" ForeColor="Red">You must validate the supplier before adding it to the table</asp:Label>
				<asp:regularexpressionvalidator id="revYearFrom" style="Z-INDEX: 129; LEFT: 352px; POSITION: absolute; TOP: 16px" runat="server" CssClass="ValidatorText" ErrorMessage="*" ToolTip="YYYY" ControlToValidate="txtYearFrom" ValidationExpression="[1-2][0-9][0-9][0-9]"></asp:regularexpressionvalidator>
				<asp:regularexpressionvalidator id="revYearTo" style="Z-INDEX: 130; LEFT: 352px; POSITION: absolute; TOP: 48px" runat="server" CssClass="ValidatorText" ErrorMessage="*" ToolTip="YYYY" ControlToValidate="txtYearTo" ValidationExpression="[1-2][0-9][0-9][0-9]"></asp:regularexpressionvalidator>
				<asp:textbox id="txtYearFrom" style="Z-INDEX: 101; LEFT: 184px; POSITION: absolute; TOP: 16px" runat="server" Width="168px" MaxLength="4"></asp:textbox>
				<asp:textbox id="txtYearTo" style="Z-INDEX: 103; LEFT: 184px; POSITION: absolute; TOP: 48px" runat="server" Width="168px" MaxLength="4"></asp:textbox>
				<asp:dropdownlist id="ddlRationType" style="Z-INDEX: 106; LEFT: 184px; POSITION: absolute; TOP: 80px" runat="server" Width="168px" Font-Size="90%"></asp:dropdownlist>
				<asp:textbox id="txtRationName" style="Z-INDEX: 105; LEFT: 184px; POSITION: absolute; TOP: 112px" runat="server" Width="168px" MaxLength="60"></asp:textbox>
				<asp:checkbox id="chkIsPrePurchase" style="Z-INDEX: 109; LEFT: 184px; POSITION: absolute; TOP: 144px" runat="server" Width="32px" Text=" "></asp:checkbox>
				<asp:textbox id="txtSupplier" style="Z-INDEX: 111; LEFT: 400px; POSITION: absolute; TOP: 48px" runat="server" Width="168px" MaxLength="30"></asp:textbox>
				<asp:button id="btnLookupSupplier" style="Z-INDEX: 112; LEFT: 584px; POSITION: absolute; TOP: 48px" runat="server" Text="Validate Supplier"></asp:button>
				<asp:button id="btnAddAsNew" style="Z-INDEX: 113; LEFT: 344px; POSITION: absolute; TOP: 144px" runat="server" Text="Add As New"></asp:button>
				<asp:button id="btnUpdateSelected" style="Z-INDEX: 114; LEFT: 456px; POSITION: absolute; TOP: 144px" runat="server" Text="Update Selected"></asp:button>
				<asp:button id="btnDeleteSelected" style="Z-INDEX: 115; LEFT: 608px; POSITION: absolute; TOP: 144px" runat="server" Text="Delete Selected"></asp:button>
				<asp:textbox id="txtID" style="Z-INDEX: 119; LEFT: 312px; POSITION: absolute; TOP: 144px" runat="server" Width="16px" Visible="False"></asp:textbox>
				<asp:textbox id="txtSupplierID" style="Z-INDEX: 120; LEFT: 400px; POSITION: absolute; TOP: 112px" runat="server" Width="72px" Visible="False"></asp:textbox>
				<asp:Label id="lblYearFromEmpty" style="Z-INDEX: 126; LEFT: 352px; POSITION: absolute; TOP: 16px" runat="server" CssClass="ValidatorText" ForeColor="Red" Visible="False" ToolTip="You must enter a Year From value.">*</asp:Label>
				<asp:Label id="lblYearToEmpty" style="Z-INDEX: 122; LEFT: 352px; POSITION: absolute; TOP: 48px" runat="server" CssClass="ValidatorText" ForeColor="Red" Visible="False" Height="16px" ToolTip="You must enter a Year To value.">*</asp:Label>
				<asp:Label id="lblRationTypeEmpty" style="Z-INDEX: 128; LEFT: 352px; POSITION: absolute; TOP: 80px" runat="server" CssClass="ValidatorText" ForeColor="Red" Visible="False" Height="16px" ToolTip="You must choose a Ration Type">*</asp:Label>
				<asp:Label id="lblYearFromInvalid" style="Z-INDEX: 125; LEFT: 352px; POSITION: absolute; TOP: 16px" runat="server" CssClass="ValidatorText" ForeColor="Red" Visible="False" ToolTip="The date you entered was before the Birth Year.">*</asp:Label>
				<asp:Label id="lblYearToInvalid" style="Z-INDEX: 127; LEFT: 352px; POSITION: absolute; TOP: 48px" runat="server" CssClass="ValidatorText" ForeColor="Red" Visible="False" ToolTip="The date you entered was before the Year From Value.">*</asp:Label>
			</DIV>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 48px" ms_positioning="GridLayout">
				<P></P>
				<HR style="Z-INDEX: 101; LEFT: 16px; WIDTH: 96.03%; POSITION: absolute; TOP: 16px; HEIGHT: 1px" width="96.03%" SIZE="1">
				<asp:button id="btnSave" style="Z-INDEX: 102; LEFT: 592px; POSITION: absolute; TOP: 24px" runat="server" Text="  Save  "></asp:button><asp:button id="btnCancel" style="Z-INDEX: 103; LEFT: 664px; POSITION: absolute; TOP: 24px" runat="server" Text=" Cancel " CausesValidation="False"></asp:button>
				<DIV style="Z-INDEX: 104; LEFT: 496px; WIDTH: 88px; POSITION: absolute; TOP: 24px; HEIGHT: 32px" ms_positioning="GridLayout">
					<asp:Literal id="litViewDocs" runat="server"></asp:Literal></DIV>
			</DIV>
			<P><uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></P>
		</form>
	</body>
</HTML>
