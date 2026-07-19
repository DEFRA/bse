<%@ Control Language="vb" AutoEventWireup="false" Codebehind="Eartag.ascx.vb" Inherits="BSESystem.Eartag" TargetSchema="http://schemas.microsoft.com/intellisense/ie5" %>
<div style="LEFT: 0px; POSITION: absolute; TOP: 0px"><asp:textbox id="txtEartag" Width="154px" runat="server"></asp:textbox></div>
<div style="LEFT: 157px; POSITION: absolute; TOP: 2px"><asp:requiredfieldvalidator id="rfvEartag" Height="8px" ToolTip="Required Field" runat="server" ControlToValidate="txtEartag" ErrorMessage="*" CssClass="ValidatorText" Enabled="False"></asp:requiredfieldvalidator></div>
<div style="LEFT: 157px; POSITION: absolute; TOP: 2px"><asp:CustomValidator id="cvEartag" Height="8px" ToolTip="Invalid Eartag" runat="server" ControlToValidate="txtEartag" ErrorMessage="*" CssClass="ValidatorText" Enabled="True" ClientValidationFunction="EartagIsValid"></asp:CustomValidator></div>
<div style="LEFT: 167px; POSITION: absolute; TOP: 2px"><asp:Label id="lblType" CssClass="ValidatorText" runat="server" ForeColor="#9CCE00" Visible="False">*</asp:Label></div>
<asp:CheckBox id="chkChanged" runat="server" Visible="False"></asp:CheckBox>
<script language="vbScript">
Function EartagIsValid(source, arguments)
	Dim p_value
			
	p_value = arguments.value
	fn_EarTag(p_value)

	If Trim(fn_GetError()) = "" then
		arguments.IsValid = True
	else
		arguments.IsValid = False
	End If
	
	'msgbox("Eartag " & mEartag)
	'msgbox("CountryCode " & mCountryCode)
	'msgbox("ComponentHerd " & mComponentHerd)
	'msgbox("ComponentAnimal " & mComponentAnimal)
	'msgbox("mError" & mError)
End Function
</script>
<script language="javascript">

function fn_GetError(){
	return mError
}
/* Eartag functions previously written by MAFF ITD*/

var mEartag;
var mCharIndex;
var mCountryCode;
var mComponentCountry;
var mComponentHerd;
var mComponentAnimal;
var miDel;
var mError;

function fn_ECCountry(p_CountryCode){
	var arrCountry;
	var txtCountrys = 'AT,BE,DE,DK,EL,ES,FI,FR,IE,IT,LU,NL,PT,SE,UK,GB0,GB1,GB2,8260,8261,8262,'
	arrCountry = eval ("txtCountrys.split(',')");
	for (i=0; i<arrCountry.length; i++) {
		if (p_CountryCode == arrCountry[i]) {
			return true;
		}
	}
}

function fn_EarTag(p_Value){
	var FormatField;
	var FormatField2;
	mCharIndex = 0;
	mCountryCode = null;
	mComponentCountry = null;
	mComponentHerd = null;
	mComponentAnimal = null;
	PresentationFormat = null;
	FormatID = null;
	VarError = null;
	mError = null;
	AnimalNumber = null;

	mEartag = p_Value.toUpperCase();

	if (mEartag != null && mEartag != ""){
		fn_GetCountryComponents()
		fn_MainFormat()
		fn_ReformatComponents()
		fn_ValidateEartag()
		fn_CalcPresentationFormat()
		if (VarError == null){
			VarError = ""
		}
		if (mError == null){
			mError = ""
		}
	
		if (VarError == "" && mError == ""){ 
			fn_CalcOAISortID()
			AnimalNumber = AnimalNumber * 1
			AnimalNumber = eval(AnimalNumber)
		}
		if (mError != ""){
			//fn_ShowMessage(mError)
			//event.returnValue=false
			return false;
		}		
	}
}

function fn_PadZero(as_string, ai_length) {
	var newstring = "000000" + as_string
	return newstring.substring(newstring.length - ai_length) 
}
	
