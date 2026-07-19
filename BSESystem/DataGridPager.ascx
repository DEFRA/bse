<%@ Control Language="vb" AutoEventWireup="false" Codebehind="DataGridPager.ascx.vb" Inherits="BSESystem.DataGridPager" TargetSchema="http://schemas.microsoft.com/intellisense/ie5" %>
<table cellSpacing="0" cellPadding="0" width="100%" border="0">
	<tr vAlign="center" height="28">
		<td width="60%"><asp:imagebutton id="imbtnFirstPage" ImageUrl="Images/GridPager/frew.gif" AlternateText="First Page" CausesValidation="False" Runat="server"></asp:imagebutton><asp:imagebutton id="imbtnPrevPage" ImageUrl="Images/GridPager/rew.gif" AlternateText="Previous Page" CausesValidation="False" Runat="server"></asp:imagebutton><asp:imagebutton id="imbtnNextPage" ImageUrl="Images/GridPager/fwd.gif" AlternateText="Next Page" CausesValidation="False" Runat="server"></asp:imagebutton><asp:imagebutton id="imbtnLastPage" ImageUrl="Images/GridPager/ffwd.gif" AlternateText="Last Page" CausesValidation="False" Runat="server"></asp:imagebutton>&nbsp;&nbsp;
			<asp:linkbutton id="lbtnPage1" CausesValidation="False" Runat="server" CssClass="GridPagerNavLinks"></asp:linkbutton>&nbsp;
			<asp:linkbutton id="lbtnPage2" CausesValidation="False" Runat="server" CssClass="GridPagerNavLinks"></asp:linkbutton>&nbsp;
			<asp:linkbutton id="lbtnPage3" CausesValidation="False" Runat="server" CssClass="GridPagerNavLinks"></asp:linkbutton>&nbsp;
			<asp:linkbutton id="lbtnPage4" CausesValidation="False" Runat="server" CssClass="GridPagerNavLinks"></asp:linkbutton>&nbsp;
			<asp:linkbutton id="lbtnPage5" CausesValidation="False" Runat="server" CssClass="GridPagerNavLinks"></asp:linkbutton>&nbsp;
			<asp:linkbutton id="lbtnPage6" CausesValidation="False" Runat="server" CssClass="GridPagerNavLinks"></asp:linkbutton>&nbsp;
			<asp:linkbutton id="lbtnPage7" CausesValidation="False" Runat="server" CssClass="GridPagerNavLinks"></asp:linkbutton>&nbsp;
			<asp:linkbutton id="lbtnPage8" CausesValidation="False" Runat="server" CssClass="GridPagerNavLinks"></asp:linkbutton>&nbsp;
			<asp:linkbutton id="lbtnPage9" CausesValidation="False" Runat="server" CssClass="GridPagerNavLinks"></asp:linkbutton>&nbsp;
			<asp:linkbutton id="lbtnPage10" CausesValidation="False" Runat="server" CssClass="GridPagerNavLinks"></asp:linkbutton>&nbsp;
		</td>
		<td width="20%">
			<DIV style="WIDTH: 130px; POSITION: relative; HEIGHT: 22px" ms_positioning="GridLayout">
				<asp:imagebutton id="imbtnNewRec" style="Z-INDEX: 101; LEFT: 0px; POSITION: absolute; TOP: 0px" ImageUrl="Images/GridPager/new.gif" AlternateText="New Record" CausesValidation="False" Runat="server"></asp:imagebutton>
				<asp:imagebutton id="cmdEdit" style="Z-INDEX: 102; LEFT: 24px; POSITION: absolute; TOP: 0px" ImageUrl="Images/GridPager/edit.gif" AlternateText="Edit selected record" CausesValidation="False" Runat="server" Visible="False"></asp:imagebutton>
				<asp:imagebutton id="cmdDel" style="Z-INDEX: 103; LEFT: 48px; POSITION: absolute; TOP: 0px" ImageUrl="Images/GridPager/del.gif" AlternateText="Delete selected record" CausesValidation="False" Runat="server" Visible="False"></asp:imagebutton>
				<asp:imagebutton id="cmdSave" style="Z-INDEX: 104; LEFT: 84px; POSITION: absolute; TOP: 0px" ImageUrl="Images/GridPager/ok.gif" AlternateText="Save changes" CausesValidation="True" Runat="server" Visible="False"></asp:imagebutton>
				<asp:imagebutton id="cmdCancel" style="Z-INDEX: 105; LEFT: 108px; POSITION: absolute; TOP: 0px" ImageUrl="Images/GridPager/cancel.gif" AlternateText="Cancel changes" CausesValidation="False" Runat="server" Visible="False"></asp:imagebutton>
				<asp:TextBox id="txtEditState" style="Z-INDEX: 100; LEFT: 0px; POSITION: absolute; TOP: 0px" runat="server" Visible="False" Width="20px"></asp:TextBox>
			</DIV>
			<asp:Literal id="litAnchor" runat="server"></asp:Literal>
		</td>
		<td align="right">
			<asp:label id="lblPageLocation" Runat="server" CssClass="GridPagerPageNum"></asp:label>
		</td>
	</tr>
	<tr vAlign="top">
		<td colSpan="3"><asp:label id="lblError" Runat="server" CssClass="GridPagerErrorText"></asp:label></td>
	</tr>
</table>
