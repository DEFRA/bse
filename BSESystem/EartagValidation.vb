Public Class EartagValidation

#Region "Common.vb"
    Private Const g_strUKGeographic As String = "58,59,22,23,70,71,72,73,10,11,74,75,24,25,36,37,56,57,32,33,50,51,52,53,12,13,20,21,14,15,54,55,18,19,28,29,26,27,16,17,34,35,38,39,30,31"
    Private Const g_strElectoral As String = "18,20,21,25,30,12,16,17,14,15,49,50,51,56,58,59,61,45,46,47,48,02,04,06,10,52,53,54,55,24,27,40,41,42,43,31,33,35,37,38,39,57,63,64,65,66"
    Private Const g_strCountrys As String = "AT,BE,DE,DK,EL,ES,FI,FR,IE,IT,LU,NL,PT,SE,UK,GB0,GB1,GB2,8260,8261,8262"

    Private g_lngFormatID As Integer
    Private g_strCountry As String
    Private g_strHerd As String
    Private g_strComponentAnimal As String
    Private g_strError As String

    Private g_blnUKCountry As Boolean
    Private g_blnECCountry As Boolean
#End Region

#Region "OAI.cls"
    Public Function ValidateEarTag(ByVal strEarTag As String, ByRef sEartagType As String) As String
        Dim AppException As Object
        Dim VeBusErrorHandler As Object
        On Error GoTo ErrorHandler

        g_lngFormatID = 0
        g_strCountry = ""
        g_strHerd = ""
        g_strComponentAnimal = ""
        g_strError = ""
        g_blnUKCountry = False
        g_blnECCountry = False

        Dim strPresentationFormat As String

        Call GetComponents(strEarTag)
        Call GetFormat()
        Call ReformatComponents()
        Call ValidateComponents()

        If g_strError = "" Then
            strPresentationFormat = PresentationFormat()
            ValidateEarTag = strPresentationFormat & "|" & g_lngFormatID
        Else
            ValidateEarTag = g_strError
        End If

        Select Case g_lngFormatID
            Case 1
                sEartagType = "Mainland GB Alpha-Numeric"
            Case 2
                sEartagType = "Mainland GB Numeric"
            Case 3
                sEartagType = "Jersey Alpha-Numeric"
            Case 4
                sEartagType = "Jersey Numeric"
            Case 5
                sEartagType = "Guernsey Aplha-Numeric"
            Case 6
                sEartagType = "Guernsey Numeric"
            Case 7
                sEartagType = "Isle of Man Alpha-Numeric"
            Case 8
                sEartagType = "Isle of Man Numeric"
            Case 9
                sEartagType = "Northern Ireland Alpha-Numeric"
            Case 10
                sEartagType = "Northern Ireland Numeric"
            Case 11
                sEartagType = "European Community Format"
            Case 12
                sEartagType = "Pre-BARIMO"
            Case 13
                sEartagType = "Free Format"
        End Select

        Exit Function

ErrorHandler:

        ValidateEarTag = "<VeBus><Reply>Failure</Reply><Error><ErrorNo>" & Err.Number & "<ErrorNo><ErrorDesc>" & Err.Description & "<ErrorDesc></Error></VeBus>"

        ''UPGRADE_WARNING: Couldn't resolve default property of object AppException.IsIDEMode. Click for more: 'ms-help://MS.VSCC/commoner/redir/redirect.htm?keyword="vbup1037"'
        'Dim lOrigError As Integer
        'Dim sOrigDesc As String
        'Dim oException As VeBusErrorHandler.ErrorHandler
        'If True = AppException.IsIDEMode Then
        '    Stop : Resume
        'Else
        '    oException = New VeBusErrorHandler.ErrorHandler()
        '    'UPGRADE_WARNING: Couldn't resolve default property of object AppException.ERROR_THROW. Click for more: 'ms-help://MS.VSCC/commoner/redir/redirect.htm?keyword="vbup1037"'
        '    If AppException.ERROR_THROW = Err.Number Then
        '        'UPGRADE_WARNING: Array has a new behavior. Click for more: 'ms-help://MS.VSCC/commoner/redir/redirect.htm?keyword="vbup1041"'
        '        'UPGRADE_WARNING: Couldn't resolve default property of object oException.ReleaseError. Click for more: 'ms-help://MS.VSCC/commoner/redir/redirect.htm?keyword="vbup1037"'
        '        Call oException.ReleaseError(Join(New Object() {Err.Description, "ValidateEarTag", "OAI", "OAIValidation"}, Chr(255)), lOrigError, sOrigDesc)
        '    Else
        '        'UPGRADE_WARNING: Array has a new behavior. Click for more: 'ms-help://MS.VSCC/commoner/redir/redirect.htm?keyword="vbup1041"'
        '        'UPGRADE_WARNING: Couldn't resolve default property of object oException.ReleaseError. Click for more: 'ms-help://MS.VSCC/commoner/redir/redirect.htm?keyword="vbup1037"'
        '        Call oException.ReleaseError(Join(New Object() {Err.Number, Err.Description, Erl(), "ValidateEarTag", "OAI", "OAIValidation"}, Chr(255)), lOrigError, sOrigDesc)
        '    End If
        '    'UPGRADE_NOTE: Object oException may not be destroyed until it is garbage collected. Click for more: 'ms-help://MS.VSCC/commoner/redir/redirect.htm?keyword="vbup1029"'
        '    oException = Nothing
        'Err.Raise(lOrigError, , sOrigDesc)
        'End If
    End Function

    Public Function GetErrorCode() As String
        Return g_strError
    End Function

    Public Function GetHerd() As String
        Return g_strHerd
    End Function

    Public Function GetAnimalComponent() As String
        Return g_strComponentAnimal
    End Function

    Public Function GetCountry() As String
        Return g_strCountry
    End Function

