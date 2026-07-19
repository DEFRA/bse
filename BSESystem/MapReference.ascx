<%@ Control Language="vb" AutoEventWireup="false" Codebehind="MapReference.ascx.vb" Inherits="BSESystem.MapReference" TargetSchema="http://schemas.microsoft.com/intellisense/ie5" %>
<div style="Z-INDEX:100; LEFT:0px; POSITION:absolute; TOP:0px">
	<asp:TextBox id="txtMapReference1" runat="server" MaxLength="2" Width="30px"></asp:TextBox>
</div>
<div style="Z-INDEX:100; LEFT:40px; POSITION:absolute; TOP:0px">
	<asp:TextBox id="txtMapReference2" Width="45px" MaxLength="3" runat="server"></asp:TextBox>
</div>
<div style="Z-INDEX:100; LEFT:95px; POSITION:absolute; TOP:0px">
	<asp:TextBox id="txtMapReference3" Width="45px" MaxLength="3" runat="server" AutoPostBack="True"></asp:TextBox>
</div>
<div style="Z-INDEX:101; LEFT:30px; POSITION:absolute; TOP:2px">
	<asp:RegularExpressionValidator id="revMapReference1" CssClass="ValidatorText" runat="server" Height="8px" ErrorMessage="*" ToolTip="First field must be 2 letters" ValidationExpression="[A-Z][A-Z]" ControlToValidate="txtMapReference1"></asp:RegularExpressionValidator>
</div>
<div style="Z-INDEX:101; LEFT:85px; POSITION:absolute; TOP:2px">
	<asp:RegularExpressionValidator id="revMapReference2" CssClass="ValidatorText" runat="server" Height="8px" ErrorMessage="*" ToolTip="Second field must be 3 digits" ValidationExpression="[0-9][0-9][0-9]" ControlToValidate="txtMapReference2"></asp:RegularExpressionValidator>
</div>
<div style="Z-INDEX:101; LEFT:140px; POSITION:absolute; TOP:2px">
	<asp:RegularExpressionValidator id="revMapReference3" CssClass="ValidatorText" runat="server" Height="8px" ErrorMessage="*" ToolTip="Third field must be 3 digits" ValidationExpression="[0-9][0-9][0-9]" ControlToValidate="txtMapReference3"></asp:RegularExpressionValidator>
</div>
<SCRIPT language="javascript"><!--
	function checkLength(currentControl, nextControl, charLength){
		if (currentControl.value.length == charLength){
			nextControl.focus();
		}
	}	
	
	function onKeyPressUpperCase() {
		var sChar = String.fromCharCode(window.event.keyCode);
		sChar = sChar.toUpperCase();
		window.event.keyCode = sChar.charCodeAt(0);
	}
    --></SCRIPT>
