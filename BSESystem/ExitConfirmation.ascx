<%@ Control Language="vb" AutoEventWireup="false" Codebehind="ExitConfirmation.ascx.vb" Inherits="BSESystem.ExitConfirmation" TargetSchema="http://schemas.microsoft.com/intellisense/ie5" %>
<asp:literal id="litConfirmation" runat="server"></asp:literal>
<script language="JavaScript"><!--
function pageOffset() {
    if (document.layers) {
		if (document.layers['exitConfirmationDIV']) {
			document.layers['exitConfirmationDIV'].pageX = window.pageXOffset + 240;
			document.layers['exitConfirmationDIV'].pageY = window.pageYOffset + 10;
        }
    }
    else if (document.all) {
		if (document.all['exitConfirmationDIV']) {
			document.all['exitConfirmationDIV'].style.posLeft = document.body.scrollLeft + 240;
			document.all['exitConfirmationDIV'].style.posTop = document.body.scrollTop + 10;
		} else {
			alert('The div which holds the ExitConfirmation control on this page needs to be called exitConfirmationDIV.')
		}
    }
    setTimeout('pageOffset()',100);
}
pageOffset()
//--></script>