function fn_GetCountryComponents(){
	var ChrStr="abcdefghijklmnopqrstuvwxyz1234567890"; 
	var ChIn;
	var thisChar;
	
	mCountryCode = mEartag.substring(0, 2)

	// Get codes longer than 2 characters.
	if (mCountryCode == "GB") {
        mCountryCode = mEartag.substring(0, 3)
    }
    if (mEartag.substring(0, 3) == "826") {
        mCountryCode = mEartag.substring(0, 4)
	}

	mCountryCode = mCountryCode.toUpperCase()
	
	if (fn_ECCountry(mCountryCode) == true){
		mComponentCountry = mCountryCode
		fn_GetHerdmarkComponent(mComponentCountry)
	}
	
	//if not a valid ec code
	if (mComponentCountry == null){
		if (fn_PreBarimoTest() == true){
			//next two functions set mComponentHerd to correct string 
			//fn_ValAlpha()
	
			for (var i=0; i<mEartag.length;i++) { 
				thisChar = mEartag.substring(i,[i+1]); 
				thisChar = thisChar.toLowerCase();
				if (mCharIndex == 0){
					if ([ChrStr.indexOf(thisChar)]>=0){
						//A char found - RECORD INDEX NUMBER FOR USE IN next function
						mCharIndex = i
						break;
					} 
				}
			}	
	
			if (fn_AnyDelimiters()== true){
				mComponentHerd = (mEartag.substring(mCharIndex,miDel+mCharIndex));
				fn_GetAnimalComponent(miDel+mCharIndex)
				
			}else{
				//set mComponentHerd to remainder of string characters 
				mComponentHerd = (mEartag.substring(mCharIndex));
				fn_GetAnimalComponent(0)
			}
		}else{
			mComponentHerd = null
			mComponentAnimal = mEartag
		}

} 


}	

function isUKEartag() {
	// Returns true if mComponentCountry represents a UK eartag.
	return (mComponentCountry == 'UK') || (mComponentCountry == 'GB0') || (mComponentCountry == 'GB1') || (mComponentCountry == 'GB2') ||
	(mComponentCountry == '8260') || (mComponentCountry == '8261') || (mComponentCountry == '8262');
}

function fn_GetHerdmarkComponent(mComponentCountry){

  if (isUKEartag()) {
	if (fn_ValAlpha() == true){
		if (fn_AnyDelimiters()== true){
			mComponentHerd = (mEartag.substring(mCharIndex,miDel+mCharIndex));
			fn_GetAnimalComponent(miDel+mCharIndex+1) 
		}else{
			//set mComponentHerd to a maximum of 6 characters 
			mComponentHerd = (mEartag.substring(mCharIndex,mCharIndex+6));
			fn_GetAnimalComponent(mCharIndex+6)
		}
	}else{
		//is no. 9 first numeric char (mCharIndex set in fn_ValAlpha())
		if(mEartag.charAt(mCharIndex) == 9){
			if (fn_AnyDelimiters()== true){
				mComponentHerd = (mEartag.substring(mCharIndex,miDel+mCharIndex));
				fn_GetAnimalComponent(miDel+mCharIndex+1)
			}else{
				//set mComponentHerd to a maximum of 7 characters 
				mComponentHerd = (mEartag.substring(mCharIndex,mCharIndex+7));
				fn_GetAnimalComponent(mCharIndex+7)
			}
		}else{
			if (fn_AnyDelimiters()== true){
				mComponentHerd = (mEartag.substring(mCharIndex,miDel+mCharIndex));
				fn_GetAnimalComponent(miDel+mCharIndex+1)
			}else{
				//set mComponentHerd to a maximum of 6 characters 
				mComponentHerd = (mEartag.substring(mCharIndex,mCharIndex+6));
				fn_GetAnimalComponent(mCharIndex+6)
			}
		}	
	}
		
}else {
	mComponentHerd = null
	fn_ValAlpha()
	fn_GetAnimalComponent(mCharIndex)

}
//ensure all Herd and Animal components are saved in UpperCase
if (mComponentHerd != null){
	mComponentHerd = mComponentHerd.toUpperCase()
}
if (mComponentAnimal != null){
	mComponentAnimal = mComponentAnimal.toUpperCase()
}


}
//End of C.1.3----------------------------------->

	function fn_CountryCodeLength() {
		// Returns the length of the eartag's country code.
		if (mEartag.substring(0, 3) == "826") {
			return 4;
		}
        if (mEartag.substring(0, 2) == "GB") {
            return 3;
		}
		return 2;
    }

