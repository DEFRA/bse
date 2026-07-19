<%@ Control Language="vb" AutoEventWireup="false" Codebehind="ADNSReference.ascx.vb" Inherits="BSESystem.ADNSReference" TargetSchema="http://schemas.microsoft.com/intellisense/ie5" %>
<div style="Z-INDEX:100; LEFT:0px; POSITION:absolute; TOP:0px">
	<asp:TextBox id="txtYear" runat="server" MaxLength="4" Width="40px"></asp:TextBox>
</div>
<div style="Z-INDEX:100; LEFT:50px; POSITION:absolute; TOP:0px">
	<asp:TextBox id="txtNumber" Width="48px" MaxLength="5" runat="server"></asp:TextBox>
</div>
<div style="Z-INDEX:101; LEFT:40px; POSITION:absolute; TOP:2px">
	<asp:RegularExpressionValidator id="revYear" CssClass="ValidatorText" runat="server" Height="8px" ErrorMessage="*" ToolTip="Please enter a four digit year" ValidationExpression="[0-9][0-9][0-9][0-9]" ControlToValidate="txtYear"></asp:RegularExpressionValidator>
</div>
<div style="Z-INDEX:101; LEFT:100px; POSITION:absolute; TOP:2px">
	<asp:RegularExpressionValidator id="revNumber" CssClass="ValidatorText" runat="server" Height="8px" ErrorMessage="*" ToolTip="Please enter a number" ValidationExpression="[0-9]*" ControlToValidate="txtNumber"></asp:RegularExpressionValidator>
</div>
<div style="Z-INDEX:101; LEFT:100px; POSITION:absolute; TOP:2px">
	<asp:label id="lblError" runat="server" ToolTip="Please enter an ADNS reference" CssClass="ValidatorText" EnableViewState="False" Visible="False" ForeColor="Red">*</asp:label>
</div>
