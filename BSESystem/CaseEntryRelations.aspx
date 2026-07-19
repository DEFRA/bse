<%@ Register TagPrefix="uc1" TagName="RBSE" Src="RBSE.ascx" %>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Register TagPrefix="uc1" TagName="BatchNumberDisplay" Src="BatchNumberDisplay.ascx" %>
<%@ Register TagPrefix="uc1" TagName="PartialDate" Src="PartialDate.ascx" %>
<%@ Register TagPrefix="uc1" TagName="ExitConfirmation" Src="ExitConfirmation.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="CaseEntryRelations.aspx.vb" Inherits="BSESystem.CaseEntryRelations" smartNavigation="true"%>
<%@ Register TagPrefix="uc1" TagName="ThreePartEartag" Src="ThreePartEartag.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="CalendarDate" Src="CalendarDate.ascx" %>
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
			<DIV style="Z-INDEX: 400; WIDTH: 750px; POSITION: relative; HEIGHT: 40px" ms_positioning="GridLayout"><asp:label id="lblRBSEHeader" style="Z-INDEX: 100; LEFT: 8px; POSITION: absolute; TOP: 2px" runat="server" Width="302px" Font-Bold="True">RBSE Number:  12/12/12345</asp:label>
				<DIV style="Z-INDEX: 101; LEFT: 320px; WIDTH: 242px; POSITION: absolute; TOP: 0px; HEIGHT: 27px" ms_positioning="GridLayout"><uc1:batchnumberdisplay id="BatchNumberDisplay1" runat="server"></uc1:batchnumberdisplay></DIV>
				<asp:button id="btnSave2" style="Z-INDEX: 102; LEFT: 592px; POSITION: absolute; TOP: 0px" runat="server" Text="  Save  "></asp:button><asp:button id="btnCancel2" style="Z-INDEX: 103; LEFT: 664px; POSITION: absolute; TOP: 0px" runat="server" Text=" Cancel " CausesValidation="False"></asp:button>
				<DIV style="Z-INDEX: 104; LEFT: 496px; WIDTH: 88px; POSITION: absolute; TOP: 0px; HEIGHT: 32px" ms_positioning="GridLayout">
					<asp:Literal id="litViewDocs2" runat="server"></asp:Literal></DIV>	
			</DIV>
			<DIV style="WIDTH: 754px; POSITION: relative; HEIGHT: 32px" ms_positioning="GridLayout">&nbsp;
				<DIV class="TabEnd" style="Z-INDEX: 101; LEFT: 0px; WIDTH: 8px; POSITION: absolute; TOP: 0px; HEIGHT: 27px"></DIV>
				<DIV class="UnselectedTabTitle" style="Z-INDEX: 108; LEFT: 8px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbFarm" runat="server" CssClass="tablink">Farm</asp:linkbutton></DIV>
				<DIV class="UnselectedTabTitle" style="Z-INDEX: 105; LEFT: 108px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbCaseDEFRA" runat="server" CssClass="TABLINK">Case&nbsp;(DEFRA)</asp:linkbutton></DIV>
				<DIV class="UnselectedTabTitle" style="Z-INDEX: 106; LEFT: 208px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbBAB" runat="server" CssClass="tablink">BAB</asp:linkbutton></DIV>
				<DIV class="UnselectedTabTitle" style="Z-INDEX: 107; LEFT: 308px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbCaseVLA" runat="server" CssClass="tablink">Case (VLA)</asp:linkbutton></DIV>
				<DIV class="UnselectedTabTitle" style="Z-INDEX: 104; LEFT: 408px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbClinical" runat="server" CssClass="tablink">Clinical</asp:linkbutton></DIV>
				<DIV class="UnselectedTabTitle" style="Z-INDEX: 103; LEFT: 508px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px"><asp:linkbutton id="hlbFeeds" runat="server" CssClass="tablink">Feeds</asp:linkbutton></DIV>
				<DIV class="SelectedTabTitle" style="Z-INDEX: 102; LEFT: 608px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 25px">Relations</DIV>
				<DIV class="TabEnd" style="Z-INDEX: 109; LEFT: 708px; WIDTH: 42px; POSITION: absolute; TOP: 0px; HEIGHT: 27px"></DIV>
			</DIV>
			<DIV style="Z-INDEX: 100; WIDTH: 750px; POSITION: relative; HEIGHT: 560px" ms_positioning="GridLayout">
				<asp:label id="lblDamEartag" style="Z-INDEX: 101; LEFT: 16px; POSITION: absolute; TOP: 320px" runat="server" Width="152px">Eartag</asp:label>
				<asp:label id="lblDamName" style="Z-INDEX: 102; LEFT: 408px; POSITION: absolute; TOP: 320px" runat="server" Width="152px">Name</asp:label>
				<asp:label id="lblDamBirthDate" style="Z-INDEX: 103; LEFT: 408px; POSITION: absolute; TOP: 424px" runat="server" Width="152px">Date of Birth</asp:label>
				<asp:label id="lblDamRBSE" style="Z-INDEX: 104; LEFT: 16px; POSITION: absolute; TOP: 352px" runat="server" Width="152px">RBSE</asp:label>
				<asp:label id="lblDamDetails" style="Z-INDEX: 107; LEFT: 16px; POSITION: absolute; TOP: 288px" runat="server" Width="152px" Font-Bold="True">Dam Details</asp:label>
				<asp:label id="lblDamFate" style="Z-INDEX: 110; LEFT: 16px; POSITION: absolute; TOP: 456px" runat="server" Width="152px">Fate</asp:label>
				<asp:label id="Status" style="Z-INDEX: 111; LEFT: 16px; POSITION: absolute; TOP: 424px" runat="server" Width="152px">Status</asp:label>
				<asp:label id="lblFinalResult" style="Z-INDEX: 112; LEFT: 408px; POSITION: absolute; TOP: 456px" runat="server" Width="152px">Final Result</asp:label>
				<asp:label id="lblDamFinalResultValue" style="Z-INDEX: 113; LEFT: 568px; POSITION: absolute; TOP: 456px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblSireDetails" style="Z-INDEX: 115; LEFT: 16px; POSITION: absolute; TOP: 72px" runat="server" Width="152px" Font-Bold="True">Sire Details</asp:label>
				<asp:label id="lblSireEartag" style="Z-INDEX: 116; LEFT: 16px; POSITION: absolute; TOP: 104px" runat="server" Width="152px">Eartag</asp:label>
				<asp:label id="lblSireName" style="Z-INDEX: 117; LEFT: 408px; POSITION: absolute; TOP: 104px" runat="server" Width="152px">Name</asp:label>
				<asp:label id="lblSireRBSE" style="Z-INDEX: 119; LEFT: 16px; POSITION: absolute; TOP: 136px" runat="server" Width="152px">RBSE</asp:label>
				<asp:label id="lblSireFate" style="Z-INDEX: 120; LEFT: 16px; POSITION: absolute; TOP: 208px" runat="server" Width="152px">Fate</asp:label>
				<asp:label id="lblSireBirthDate" style="Z-INDEX: 121; LEFT: 408px; POSITION: absolute; TOP: 208px" runat="server" Width="152px">Date of Birth</asp:label>
				<asp:label id="lblDamFateValue" style="Z-INDEX: 123; LEFT: 184px; POSITION: absolute; TOP: 456px" runat="server" Width="208px"></asp:label>
				<asp:label id="lblSireFateValue" style="Z-INDEX: 125; LEFT: 184px; POSITION: absolute; TOP: 208px" runat="server" Width="208px"></asp:label>
				<asp:label id="lblDamID" style="Z-INDEX: 127; LEFT: 184px; POSITION: absolute; TOP: 288px" runat="server" Width="152px" Font-Bold="True"></asp:label>
				<asp:label id="lblSireID" style="Z-INDEX: 128; LEFT: 184px; POSITION: absolute; TOP: 72px" runat="server" Width="152px" Font-Bold="True"></asp:label>
				<asp:label id="lblOtherRelations" style="Z-INDEX: 133; LEFT: 16px; POSITION: absolute; TOP: 536px" runat="server" Width="152px" Font-Bold="True">Other Relations</asp:label>
				<asp:label id="lblDamOffspring" style="Z-INDEX: 134; LEFT: 408px; POSITION: absolute; TOP: 288px" runat="server" Width="152px">Number of Offspring:</asp:label>
				<asp:label id="lblDamOffspringValue" style="Z-INDEX: 135; LEFT: 568px; POSITION: absolute; TOP: 288px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblSireOffspring" style="Z-INDEX: 136; LEFT: 408px; POSITION: absolute; TOP: 72px" runat="server" Width="152px">Number of Offspring:</asp:label>
				<asp:label id="lblSireOffspringValue" style="Z-INDEX: 137; LEFT: 576px; POSITION: absolute; TOP: 72px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblCaseHerdbook" style="Z-INDEX: 139; LEFT: 16px; POSITION: absolute; TOP: 24px" runat="server" Width="152px">Case Herdbook</asp:label>
				<asp:label id="lblDamHerdbook" style="Z-INDEX: 141; LEFT: 408px; POSITION: absolute; TOP: 352px" runat="server" Width="152px">Herdbook</asp:label>
				<asp:label id="lblSireHerdbook" style="Z-INDEX: 142; LEFT: 408px; POSITION: absolute; TOP: 136px" runat="server" Width="152px">Herdbook</asp:label>
				<DIV id="exitConfirmationDIV" style="Z-INDEX: 148; LEFT: 296px; WIDTH: 316px; POSITION: absolute; TOP: 8px; HEIGHT: 8px" ms_positioning="GridLayout">
					<uc1:ExitConfirmation id="ctlExitConfirmation" runat="server"></uc1:ExitConfirmation></DIV>
				<HR style="Z-INDEX: 105; LEFT: 16px; WIDTH: 96.03%; POSITION: absolute; TOP: 272px; HEIGHT: 1px" width="96.03%" SIZE="1">
				<HR style="Z-INDEX: 138; LEFT: 16px; WIDTH: 96.03%; POSITION: absolute; TOP: 56px; HEIGHT: 1px" width="96.03%" SIZE="1">
				<HR style="Z-INDEX: 147; LEFT: 16px; WIDTH: 96.03%; POSITION: absolute; TOP: 520px; HEIGHT: 1px" width="96.03%" SIZE="1">
				<asp:textbox id="txtCaseHerdbook" style="Z-INDEX: 140; LEFT: 184px; POSITION: absolute; TOP: 24px" runat="server" Width="168px" MaxLength="15"></asp:textbox>
				<asp:textbox id="txtSireEartag" style="Z-INDEX: 130; LEFT: 184px; POSITION: absolute; TOP: 104px;" runat="server" ms_positioning="FlowLayout" Width="168px"></asp:textbox>
				<asp:textbox id="txtSireName" style="Z-INDEX: 118; LEFT: 568px; POSITION: absolute; TOP: 104px" runat="server" Width="168px"></asp:textbox>
				<DIV style="Z-INDEX: 126; LEFT: 184px; WIDTH: 184px; POSITION: absolute; TOP: 128px; HEIGHT: 32px" ms_positioning="FlowLayout">
					<uc1:rbse id="ctlSireRBSE" runat="server"></uc1:rbse>
				</DIV>
				<asp:textbox id="txtSireHerdbook" style="Z-INDEX: 143; LEFT: 568px; POSITION: absolute; TOP: 136px" runat="server" Width="168px" MaxLength="15"></asp:textbox>
				<asp:button id="btnSireLookUp" style="Z-INDEX: 146; LEFT: 16px; POSITION: absolute; TOP: 168px" runat="server" Width="63px" Text="Look Up"></asp:button>
				<DIV style="Z-INDEX: 132; LEFT: 576px; WIDTH: 170px; POSITION: absolute; TOP: 200px; HEIGHT: 56px" ms_positioning="FlowLayout">
					<uc1:partialdate id="ctlSireBirthDate" runat="server"></uc1:partialdate>
				</DIV>
				<asp:Button id="btnRemoveSire" style="Z-INDEX: 149; LEFT: 16px; POSITION: absolute; TOP: 240px" runat="server" Text="Remove Sire"></asp:Button>
				<asp:button id="btnSireRelations" style="Z-INDEX: 122; LEFT: 128px; POSITION: absolute; TOP: 240px" runat="server" Width="136px" Text="View Sire's Relations"></asp:button>
				<asp:textbox style="Z-INDEX: 129; LEFT: 184px; WIDTH: 168px; POSITION: absolute; TOP: 320px" runat="server" id="txtDamEartag" ms_positioning="FlowLayout"></asp:textbox>
				<asp:textbox id="txtDamName" style="Z-INDEX: 109; LEFT: 568px; POSITION: absolute; TOP: 320px" runat="server" Width="168px" MaxLength="80"></asp:textbox>
				<DIV style="Z-INDEX: 124; LEFT: 184px; WIDTH: 184px; POSITION: absolute; TOP: 352px; HEIGHT: 32px" ms_positioning="FlowLayout">
					<uc1:rbse id="ctlDamRBSE" runat="server"></uc1:rbse>
				</DIV>
				<asp:textbox id="txtDamHerdbook" style="Z-INDEX: 144; LEFT: 568px; POSITION: absolute; TOP: 352px" runat="server" Width="168px" MaxLength="15"></asp:textbox>
				<asp:button id="btnDamLookUp" style="Z-INDEX: 108; LEFT: 16px; POSITION: absolute; TOP: 384px" runat="server" Width="67px" Text="Look Up"></asp:button>
				<asp:dropdownlist id="ddlDamStatus" style="Z-INDEX: 106; LEFT: 184px; POSITION: absolute; TOP: 424px" runat="server" Width="184px" Font-Size="90%"></asp:dropdownlist>
				<DIV style="Z-INDEX: 131; LEFT: 568px; WIDTH: 178px; POSITION: absolute; TOP: 416px; HEIGHT: 58px" ms_positioning="FlowLayout">
					<uc1:partialdate id="ctlDamBirthDate" runat="server"></uc1:partialdate>
				</DIV>
				<asp:Button id="btnRemoveDam" style="Z-INDEX: 150; LEFT: 16px; POSITION: absolute; TOP: 488px" runat="server" Text="Remove Dam"></asp:Button>
				<asp:button id="btnDamRelations" style="Z-INDEX: 114; LEFT: 136px; POSITION: absolute; TOP: 488px" runat="server" Width="136px" Text="View Dam's Relations"></asp:button>
			</DIV>
			<P><asp:datagrid id="grdRelations" runat="server" Width="740px" AllowPaging="True" AllowSorting="True" AutoGenerateColumns="False">
					<SelectedItemStyle CssClass="GridSelectedItem"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItem"></EditItemStyle>
					<ItemStyle CssClass="GridItem"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:ButtonColumn Text="&lt;img src=&quot;Images/GridPager/sel.gif&quot;&gt;" CommandName="Select"></asp:ButtonColumn>
						<asp:TemplateColumn SortExpression="RelationType" HeaderText="Relation Type">
							<HeaderStyle Width="80px"></HeaderStyle>
							<ItemTemplate>
								<asp:Label runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.RelationTypeDesc") %>'>
								</asp:Label>
							</ItemTemplate>
							<EditItemTemplate>
								<asp:Label runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.RelationTypeDesc") %>' ID="Label2">
								</asp:Label>
							</EditItemTemplate>
						</asp:TemplateColumn>
						<asp:TemplateColumn SortExpression="RelationRBSE" HeaderText="RBSE">
							<ItemTemplate>
								<asp:Label id=Label1 runat="server" Text='<%# FormatRBSE(DataBinder.Eval(Container, "DataItem.RelationRBSE").ToString()) %>'>
								</asp:Label>
							</ItemTemplate>
							<EditItemTemplate>
								<asp:Label id="Label4" runat="server" Text='<%# FormatRBSE(DataBinder.Eval(Container, "DataItem.RelationRBSE").ToString()) %>'>
								</asp:Label>
							</EditItemTemplate>
						</asp:TemplateColumn>
						<asp:TemplateColumn SortExpression="Sex" HeaderText="Sex">
							<ItemTemplate>
								<asp:Label runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.SexDesc") %>'>
								</asp:Label>
							</ItemTemplate>
							<EditItemTemplate>
								<asp:Label runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.SexDesc") %>' ID="Label5">
								</asp:Label>
							</EditItemTemplate>
						</asp:TemplateColumn>
						<asp:TemplateColumn SortExpression="BirthYear, BirthMonth, BirthDay" HeaderText="Birth Date">
							<ItemTemplate>
								<asp:Label runat="server" Text='<%# FormatPartialDate(DataBinder.Eval(Container, "DataItem.BirthDay").tostring(), DataBinder.Eval(Container, "DataItem.BirthMonth").tostring(), DataBinder.Eval(Container, "DataItem.BirthYear").tostring()) %>'>
								</asp:Label>
							</ItemTemplate>
							<EditItemTemplate>
								<asp:TextBox runat="server" Text='<%# FormatPartialDate(DataBinder.Eval(Container, "DataItem.BirthDay").tostring(), DataBinder.Eval(Container, "DataItem.BirthMonth").tostring(), DataBinder.Eval(Container, "DataItem.BirthYear").tostring()) %>'>
								</asp:TextBox>
							</EditItemTemplate>
						</asp:TemplateColumn>
						<asp:TemplateColumn SortExpression="RelationFate" HeaderText="Fate">
							<ItemTemplate>
								<asp:Label runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.RelationFateDesc") %>'>
								</asp:Label>
							</ItemTemplate>
							<EditItemTemplate>
								<asp:Label runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.RelationFateDesc") %>' ID="Label6">
								</asp:Label>
							</EditItemTemplate>
						</asp:TemplateColumn>
						<asp:TemplateColumn SortExpression="LeftDate" HeaderText="Date Left">
							<ItemTemplate>
								<asp:Label id=Label3 runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.LeftDate", "{0:d}")  %>'>
								</asp:Label>
							</ItemTemplate>
							<EditItemTemplate>
								<asp:Label id="Label7" runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.LeftDate", "{0:d}")  %>'>
								</asp:Label>
							</EditItemTemplate>
						</asp:TemplateColumn>
						<asp:TemplateColumn SortExpression="Eartag" HeaderText="Eartag">
							<ItemTemplate>
								<asp:Label runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.EartagCountry") + " " + DataBinder.Eval(Container, "DataItem.EartagHerdmark") + " " + DataBinder.Eval(Container, "DataItem.Eartag") %>'>
								</asp:Label>
							</ItemTemplate>
							<EditItemTemplate>
								<asp:TextBox runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.EartagCountry") + " " + DataBinder.Eval(Container, "DataItem.EartagHerdmark") + " " + DataBinder.Eval(Container, "DataItem.Eartag") %>'>
								</asp:TextBox>
							</EditItemTemplate>
						</asp:TemplateColumn>
						<asp:BoundColumn DataField="Sire" SortExpression="Sire" HeaderText="Sire"></asp:BoundColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid></P>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 28px" ms_positioning="GridLayout">
				<P><uc1:datagridpager id="RelationsPager" runat="server"></uc1:datagridpager></P>
			</DIV>
			<DIV style="WIDTH: 816px; POSITION: relative; HEIGHT: 226px" ms_positioning="GridLayout"><asp:label id="lblBirthDate" style="Z-INDEX: 112; LEFT: 448px; POSITION: absolute; TOP: 48px" runat="server" Width="152px">Date of Birth</asp:label><asp:label id="lblRelationRBSE" style="Z-INDEX: 109; LEFT: 448px; POSITION: absolute; TOP: 16px" runat="server" Width="152px">RBSE</asp:label><asp:label id="lblRelationSex" style="Z-INDEX: 110; LEFT: 16px; POSITION: absolute; TOP: 48px" runat="server" Width="152px">Sex</asp:label><asp:label id="lblRelationType" style="Z-INDEX: 106; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Width="152px">Relation Type</asp:label><asp:label id="lblRelationFate" style="Z-INDEX: 114; LEFT: 16px; POSITION: absolute; TOP: 80px" runat="server" Width="152px">Fate</asp:label><asp:label id="lblRelationLeftDate" style="Z-INDEX: 115; LEFT: 448px; POSITION: absolute; TOP: 80px" runat="server" Width="152px">Date Left</asp:label><asp:label id="lblRelationEartag" style="Z-INDEX: 117; LEFT: 16px; POSITION: absolute; TOP: 112px" runat="server" Width="152px">Eartag</asp:label><asp:label id="lblrfvRelationType" style="Z-INDEX: 124; LEFT: 376px; POSITION: absolute; TOP: 16px" runat="server" Width="16px" CssClass="ValidatorText" Height="12px" ToolTip="Please select a relation type" Visible="False">*</asp:label><asp:label id="lblRelationFateValue" style="Z-INDEX: 120; LEFT: 184px; POSITION: absolute; TOP: 80px" runat="server" Width="180px" Visible="False">XXX</asp:label><asp:label id="lblRelationSire" style="Z-INDEX: 125; LEFT: 448px; POSITION: absolute; TOP: 112px" runat="server" Width="152px">Sire</asp:label>
				<HR style="Z-INDEX: 100; LEFT: 16px; WIDTH: 96.03%; POSITION: absolute; TOP: 192px; HEIGHT: 1px" width="96.03%" SIZE="1">
				<asp:dropdownlist id="ddlRelationType" style="Z-INDEX: 107; LEFT: 184px; POSITION: absolute; TOP: 16px" runat="server" Width="184px" Font-Size="90%"></asp:dropdownlist>
				<DIV style="Z-INDEX: 108; LEFT: 616px; WIDTH: 184px; POSITION: absolute; TOP: 8px; HEIGHT: 32px" ms_positioning="FlowLayout"><uc1:rbse id="ctlRelationRBSE" runat="server"></uc1:rbse></DIV>
				<asp:dropdownlist id="ddlRelationSex" style="Z-INDEX: 111; LEFT: 184px; POSITION: absolute; TOP: 48px" runat="server" Width="184px" Font-Size="90%"></asp:dropdownlist>
				<DIV style="Z-INDEX: 127; LEFT: 616px; WIDTH: 184px; POSITION: absolute; TOP: 40px; HEIGHT: 32px" ms_positioning="FlowLayout">
					<uc1:CalendarDate id="ctlRelationBirthDate" runat="server"></uc1:CalendarDate></DIV>
				<asp:dropdownlist id="ddlRelationFate" style="Z-INDEX: 122; LEFT: 184px; POSITION: absolute; TOP: 80px" runat="server" Width="184px" Font-Size="90%"></asp:dropdownlist>
				<DIV style="Z-INDEX: 126; LEFT: 616px; WIDTH: 184px; POSITION: absolute; TOP: 72px; HEIGHT: 32px" ms_positioning="FlowLayout">
					<uc1:calendardate id="ctlRelationLeftDate" runat="server"></uc1:calendardate>
				</DIV>
				<DIV style="Z-INDEX: 118; LEFT: 184px; WIDTH: 184px; POSITION: absolute; TOP: 111px; HEIGHT: 32px" ms_positioning="FlowLayout">
					<uc1:ThreePartEartag id="ctlRelationEartag" runat="server"></uc1:ThreePartEartag>
				</DIV>
				<asp:textbox id="txtRelationSire" style="Z-INDEX: 116; LEFT: 616px; POSITION: absolute; TOP: 112px" runat="server" Width="174px" MaxLength="80"></asp:textbox>
				<asp:button id="btnViewRelations" style="Z-INDEX: 119; LEFT: 264px; POSITION: absolute; TOP: 160px" runat="server" Width="136px" Text="View Relations"></asp:button>
				<asp:button id="btnAddAsNew" style="Z-INDEX: 103; LEFT: 416px; POSITION: absolute; TOP: 160px" runat="server" Text="Add As New"></asp:button>
				<asp:button id="btnUpdateSelected" style="Z-INDEX: 104; LEFT: 528px; POSITION: absolute; TOP: 160px" runat="server" Text="Update Selected"></asp:button>
				<asp:button id="btnDeleteSelected" style="Z-INDEX: 105; LEFT: 680px; POSITION: absolute; TOP: 160px" runat="server" Text="Delete Selected"></asp:button>
				<asp:button id="btnSave" style="Z-INDEX: 101; LEFT: 664px; POSITION: absolute; TOP: 200px" runat="server" Text="  Save  "></asp:button>
				<asp:button id="btnCancel" style="Z-INDEX: 102; LEFT: 736px; POSITION: absolute; TOP: 200px" runat="server" Text=" Cancel "></asp:button>
				<DIV style="Z-INDEX: 104; LEFT: 496px; WIDTH: 88px; POSITION: absolute; TOP: 200px; HEIGHT: 32px" ms_positioning="GridLayout">
					<asp:Literal id="litViewDocs" runat="server"></asp:Literal></DIV>
				<asp:textbox id="txtRelationID" style="Z-INDEX: 121; LEFT: 16px; POSITION: absolute; TOP: 152px" runat="server" Width="168px" Visible="False"></asp:textbox><asp:label id="lblrfvSex" style="Z-INDEX: 127; LEFT: 376px; POSITION: absolute; TOP: 48px" runat="server" CssClass="ValidatorText" Height="20px" ToolTip="Please select a sex" Visible="False">*</asp:label></DIV>
			<P><uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></P>
		</FORM>
	</BODY>
</HTML>
