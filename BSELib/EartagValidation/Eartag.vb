Imports System.Linq

Public MustInherit Class Eartag

    Private Shared mNonUKCountries As String() = {"AT", "BE", "DE", "DK", "EL", "ES", "FI", "FR", "IE", "IT", "LU", "NL", "PT", "SE"}
    Public Shared mUKCountries As String() = {"UK", "GB0", "GB1", "GB2", "8260", "8261", "8262"}
    Private Shared mCountries As String() = mNonUKCountries.Concat(mUKCountries).ToArray()

    Protected mRawEartag As String
    Protected mCountryCode As String
    Protected mHerdComponent As String
    Protected mAnimalComponent As String
    Protected mFormat As IEartagFormat
    Protected mErrorCode As String
    Protected mPresentationEartag As String

    Public ReadOnly Property RawValue() As String
        Get
            Return mRawEartag
        End Get
    End Property

    Public ReadOnly Property PresentationValue() As String
        Get
            Return mPresentationEartag
        End Get
    End Property

    Public ReadOnly Property CountryCode() As String
        Get
            Return mCountryCode
        End Get
    End Property

    Public ReadOnly Property HerdComponent() As String
        Get
            Return mHerdComponent
        End Get
    End Property

    Public ReadOnly Property AnimalComponent() As String
        Get
            Return mAnimalComponent
        End Get
    End Property

    Public ReadOnly Property FormatId() As Integer
        Get
            Return mFormat.FormatId
        End Get
    End Property

    Public ReadOnly Property FormatName() As String
        Get
            Return mFormat.FormatName
        End Get
    End Property

    Public ReadOnly Property ErrorCode() As String
        Get
            Return mErrorCode
        End Get
    End Property

    Public Shared Function GetEartag(ByVal countryCode As String, ByVal herdComponent As String, ByVal animalComponent As String) As Eartag

        If IsNewVersionId(countryCode) Then
            Return New IsoEarTag(countryCode, herdComponent, animalComponent)
        End If

        If mUKCountries.Contains(countryCode) Then
            Return New UKEartag(countryCode, herdComponent, animalComponent)
        End If

        If IsECCountryCode(countryCode) Then
            Return New ECEartag(countryCode, herdComponent, animalComponent)
        End If

        Return New NoCountryEartag(countryCode, herdComponent, animalComponent)

    End Function

    Protected Sub New(ByVal countryCode As String, ByVal herdComponent As String, ByVal animalComponent As String)

        mRawEartag = Nothing
        mCountryCode = countryCode
        mHerdComponent = herdComponent
        mAnimalComponent = animalComponent
        GetFormat()
        ReformatComponents()
        ValidateComponents()
        If mErrorCode.Length = 0 Then
            GetPresentationFormat()
        End If

    End Sub

    Protected MustOverride Sub GetFormat()

    Protected Overridable Sub ReformatComponents()

        If mHerdComponent.Length > 0 Then
            mHerdComponent = mFormat.ReformatHerdComponent(mHerdComponent)
        End If

        If mAnimalComponent.Length > 0 Then
            mAnimalComponent = mFormat.ReformatAnimalComponent(mAnimalComponent)
        End If

    End Sub

    Protected Overridable Sub ValidateComponents()

        mErrorCode = mFormat.Validate(mCountryCode, mHerdComponent, mAnimalComponent)

    End Sub

    Protected Overridable Sub GetPresentationFormat()

        mPresentationEartag = mFormat.GetPresentationFormat(mCountryCode, mHerdComponent, mAnimalComponent)

    End Sub

#Region "helper functions"

    Protected Function GetHerdComponent(ByVal strEarTag As String, ByVal lngStartIndex As Integer, ByVal lngEndIndex As Integer) As String

        Dim strComponentHerd As String
        strComponentHerd = Mid(strEarTag, lngStartIndex, lngEndIndex)

        Return Trim(Replace(Replace(Replace(Replace(Replace(strComponentHerd, "/", ""), "\", ""), "-", ""), "~", ""), " ", ""))

    End Function

    Protected Function GetAnimalComponent(ByVal strEarTag As String, ByVal lngIndex As Integer) As String

        Dim strComponentAnimal As String
        strComponentAnimal = Mid(strEarTag, lngIndex)

        Return Trim(Replace(Replace(Replace(Replace(Replace(strComponentAnimal, "/", ""), "\", ""), "-", ""), "~", ""), " ", ""))

    End Function

    Protected Function IndexOfAlpha(ByVal lngStart As Integer, ByVal strString As String) As Integer

        Dim ChrString As String
        ChrString = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
        Dim strNode As String
        Dim lngCount As Integer

        For lngCount = lngStart To Len(strString) - 1
            strNode = Mid(strString, lngCount, 1)
            If (InStr(ChrString, strNode) > 0) Then
                IndexOfAlpha = lngCount
                Exit For
            End If
        Next
    End Function

    Protected Function IndexOfDelimiter(ByVal lngStart As Integer, ByVal strString As String) As Integer

        Dim ChrString As String
        ChrString = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"
        Dim strNode As String
        Dim lngCount As Integer

        For lngCount = lngStart To Len(strString) - 1
            strNode = Mid(strString, lngCount, 1)
            If Not (InStr(ChrString, strNode) > 0) Then
                IndexOfDelimiter = lngCount
                Exit For
            End If
        Next
    End Function

    Protected Function IndexOfAlphaNumeric(ByVal lngStart As Integer, ByVal strString As String) As Integer

        Dim ChrString As String
        ChrString = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
        Dim NumString As String
        NumString = "0123456789"
        Dim strNode As String
        Dim lngCount As Integer

        For lngCount = lngStart To Len(strString) - 1
            strNode = Mid(strString, lngCount, 1)
            If (InStr(NumString, strNode) > 0) Then
                IndexOfAlphaNumeric = lngCount
                Exit For
            ElseIf (InStr(ChrString, strNode) > 0) Then
                IndexOfAlphaNumeric = lngCount
                Exit For
            End If
        Next
    End Function

    Protected Friend Shared Function IsRegexPatternMatch(ByVal pattern As String, ByVal checkString As String) As Boolean

        Dim regexMatch As System.Text.RegularExpressions.Match = System.Text.RegularExpressions.Regex.Match(checkString, pattern)
        Return regexMatch.Success

    End Function

    Protected Friend Shared Function IsECCountryCode(ByVal countryCode As String) As Boolean

        For Each currentCountryCode As String In mCountries
            If countryCode = currentCountryCode Then
                Return True
            End If
        Next

        Return False

    End Function

    Protected Friend Shared Function IsNewVersionId(ByVal countryCode As String) As Boolean
        If countryCode.Length > 2 Then
            Return True
        End If
    End Function

#End Region

End Class
