<%@ Control Language="vb" AutoEventWireup="false" Codebehind="CPHH.ascx.vb" Inherits="BSESystem.CPHH" TargetSchema="http://schemas.microsoft.com/intellisense/ie5" %>
<div style="LEFT:0px; POSITION:absolute; TOP:0px">
	<asp:TextBox id="txtCPHH" runat="server" MaxLength="14" Width="154px" ToolTip="CPHH Format: NN[/]NNN[/]NNNN[/NN]"></asp:TextBox>
</div>
<div style="LEFT:157px; POSITION:absolute; TOP:2px">
	<asp:Label id="lblInvalid" runat="server" CssClass="ValidatorText" Visible="False" ForeColor="Red">*</asp:Label>
</div>
<div style="LEFT:157px; POSITION:absolute; TOP:2px">
	<asp:RegularExpressionValidator id="revCPHH" CssClass="ValidatorText" runat="server" Height="8px" ErrorMessage="*" ToolTip="CPHH Format: NN[/]NNN[/]NNNN[/NN]" ValidationExpression="\d{2}(/)?\d{3}(/)?\d{4}(/)?\d{2}" ControlToValidate="txtCPHH"></asp:RegularExpressionValidator>
</div>
<div style="LEFT:157px; POSITION:absolute; TOP:2px">
	<asp:RequiredFieldValidator id="rfvCPHH" CssClass="ValidatorText" runat="server" Height="8px" ErrorMessage="*" ToolTip="Required Field" ControlToValidate="txtCPHH" Enabled="False"></asp:RequiredFieldValidator>
</div>
