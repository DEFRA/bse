<%@ Control Language="vb" AutoEventWireup="false" Codebehind="RBSE.ascx.vb" Inherits="BSESystem.RBSE" TargetSchema="http://schemas.microsoft.com/intellisense/ie5" %>
<div style="LEFT: 0px; POSITION: absolute; TOP: 0px"><asp:textbox id="txtRBSE" ToolTip="RBSE Format: NN/NN/NNNNN" Width="154px" MaxLength="11" runat="server"></asp:textbox></div>
<div style="LEFT: 157px; POSITION: absolute; TOP: 2px"><asp:label id="lblInvalid" runat="server" CssClass="ValidatorText" Height="8px" Visible="False" ForeColor="Red">*</asp:label></div>
<div style="LEFT: 157px; POSITION: absolute; TOP: 2px"><asp:regularexpressionvalidator id="revRBSE" ToolTip="RBSE Format: NN/NN/NNNNN" runat="server" CssClass="ValidatorText" ErrorMessage="*" ValidationExpression="[0-9]?[0-9]?(/)?[0-9X]?[0-9X]/[0-9]?[0-9]?[0-9]?[0-9]?[0-9]" ControlToValidate="txtRBSE" Height="8px"></asp:regularexpressionvalidator></div>
<div style="LEFT: 157px; POSITION: absolute; TOP: 2px"><asp:requiredfieldvalidator id="rfvRBSE" ToolTip="Required Field" runat="server" CssClass="ValidatorText" ErrorMessage="*" ControlToValidate="txtRBSE" Height="8px" Enabled="False"></asp:requiredfieldvalidator></div>