function fn_ValAlpha() { 
//checks to see if remining part of ear tag (excluding first 2 chars) has any alphabetic characters.
//Could be performed with pattern matching except the index number of any alpha and numeric characters must be captured
var ChrStr="abcdefghijklmnopqrstuvwxyz"; 
var NumStr="0123456789"; 
var thisChar; 

    for (var i = fn_CountryCodeLength(); i<mEartag.length;i++) {
		thisChar = mEartag.substring(i,[i+1]); 
		thisChar = thisChar.toLowerCase();
		if (mCharIndex == 0){
			if ([NumStr.indexOf(thisChar)]>=0){
				//A numeric char found - RECORD INDEX NUMBER FOR USE IN next function
				mCharIndex = i
			} 
		}
		if ([ChrStr.indexOf(thisChar)]>=0){
			//An alphabetic char found - break AND SEND ELSE WHERE
			if (mCharIndex == 0){
				mCharIndex = i
			}
			return(true);	
			break;			
		}
	} 
//No other alphabetic characters
return(false)
}

function fn_AnyDelimiters(){
//Check for any non-alphanumeric characters in a string
//could use pattern matching except the routine needs to record index number of any numeric/alpha characters
var checkOK = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
var checkStr = mEartag.substring(mCharIndex);
var allValid = true;

		// allow ONLY alphanumeric keys, no symbols or punctuation
		for (miDel = 0;  miDel < checkStr.length;  miDel++){
		  	ch = checkStr.charAt(miDel);
    			for (j = 0;  j < checkOK.length;  j++)
      				if (ch == checkOK.charAt(j))
        				break;
    				if (j == checkOK.length){
  					return(true)
					break;
				}
				
  		}
return(false)	
}

function fn_PreBarimoTest(){
var barimo = /^[^a-z0-9]{0,}[a-zA-Z]{1,2}[0-9]{1,4}[^a-z0-9]{1,}[0-9]{1,5}[a-zA-Z]{0,1}$/i

if (barimo.test(mEartag) == true){
	return(true);
}else{
	return(false);
}
}


function fn_GetAnimalComponent(startIndex){

mComponentAnimal = mEartag.substring(startIndex)
//removes all delimiters and spaces from AnimalComponent, JavaScript 1.2 only, will not work on older browsers
mComponentAnimal = mComponentAnimal.replace(/\W/g, "")

}

function fn_MainFormat(){
//Interpretation code to find FormatID

if (fn_ECCountry(mComponentCountry) == true){
    if ((isUKEartag())){
		fn_InterpretUK()
	}else{
		//mEartag in EC format
		FormatID = 11
	}
}else{
	if (mComponentCountry == null){
		if (mComponentHerd == null){
			//freeformat
			FormatID = 13
		}else{
			//PreBarimo	
			FormatID = 12
		}
	}else{
		//freeformat
		FormatID = 13
	}	
}
}


