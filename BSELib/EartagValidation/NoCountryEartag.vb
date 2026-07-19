Public Class NoCountryEartag
    Inherits Eartag

    'Friend Sub New(ByVal rawEartag As String)

    '    MyBase.New(rawEartag)

    'End Sub

    Friend Sub New(ByVal countryCode As String, ByVal herdComponent As String, ByVal animalComponent As String)

        MyBase.New(countryCode, herdComponent, animalComponent)

    End Sub

    Protected Overrides Sub GetFormat()

        'Dim charIndex As Integer
        'Dim delimiterIndex As Integer

        'mCountryCode = String.Empty
        'mHerdComponent = String.Empty
        'mAnimalComponent = String.Empty

        'Dim testEartag As String = Trim(mRawEartag)

        'If IsPreBarimo(testEartag) Then
        '    charIndex = IndexOfAlphaNumeric(1, mRawEartag)

        '    delimiterIndex = IndexOfDelimiter(charIndex, mRawEartag)
        '    If (delimiterIndex > 1) Then
        '        mHerdComponent = GetHerdComponent(mRawEartag, charIndex, delimiterIndex - charIndex)
        '        mAnimalComponent = GetAnimalComponent(mRawEartag, delimiterIndex + 1)
        '    Else
        '        mHerdComponent = GetHerdComponent(mRawEartag, charIndex, Len(mRawEartag))
        '    End If
        'Else
        '    mAnimalComponent = mRawEartag
        'End If

        If mHerdComponent.Length = 0 Then
            mFormat = New FreeEartagFormat()
        Else
            mFormat = New PreBarimoEartagFormat()
        End If

    End Sub

    Private Function IsPreBarimo(ByVal testEartag As String) As Boolean

        Dim pattern As String = "^[^0-9A-Z]{0,}[A-Z]{1,2}[0-9]{1,4}[^0-9A-Z]{1,}[0-9]{1,5}[A-Z]{0,1}$"

        IsPreBarimo = Eartag.IsRegexPatternMatch(pattern, testEartag)

    End Function

End Class