#End Region

#Region "SplitComponents.vb"
    Private Sub GetAnimalComponent(ByVal strEarTag As String, ByVal lngIndex As Integer)
        Dim strComponentAnimal As String
        strComponentAnimal = Mid(strEarTag, lngIndex)

        g_strComponentAnimal = Trim(Replace(Replace(Replace(Replace(Replace(strComponentAnimal, "/", ""), "\", ""), "-", ""), "~", ""), " ", ""))
    End Sub

    Private Sub GetHerdComponent(ByVal strEarTag As String, ByVal lngStartIndex As Integer, ByVal lngEndIndex As Integer)
        Dim strComponentHerd As String
        strComponentHerd = Mid(strEarTag, lngStartIndex, lngEndIndex)

        g_strHerd = Trim(Replace(Replace(Replace(Replace(Replace(strComponentHerd, "/", ""), "\", ""), "-", ""), "~", ""), " ", ""))
    End Sub

    Private Function isUKEartag(countryCode As String) As Boolean
        If countryCode = "UK" Or countryCode = "GB0" Or countryCode = "GB1" Or countryCode = "GB2" Or
           countryCode = "8260" Or countryCode = "8261" Or countryCode = "8262" Then
            Return True
        End If
        Return False
    End Function


    Private Sub GetCountryComponent(ByVal strEarTag As String)
        Dim strCountry As String
        Dim strCountryComponent As String

        For Each strCountry In Split(g_strCountrys, ",")
            strCountryComponent = Mid(strEarTag, 1, Len(strCountry))
            If (strCountryComponent = strCountry) Then
                g_strCountry = strCountryComponent
                g_blnECCountry = True
                If isUKEartag(strCountryComponent) Then
                    g_blnUKCountry = True
                End If
                Exit For
            End If
        Next strCountry
    End Sub

    Private Sub GetComponents(ByVal strEarTag As String)
        Dim lngCharIndex As Integer
        Dim lngDelIndex As Integer
        Dim strUKHerdAnimal As String
        Dim strTestEarTag As String

        Call GetCountryComponent(strEarTag)

        If (g_blnUKCountry) Then
            strUKHerdAnimal = Trim(Mid(strEarTag, Len(g_strCountry) + 1))
            lngCharIndex = checkAlpha(1, strUKHerdAnimal)
            lngDelIndex = checkDelimiters(1, strUKHerdAnimal)
            If lngDelIndex = 1 Then
                strUKHerdAnimal = Mid(strUKHerdAnimal, 2)
                lngCharIndex = checkAlpha(1, strUKHerdAnimal)
                lngDelIndex = checkDelimiters(1, strUKHerdAnimal)
            End If
            If lngCharIndex > 1 Then
                If lngDelIndex > 1 And lngDelIndex < lngCharIndex Then
                    Call GetHerdComponent(strUKHerdAnimal, 1, lngDelIndex - 1)
                    Call GetAnimalComponent(strUKHerdAnimal, lngDelIndex + 1)
                Else
                    Call GetHerdComponent(strUKHerdAnimal, 1, 6)
                    Call GetAnimalComponent(strUKHerdAnimal, 7)
                End If
            Else
                If Mid(strUKHerdAnimal, 1, 1) = "9" Then
                    If lngDelIndex > 1 Then
                        Call GetHerdComponent(strUKHerdAnimal, 1, lngDelIndex - 1)
                        Call GetAnimalComponent(strUKHerdAnimal, lngDelIndex + 1)
                    Else
                        Call GetHerdComponent(strUKHerdAnimal, 1, 7)
                        Call GetAnimalComponent(strUKHerdAnimal, 8)
                    End If
                Else
                    If lngDelIndex > 1 Then
                        Call GetHerdComponent(strUKHerdAnimal, 1, lngDelIndex - 1)
                        Call GetAnimalComponent(strUKHerdAnimal, lngDelIndex + 1)
                    Else
                        Call GetHerdComponent(strUKHerdAnimal, 1, 6)
                        Call GetAnimalComponent(strUKHerdAnimal, 7)
                    End If
                End If
            End If
        ElseIf (g_blnECCountry) Then
            lngCharIndex = checkAlpha(3, strEarTag)
            g_strHerd = ""
            Call GetAnimalComponent(strEarTag, lngCharIndex)
        Else
            strTestEarTag = Trim(strEarTag)
            If preBarimoTest(strTestEarTag) Then
                lngCharIndex = checkAlphaNumeric(1, strEarTag)

                lngDelIndex = checkDelimiters(lngCharIndex, strEarTag)
                If (lngDelIndex > 1) Then
                    Call GetHerdComponent(strEarTag, lngCharIndex, lngDelIndex - lngCharIndex)
                    Call GetAnimalComponent(strEarTag, lngDelIndex + 1)
                Else
                    Call GetHerdComponent(strEarTag, lngCharIndex, Len(strEarTag))
                End If
            Else
                g_strHerd = ""
                g_strComponentAnimal = strEarTag
            End If
        End If
    End Sub
#End Region

#Region "Validate.vb"

    Private Function padZero(ByVal strPart As String) As String

        If Len(strPart) < 4 Then
            padZero = (Mid("0000", 1, 4 - Len(strPart)) & strPart)
        Else
            padZero = strPart
        End If

    End Function

    Private Function CalcModulus(ByVal strNumer As String, ByVal strDenom As String) As Integer

        Dim dblNumer As Double
        Dim dblDenom As Double

        dblNumer = Int(CDbl(strNumer))
        dblDenom = Int(CDbl(strDenom))

        CalcModulus = dblNumer - (dblDenom * Fix(dblNumer / dblDenom))

    End Function

    Private Function checkCheckDigitFormat9() As Boolean

        Dim strElectoralID As String
        Dim strHerdNumber As String
        Dim strAnimalNumber As String
        Dim strCheck As String
        Dim dblCheck As Double
        Dim lngMod As Integer


        Dim arrCheck As Object

        strElectoralID = Mid(g_strHerd, 1, 2)
        strHerdNumber = Mid(g_strHerd, 3)
        strAnimalNumber = Left(g_strComponentAnimal, Len(g_strComponentAnimal) - 1)

        strHerdNumber = padZero(strHerdNumber)

        strCheck = strElectoralID & strHerdNumber
        dblCheck = CDbl(strCheck) * 10000
        dblCheck = dblCheck + CInt(strAnimalNumber)

        lngMod = CalcModulus(CStr(dblCheck), "23")

        'UPGRADE_WARNING: Couldn't resolve default property of object arrCheck. Click for more: 'ms-help://MS.VSCC/commoner/redir/redirect.htm?keyword="vbup1037"'
        arrCheck = Split("A,B,C,D,E,F,H,I,K,L,M,N,O,P,R,S,T,U,V,W,X,Y,Z", ",")

        'UPGRADE_WARNING: Couldn't resolve default property of object arrCheck(). Click for more: 'ms-help://MS.VSCC/commoner/redir/redirect.htm?keyword="vbup1037"'
        strCheck = arrCheck(lngMod)

        If Right(g_strComponentAnimal, 1) <> strCheck Then

            checkCheckDigitFormat9 = False
        Else

            checkCheckDigitFormat9 = True

        End If
    End Function

    Private Function checkCheckDigitFormat10() As Boolean

        Dim strHerdMark As String
        Dim strHerdNumber As String
        Dim strAnimalNumber As String
        Dim strCheck As String
        Dim lngMod As Integer

        strHerdMark = Mid(g_strHerd, 1, 3)
        strHerdNumber = Mid(g_strHerd, 4)
        strAnimalNumber = Mid(g_strComponentAnimal, 1, Len(g_strComponentAnimal) - 1)

        strHerdNumber = padZero(strHerdNumber)
        strAnimalNumber = padZero(strAnimalNumber)

        strCheck = strHerdMark & strHerdNumber & strAnimalNumber

        lngMod = CalcModulus(strCheck, "7") + 1

        If CStr(lngMod) <> Right(g_strComponentAnimal, 1) Then
            checkCheckDigitFormat10 = False
        Else
            checkCheckDigitFormat10 = True
        End If

    End Function

    Private Function CheckElectoral(ByVal strCheck As String) As Boolean

        Dim strNode As Object

        For Each strNode In Split(g_strElectoral, ",")
            'UPGRADE_WARNING: Couldn't resolve default property of object strNode. Click for more: 'ms-help://MS.VSCC/commoner/redir/redirect.htm?keyword="vbup1037"'
            If (strCheck = strNode) Then
                CheckElectoral = True
                Exit For
            End If
        Next strNode
    End Function

    Private Function CheckUKGeographic(ByVal strCheck As String) As Boolean

        Dim strNode As Object

        For Each strNode In Split(g_strUKGeographic, ",")
            'UPGRADE_WARNING: Couldn't resolve default property of object strNode. Click for more: 'ms-help://MS.VSCC/commoner/redir/redirect.htm?keyword="vbup1037"'
            If (strCheck = strNode) Then
                CheckUKGeographic = True
                Exit For
            End If
        Next strNode
    End Function

    Private Sub ValidateComponents()

        Dim strHerd As String
        Dim strAnimal As String
        Dim strAnimalNum As String
        Dim strFirstChar As String
        Dim strLastChar As String
        Dim strFirstNum As String
        Dim strInvalidChar As String

        strFirstNum = "\d{1}"

        Dim strMod As String
        Dim lngMod As Integer

        Select Case (g_lngFormatID)
            Case 1

                strHerd = "^[a-zA-Z]{1,2}[0-9]{1,4}$"
                strAnimal = "^[a-zA-Z]{0,1}[0-9]{1,5}[a-zA-Z]{0,1}$"
                strAnimalNum = "^[a-zA-Z]{0,1}[0]{1,5}[a-zA-Z]{0,1}$"
                strFirstChar = "^[a-zA-Z]{1}$"
                strLastChar = "^[a-zA-Z]{1}$"
                strInvalidChar = "I,O,P,R,U,X"

                If isUKEartag(g_strCountry) Then
                    If (valRegularExpression(strHerd, g_strHerd)) Then
                        If CInt(Mid(g_strHerd, checkNumeric(2, g_strHerd))) > 0 Then
                            If (valRegularExpression(strAnimal, g_strComponentAnimal)) Then
                                If (valRegularExpression(strFirstChar, Left(g_strComponentAnimal, 1))) Then
                                    If ((Mid(g_strComponentAnimal, 1, 1) <> "X") And (Mid(g_strComponentAnimal, 1, 1) <> "R")) Then
                                        g_strError = "E172"
                                        Exit Sub
                                    End If
                                End If
                                If Not CheckString(Right(g_strComponentAnimal, 1), strInvalidChar) Then
                                    If (Not (valRegularExpression(strAnimalNum, g_strComponentAnimal))) Then
                                        Exit Sub
                                    Else
                                        g_strError = "E174"
                                    End If
                                Else
                                    g_strError = "E173"
                                End If
                            Else
                                g_strError = "E160"
                            End If
                        Else
                            g_strError = "E139"
                        End If
                    Else
                        g_strError = "E121"
                    End If
                Else
                    g_strError = "E102"
                End If

            Case 2
                strAnimal = "\d{2,6}"
                strHerd = "\d{3,6}"

                If isUKEartag(g_strCountry) Then
                    If (valRegularExpression(strHerd, g_strHerd)) Then
                        If CheckUKGeographic(Mid(g_strHerd, 1, 2)) Then
                            If CInt(Mid(g_strHerd, 3)) > 0 Then
                                If (valRegularExpression(strAnimal, g_strComponentAnimal)) Then
                                    strMod = g_strHerd & Mid(g_strComponentAnimal, 2)
                                    lngMod = CalcModulus(strMod, CStr(7))
                                    If Left(g_strComponentAnimal, 1) = CStr(lngMod + 1) Then
                                        If (CInt(CDbl(Mid(g_strComponentAnimal, 2, Len(g_strComponentAnimal))) <> 0)) Then
                                            Exit Sub
                                        Else
                                            g_strError = "E175"
                                        End If
                                    Else
                                        g_strError = "E222"
                                    End If
                                Else
                                    g_strError = "E161"
                                End If
                            Else
                                g_strError = "E140"
                            End If
                        Else
                            g_strError = "E129"
                        End If
                    Else
                        g_strError = "E115"
                    End If
                Else
                    g_strError = "E103"
                End If

            Case 3
                strHerd = "^(JY)[0-9]{1,4}$"
                strAnimal = "^[0-9]{1,5}$"
                strAnimalNum = "^[a-zA-Z]{0,1}[0]{1,5}[a-zA-Z]{0,1}$"

                If isUKEartag(g_strCountry) Then
                    If (valRegularExpression(strHerd, g_strHerd)) Then
                        If CInt(Mid(g_strHerd, 3)) > 0 Then
                            If (valRegularExpression(strAnimal, g_strComponentAnimal)) Then
                                If (Not (valRegularExpression(strAnimalNum, g_strComponentAnimal))) Then
                                    Exit Sub
                                Else
                                    g_strError = "E176"
                                End If
                            Else
                                g_strError = "E150"
                            End If
                        Else
                            g_strError = "E141"
                        End If
                    Else
                        g_strError = "E130"
                    End If
                Else
                    g_strError = "E104"
                End If

            Case 4
                strHerd = "^(03)[0-9]{1,4}$"
                strAnimal = "^[0-9]{2,6}$"

                If isUKEartag(g_strCountry) Then
                    If (valRegularExpression(strHerd, g_strHerd)) Then
                        If CInt(Mid(g_strHerd, 3)) > 0 Then
                            If (valRegularExpression(strAnimal, g_strComponentAnimal)) Then
                                strMod = g_strHerd & Mid(g_strComponentAnimal, 2)
                                lngMod = CalcModulus(strMod, CStr(7))
                                If Left(g_strComponentAnimal, 1) = CStr(lngMod + 1) Then
                                    If (CInt(Mid(g_strComponentAnimal, 2, Len(g_strComponentAnimal))) <> 0) Then
                                        Exit Sub
                                    Else
                                        g_strError = "E177"
                                    End If
                                Else
                                    g_strError = "E222"
                                End If
                            Else
                                g_strError = "E153"
                            End If
                        Else
                            g_strError = "E142"
                        End If
                    Else
                        g_strError = "E116"
                    End If
                Else
                    g_strError = "E105"
                End If

            Case 5
                strHerd = "^(GY)[0-9]{1}$"
                strAnimal = "^[0-9]{1,5}$"
                strAnimalNum = "^[a-zA-Z]{0,1}[0]{1,5}[a-zA-Z]{0,1}$"

                If isUKEartag(g_strCountry) Then
                    If (valRegularExpression(strHerd, g_strHerd)) Then
                        If Mid(g_strHerd, 3) <> "0" Then
                            If (valRegularExpression(strAnimal, g_strComponentAnimal)) Then
                                If (Not (valRegularExpression(strAnimalNum, g_strComponentAnimal))) Then
                                    Exit Sub
                                Else
                                    g_strError = "E178"
                                End If
                            Else
                                g_strError = "E151"
                            End If
                        Else
                            g_strError = "E143"
                        End If
                    Else
                        g_strError = "E132"
                    End If
                Else
                    g_strError = "E106"
                End If

            Case 6
                strHerd = "^(02)[0-9]{1,4}$"
                strAnimal = "^[0-9]{2,6}$"

                If isUKEartag(g_strCountry) Then
                    If (valRegularExpression(strHerd, g_strHerd)) Then
                        If CInt(Mid(g_strHerd, 3)) > 0 Then
                            If (valRegularExpression(strAnimal, g_strComponentAnimal)) Then
                                strMod = g_strHerd & Mid(g_strComponentAnimal, 2)
                                lngMod = CalcModulus(strMod, CStr(7))
                                If Left(g_strComponentAnimal, 1) = CStr(lngMod + 1) Then
                                    If (CInt(Mid(g_strComponentAnimal, 2, Len(g_strComponentAnimal))) <> 0) Then
                                        Exit Sub
                                    Else
                                        g_strError = "E179"
                                    End If
                                Else
                                    g_strError = "E222"
                                End If
                            Else
                                g_strError = "E154"
                            End If
                        Else
                            g_strError = "E144"
                        End If
                    Else
                        g_strError = "E117"
                    End If
                Else
                    g_strError = "E107"
                End If

            Case 7
                strHerd = "^(MN)[0-9]{1,3}$"
                strAnimal = "^[0-9]{1,5}$"
                strAnimalNum = "^[a-zA-Z]{0,1}[0]{1,5}[a-zA-Z]{0,1}$"

                If isUKEartag(g_strCountry) Then
                    If (valRegularExpression(strHerd, g_strHerd)) Then
                        If CInt(Mid(g_strHerd, 3)) > 0 Then
                            If (valRegularExpression(strAnimal, g_strComponentAnimal)) Then
                                If (Not (valRegularExpression(strAnimalNum, g_strComponentAnimal))) Then
                                    Exit Sub
                                Else
                                    g_strError = "E180"
                                End If
                            Else
                                g_strError = "E152"
                            End If
                        Else
                            g_strError = "E145"
                        End If
                    Else
                        g_strError = "E134"
                    End If
                Else
                    g_strError = "E108"
                End If

            Case 8
                strHerd = "^(01)[0-9]{1,4}$"
                strAnimal = "^[0-9]{2,6}$"

                If isUKEartag(g_strCountry) Then
                    If (valRegularExpression(strHerd, g_strHerd)) Then
                        If CInt(Mid(g_strHerd, 3)) > 0 Then
                            If (valRegularExpression(strAnimal, g_strComponentAnimal)) Then
                                strMod = g_strHerd & Mid(g_strComponentAnimal, 2)
                                lngMod = CalcModulus(strMod, CStr(7))
                                If Left(g_strComponentAnimal, 1) = CStr(lngMod + 1) Then
                                    If (CInt(Mid(g_strComponentAnimal, 2, Len(g_strComponentAnimal))) <> 0) Then
                                        Exit Sub
                                    Else
                                        g_strError = "E181"
                                    End If
                                Else
                                    g_strError = "E222"
                                End If
                            Else
                                g_strError = "E155"
                            End If
                        Else
                            g_strError = "E146"
                        End If
                    Else
                        g_strError = "E118"
                    End If
                Else
                    g_strError = "E109"
                End If

            Case 9
                strHerd = "^[0-9]{3,6}$"
                strAnimal = "^[0-9]{1,4}[A-Z]{1}$"

                If isUKEartag(g_strCountry) Then
                    If (valRegularExpression(strHerd, g_strHerd)) Then
                        If CheckElectoral(Mid(g_strHerd, 1, 2)) Then
                            If CInt(Mid(g_strHerd, 3)) > 0 Then
                                If (valRegularExpression(strAnimal, g_strComponentAnimal)) Then
                                    If (checkCheckDigitFormat9()) Then
                                        If Mid(g_strComponentAnimal, 1, Len(g_strComponentAnimal) - 1) <> "0" Then
                                            Exit Sub
                                        Else
                                            g_strError = "E182"
                                        End If
                                    Else
                                        g_strError = "E222"
                                    End If
                                Else
                                    g_strError = "E156"
                                End If
                            Else
                                g_strError = "E147"
                            End If
                        Else
                            g_strError = "E136"
                        End If
                    Else
                        g_strError = "E119"
                    End If
                Else
                    g_strError = "E110"
                End If

            Case 10
                strHerd = "^(9)[0-9]{3,6}$"
                strAnimal = "^[0-9]{2,5}$"

                If isUKEartag(g_strCountry) Then
                    If valRegularExpression(strHerd, g_strHerd) Then
                        If CInt(Mid(g_strHerd, 4)) > 0 Then
                            If CheckElectoral(Mid(g_strHerd, 2, 2)) Then
                                If (valRegularExpression(strAnimal, g_strComponentAnimal)) Then
                                    If checkCheckDigitFormat10() Then
                                        If Mid(g_strComponentAnimal, 1, Len(g_strComponentAnimal) - 1) <> "0" Then
                                            Exit Sub
                                        Else
                                            g_strError = "E183"
                                        End If
                                    Else
                                        g_strError = "E222"
                                    End If
                                Else
                                    g_strError = "E157"
                                End If
                            Else
                                g_strError = "E138"
                            End If
                        Else
                            g_strError = "E148"
                        End If
                    Else
                        g_strError = "E120"
                    End If
                Else
                    g_strError = "E111"
                End If

            Case 11
                strAnimal = "^[0-9A-Z]{1,12}$"

                If (g_blnECCountry) Then
                    If (valRegularExpression(strAnimal, g_strComponentAnimal)) Then
                        Exit Sub
                    Else
                        g_strError = "E158"
                    End If
                Else
                    g_strError = "E112"
                End If

            Case 12
                strHerd = "^[A-Z]{1,2}[0-9]{1,4}$"
                strAnimal = "^[0-9]{1,5}[A-Z]{0,1}$"

                If (g_strCountry = "") Then
                    If (valRegularExpression(strHerd, g_strHerd)) Then
                        If Mid(g_strHerd, 3) <> "0" Then
                            If (valRegularExpression(strAnimal, g_strComponentAnimal)) Then
                                If g_strComponentAnimal <> "0" Then
                                    Exit Sub
                                Else
                                    g_strError = "E184"
                                End If
                            Else
                                g_strError = "E171"
                            End If
                        Else
                            g_strError = "E149"
                        End If
                    Else
                        g_strError = "E122"
                    End If
                Else
                    g_strError = "E113"
                End If

            Case Else

                If (g_strCountry = "") Then
                    If (Len(g_strComponentAnimal) < 23 And Len(g_strComponentAnimal) > 0) Then
                        Exit Sub
                    Else
                        g_strError = "E159"
                    End If
                Else
                    g_strError = "E114"
                End If

        End Select
    End Sub
#End Region

#Region "Reformat.vb"

    Private Sub ReformatComponents()

        Dim lngPart As Integer
        Dim strPartA As String
        Dim strPartB As String

        Dim strAnimal1 As String
        Dim strAnimal2 As String
        Dim strAnimal3 As String
        Dim strFormat13 As String
        Dim strTwoChar As String

        strAnimal1 = "^[a-zA-Z]{1}[0-9]{1,5}[a-zA-Z]{1}$"
        strAnimal2 = "^[a-zA-Z]{1}[0-9]{1,5}$"
        strAnimal3 = "^[0-9]{1,5}[a-zA-Z]{1}$"
        strFormat13 = "^[A-Z0-9]{1,}$"
        strTwoChar = "^[A-Z]{2}$"

        If (g_strHerd <> "") Then

            If (g_lngFormatID = 1 Or g_lngFormatID = 3) Then

                If (valRegularExpression(strTwoChar, Mid(g_strHerd, 1, 2))) Then

                    strPartA = Mid(g_strHerd, 1, 2)
                    strPartB = Mid(g_strHerd, 3)
                    If Len(strPartB) < 4 Then
                        strPartB = Mid("0000", 1, 4 - Len(strPartB)) & strPartB
                    End If
                    g_strHerd = strPartA & strPartB

                Else

                    strPartA = Mid(g_strHerd, 1, 1)
                    strPartB = Mid(g_strHerd, 2)
                    If Len(strPartB) < 4 Then
                        strPartB = Mid("0000", 1, 4 - Len(strPartB)) & strPartB
                    End If
                    g_strHerd = strPartA & strPartB

                End If

            ElseIf (g_lngFormatID = 2 Or g_lngFormatID = 4 Or g_lngFormatID = 6 Or g_lngFormatID = 8) Then

                strPartA = Mid(g_strHerd, 1, 2)
                strPartB = Mid(g_strHerd, 3)
                If Len(strPartB) < 4 Then
                    strPartB = Mid("0000", 1, 4 - Len(strPartB)) & strPartB
                End If
                g_strHerd = strPartA & strPartB

            ElseIf (g_lngFormatID = 7) Then

                strPartA = Mid(g_strHerd, 1, 2)
                strPartB = Mid(g_strHerd, 3)
                If Len(strPartB) < 3 Then
                    strPartB = Mid("000", 1, 3 - Len(strPartB)) & strPartB
                End If
                g_strHerd = strPartA & strPartB
            End If
        End If

        If (g_strComponentAnimal <> "") Then

            If (g_lngFormatID = 1 Or g_lngFormatID = 3 Or g_lngFormatID = 5 Or g_lngFormatID = 7) Then

                If (valRegularExpression(strAnimal1, g_strComponentAnimal)) Then

                    strPartA = Mid(g_strComponentAnimal, 1, 1)
                    strPartB = Mid(g_strComponentAnimal, 2)
                    If Len(strPartB) < 6 Then
                        strPartB = Mid("000000", 1, 6 - Len(strPartB)) & strPartB
                    End If
                    g_strComponentAnimal = strPartA & strPartB

                ElseIf (valRegularExpression(strAnimal2, g_strComponentAnimal)) Then

                    strPartA = Mid(g_strComponentAnimal, 1, 1)
                    strPartB = Mid(g_strComponentAnimal, 2)
                    If Len(strPartB) < 5 Then
                        strPartB = Mid("00000", 1, 5 - Len(strPartB)) & strPartB
                    End If
                    g_strComponentAnimal = strPartA & strPartB

                ElseIf (valRegularExpression(strAnimal3, g_strComponentAnimal)) Then

                    g_strComponentAnimal = Mid("000000", 1, 6 - Len(g_strComponentAnimal)) & g_strComponentAnimal
                Else

                    If Len(g_strComponentAnimal) < 5 Then
                        g_strComponentAnimal = Mid("00000", 1, 5 - Len(g_strComponentAnimal)) & g_strComponentAnimal
                    End If

                End If

            ElseIf (g_lngFormatID = 2 Or g_lngFormatID = 4 Or g_lngFormatID = 6 Or g_lngFormatID = 8) Then

                strPartA = Mid(g_strComponentAnimal, 1, 1)
                strPartB = Mid(g_strComponentAnimal, 2)
                If Len(strPartB) < 5 Then
                    strPartB = Mid("00000", 1, 5 - Len(strPartB)) & strPartB
                End If
                g_strComponentAnimal = strPartA & strPartB
            End If
        End If


        If (g_lngFormatID = 13) Then
            Do While ((valRegularExpression(strFormat13, Mid(g_strComponentAnimal, 1, 1)) = False) And Len(g_strComponentAnimal) > 0)
                g_strComponentAnimal = Mid(g_strComponentAnimal, 2)
            Loop
        End If

    End Sub
#End Region

#Region "Presentation.vb"

    Private Function PresentationFormat() As String

        Dim strSplitOne As String
        Dim strSplitTwo As String

        If (g_lngFormatID < 9) Then

            PresentationFormat = g_strCountry & " " & g_strHerd & " " & g_strComponentAnimal

        ElseIf (g_lngFormatID = 9) Then
            strSplitOne = Mid(g_strComponentAnimal, 1, Len(g_strComponentAnimal) - 1)
            strSplitOne = Replace(strSplitOne, "-", "")
            strSplitTwo = Mid(g_strComponentAnimal, Len(g_strComponentAnimal), 1)
            PresentationFormat = g_strCountry & " " & g_strHerd & " " & strSplitOne & "-" & strSplitTwo

        ElseIf (g_lngFormatID = 10) Then

            strSplitOne = Mid(g_strComponentAnimal, 1, Len(g_strComponentAnimal) - 1)
            strSplitTwo = Mid(g_strComponentAnimal, Len(g_strComponentAnimal), 1)
            PresentationFormat = g_strCountry & " " & g_strHerd & " " & strSplitOne & " " & strSplitTwo

        ElseIf (g_lngFormatID = 11) Then

            PresentationFormat = g_strCountry & " " & g_strComponentAnimal

        ElseIf (g_lngFormatID = 12) Then

            PresentationFormat = g_strHerd & " " & g_strComponentAnimal

        ElseIf (g_lngFormatID = 13) Then

            PresentationFormat = g_strComponentAnimal

        End If

    End Function
#End Region

#Region "Format.vb"
    Private Sub InterpretUK(ByRef strComponentHerd As String, ByRef strComponentAnimal As String)

        If (IsNumeric(strComponentHerd)) And (IsNumeric(strComponentAnimal)) Then
            If (Mid(strComponentHerd, 1, 1) = "9") Then
                g_lngFormatID = 10
            ElseIf (Mid(strComponentHerd, 1, 2) = "01") Then
                g_lngFormatID = 8
            ElseIf (Mid(strComponentHerd, 1, 2) = "02") Then
                g_lngFormatID = 6
            ElseIf (Mid(strComponentHerd, 1, 2) = "03") Then
                g_lngFormatID = 4
            Else
                g_lngFormatID = 2
            End If
        ElseIf (IsNumeric(strComponentHerd)) Then
            g_lngFormatID = 9
        Else
            If (Mid(strComponentHerd, 1, 2) = "MN") Then
                g_lngFormatID = 7
            ElseIf (Mid(strComponentHerd, 1, 2) = "GY") Then
                g_lngFormatID = 5
            ElseIf (Mid(strComponentHerd, 1, 2) = "JY") Then
                g_lngFormatID = 3
            Else
                g_lngFormatID = 1
            End If
        End If
    End Sub

    Private Sub GetFormat()
        If g_blnECCountry Then
            If isUKEartag(g_strCountry) Then
                Call InterpretUK(g_strHerd, g_strComponentAnimal)
            Else
                g_lngFormatID = 11
            End If
        ElseIf g_strCountry = "" Then
            If g_strHerd = "" Then
                g_lngFormatID = 13
            Else
                g_lngFormatID = 12
            End If
        Else
            g_lngFormatID = 13
        End If
    End Sub
#End Region

#Region "Routines.vb"

    Private Function valRegularExpression(ByVal strPatrn As String, ByVal strString As String) As Boolean
        Dim objMatch As System.Text.RegularExpressions.Match = System.Text.RegularExpressions.Regex.Match(strString, strPatrn)
        valRegularExpression = objMatch.Success
    End Function

    Private Function preBarimoTest(ByVal strEarTag As String) As Boolean
        Dim strBarimo As String
        strBarimo = "^[^0-9A-Z]{0,}[A-Z]{1,2}[0-9]{1,4}[^0-9A-Z]{1,}[0-9]{1,5}[A-Z]{0,1}$"

        preBarimoTest = valRegularExpression(strBarimo, strEarTag)
    End Function

    Private Function ECCountry(ByRef strCountryCode As String) As Boolean
        Dim strCountry As String

        g_blnECCountry = False
        ECCountry = False

        For Each strCountry In Split(g_strCountry, ",")
            If (strCountryCode = strCountry) Then
                g_blnECCountry = True
                ECCountry = True
                Exit For
            End If
        Next strCountry
    End Function

    Private Function checkAlpha(ByVal lngStart As Integer, ByVal strString As String) As Integer

        Dim ChrString As String
        ChrString = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
        Dim strNode As String
        Dim lngCount As Integer

        For lngCount = lngStart To Len(strString) - 1
            strNode = Mid(strString, lngCount, 1)
            If (InStr(ChrString, strNode) > 0) Then
                checkAlpha = lngCount
                Exit For
            End If
        Next
    End Function

    Private Function checkNumeric(ByVal lngStart As Integer, ByVal strString As String) As Integer

        Dim ChrString As String
        ChrString = "0123456789"
        Dim strNode As String
        Dim lngCount As Integer

        For lngCount = lngStart To Len(strString)
            strNode = Mid(strString, lngCount, 1)
            If (InStr(ChrString, strNode) > 0) Then
                checkNumeric = lngCount
                Exit For
            End If
        Next
    End Function

    Private Function checkAlphaNumeric(ByVal lngStart As Integer, ByVal strString As String) As Integer

        Dim ChrString As String
        ChrString = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
        Dim NumString As String
        NumString = "0123456789"
        Dim strNode As String
        Dim lngCount As Integer

        For lngCount = lngStart To Len(strString) - 1
            strNode = Mid(strString, lngCount, 1)
            If (InStr(NumString, strNode) > 0) Then
                checkAlphaNumeric = lngCount
                Exit For
            ElseIf (InStr(ChrString, strNode) > 0) Then
                checkAlphaNumeric = lngCount
                Exit For
            End If
        Next
    End Function

    Private Function checkDelimiters(ByVal lngStart As Integer, ByVal strString As String) As Integer

        Dim ChrString As String
        ChrString = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"
        Dim strNode As String
        Dim lngCount As Integer

        For lngCount = lngStart To Len(strString) - 1
            strNode = Mid(strString, lngCount, 1)
            If Not (InStr(ChrString, strNode) > 0) Then
                checkDelimiters = lngCount
                Exit For
            End If
        Next
    End Function

    Private Function checkDelimitersFull(ByVal lngStart As Integer, ByVal strString As String) As Boolean

        Dim ChrString As String
        ChrString = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"
        Dim strNode As String
        Dim lngCount As Integer

        For lngCount = lngStart To Len(strString)
            strNode = Mid(strString, lngCount, 1)
            If Not (InStr(ChrString, strNode) > 0) Then
                checkDelimitersFull = True
                Exit For
            End If
        Next
    End Function

    Private Function CheckString(ByVal strCheck As String, ByVal strSplit As String) As Boolean
        Dim strNode As String

        For Each strNode In Split(strSplit, ",")
            If (strCheck = strNode) Then
                CheckString = True
                Exit For
            End If
        Next strNode
    End Function
#End Region

End Class