function fn_InterpretUK(){
//function that determines the mEartag formatID from the Country, Herd and Animal components
var numeric = /^[0-9]{1,}$/i
		
	if ((numeric.test(mComponentHerd) == true) && (numeric.test(mComponentAnimal) == true)){
		if (mComponentHerd.charAt(0) == "9"){
			//N Ireland numeric
			FormatID = 10
		}else if (mComponentHerd.substring(0,2) == "01"){
			//Isle Of Man alpha-numeric
			FormatID = 8
		}else if (mComponentHerd.substring(0,2) == "02"){
			//Channel Islands
			FormatID = 6
		}else if (mComponentHerd.substring(0,2) == "03"){	
			//Gurnsey Numeric
			FormatID = 4
		}else{
			//Mainland GB numeric
			FormatID = 2				
		}				
	}else if (numeric.test(mComponentHerd) == true){
		//N Ireland numeric
		FormatID = 9
	}else{ 
		mComponentHerd = mComponentHerd.toUpperCase()
		if (mComponentHerd.substring(0,2) == "MN"){
			//Isle Of Man Numeric
			FormatID = 7
		}else if (mComponentHerd.substring(0,2) == "GY"){
			//Gurnesy alpha-Numeric
			FormatID = 5	
		}else if (mComponentHerd.substring(0,2) == "JY"){
			//Jersey alpha-Numeric
			FormatID = 3	
		}else{
			//Mainland GB Alpha-Numeric
			FormatID = 1
		}
	}
}

//C.1.2--------------------------------------->
function fn_ReformatComponents(){
//function to reformat components to comply with their FormatID
var iPart
var partA
var partB
var Animal1 = /^[a-zA-Z]{1}[0-9]{1,5}[a-zA-Z]{1}$/i
var Animal2 = /^[a-zA-Z]{1}[0-9]{1,5}$/i
var Animal3 = /^[0-9]{1,5}[a-zA-Z]{1}$/i
var Format13= /^[a-z0-9]{1}$/i
var TwoChar= /^[a-z]{2}$/i

if (mComponentHerd != null){
	if (FormatID == 1 || FormatID ==3){
		if (TwoChar.test(mComponentHerd.substring(0,2)) == true){
			partA = mComponentHerd.substring(0,2)
			partB = mComponentHerd.substring(2)
			while (partB.length < 4){
				partB = "0" + partB
			}
		}else{
			partA = mComponentHerd.substring(0,1)
			partB = mComponentHerd.substring(1)
			while (partB.length < 4){
				partB = "0" + partB
			}
		
		}
		mComponentHerd = partA + partB
	
	}else if (FormatID ==2 || FormatID ==4 || FormatID ==6 || FormatID ==8){

		partA = mComponentHerd.substring(0,2)
		partB = mComponentHerd.substring(2)
		while (partB.length < 4){
			partB = "0" + partB
		}
		mComponentHerd = partA + partB

	}else if (FormatID == 7){

		partA = mComponentHerd.substring(0,2)
		partB = mComponentHerd.substring(2)
		while (partB.length < 3){
			partB = "0" + partB
		}
		mComponentHerd = partA + partB
	}
}

if (mComponentAnimal != null){
	if (FormatID == 1 || FormatID ==3 || FormatID ==5 || FormatID ==7){
		
		if (Animal1.test(mComponentAnimal) == true){	
			partA = mComponentAnimal.charAt(0)
			partB = mComponentAnimal.substring(1)
			while (partB.length < 6){
				partB = "0" + partB
			}
			mComponentAnimal = partA + partB
			AnimalNumber = mComponentAnimal.substring(1,[mComponentAnimal.length - 1])	
		}else if (Animal2.test(mComponentAnimal) == true){
			partA = mComponentAnimal.charAt(0)
			partB = mComponentAnimal.substring(1)
			while (partB.length < 5){
				partB = "0" + partB
			}
			mComponentAnimal = partA + partB			
			AnimalNumber = mComponentAnimal.substring(1,mComponentAnimal.length)
		}else if (Animal3.test(mComponentAnimal) == true){
			while (mComponentAnimal.length < 6){
				mComponentAnimal = "0" + mComponentAnimal
			}
			AnimalNumber = mComponentAnimal.substring(0,[mComponentAnimal.length-1])
		}else{
			while (mComponentAnimal.length < 5){
				mComponentAnimal = "0" + mComponentAnimal
			}
			AnimalNumber = mComponentAnimal
		}	
	}else if (FormatID == 2 || FormatID==4 || FormatID ==6 || FormatID ==8){
			partA = mComponentAnimal.charAt(0)
			partB = mComponentAnimal.substring(1)
			while (partB.length < 5){
				partB = "0" + partB
			}
			mComponentAnimal = partA + partB
	}
}


if (FormatID==13){

//Not in spec in mError	
	while (Format13.test(mComponentAnimal.charAt(0)) == false && mComponentAnimal.length>0 ){
		mComponentAnimal = mComponentAnimal.substring(1)
	}
}
}


