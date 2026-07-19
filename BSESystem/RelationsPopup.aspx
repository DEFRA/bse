<%@ Register TagPrefix="uc1" TagName="CalendarDate" Src="CalendarDate.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="Eartag" Src="Eartag.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="RelationsPopup.aspx.vb" Inherits="BSESystem.RelationsPopup"%>
<%@ Register TagPrefix="uc1" TagName="PartialDate" Src="PartialDate.ascx" %>
<%@ Register TagPrefix="uc1" TagName="BatchNumberDisplay" Src="BatchNumberDisplay.ascx" %>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Register TagPrefix="uc1" TagName="RBSE" Src="RBSE.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>Relations Information for Case
			<% = FormatRBSE(Request.QueryString("rbse"))%>
		</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<DIV style="Z-INDEX: 400; WIDTH: 576px; POSITION: relative; HEIGHT: 24px" ms_positioning="GridLayout"><asp:label id="lblRBSEHeader" style="Z-INDEX: 101; LEFT: 8px; POSITION: absolute; TOP: 2px" runat="server" Width="302px" Font-Bold="True">RBSE Number:  12/12/12345</asp:label>
				<INPUT style="Z-INDEX: 102; LEFT: 488px; WIDTH: 72px; POSITION: absolute; TOP: 0px; HEIGHT: 21px" type="button" value="Close" onclick="window.close()"></DIV>
			<DIV style="WIDTH: 576px; POSITION: relative; HEIGHT: 352px" ms_positioning="GridLayout"><asp:label id="lblDamEartag" style="Z-INDEX: 100; LEFT: 16px; POSITION: absolute; TOP: 48px" runat="server" Width="72px">Eartag</asp:label><asp:label id="lblDamName" style="Z-INDEX: 101; LEFT: 296px; POSITION: absolute; TOP: 48px" runat="server" Width="80px">Name</asp:label><asp:label id="lblDamBirthDate" style="Z-INDEX: 102; LEFT: 16px; POSITION: absolute; TOP: 112px" runat="server" Width="80px">Date of Birth</asp:label><asp:label id="lblDamRBSE" style="Z-INDEX: 103; LEFT: 16px; POSITION: absolute; TOP: 80px" runat="server" Width="72px">RBSE</asp:label>
				<P>&nbsp;</P>
				<HR style="Z-INDEX: 104; LEFT: 8px; WIDTH: 98.66%; POSITION: absolute; TOP: 176px; HEIGHT: 1px" width="98.66%" SIZE="1">
				<P>&nbsp;</P>
				<P>&nbsp;</P>
				<asp:label id="lblDamDetails" style="Z-INDEX: 105; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Width="96px" Font-Bold="True">Dam Details</asp:label><asp:label id="lblDamFate" style="Z-INDEX: 106; LEFT: 16px; POSITION: absolute; TOP: 144px" runat="server" Width="72px">Fate</asp:label><asp:label id="lblFinalResult" style="Z-INDEX: 107; LEFT: 296px; POSITION: absolute; TOP: 112px" runat="server" Width="80px">Final Result</asp:label><asp:label id="lblDamFinalResultValue" style="Z-INDEX: 108; LEFT: 408px; POSITION: absolute; TOP: 112px" runat="server" Width="152px"></asp:label><asp:label id="lblSireDetails" style="Z-INDEX: 109; LEFT: 16px; POSITION: absolute; TOP: 200px" runat="server" Width="104px" Font-Bold="True">Sire Details</asp:label><asp:label id="lblSireEartag" style="Z-INDEX: 110; LEFT: 16px; POSITION: absolute; TOP: 232px" runat="server" Width="80px">Eartag</asp:label><asp:label id="lblSireName" style="Z-INDEX: 111; LEFT: 296px; POSITION: absolute; TOP: 232px" runat="server" Width="80px">Name</asp:label><asp:label id="lblSireRBSE" style="Z-INDEX: 112; LEFT: 16px; POSITION: absolute; TOP: 264px" runat="server" Width="80px">RBSE</asp:label><asp:label id="lblSireFate" style="Z-INDEX: 113; LEFT: 16px; POSITION: absolute; TOP: 296px" runat="server" Width="80px">Fate</asp:label>
				<P></P>
				<P></P>
				<HR style="Z-INDEX: 114; LEFT: 0px; WIDTH: 98.79%; POSITION: absolute; TOP: 328px; HEIGHT: 2px" width="98.79%" SIZE="2">
				<asp:label id="lblSireBirthDate" style="Z-INDEX: 115; LEFT: 296px; POSITION: absolute; TOP: 296px" runat="server" Width="80px">Date of Birth</asp:label><asp:label id="lblDamFateValue" style="Z-INDEX: 116; LEFT: 112px; POSITION: absolute; TOP: 144px" runat="server" Width="192px"></asp:label><asp:label id="lblSireFateValue" style="Z-INDEX: 117; LEFT: 112px; POSITION: absolute; TOP: 296px" runat="server" Width="168px"></asp:label><asp:label id="lblDamID" style="Z-INDEX: 118; LEFT: 136px; POSITION: absolute; TOP: 16px" runat="server" Width="152px" Font-Bold="True"></asp:label><asp:label id="lblSireID" style="Z-INDEX: 119; LEFT: 144px; POSITION: absolute; TOP: 200px" runat="server" Width="136px" Font-Bold="True"></asp:label><asp:label id="lblDamEartagValue" style="Z-INDEX: 120; LEFT: 112px; POSITION: absolute; TOP: 48px" runat="server" Width="152px"></asp:label><asp:label id="lblDamRBSEValue" style="Z-INDEX: 121; LEFT: 112px; POSITION: absolute; TOP: 80px" runat="server" Width="152px"></asp:label><asp:label id="lblDamNameValue" style="Z-INDEX: 122; LEFT: 408px; POSITION: absolute; TOP: 48px" runat="server" Width="152px"></asp:label><asp:label id="lblDamBirthDateValue" style="Z-INDEX: 123; LEFT: 112px; POSITION: absolute; TOP: 112px" runat="server" Width="152px"></asp:label><asp:label id="lblSireEartagValue" style="Z-INDEX: 124; LEFT: 112px; POSITION: absolute; TOP: 232px" runat="server" Width="152px"></asp:label><asp:label id="lblSireRBSEValue" style="Z-INDEX: 125; LEFT: 112px; POSITION: absolute; TOP: 264px" runat="server" Width="152px"></asp:label><asp:label id="lblSireNameValue" style="Z-INDEX: 126; LEFT: 408px; POSITION: absolute; TOP: 232px" runat="server" Width="152px"></asp:label><asp:label id="lblSireBirthDateValue" style="Z-INDEX: 127; LEFT: 408px; POSITION: absolute; TOP: 296px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblOtherRelations" style="Z-INDEX: 128; LEFT: 16px; POSITION: absolute; TOP: 336px" runat="server" Font-Bold="True" Width="152px">Other Relations</asp:label>
				<asp:label id="lblSireHerdbook" style="Z-INDEX: 129; LEFT: 296px; POSITION: absolute; TOP: 264px" runat="server" Width="80px">Herdbook</asp:label>
				<asp:label id="lblSireHerdbookValue" style="Z-INDEX: 130; LEFT: 408px; POSITION: absolute; TOP: 264px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblSireOffspring" style="Z-INDEX: 132; LEFT: 296px; POSITION: absolute; TOP: 200px" runat="server" Width="104px">Offspring Count</asp:label>
				<asp:label id="lblSireOffspringCountValue" style="Z-INDEX: 133; LEFT: 408px; POSITION: absolute; TOP: 200px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblDamOffspringCount" style="Z-INDEX: 134; LEFT: 296px; POSITION: absolute; TOP: 16px" runat="server" Width="104px">Offspring Count</asp:label>
				<asp:label id="lblDamOffspringCountValue" style="Z-INDEX: 135; LEFT: 408px; POSITION: absolute; TOP: 16px" runat="server" Width="152px"></asp:label>
				<asp:label id="lblDamHerdbook" style="Z-INDEX: 136; LEFT: 296px; POSITION: absolute; TOP: 80px" runat="server" Width="80px">Herdbook</asp:label>
				<asp:label id="lblDamHerdbookValue" style="Z-INDEX: 137; LEFT: 408px; POSITION: absolute; TOP: 80px" runat="server" Width="152px"></asp:label></DIV>
			<P><asp:datagrid id="grdRelations" runat="server" Width="578px" AutoGenerateColumns="False" AllowSorting="True" AllowPaging="True">
					<SelectedItemStyle CssClass="GridSelectedItem"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItem"></EditItemStyle>
					<ItemStyle CssClass="GridItem"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:TemplateColumn SortExpression="RelationType" HeaderText="Relation Type">
							<HeaderStyle Width="80px"></HeaderStyle>
							<ItemTemplate>
								<asp:Label runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.RelationTypeDesc") %>' ID="Label1">
								</asp:Label>
							</ItemTemplate>
							<EditItemTemplate>
								<asp:dropdownlist id="ddlRelationType" runat="server" />
							</EditItemTemplate>
						</asp:TemplateColumn>
						<asp:TemplateColumn HeaderText="RBSE">
							<ItemTemplate>
								<asp:Label id="Label2" runat="server" Text='<%# FormatRBSE(DataBinder.Eval(Container, "DataItem.RelationRBSE").ToString()) %>'>
								</asp:Label>
							</ItemTemplate>
							<EditItemTemplate>
								<asp:Label id="Label6" runat="server" Text='<%# FormatRBSE(DataBinder.Eval(Container, "DataItem.RelationRBSE").ToString()) %>'>
								</asp:Label>
							</EditItemTemplate>
						</asp:TemplateColumn>
						<asp:TemplateColumn HeaderText="Sex">
							<ItemTemplate>
								<asp:Label runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.SexDesc") %>' ID="Label3">
								</asp:Label>
							</ItemTemplate>
							<EditItemTemplate>
								<asp:dropdownlist id="ddlSex" runat="server" />
							</EditItemTemplate>
						</asp:TemplateColumn>
						<asp:TemplateColumn HeaderText="Birth Date">
							<ItemTemplate>
								<asp:Label runat="server" Text='<%# FormatPartialDate(DataBinder.Eval(Container, "DataItem.BirthDay"), DataBinder.Eval(Container, "DataItem.BirthMonth"), DataBinder.Eval(Container, "DataItem.BirthYear")) %>'>
								</asp:Label>
							</ItemTemplate>
							<EditItemTemplate>
								<asp:TextBox runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.BirthDay") %>'>
								</asp:TextBox>
							</EditItemTemplate>
						</asp:TemplateColumn>
						<asp:TemplateColumn HeaderText="Fate">
							<ItemTemplate>
								<asp:Label runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.RelationFateDesc") %>' ID="Label4">
								</asp:Label>
							</ItemTemplate>
							<EditItemTemplate>
								<asp:dropdownlist id="ddlRelationFate" runat="server" />
							</EditItemTemplate>
						</asp:TemplateColumn>
						<asp:TemplateColumn HeaderText="Date Left">
							<ItemTemplate>
								<asp:Label id="Label5" runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.LeftDate") %>'>
								</asp:Label>
							</ItemTemplate>
							<EditItemTemplate>
								<DIV style="WIDTH: 216px; POSITION: relative; HEIGHT: 24px" ms_positioning="GridLayout">
									<uc1:CalendarDate id="ctlLeftDate" runat="server"></uc1:CalendarDate></DIV>
							</EditItemTemplate>
						</asp:TemplateColumn>
						<asp:TemplateColumn HeaderText="Eartag">
							<ItemTemplate>
								<asp:Label runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.EartagHerdmark") + " " + DataBinder.Eval(Container, "DataItem.Eartag") %>'>
								</asp:Label>
							</ItemTemplate>
							<EditItemTemplate>
								<asp:TextBox runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.EartagHerdmark") + " " + DataBinder.Eval(Container, "DataItem.Eartag") %>'>
								</asp:TextBox>
							</EditItemTemplate>
						</asp:TemplateColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid></P>
			<DIV style="WIDTH: 581px; POSITION: relative; HEIGHT: 28px" ms_positioning="GridLayout">
				<P><uc1:datagridpager id="RelationsPager" runat="server"></uc1:datagridpager></P>
			</DIV>
			<P>&nbsp;</P>
		</form>
	</body>
</HTML>
