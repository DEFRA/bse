<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Register TagPrefix="uc1" TagName="MapReference" Src="MapReference.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="CaseEntryFarm.aspx.vb" Inherits="BSESystem.CaseEntryFarm" smartNavigation="true"%>
<%@ Register TagPrefix="uc1" TagName="ExitConfirmation" Src="ExitConfirmation.ascx" %>
<%@ Register TagPrefix="uc1" TagName="BatchNumberDisplay" Src="BatchNumberDisplay.ascx" %>
<%@ Register TagPrefix="uc1" TagName="CPHH" Src="CPHH.ascx" %>
<%@ Register TagPrefix="uc1" TagName="RBSE" Src="RBSE.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BSE System : Case Details</title>
		<META content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<META content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<META content="JavaScript" name="vs_defaultClientScript">
		<META content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<BODY>
		<FORM id="Form1" method="post" runat="server">
			<P><uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader></P>
			<DIV style="Z-INDEX: 400; WIDTH: 750px; POSITION: relative; HEIGHT: 40px" ms_positioning="GridLayout"><asp:label id="lblRBSEHeader" style="Z-INDEX: 101; LEFT: 8px; POSITION: absolute; TOP: 2px" runat="server" Width="200px" Font-Bold="True">RBSE Number:  12/12/12345</asp:label>
				<DIV style="Z-INDEX: 102; LEFT: 320px; WIDTH: 242px; POSITION: absolute; TOP: 0px; HEIGHT: 27px" ms_positioning="GridLayout"><uc1:batchnumberdisplay id="BatchNumberDisplay1" runat="server"></uc1:batchnumberdisplay></DIV>
				<asp:button id="btnSave2" style="Z-INDEX: 103; LEFT: 592px; POSITION: absolute; TOP: 0px" runat="server" Text="  Save  "></asp:button><asp:button id="btnCancel2" style="Z-INDEX: 104; LEFT: 664px; POSITION: absolute; TOP: 0px" runat="server" Text=" Cancel " CausesValidation="False"></asp:button>
				<DIV style="Z-INDEX: 104; LEFT: 496px; WIDTH: 88px; POSITION: absolute; TOP: 0px; HEIGHT: 32px" ms_positioning="GridLayout"><asp:literal id="litViewDocs2" runat="server"></asp:literal></DIV>
				<asp:label id="lblConfidential" style="Z-INDEX: 105; LEFT: 184px; POSITION: absolute; TOP: 8px" runat="server" Width="137px" Font-Bold="True" ForeColor="Red">CONFIDENTIAL DATA</asp:label></DIV>
			<DIV style="WIDTH: 754px; POSITION: relative; HEIGHT: 32px" ms_positioning="GridLayout">&nbsp;
				<DIV class="TabEnd" style="Z-INDEX: 101; LEFT: 0px; WIDTH: 8px; POSITION: absolute; TOP: 0px; HEIGHT: 27px"></DIV>
				<DIV class="SelectedTabTitle" style="Z-INDEX: 102; LEFT: 8px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px">Farm</DIV>
				<DIV class="UnselectedTabTitle" style="Z-INDEX: 105; LEFT: 108px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbCaseDEFRA" runat="server" CssClass="TABLINK">Case&nbsp;(DEFRA)</asp:linkbutton></DIV>
				<DIV class="UnselectedTabTitle" style="Z-INDEX: 106; LEFT: 208px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbBAB" runat="server" CssClass="tablink">BAB</asp:linkbutton></DIV>
				<DIV class="UnselectedTabTitle" style="Z-INDEX: 107; LEFT: 308px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbCaseVLA" runat="server" CssClass="tablink">Case (VLA)</asp:linkbutton></DIV>
				<DIV class="UnselectedTabTitle" style="Z-INDEX: 104; LEFT: 408px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbClinical" runat="server" CssClass="tablink">Clinical</asp:linkbutton></DIV>
				<DIV class="UnselectedTabTitle" style="Z-INDEX: 103; LEFT: 508px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbFeeds" runat="server" CssClass="tablink">Feeds</asp:linkbutton></DIV>
				<DIV class="UnselectedTabTitle" style="Z-INDEX: 108; LEFT: 608px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbRelations" runat="server" CssClass="tablink">Relations</asp:linkbutton></DIV>
				<DIV class="TabEnd" style="Z-INDEX: 109; LEFT: 708px; WIDTH: 42px; POSITION: absolute; TOP: 0px; HEIGHT: 27px"></DIV>
			</DIV>
			<DIV style="Z-INDEX: 200; WIDTH: 758px; POSITION: relative; HEIGHT: 650px" ms_positioning="GridLayout"><asp:label id="lblConfirmedCases" style="Z-INDEX: 101; LEFT: 12px; POSITION: absolute; TOP: 11px" runat="server" Width="416px" Font-Bold="True"> Number of Confirmed Cases:</asp:label><asp:label id="lblCPHH" style="Z-INDEX: 110; LEFT: 16px; POSITION: absolute; TOP: 48px" runat="server" Width="120px">CPHH</asp:label><asp:label id="lblOwnerName" style="Z-INDEX: 111; LEFT: 16px; POSITION: absolute; TOP: 96px" runat="server" Width="152px">Owner Name</asp:label><asp:label id="lblAddress" style="Z-INDEX: 112; LEFT: 16px; POSITION: absolute; TOP: 128px" runat="server" Width="152px">Address</asp:label><asp:label id="lblPostcode" style="Z-INDEX: 113; LEFT: 16px; POSITION: absolute; TOP: 224px" runat="server" Width="152px">Postcode</asp:label><asp:label id="lblCorrespondenceAddress" style="Z-INDEX: 114; LEFT: 16px; POSITION: absolute; TOP: 296px" runat="server" Width="152px">Correspondence Address</asp:label><asp:label id="lblParish" style="Z-INDEX: 115; LEFT: 400px; POSITION: absolute; TOP: 96px" runat="server" Width="154px">Parish</asp:label><asp:label id="lblCorrespondencePostcode" style="Z-INDEX: 116; LEFT: 16px; POSITION: absolute; TOP: 392px" runat="server" Width="168px">Correspondence Postcode</asp:label><asp:label id="lblCounty" style="Z-INDEX: 117; LEFT: 400px; POSITION: absolute; TOP: 160px" runat="server" Width="154px">County</asp:label><asp:label id="lblDistrict" style="Z-INDEX: 118; LEFT: 400px; POSITION: absolute; TOP: 128px" runat="server" Width="155px">District</asp:label><asp:label id="lblMapReference" style="Z-INDEX: 125; LEFT: 400px; POSITION: absolute; TOP: 232px" runat="server" Width="94px">Map Reference</asp:label><asp:label id="lblAHO" style="Z-INDEX: 126; LEFT: 400px; POSITION: absolute; TOP: 192px" runat="server" Width="154px">AHO</asp:label><asp:label id="lblHerdmark1" style="Z-INDEX: 131; LEFT: 16px; POSITION: absolute; TOP: 440px" runat="server" Width="152px">Herdmark 1</asp:label><asp:label id="lblHerdmark2" style="Z-INDEX: 137; LEFT: 16px; POSITION: absolute; TOP: 472px" runat="server" Width="152px">Herdmark 2</asp:label><asp:label id="lblNumericHerdmark1" style="Z-INDEX: 138; LEFT: 400px; POSITION: absolute; TOP: 440px" runat="server" Width="153px">Numeric Herdmark 1</asp:label><asp:label id="lblNumericHerdmark2" style="Z-INDEX: 139; LEFT: 400px; POSITION: absolute; TOP: 472px" runat="server" Width="156px">Numeric Herdmark 2</asp:label><asp:label id="lblHerdmark3" style="Z-INDEX: 140; LEFT: 16px; POSITION: absolute; TOP: 504px" runat="server" Width="152px">Herdmark 3</asp:label><asp:label id="lblHerdType" style="Z-INDEX: 143; LEFT: 16px; POSITION: absolute; TOP: 552px" runat="server" Width="152px">Herd Type</asp:label><asp:label id="lblPedigree" style="Z-INDEX: 144; LEFT: 400px; POSITION: absolute; TOP: 552px" runat="server" Width="157px">Pedigree</asp:label><asp:label id="lblIsADealer" style="Z-INDEX: 147; LEFT: 16px; POSITION: absolute; TOP: 584px" runat="server" Width="152px">Is A Dealer?</asp:label><asp:label id="lblInvalidMapReference" style="Z-INDEX: 157; LEFT: 640px; POSITION: absolute; TOP: 224px" runat="server" ForeColor="#9CCE00" CssClass="validatortext" ToolTip="This map reference isn't within the county" Visible="False">*</asp:label><asp:label id="lblNonGBCPHH" style="Z-INDEX: 158; LEFT: 448px; POSITION: absolute; TOP: 48px" runat="server" ForeColor="Red" CssClass="validatortext" ToolTip="The CPHH you have entered is for a non-GB Farm" Visible="False">*</asp:label><asp:regularexpressionvalidator id="revHerdmark1" style="Z-INDEX: 152; LEFT: 352px; POSITION: absolute; TOP: 440px" runat="server" CssClass="ValidatorText" ToolTip="[XXXX][NNNN]" ValidationExpression="[a-zA-Z]?[a-zA-Z]?[a-zA-Z]?[a-zA-Z]?[0-9]?[0-9]?[0-9]?[0-9]?" ControlToValidate="txtHerdmark1" ErrorMessage="*"></asp:regularexpressionvalidator><asp:regularexpressionvalidator id="revHerdmark2" style="Z-INDEX: 153; LEFT: 352px; POSITION: absolute; TOP: 472px" runat="server" CssClass="ValidatorText" ToolTip="[XXXX][NNNN]" ValidationExpression="[a-zA-Z]?[a-zA-Z]?[a-zA-Z]?[a-zA-Z]?[0-9]?[0-9]?[0-9]?[0-9]?" ControlToValidate="txtHerdmark2" ErrorMessage="*"></asp:regularexpressionvalidator><asp:regularexpressionvalidator id="revHerdmark3" style="Z-INDEX: 154; LEFT: 352px; POSITION: absolute; TOP: 504px" runat="server" CssClass="ValidatorText" ToolTip="[XXXX][NNNN]" ValidationExpression="[a-zA-Z]?[a-zA-Z]?[a-zA-Z]?[a-zA-Z]?[0-9]?[0-9]?[0-9]?[0-9]?" ControlToValidate="txtHerdmark3" ErrorMessage="*"></asp:regularexpressionvalidator><asp:regularexpressionvalidator id="revNumericHerdmark1" style="Z-INDEX: 155; LEFT: 728px; POSITION: absolute; TOP: 440px" runat="server" CssClass="ValidatorText" ToolTip="NNNNNN" ValidationExpression="[0-9][0-9][0-9][0-9][0-9][0-9]" ControlToValidate="txtNumericHerdmark1" ErrorMessage="*"></asp:regularexpressionvalidator><asp:regularexpressionvalidator id="revNumericHerdmark2" style="Z-INDEX: 156; LEFT: 728px; POSITION: absolute; TOP: 472px" runat="server" CssClass="ValidatorText" ToolTip="[NNNNNN]" ValidationExpression="[0-9][0-9][0-9][0-9][0-9][0-9]" ControlToValidate="txtNumericHerdmark2" ErrorMessage="*"></asp:regularexpressionvalidator>
				<HR style="Z-INDEX: 102; LEFT: 16px; WIDTH: 96.14%; POSITION: absolute; TOP: 40px; HEIGHT: 1px" width="96.14%" SIZE="1">
				<HR style="Z-INDEX: 123; LEFT: 16px; WIDTH: 96.03%; POSITION: absolute; TOP: 80px; HEIGHT: 1px" width="96.03%" SIZE="1">
				<HR style="Z-INDEX: 129; LEFT: 16px; WIDTH: 96.03%; POSITION: absolute; TOP: 256px; HEIGHT: 1px" width="96.03%" SIZE="1">
				<HR style="Z-INDEX: 130; LEFT: 16px; WIDTH: 96.03%; POSITION: absolute; TOP: 424px; HEIGHT: 1px" width="96.03%" SIZE="1">
				<HR style="Z-INDEX: 141; LEFT: 16px; WIDTH: 96.03%; POSITION: absolute; TOP: 536px; HEIGHT: 1px" width="96.03%" SIZE="1">
				<HR style="Z-INDEX: 146; LEFT: 16px; WIDTH: 96.03%; POSITION: absolute; TOP: 616px; HEIGHT: 1px" width="96.03%" SIZE="1">
				<DIV style="Z-INDEX: 104; LEFT: 472px; WIDTH: 152px; POSITION: absolute; TOP: 48px; HEIGHT: 32px" ms_positioning="GridLayout"><asp:literal id="litViewDocsForCPHH" runat="server"></asp:literal></DIV>
				<DIV style="Z-INDEX: 150; LEFT: 152px; WIDTH: 144px; POSITION: absolute; TOP: 48px; HEIGHT: 27px" ms_positioning="GridLayout"><uc1:cphh id="CPHH1" runat="server"></uc1:cphh></DIV>
				<asp:label id="lblADNSRegion" style="Z-INDEX: 161; LEFT: 400px; POSITION: absolute; TOP: 336px" runat="server" Width="154px">ADNS Region</asp:label><asp:label id="Label1" style="Z-INDEX: 162; LEFT: 400px; POSITION: absolute; TOP: 304px" runat="server" Width="154px">Local Authority</asp:label><asp:label id="Label2" style="Z-INDEX: 163; LEFT: 400px; POSITION: absolute; TOP: 272px" runat="server" Width="154px">Authority County</asp:label><asp:button id="btnLookUp" style="Z-INDEX: 124; LEFT: 336px; POSITION: absolute; TOP: 48px" runat="server" Width="104px" Text="Look Up"></asp:button><asp:textbox id="txtOwnerName" style="Z-INDEX: 106; LEFT: 184px; POSITION: absolute; TOP: 96px" runat="server" Width="168px" MaxLength="100"></asp:textbox><asp:textbox id="txtAddress1" style="Z-INDEX: 108; LEFT: 184px; POSITION: absolute; TOP: 128px" runat="server" Width="168px" MaxLength="50"></asp:textbox><asp:textbox id="txtAddress2" style="Z-INDEX: 109; LEFT: 184px; POSITION: absolute; TOP: 160px" runat="server" Width="168px" MaxLength="50"></asp:textbox><asp:textbox id="txtAddress3" style="Z-INDEX: 107; LEFT: 184px; POSITION: absolute; TOP: 192px" runat="server" Width="168px" MaxLength="50"></asp:textbox><asp:textbox id="txtPostcode" style="Z-INDEX: 103; LEFT: 184px; POSITION: absolute; TOP: 224px" runat="server" Width="168px" MaxLength="10"></asp:textbox><asp:textbox id="txtParish" style="Z-INDEX: 105; LEFT: 560px; POSITION: absolute; TOP: 96px" runat="server" Width="168px" MaxLength="50"></asp:textbox><asp:textbox id="txtDistrict" style="Z-INDEX: 104; LEFT: 560px; POSITION: absolute; TOP: 128px" runat="server" Width="168px" MaxLength="50"></asp:textbox><asp:dropdownlist id="ddlCounty" style="Z-INDEX: 128; LEFT: 560px; POSITION: absolute; TOP: 160px" runat="server" Width="168px" Font-Size="90%" Height="21"></asp:dropdownlist><asp:dropdownlist id="ddlAHO" style="Z-INDEX: 127; LEFT: 560px; POSITION: absolute; TOP: 192px" runat="server" Width="168px" Font-Size="90%" Height="22px"></asp:dropdownlist>
				<DIV style="Z-INDEX: 151; LEFT: 488px; WIDTH: 242px; POSITION: absolute; TOP: 224px; HEIGHT: 24px" ms_positioning="GridLayout"><uc1:mapreference id="MapReference1" runat="server"></uc1:mapreference><asp:button id="btnEstimate" style="Z-INDEX: 154; LEFT: 168px; POSITION: absolute; TOP: 0px" runat="server" Text="Estimate"></asp:button></DIV>
				<asp:button id="btnUseAboveAddress" style="Z-INDEX: 142; LEFT: 16px; POSITION: absolute; TOP: 264px" runat="server" Width="120px" Text="Use Above Address"></asp:button><asp:textbox id="txtCorrespondenceAddress1" style="Z-INDEX: 119; LEFT: 184px; POSITION: absolute; TOP: 296px" runat="server" Width="168px" MaxLength="50"></asp:textbox><asp:textbox id="txtCorrespondenceAddress2" style="Z-INDEX: 120; LEFT: 184px; POSITION: absolute; TOP: 328px" runat="server" Width="168px" MaxLength="50"></asp:textbox><asp:textbox id="txtCorrespondenceAddress3" style="Z-INDEX: 121; LEFT: 184px; POSITION: absolute; TOP: 360px" runat="server" Width="168px" MaxLength="50"></asp:textbox><asp:textbox id="txtCorrespondencePostcode" style="Z-INDEX: 122; LEFT: 184px; POSITION: absolute; TOP: 392px" runat="server" Width="168px" MaxLength="10"></asp:textbox><asp:dropdownlist id="ddlAuthorityCounty" style="Z-INDEX: 166; LEFT: 560px; POSITION: absolute; TOP: 272px" runat="server" Width="168px" Font-Size="90%" Height="21" AutoPostBack="True"></asp:dropdownlist><asp:dropdownlist id="ddlLocalAuthority" style="Z-INDEX: 165; LEFT: 560px; POSITION: absolute; TOP: 304px" runat="server" Width="168px" Font-Size="90%" Height="21" AutoPostBack="True"></asp:dropdownlist><asp:dropdownlist id="ddlADNSRegion" style="Z-INDEX: 164; LEFT: 560px; POSITION: absolute; TOP: 336px" runat="server" Width="168px" Font-Size="90%" Height="21"></asp:dropdownlist><asp:textbox id="txtHerdmark1" style="Z-INDEX: 132; LEFT: 184px; POSITION: absolute; TOP: 440px" runat="server" Width="168px" MaxLength="8"></asp:textbox><asp:textbox id="txtHerdmark2" style="Z-INDEX: 133; LEFT: 184px; POSITION: absolute; TOP: 472px" runat="server" Width="168px" MaxLength="8"></asp:textbox><asp:textbox id="txtHerdmark3" style="Z-INDEX: 134; LEFT: 184px; POSITION: absolute; TOP: 504px" runat="server" Width="168px" MaxLength="8"></asp:textbox><asp:textbox id="txtNumericHerdmark1" style="Z-INDEX: 135; LEFT: 560px; POSITION: absolute; TOP: 440px" runat="server" Width="168px" MaxLength="6"></asp:textbox><asp:textbox id="txtNumericHerdmark2" style="Z-INDEX: 136; LEFT: 560px; POSITION: absolute; TOP: 472px" runat="server" Width="168px" MaxLength="6"></asp:textbox><asp:dropdownlist id="ddlHerdType" style="Z-INDEX: 148; LEFT: 184px; POSITION: absolute; TOP: 552px" runat="server" Width="168px" Font-Size="90%"></asp:dropdownlist><asp:dropdownlist id="ddlPedigree" style="Z-INDEX: 149; LEFT: 560px; POSITION: absolute; TOP: 552px" runat="server" Width="168px" Font-Size="90%"></asp:dropdownlist><asp:checkbox id="chkIsDealer" style="Z-INDEX: 145; LEFT: 184px; POSITION: absolute; TOP: 584px" runat="server" Width="104px" Text=" "></asp:checkbox><asp:label id="lblLinkedFarms" style="Z-INDEX: 159; LEFT: 16px; POSITION: absolute; TOP: 632px" runat="server" Width="224px" Font-Bold="True">Linked Farms</asp:label><asp:checkbox id="chkNonGBFarm" style="Z-INDEX: 160; LEFT: 656px; POSITION: absolute; TOP: 48px" runat="server" Visible="False"></asp:checkbox>
				<DIV id="exitConfirmationDIV" style="Z-INDEX: 167; LEFT: 296px; WIDTH: 316px; POSITION: absolute; TOP: 8px; HEIGHT: 8px" ms_positioning="GridLayout"><uc1:exitconfirmation id="ctlExitConfirmation" runat="server"></uc1:exitconfirmation></DIV>
			</DIV>
			<asp:datagrid id="grdLinkedFarms" runat="server" Width="740px" AllowPaging="True" AllowSorting="True" AutoGenerateColumns="False">
				<SelectedItemStyle CssClass="GridSelectedItem"></SelectedItemStyle>
				<EditItemStyle CssClass="GridEditItem"></EditItemStyle>
				<ItemStyle CssClass="GridItem"></ItemStyle>
				<HeaderStyle CssClass="GridHeader"></HeaderStyle>
				<Columns>
					<asp:ButtonColumn Text="&lt;img src=&quot;Images/GridPager/sel.gif&quot;&gt;" CommandName="Select"></asp:ButtonColumn>
					<asp:TemplateColumn SortExpression="RelatedCPHH" HeaderText="CPHH">
						<ItemTemplate>
							<asp:Label id="lblLinkedCPHH" runat="server"></asp:Label>
						</ItemTemplate>
						<EditItemTemplate>
							<DIV style="WIDTH: 202px; POSITION: relative; HEIGHT: 30px" ms_positioning="GridLayout">
								<uc1:CPHH id="ctlLinkedCPHH" runat="server"></uc1:CPHH>
								<DIV style="LEFT: 175px; POSITION: absolute; TOP: 2px">
									<asp:Label id="lblRepeatedCPHH" runat="server" CssClass="validatortext" Visible="False" ForeColor="Red" ToolTip="You have entered this CPHH into the table already">*</asp:Label></DIV>
							</DIV>
						</EditItemTemplate>
					</asp:TemplateColumn>
					<asp:BoundColumn DataField="Status" SortExpression="Status" ReadOnly="True" HeaderText="Status"></asp:BoundColumn>
				</Columns>
				<PagerStyle Visible="False"></PagerStyle>
			</asp:datagrid>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 28px" ms_positioning="GridLayout">
				<P><uc1:datagridpager id="LinkedFarmsPager" runat="server"></uc1:datagridpager></P>
			</DIV>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 52px" ms_positioning="GridLayout">
				<HR style="Z-INDEX: 100; LEFT: 16px; WIDTH: 96.03%; POSITION: absolute; TOP: 16px; HEIGHT: 1px" width="96.03%" SIZE="1">
				<DIV class="GridHeader" style="DISPLAY: inline; Z-INDEX: 101; LEFT: 210px; WIDTH: 532px; POSITION: absolute; TOP: 32px; HEIGHT: 15px; TEXT-ALIGN: center" ms_positioning="FlowLayout">Lactation</DIV>
				<asp:label id="lblHerdSize" style="Z-INDEX: 102; LEFT: 16px; POSITION: absolute; TOP: 32px" runat="server" Width="160px" Font-Bold="True">Herd Size</asp:label></DIV>
			<asp:datagrid id="grdHerdSize" runat="server" AllowPaging="True" AllowSorting="True" AutoGenerateColumns="False">
				<SelectedItemStyle CssClass="GridSelectedItem"></SelectedItemStyle>
				<EditItemStyle CssClass="GridEditItem"></EditItemStyle>
				<ItemStyle CssClass="GridItem"></ItemStyle>
				<HeaderStyle CssClass="GridHeader"></HeaderStyle>
				<Columns>
					<asp:ButtonColumn Text="&lt;img src=&quot;Images/GridPager/sel.gif&quot;&gt;" CommandName="Select">
						<HeaderStyle Width="21px"></HeaderStyle>
					</asp:ButtonColumn>
					<asp:TemplateColumn SortExpression="HerdYear" HeaderText="Year">
						<HeaderStyle Width="81px"></HeaderStyle>
						<ItemTemplate>
							<DIV style="WIDTH: 81px; POSITION: relative" ms_positioning="GridLayout">
								<asp:Label id="lblGridYear" runat="server"></asp:Label>
								<DIV style="LEFT: 65px; POSITION: absolute; TOP: -4px">
									<asp:Label id="lblGridYearRepeated" runat="server" CssClass="validatortext" ToolTip="You have entered more than one row for this year." Visible="False" ForeColor="#9CCE00">*</asp:Label></DIV>
							</DIV>
						</ItemTemplate>
						<EditItemTemplate>
							<DIV style="WIDTH: 81px; POSITION: relative" ms_positioning="GridLayout">
								<asp:TextBox id="txtGridYear" runat="server" Width="64px" MaxLength="4"></asp:TextBox>
								<DIV style="LEFT: 65px; POSITION: absolute; TOP: -4px">
									<asp:RequiredFieldValidator id="rfvGridYear" runat="server" CssClass="Validatortext" ErrorMessage="*" ToolTip="You must enter a Year" ControlToValidate="txtGridYear"></asp:RequiredFieldValidator></DIV>
								<DIV style="LEFT: 65px; POSITION: absolute; TOP: -4px">
									<asp:RangeValidator id=rvGridYear runat="server" CssClass="ValidatorText" ErrorMessage="*" ToolTip="<%# GetDateText() %>" ControlToValidate="txtGridYear" MinimumValue="1975" MaximumValue="<%# Year(Now()) %>">
									</asp:RangeValidator></DIV>
							</DIV>
						</EditItemTemplate>
					</asp:TemplateColumn>
					<asp:TemplateColumn SortExpression="TotalSize" HeaderText="Size">
						<HeaderStyle Width="81px"></HeaderStyle>
						<ItemTemplate>
							<DIV style="WIDTH: 81px; POSITION: relative" ms_positioning="GridLayout">
								<asp:Label id="lblGridSize" runat="server"></asp:Label>
								<DIV style="LEFT: 65px; POSITION: absolute; TOP: -4px">
									<asp:Label id="lblGridSizeIncorrect" runat="server" CssClass="validatortext" ToolTip="The values in the Lactation fields do not sum to the herd size" Visible="False" ForeColor="#9CCE00">*</asp:Label></DIV>
							</DIV>
						</ItemTemplate>
						<EditItemTemplate>
							<DIV style="WIDTH: 81px; POSITION: relative" ms_positioning="GridLayout">
								<asp:TextBox id="txtGridSize" runat="server" Width="64px" MaxLength="4"></asp:TextBox>
								<DIV style="LEFT: 65px; POSITION: absolute; TOP: -4px">
									<asp:RequiredFieldValidator id="RequiredFieldValidator1" runat="server" CssClass="validatortext" ErrorMessage="*" ControlToValidate="txtGridSize" tooltip="You must enter a value for Herd Size"></asp:RequiredFieldValidator></DIV>
								<DIV style="LEFT: 65px; POSITION: absolute; TOP: -4px">
									<asp:RangeValidator id="rvGridSize" runat="server" CssClass="ValidatorText" ErrorMessage="*" ToolTip="Size must be between 1 and 1999" ControlToValidate="txtGridSize" MinimumValue="1" MaximumValue="1999" Type="Integer"></asp:RangeValidator></DIV>
							</DIV>
						</EditItemTemplate>
					</asp:TemplateColumn>
					<asp:TemplateColumn SortExpression="Lactation1Size" HeaderText="1">
						<HeaderStyle Width="50px"></HeaderStyle>
						<ItemTemplate>
							<asp:Label id="lblLactation1" runat="server"></asp:Label>
						</ItemTemplate>
						<EditItemTemplate>
							<DIV style="WIDTH: 50px; POSITION: relative" ms_positioning="GridLayout">
								<asp:TextBox id="txtLactation1" runat="server" Width="35" MaxLength="3"></asp:TextBox>
								<DIV style="LEFT: 36px; POSITION: absolute; TOP:-4px">
									<asp:RegularExpressionValidator id="revLactation1" runat="server" CssClass="validatorText" ToolTip="You must enter an integer value for this field" ErrorMessage="*" ControlToValidate="txtLactation1" ValidationExpression="[0-9]?[0-9]?[0-9]?"></asp:RegularExpressionValidator></DIV>
							</DIV>
						</EditItemTemplate>
					</asp:TemplateColumn>
					<asp:TemplateColumn SortExpression="Lactation2Size" HeaderText="2">
						<HeaderStyle Width="50px"></HeaderStyle>
						<ItemTemplate>
							<asp:Label id="lblLactation2" runat="server"></asp:Label>
						</ItemTemplate>
						<EditItemTemplate>
							<DIV style="WIDTH: 50px; POSITION: relative" ms_positioning="GridLayout">
								<asp:TextBox id="txtLactation2" runat="server" Width="35" MaxLength="3"></asp:TextBox>
								<DIV style="LEFT: 36px; POSITION: absolute; TOP: -4px">
									<asp:RegularExpressionValidator id="revLcatation2" runat="server" CssClass="validatortext" ToolTip="This value must be an integer" ErrorMessage="*" ControlToValidate="txtLactation2" ValidationExpression="[0-9]?[0-9]?[0-9]?"></asp:RegularExpressionValidator></DIV>
							</DIV>
							<DIV></DIV>
						</EditItemTemplate>
					</asp:TemplateColumn>
					<asp:TemplateColumn SortExpression="Lactation3Size" HeaderText="3">
						<HeaderStyle Width="50px"></HeaderStyle>
						<ItemTemplate>
							<asp:Label id="lblLactation3" runat="server"></asp:Label>
						</ItemTemplate>
						<EditItemTemplate>
							<DIV style="WIDTH: 50px; POSITION: relative" ms_positioning="GridLayout">
								<asp:TextBox id="txtLactation3" runat="server" Width="35" MaxLength="3"></asp:TextBox>
								<DIV style="LEFT: 36px; POSITION: absolute; TOP: -4px">
									<asp:RegularExpressionValidator id="revLactation3" runat="server" CssClass="validatortext" ToolTip="You must enter an integer for this value" ErrorMessage="*" ControlToValidate="txtLactation3" ValidationExpression="[0-9]?[0-9]?[0-9]?"></asp:RegularExpressionValidator></DIV>
							</DIV>
							<DIV></DIV>
						</EditItemTemplate>
					</asp:TemplateColumn>
					<asp:TemplateColumn SortExpression="Lactation4Size" HeaderText="4">
						<HeaderStyle Width="50px"></HeaderStyle>
						<ItemTemplate>
							<asp:Label id="lblLactation4" runat="server"></asp:Label>
						</ItemTemplate>
						<EditItemTemplate>
							<DIV style="WIDTH: 50px; POSITION: relative" ms_positioning="GridLayout">
								<asp:TextBox id="txtLactation4" runat="server" Width="35" MaxLength="3"></asp:TextBox>
								<DIV style="LEFT: 36px; POSITION: absolute; TOP: -4px">
									<asp:RegularExpressionValidator id="revLactation4" runat="server" CssClass="validatortext" ToolTip="You must enter an integer for this value" ErrorMessage="*" ControlToValidate="txtLactation4" ValidationExpression="[0-9]?[0-9]?[0-9]?"></asp:RegularExpressionValidator></DIV>
							</DIV>
							<DIV></DIV>
						</EditItemTemplate>
					</asp:TemplateColumn>
					<asp:TemplateColumn SortExpression="Lactation5Size" HeaderText="5">
						<HeaderStyle Width="50px"></HeaderStyle>
						<ItemTemplate>
							<asp:Label id="lblLactation5" runat="server"></asp:Label>
						</ItemTemplate>
						<EditItemTemplate>
							<DIV style="WIDTH: 50px; POSITION: relative" ms_positioning="GridLayout">
								<asp:TextBox id="txtLactation5" runat="server" Width="35" MaxLength="3"></asp:TextBox>
								<DIV style="LEFT: 36px; POSITION: absolute; TOP: -4px">
									<asp:RegularExpressionValidator id="revLactation5" runat="server" CssClass="validatortext" ToolTip="You must enter an integer for this value" ErrorMessage="*" ControlToValidate="txtLactation5" ValidationExpression="[0-9]?[0-9]?[0-9]?"></asp:RegularExpressionValidator></DIV>
							</DIV>
							<DIV></DIV>
						</EditItemTemplate>
					</asp:TemplateColumn>
					<asp:TemplateColumn SortExpression="Lactation6Size" HeaderText="6">
						<HeaderStyle Width="50px"></HeaderStyle>
						<ItemTemplate>
							<asp:Label id="lblLactation6" runat="server"></asp:Label>
						</ItemTemplate>
						<EditItemTemplate>
							<DIV style="WIDTH: 50px; POSITION: relative" ms_positioning="GridLayout">
								<asp:TextBox id="txtLactation6" runat="server" Width="35" MaxLength="3"></asp:TextBox>
								<DIV style="LEFT: 36px; POSITION: absolute; TOP: -4px">
									<asp:RegularExpressionValidator id="revLactation6" runat="server" CssClass="validatortext" ToolTip="You must enter an integer for this value" ErrorMessage="*" ControlToValidate="txtLactation6" ValidationExpression="[0-9]?[0-9]?[0-9]?"></asp:RegularExpressionValidator></DIV>
							</DIV>
							<DIV></DIV>
						</EditItemTemplate>
					</asp:TemplateColumn>
					<asp:TemplateColumn SortExpression="Lactation7Size" HeaderText="7">
						<HeaderStyle Width="50px"></HeaderStyle>
						<ItemTemplate>
							<asp:Label id="lblLactation7" runat="server"></asp:Label>
						</ItemTemplate>
						<EditItemTemplate>
							<DIV style="WIDTH: 50px; POSITION: relative" ms_positioning="GridLayout">
								<asp:TextBox id="txtLactation7" runat="server" Width="35" MaxLength="3"></asp:TextBox>
								<DIV style="LEFT: 36px; POSITION: absolute; TOP: -4px">
									<asp:RegularExpressionValidator id="revLactation7" runat="server" CssClass="validatortext" ToolTip="You must enter an integer for this value" ErrorMessage="*" ControlToValidate="txtLactation7" ValidationExpression="[0-9]?[0-9]?[0-9]?"></asp:RegularExpressionValidator></DIV>
							</DIV>
							<DIV></DIV>
						</EditItemTemplate>
					</asp:TemplateColumn>
					<asp:TemplateColumn SortExpression="Lactation8Size" HeaderText="8">
						<HeaderStyle Width="50px"></HeaderStyle>
						<ItemTemplate>
							<asp:Label id="lblLactation8" runat="server"></asp:Label>
						</ItemTemplate>
						<EditItemTemplate>
							<DIV style="WIDTH: 50px; POSITION: relative" ms_positioning="GridLayout">
								<asp:TextBox id="txtLactation8" runat="server" Width="35" MaxLength="3"></asp:TextBox>
								<DIV style="LEFT: 36px; POSITION: absolute; TOP: -4px">
									<asp:RegularExpressionValidator id="revLactation8" runat="server" CssClass="validatortext" ToolTip="You must enter an Integer for this value" ErrorMessage="*" ControlToValidate="txtLactation8" ValidationExpression="[0-9]?[0-9]?[0-9]?"></asp:RegularExpressionValidator></DIV>
							</DIV>
							<DIV></DIV>
						</EditItemTemplate>
					</asp:TemplateColumn>
					<asp:TemplateColumn SortExpression="Lactation9Size" HeaderText="9">
						<HeaderStyle Width="50px"></HeaderStyle>
						<ItemTemplate>
							<asp:Label id="lblLactation9" runat="server"></asp:Label>
						</ItemTemplate>
						<EditItemTemplate>
							<DIV style="WIDTH: 50px; POSITION: relative" ms_positioning="GridLayout">
								<asp:TextBox id="txtLactation9" runat="server" Width="35" MaxLength="3"></asp:TextBox>
								<DIV style="LEFT: 36px; POSITION: absolute; TOP: -4px">
									<asp:RegularExpressionValidator id="revLactation9" runat="server" CssClass="validatortext" ToolTip="You must enter an Integer for this value" ErrorMessage="*" ControlToValidate="txtLactation9" ValidationExpression="[0-9]?[0-9]?[0-9]?"></asp:RegularExpressionValidator></DIV>
							</DIV>
							<DIV></DIV>
						</EditItemTemplate>
					</asp:TemplateColumn>
					<asp:TemplateColumn SortExpression="Lactation10Size" HeaderText="10">
						<HeaderStyle Width="50px"></HeaderStyle>
						<ItemTemplate>
							<asp:Label id="lblLactation10" runat="server"></asp:Label>
						</ItemTemplate>
						<EditItemTemplate>
							<DIV style="WIDTH: 50px; POSITION: relative" ms_positioning="GridLayout">
								<asp:TextBox id="txtLactation10" runat="server" Width="35" MaxLength="3"></asp:TextBox>
								<DIV style="LEFT: 36px; POSITION: absolute; TOP: -4px">
									<asp:RegularExpressionValidator id="revLactation10" runat="server" CssClass="validatortext" ToolTip="You must enter an Integer for this value" ErrorMessage="*" ControlToValidate="txtLactation10" ValidationExpression="[0-9]?[0-9]?[0-9]?"></asp:RegularExpressionValidator></DIV>
							</DIV>
							<DIV></DIV>
						</EditItemTemplate>
					</asp:TemplateColumn>
					<asp:TemplateColumn SortExpression="Lactation1PlusSize" HeaderText="10+">
						<HeaderStyle Width="50px"></HeaderStyle>
						<ItemTemplate>
							<asp:Label id="lblLactation10Plus" runat="server"></asp:Label>
						</ItemTemplate>
						<EditItemTemplate>
							<DIV style="WIDTH: 50px; POSITION: relative" ms_positioning="GridLayout">
								<asp:TextBox id="txtLactation10Plus" runat="server" Width="35" MaxLength="3"></asp:TextBox>
								<DIV style="LEFT: 36px; POSITION: absolute; TOP: -4px">
									<asp:RegularExpressionValidator id="revLactation10Plus" runat="server" CssClass="validatortext" ToolTip="You must enter an Integer for this value" ErrorMessage="*" ControlToValidate="txtLactation10Plus" ValidationExpression="[0-9]?[0-9]?[0-9]?"></asp:RegularExpressionValidator></DIV>
							</DIV>
							<DIV></DIV>
						</EditItemTemplate>
					</asp:TemplateColumn>
				</Columns>
				<PagerStyle Visible="False"></PagerStyle>
			</asp:datagrid>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 28px" ms_positioning="GridLayout">
				<P><uc1:datagridpager id="HerdSizePager" runat="server"></uc1:datagridpager></P>
			</DIV>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 48px" ms_positioning="GridLayout">
				<HR style="Z-INDEX: 101; LEFT: 16px; WIDTH: 96.03%; POSITION: absolute; TOP: 16px; HEIGHT: 1px" width="96.03%" SIZE="1">
				<asp:button id="btnSave" style="Z-INDEX: 102; LEFT: 592px; POSITION: absolute; TOP: 24px" runat="server" Text="  Save  "></asp:button><asp:button id="btnCancel" style="Z-INDEX: 103; LEFT: 664px; POSITION: absolute; TOP: 24px" runat="server" Text=" Cancel " CausesValidation="False"></asp:button><asp:button id="btnFarmAuditLog" style="Z-INDEX: 151; LEFT: 16px; POSITION: absolute; TOP: 24px" runat="server" Width="109px" Text="Farm Audit Log"></asp:button>
				<DIV style="Z-INDEX: 104; LEFT: 496px; WIDTH: 88px; POSITION: absolute; TOP: 24px; HEIGHT: 32px" ms_positioning="GridLayout"><asp:literal id="litViewDocs" runat="server"></asp:literal></DIV>
			</DIV>
			<P><uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></P>
		</FORM>
	</BODY>
</HTML>