function fn_checkCheckDigitFormat9(){

var strElectoralID = mComponentHerd.substr(0,2)
var strHerdNumber = mComponentHerd.substr(2,mComponentHerd.length)
var strAnimalNumber=parseInt(mComponentAnimal.substr (0, mComponentAnimal.length-1),10)

strHerdNumber = fn_PadZero(strHerdNumber,4)

var strCheckDigit = strElectoralID + strHerdNumber
strCheckDigit=strCheckDigit * 10000
strCheckDigit=parseInt(strCheckDigit,10) + parseInt(strAnimalNumber,10)
strCheckDigit=strCheckDigit%23

var CheckDigitArray = new Array('A','B','C','D','E','F','H','I','K','L','M','N','O','P','R','S','T','U','V','W','X','Y','Z')
strCheckDigit = CheckDigitArray[strCheckDigit]

if (mComponentAnimal.substr(mComponentAnimal.length-1,1) != strCheckDigit){
	return false
	}
return true
}//end function fn_checkCheckDigitFormat9

function fn_checkCheckDigitFormat10(){
var strHerdMark = mComponentHerd.substr(0,3)
var strHerdNumber = mComponentHerd.substr(3,mComponentHerd.length)
var strAnimalNumber = mComponentAnimal.substr (0,mComponentAnimal.length-1)

strHerdNumber = fn_PadZero(strHerdNumber,4)
strAnimalNumber = fn_PadZero(strAnimalNumber,4)

var strCheckDigit = strHerdMark + strHerdNumber + strAnimalNumber
strCheckDigit=(parseInt(strCheckDigit,10)%7)+1
if (strCheckDigit != mComponentAnimal.substr(mComponentAnimal.length-1,1)){
	return false}
return true
}//end function fn_checkCheckDigitFormat10

function GetFirstNumberIndex(input_string){
	for (var x=0; x<input_string.length; x++){
		if (!isNaN(input_string.charAt(x))){
			return x;
		}
	}
	return 0;
}//end function GetFirstNumberIndex()

