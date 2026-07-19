<%@ Control Language="vb" AutoEventWireup="false" Codebehind="PartialDate.ascx.vb" Inherits="BSESystem.PartialDate" TargetSchema="http://schemas.microsoft.com/intellisense/ie5" %>
<div style="LEFT:0px; POSITION:absolute; TOP:0px">
	<asp:TextBox id="txtDay" runat="server" MaxLength="2" Width="25px" ToolTip="Day"></asp:TextBox>
</div>
<div style="LEFT:26px; POSITION:absolute; TOP:2px">
	<asp:RangeValidator id="rvDay" CssClass="ValidatorText" runat="server" ErrorMessage="*" ToolTip="Please enter a valid day between 1 and 31" ControlToValidate="txtDay" MaximumValue="31" MinimumValue="1" Type="Integer"></asp:RangeValidator>
</div>
<div style="LEFT:40px; POSITION:absolute; TOP:0px">
	<asp:TextBox id="txtMonth" runat="server" MaxLength="2" Width="25px" ToolTip="Month"></asp:TextBox>
</div>
<div style="LEFT:66px; POSITION:absolute; TOP:2px">
	<asp:RangeValidator id="rvMonth" CssClass="ValidatorText" runat="server" ErrorMessage="*" ToolTip="Please enter a month between 1 and 12" ControlToValidate="txtMonth" MaximumValue="12" MinimumValue="1" Type="Integer"></asp:RangeValidator>
</div>
<div style="LEFT:80px; POSITION:absolute; TOP:0px">
	<asp:TextBox id="txtYear" runat="server" MaxLength="4" Width="50px" ToolTip="Year"></asp:TextBox>
</div>
<div style="LEFT:131px; POSITION:absolute; TOP:2px">
	<asp:RequiredFieldValidator id="rfvYear" CssClass="ValidatorText" runat="server" ErrorMessage="*" ToolTip="Please enter a year" ControlToValidate="txtYear" Enabled="False"></asp:RequiredFieldValidator>
</div>
<div style="LEFT:131px; POSITION:absolute; TOP:2px">
	<asp:regularexpressionvalidator id="revYear" CssClass="ValidatorText" runat="server" ErrorMessage="*" ToolTip="Please enter a four digit year" ControlToValidate="txtYear" ValidationExpression="[0-9][0-9][0-9][0-9]"></asp:regularexpressionvalidator>
</div>
<div style="LEFT:131px; POSITION:absolute; TOP:2px">
	<asp:Label id="lblError" CssClass="ValidatorText" ToolTip="Please enter a valid date" runat="server" Visible="False" ForeColor="Red">*</asp:Label>
</div>