function fn_ValidateEartag(){
//Validation routine for mEartag formats. Sets VarError to 'Failed Validation' is failed.
//Documentation does specify errors to be used with mError numbers. You coul insert individual (more specific errors) where appropriate.
var AlphaChar = /^[a-zA-Z]{1}$/i

switch(FormatID){
case 1:
var Herd = /^[a-zA-Z]{1,2}[0-9]{1,4}$/i
var Animal = /^[a-zA-Z]{0,1}[0-9]{1,5}[a-zA-Z]{0,1}$/i
var AnimalNum = /^[a-zA-Z]{0,1}[0]{1,5}[a-zA-Z]{0,1}$/i
var FirstChar =  /^[a-zA-Z]{1}$/i
var LastChar =  /^[a-zA-Z]{1}$/i
var InvalidChar = "IOPRUX"

  if (isUKEartag()) {
	if (Herd.test(mComponentHerd) == true){
		if ((parseInt(mComponentHerd.substring(GetFirstNumberIndex(mComponentHerd)),10) != 0)){
			if (Animal.test(mComponentAnimal) == true) {
				if (FirstChar.test(mComponentAnimal.charAt(0)) == true){
					if ((mComponentAnimal.charAt(0) != 'X') && (mComponentAnimal.charAt(0) != 'R')){
						mError = "E172"
						return false;
						break;				
					}
				}
				if ([InvalidChar.indexOf(mComponentAnimal.charAt(mComponentAnimal.length -1))] <=0 ){
					if (AnimalNum.test(mComponentAnimal) == false) {
						return true;
						break;
					}else {mError = "E174"}
				} else {mError = "E173"}
			} else {mError = "E160"}
		}else {mError = "E139"}
	}else {mError = "E121"}
}else{mError = "E102"}
break;


case 2:
var Animal = /^[0-9]{2,6}$/i
//
  if (isUKEartag()){
	if (mComponentHerd.match(/\d{3,6}/) != "0"){
		var sUKGeographicID = "58,59,22,23,70,71,72,73,10,11,74,75,24,25,36,37,56,57,32,33,50,51,52,53,12,13,20,21,14,15,54,55,18,19,28,29,26,27,16,17,34,35,38,39,30,31,"
		if (sUKGeographicID.indexOf(mComponentHerd.substring(0,2))>= 0){
			if (parseInt(mComponentHerd.substring(2),10) != 0) {
				if (Animal.test(mComponentAnimal) == true){
					if (mComponentAnimal.substring(0,1) == [mComponentHerd + mComponentAnimal.substring(1,mComponentAnimal.length)]%7 + 1){
						if (mComponentAnimal.substring(1,mComponentAnimal.length) != 0){
							return true;
							break;
						}else {mError = "E175"}
					}else {mError = "E222"}//failed check digit calculation
				}else {mError = "E161"}
			}else {mError = "E140"}	
		}else {mError = "E129"}
	}else {mError = "E115"}
}else {mError = "E103"}
break;

case 3:
var Herd = /^(JY)[0-9]{1,4}$/i
var Animal =  /^[0-9]{1,5}$/i
var AnimalNum = /^[a-zA-Z]{0,1}[0]{1,5}[a-zA-Z]{0,1}$/i

if (isUKEartag()){
	if (Herd.test(mComponentHerd) == true){
		if (parseInt(mComponentHerd.substring(2),10) != 0) {
			if (Animal.test(mComponentAnimal) == true){
				if (AnimalNum.test(mComponentAnimal) == false) {
					return true;
					break;	
				}else {mError = "E176"}
			}else {mError = "E150"}
		}else {mError = "E141"}
	}else {mError = "E130"}
}else {mError = "E104"}
break;


case 4:
var Herd = /^(03)[0-9]{1,4}$/i
var Animal = /^[0-9]{2,6}$/i

if (isUKEartag()){
	if (Herd.test(mComponentHerd) == true){
		if (parseInt(mComponentHerd.substring(2),10) != 0) {
			if (Animal.test(mComponentAnimal) == true){
				if (mComponentAnimal.substring(0,1) == [mComponentHerd + mComponentAnimal.substring(1,mComponentAnimal.length)]%7 + 1){
					if (mComponentAnimal.substring(1,mComponentAnimal.length) != 0){
						return true;
						break;
					}else {mError = "E177"}
				}else {mError = "E222"}
			}else {mError = "E153"}//also ensures all numeric(E163)
		}else {mError = "E142"}
	}else {mError = "E116"}//also ensures all numeric (E124) AND first two chars are 03 (E131)
}else {mError = "E105"}
break;

case 5:
var Herd = /^(GY)[0-9]{1}$/i
var Animal =  /^[0-9]{1,5}$/i
var AnimalNum = /^[a-zA-Z]{0,1}[0]{1,5}[a-zA-Z]{0,1}$/i

if (isUKEartag()){
	if (Herd.test(mComponentHerd) == true){
		if (parseInt(mComponentHerd.substring(2),10) != 0) {
			if (Animal.test(mComponentAnimal) == true){
				if (AnimalNum.test(mComponentAnimal) == false) {
					return true;
					break;
				}else {mError = "E178"}//also ensures all numeric (E164)
			}else {mError = "E151"}//also ensures all numeric (E164)
		}else {mError = "E143"}
	}else {mError = "E132"}
}else {mError = "E106"}
break;

case 6:
var Herd = /^(02)[0-9]{1,4}$/i
var Animal = /^[0-9]{2,6}$/i

if (isUKEartag()){
	if (Herd.test(mComponentHerd) == true){
		if (parseInt(mComponentHerd.substring(2),10) != 0) {
			if (Animal.test(mComponentAnimal) == true){
				if (mComponentAnimal.substring(0,1) == [mComponentHerd + mComponentAnimal.substring(1,mComponentAnimal.length)]%7 + 1){
					if (mComponentAnimal.substring(1,mComponentAnimal.length) != 0){
						return true;
						break;
					}else {mError="E179"}
				}else {mError="E222"}
			}else {mError = "E154"}//also ensures all numeric (E165)
		}else {mError = "E144"}
	}else {mError = "E117"}//also ensures all numeric (E125) AND first two chars are '02' (E133)
}else {mError = "E107"}
break;

case 7:
var Herd = /^(MN)[0-9]{1,3}$/i
var Animal =  /^[0-9]{1,5}$/i
var AnimalNum = /^[a-zA-Z]{0,1}[0]{1,5}[a-zA-Z]{0,1}$/i

if (isUKEartag()){
	if (Herd.test(mComponentHerd) == true){
		if (parseInt(mComponentHerd.substring(2),10) != 0) {
			if (Animal.test(mComponentAnimal) == true){
				if (AnimalNum.test(mComponentAnimal) == false) {
					return true;
					break;
					}else {mError = "E180"}//also ensures all numeric (E164)
			}else {mError = "E152"}//also ensures all numeric (E166)
		}else {mError = "E145"}	
	}else {mError = "E134"}
}else {mError = "E108"}
break;

case 8:
var Herd = /^(01)[0-9]{1,4}$/i
var Animal = /^[0-9]{2,6}$/i

if (isUKEartag()){
	if (Herd.test(mComponentHerd) == true){
		if (parseInt(mComponentHerd.substring(2),10) != 0) {
			if (Animal.test(mComponentAnimal) == true){
				if (mComponentAnimal.substring(0,1) == [mComponentHerd + mComponentAnimal.substring(1,mComponentAnimal.length)]%7 + 1){
					if (mComponentAnimal.substring(1,mComponentAnimal.length) != 0){
						return true;
						break;
					}else {mError="E181"}
				}else {mError="E222"}
			}else {mError = "E155"}//also ensures all numeric (E167)
		}else {mError = "E146"}	
	}else {mError = "E118"}//also ensures all numeric (E146) AND first two chars are '01' (E135)
}else {mError = "E109"}
break;

case 9:
var Herd = /^[0-9]{3,6}$/i
var Animal = /^[0-9]{1,4}[A-Z]{1}$/i

if (isUKEartag()){
	if (Herd.test(mComponentHerd) == true){
		var sElectoralID = '18,20,21,25,30,12,16,17,14,15,49,50,51,56,58,59,61,45,46,47,48,02,04,06,10,52,53,54,55,24,27,40,41,42,43,31,33,35,37,38,39,57,63,64,65,66,'
		if (sElectoralID.indexOf(mComponentHerd.substring(0,2))>= 0){
			if (parseInt(mComponentHerd.substring(2),10) != 0) {
				if (fn_checkCheckDigitFormat9()){
					if (Animal.test(mComponentAnimal) == true){
						if (mComponentAnimal.substring(0,mComponentAnimal.length-1) != 0){
							return true;
							break;
						}else {mError = "E182"}
					}else {mError = "E156"}//also ensures 1-4 numerics followed by 1 alphanumeric (E169)
				}else {mError = "E222"}
			}else {mError = "E147"}
		}else {mError = "E136"}
	}else {mError = "E119"}//also ensures all numeric (E147)
}else {mError = "E110"}
break;

case 10:
var Herd = /^(9)[0-9]{3,6}$/i
var Animal = /^[0-9]{2,5}$/i

if (isUKEartag()){
	if (Herd.test(mComponentHerd) == true){
		if (parseInt(mComponentHerd.substring(3),10) != 0) {
			var sElectoralID = '18,20,21,25,30,12,16,17,14,15,49,50,51,56,58,59,61,45,46,47,48,02,04,06,10,52,53,54,55,24,27,40,41,42,43,31,33,35,37,38,39,57,63,64,65,66,'
			if (sElectoralID.indexOf(mComponentHerd.substring(1,3))>= 0){
				if (fn_checkCheckDigitFormat10()){
					if (Animal.test(mComponentAnimal) == true){
						if (mComponentAnimal.substring(0,mComponentAnimal.length-1) != 0){
							return true;
							break;
						}else {mError = "E183"}//is this herd number element of herd mark component <> zero (E148)?
					}else {mError = "E157"}//also ensures all numeric (E168)
				}else {mError="E222"}
			}else {mError = "E138"}
		}else {mError = "E148"}//checks Herd number isn't 0 - needs cheeking.
	}else {mError = "E120"}//also ensures all numeric (E128) AND first char = '9' (E137)
}else {mError = "E111"}
break;

case 11:
var Animal = /^[0-9,A-Z]{1,12}$/i

if (fn_ECCountry(mComponentCountry) == true){
	if (Animal.test(mComponentAnimal) == true){
		return true;
		break;
	}else {mError = "E158"}
}else {mError = "E112"}
break;

case 12:
var Herd = /^[A-Z]{1,2}[0-9]{1,4}$/i
var Animal = /^[0-9]{1,5}[A-Z]{0,1}$/i

if (mComponentCountry == null){
	if (Herd.test(mComponentHerd) == true){
		if ((parseInt(mComponentHerd.substring(GetFirstNumberIndex(mComponentHerd)),10) != 0)){
			if (Animal.test(mComponentAnimal) == true){
				if(mComponentAnimal != 0){
					return true;
					break;
				}else {mError = "E184"}
			}else {mError = "E171"}
		}else {mError = "E149"}
	}else {mError = "E122"}
}else {mError = "E113"}
break;

case 13:
//var Animal = /^[0-9A-Z]{1,23}$/i
if (mComponentCountry == null){
	if (mComponentAnimal.length < 23 && mComponentAnimal.length > 0){
		return true;
		break;
	}else {mError = "E159"}
}else {mError = "E114"}
break;
}

}


function fn_CalcPresentationFormat(){
//Save the mEartag in appropriate presentation format.
if (FormatID < 9){
	PresentationFormat = mComponentCountry + " " + mComponentHerd + " " + mComponentAnimal
}else if (FormatID == 9){
	Charone = mComponentAnimal.substring(0,mComponentAnimal.length -1)
	Chartwo = mComponentAnimal.charAt(mComponentAnimal.length -1)
	PresentationFormat = mComponentCountry + " " + mComponentHerd + " " + Charone + "-" + Chartwo
}else if (FormatID == 10){
	Charone = mComponentAnimal.substring(0,mComponentAnimal.length -1)
	Chartwo = mComponentAnimal.charAt(mComponentAnimal.length -1)
	PresentationFormat = mComponentCountry + " " + mComponentHerd + " " + Charone + " " + Chartwo
}else if (FormatID == 11){
	PresentationFormat = mComponentCountry + " " + mComponentAnimal
}else if (FormatID == 12){
	PresentationFormat = mComponentHerd + " " + mComponentAnimal
}else if (FormatID == 13){
	PresentationFormat = mComponentAnimal
}
}

function fn_CalcOAISortID(){
//Save the mEartag in appropriate presentation format.
if (FormatID == 2 || FormatID == 4 || FormatID == 6 || FormatID == 8){
	AnimalNumber = mComponentAnimal.substring(1,[mComponentAnimal.length])
}else if(FormatID == 9){
	AnimalNumber = mComponentAnimal.substring(0,[mComponentAnimal.length - 1])
	//alert(mComponentAnimal)
	//alert(AnimalNumber)
}else if(FormatID == 10){
	AnimalNumber = mComponentAnimal.substring(0,[mComponentAnimal.length - 1])
}else if(FormatID == 12){
	if (isNaN(mComponentAnimal) ){
		AnimalNumber = mComponentAnimal.substring(0,[mComponentAnimal.length - 1])
	}else{
		AnimalNumber = mComponentAnimal
	}
}else if(FormatID == 11){
	AnimalNumber = 999999
}else if(FormatID == 13){
	AnimalNumber = 999999
}
}

</script>
