Imports System
Imports System.Data
Imports System.Data.SqlClient

Namespace libDataAccess

#Region "Enums"

    Public Enum DbtType
        dbtBoolean
        dbtBinary
        dbtTinyInt
        dbtSmallInt
        dbtInteger
        dbtBigInt
        dbtDate
        dbtTime
        dbtDateTime
        dbtSingle
        dbtDouble
        dbtDecimal
        dbtCurrency
        dbtString
        dbtAnsiString
        dbtSByte
        dbtGUID
        dbtText
    End Enum

#End Region

    '=======================================================================================
    ' Class     : Parameter
    ' Desc      : Holds information for a parameter that will be bound to a data command
    '             object at some point.
    '---------------------------------------------------------------------------------------
    ' Attributes: 
    ' [Private] m_strKey         [String]             - Key to parameter to set/get properties
    ' [Private] m_spType         [StoredProcType]     - Select/Insert/Update/Delete
    ' [Private] m_dbType         [DbtType]            - Integer/DateTime/Double etc
    ' [Private] m_strName        [String]             - Mainly used for SQL Stored Procs
    ' [Private] m_strColumn      [String]             - Column mapping to database
    ' [Private] m_intSize        [Integer]            - Size of type
    ' [Private] m_pdDirection    [ParameterDirection] - Input/Output/InputOutput/Return
    ' [Private] m_drvSrcVer      [DataRowVersion]     - Default/Original etc
    ' [Private] m_objValue       [Object]             - Object that is the parameter value
    ' [Private] m_dbSpecificType [Object]             - Internally sets type for SQL or OleDB
    '
    '---------------------------------------------------------------------------------------
    ' Methods   : [Public] New (Constructor)  - Initialises parameter with property values
    '             [Public] Get/Set property   - Get/Set properties for the class attributes
    '             [Public] ConvertToSQLDBType - Converts parameter type specifically for SQL
    '             [Public] ConvertToOleDBType - Converts parameter type specifically for OleDB
    '---------------------------------------------------------------------------------------
    ' Author    : PSH
    ' Created   : 27 June 2003
    '=======================================================================================
    ' Change Log
    '
    ' Date      Author  Comments
    ' ----      ------  --------
    '=======================================================================================
    Public Class Parameter
        Private m_strKey As String
        Private m_dbType As DbtType
        Private m_strName As String
        Private m_intSize As Integer
        Private m_strColumn As String
        Private m_drvSrcVer As DataRowVersion
        Private m_pdDirection As ParameterDirection
        Private m_bytScale As Byte = 0
        Private m_bytPrecision As Byte = 0
        Private m_blnNullable As Boolean = False
        Private m_objValue As Object = Nothing
        Private m_dbSpecificType As System.Data.SqlDbType = Nothing

        ' hash table for reverse type lookups
        Private Shared htSysTypes As Hashtable

        ' Initialise Parameter's attribute data
        Public Sub New(ByVal strKey As String, _
                       ByVal dbType As DbtType, _
                       ByVal strName As String, _
                       Optional ByVal strColumn As String = "", _
                       Optional ByVal intSize As Integer = 0, _
                       Optional ByVal objValue As Object = Nothing, _
                       Optional ByVal pdDirection As ParameterDirection = ParameterDirection.Input, _
                       Optional ByVal drvSrcVer As DataRowVersion = DataRowVersion.Default)

            m_strKey = strKey
            m_dbType = dbType
            m_strName = strName
            m_intSize = intSize
            m_strColumn = strColumn
            If (objValue Is Nothing) Then
                ' Check if object value is actually nothing or just holding a 'nothing'
                ' value e.g. boolean{false} = nothing and would get set to DBNull here
                ' if we did not try GetType()
                Try
                    objValue.GetType()
                    m_objValue = objValue
                Catch
                    m_objValue = DBNull.Value
                End Try
            Else
                m_objValue = objValue
            End If
            m_pdDirection = pdDirection
            m_drvSrcVer = drvSrcVer

        End Sub

        Public Function GetKey() As String
            Return m_strKey
        End Function

        Public Function GetDirection() As System.Data.ParameterDirection
            Return m_pdDirection
        End Function

        ' Allow Get/Set for value property
        Public Property Value() As Object
            Get
                If (IsDBNull(m_objValue)) Then
                    Return Nothing
                Else
                    Return m_objValue
                End If
            End Get
            Set(ByVal objValue As Object)
                If objValue Is Nothing Then
                    m_objValue = DBNull.Value
                Else
                    m_objValue = objValue
                End If
            End Set
        End Property

        Public Property Type() As DbtType
            Get
                Return m_dbType
            End Get
            Set(ByVal Value As DbtType)
                m_dbType = Value
            End Set
        End Property

        Public Property Name() As String
            Get
                Return m_strName
            End Get
            Set(ByVal Value As String)
                m_strName = Value
            End Set
        End Property

        Public Property Column() As String
            Get
                Return m_strColumn
            End Get
            Set(ByVal Value As String)
                m_strColumn = Value
            End Set
        End Property

        Public Property Size() As Integer
            Get
                Return m_intSize
            End Get
            Set(ByVal Value As Integer)
                m_intSize = Value
            End Set
        End Property

        Public Property Nullable() As Boolean
            Get
                Return m_blnNullable
            End Get
            Set(ByVal Value As Boolean)
                m_blnNullable = Value
            End Set
        End Property

        Public Property Scale() As Byte
            Get
                Return m_bytScale
            End Get
            Set(ByVal Value As Byte)
                m_bytScale = Value
            End Set
        End Property

        Public Property Precision() As Byte
            Get
                Return m_bytPrecision
            End Get
            Set(ByVal Value As Byte)
                m_bytPrecision = Value
            End Set
        End Property

        Public Property Direction() As ParameterDirection
            Get
                Return m_pdDirection
            End Get
            Set(ByVal Value As ParameterDirection)
                m_pdDirection = Value
            End Set
        End Property

        Public Property RowVersion() As DataRowVersion
            Get
                Return m_drvSrcVer
            End Get
            Set(ByVal Value As DataRowVersion)
                m_drvSrcVer = Value
            End Set
        End Property

        ' convert System.type to Parameter type
        Public Shared Function ToDbtType(ByVal SysType As System.Type) As DbtType

            If htSysTypes Is Nothing Then
                ' populate the hash table
                SyncLock GetType(libDataAccess.Parameter)
                    htSysTypes = New Hashtable(20)
                    htSysTypes.Add(GetType(Boolean), DbtType.dbtBoolean)
                    htSysTypes.Add(GetType(Byte()), DbtType.dbtBinary)
                    'htSysTypes.Add(GetType(Byte), DbtType.dbtTinyInt)
                    htSysTypes.Add(GetType(Short), DbtType.dbtSmallInt)
                    htSysTypes.Add(GetType(Integer), DbtType.dbtInteger)
                    htSysTypes.Add(GetType(Long), DbtType.dbtBigInt)
                    'htSysTypes.Add(GetType(Date), DbtType.dbtDate)
                    'htSysTypes.Add(GetType(Date), DbtType.dbtTime)
                    htSysTypes.Add(GetType(Date), DbtType.dbtDateTime)
                    htSysTypes.Add(GetType(Single), DbtType.dbtSingle)
                    htSysTypes.Add(GetType(Double), DbtType.dbtDouble)
                    htSysTypes.Add(GetType(Decimal), DbtType.dbtDecimal)
                    'htSysTypes.Add(GetType(Decimal), DbtType.dbtCurrency)
                    htSysTypes.Add(GetType(String), DbtType.dbtString)
                    'htSysTypes.Add(GetType(String), DbtType.dbtAnsiString)
                    htSysTypes.Add(GetType(Byte), DbtType.dbtSByte)
                    htSysTypes.Add(GetType(System.Guid), DbtType.dbtGUID)
                    'htSysTypes.Add(GetType(String), DbtType.dbtText)
                End SyncLock
            End If

            If htSysTypes.Item(SysType) Is Nothing Then
                Throw New Exception("No DbtType found for system type """ _
                                    & SysType.Name & """")
            End If

            Return CType(htSysTypes.Item(SysType), DbtType)

        End Function

        ' Convert parameter type to specific SQL type
        Private Function ConvertToSQLDBType() As SqlDbType

            m_dbSpecificType = New SqlDbType()

            Select Case (m_dbType)
                Case DbtType.dbtBoolean
                    m_dbSpecificType = SqlDbType.Bit
                Case DbtType.dbtBinary
                    m_dbSpecificType = SqlDbType.VarBinary
                Case DbtType.dbtTinyInt
                    m_dbSpecificType = SqlDbType.TinyInt
                Case DbtType.dbtSmallInt
                    m_dbSpecificType = SqlDbType.SmallInt
                Case DbtType.dbtInteger
                    m_dbSpecificType = SqlDbType.Int
                Case DbtType.dbtBigInt
                    m_dbSpecificType = SqlDbType.BigInt
                Case DbtType.dbtDate
                    m_dbSpecificType = SqlDbType.DateTime
                Case DbtType.dbtTime
                    m_dbSpecificType = SqlDbType.DateTime
                Case DbtType.dbtDateTime
                    m_dbSpecificType = SqlDbType.DateTime
                Case DbtType.dbtSingle
                    m_dbSpecificType = SqlDbType.Real
                Case DbtType.dbtDouble
                    m_dbSpecificType = SqlDbType.Float
                Case DbtType.dbtDecimal
                    m_dbSpecificType = SqlDbType.Decimal
                Case DbtType.dbtCurrency
                    m_dbSpecificType = SqlDbType.Money
                Case DbtType.dbtString
                    m_dbSpecificType = SqlDbType.NVarChar
                Case DbtType.dbtAnsiString
                    m_dbSpecificType = SqlDbType.VarChar
                Case DbtType.dbtSByte
                    m_dbSpecificType = SqlDbType.TinyInt
                Case DbtType.dbtGUID
                    m_dbSpecificType = SqlDbType.UniqueIdentifier
                Case DbtType.dbtText
                    m_dbSpecificType = SqlDbType.Text
                    ' Override size to size of 'Text' datatype in SQL Server
                    m_intSize = 2147483647
            End Select

            Return m_dbSpecificType

        End Function

        '=======================================================================================
        ' Sub       : GetFromReader
        ' Desc      : Adds either a SQLParameter or OleDBParameter to the command object
        '             depending on the value of m_blnUseSQL.
        '---------------------------------------------------------------------------------------
        ' Arguments : intParam     [IN] [Integer]      - Parameter number to retrive
        '             objReader    [IN] [IDataReader]  - reader interface to read data from
        '---------------------------------------------------------------------------------------
        ' Author    : PSH
        ' Created   : 23 June 2003
        '=======================================================================================
        ' Change Log
        '
        ' Date      Author  Comments
        ' ----      ------  --------
        '=======================================================================================
        Public Sub GetFromReader(ByVal intParam As Integer, _
                                 ByRef objReader As SqlDataReader)

            If Not (objReader.IsDBNull(intParam)) Then
                Select Case (m_dbType)
                    Case DbtType.dbtBoolean
                        m_objValue = objReader.GetBoolean(intParam)
                    Case DbtType.dbtBinary
                        m_objValue = GetBinaryData(intParam, objReader)
                    Case DbtType.dbtTinyInt
                        m_objValue = objReader.GetValue(intParam)
                    Case DbtType.dbtSmallInt
                        m_objValue = objReader.GetInt16(intParam)
                    Case DbtType.dbtInteger
                        m_objValue = objReader.GetInt32(intParam)
                    Case DbtType.dbtBigInt
                        m_objValue = objReader.GetInt64(intParam)
                    Case DbtType.dbtDate
                        m_objValue = objReader.GetDateTime(intParam)
                    Case DbtType.dbtTime
                        m_objValue = objReader.GetDateTime(intParam)
                    Case DbtType.dbtDateTime
                        m_objValue = objReader.GetDateTime(intParam)
                    Case DbtType.dbtSingle
                        m_objValue = objReader.GetFloat(intParam)
                    Case DbtType.dbtDouble
                        m_objValue = objReader.GetDouble(intParam)
                    Case DbtType.dbtDecimal
                        m_objValue = objReader.GetDecimal(intParam)
                    Case DbtType.dbtCurrency
                        m_objValue = objReader.GetDecimal(intParam)
                    Case DbtType.dbtString
                        m_objValue = objReader.GetString(intParam)
                    Case DbtType.dbtAnsiString
                        m_objValue = objReader.GetString(intParam)
                    Case DbtType.dbtSByte
                        m_objValue = objReader.GetByte(intParam)
                    Case DbtType.dbtGUID
                        m_objValue = objReader.GetGuid(intParam)
                    Case DbtType.dbtText
                        m_objValue = objReader.GetString(intParam)
                End Select
            End If

        End Sub

        '=======================================================================================
        ' Function  : GetBinaryData
        ' Desc      : Special Get operation for Binary data. This data type does not have a
        '             known size so we must keep calling GetBytes() while the returned bytes
        '             are as big as the buffer.
        '---------------------------------------------------------------------------------------
        ' Arguments : intParam     [IN] [Integer]      - Parameter number to retrive
        '             objReader    [IN] [IDataReader]  - reader interface to read data from
        '
        ' Returns   : [Byte()] - Byte array containing the Binary Data
        '---------------------------------------------------------------------------------------
        ' Author    : PSH
        ' Created   : 23 June 2003
        '=======================================================================================
        ' Change Log
        '
        ' Date      Author  Comments
        ' ----      ------  --------
        '=======================================================================================
        Private Function GetBinaryData(ByVal intParam As Integer, _
                                       ByRef objReader As SqlDataReader) As Byte()

            Dim intBuffSize As Integer = 100
            Dim intRetBytes As Long = 100
            Dim intStartIndex As Long = 0
            Dim bytBuffer(intBuffSize - 1) As Byte

            While (intRetBytes = intBuffSize)

                intRetBytes = objReader.GetBytes(intParam, intStartIndex, bytBuffer, CInt(intStartIndex), intBuffSize)
                ' make array size bigger if more data to come
                If (intRetBytes = intBuffSize) Then
                    intBuffSize = intBuffSize + 100
                    ReDim bytBuffer(intBuffSize)
                End If

            End While

            Return bytBuffer

        End Function

        Public Sub AddToCommand(ByRef objCommand As SqlCommand)

            ConvertToSQLDBType()
            objCommand.Parameters.Add(New SqlParameter(m_strName, m_dbSpecificType, m_intSize, m_pdDirection, m_blnNullable, m_bytPrecision, m_bytScale, m_strColumn, m_drvSrcVer, m_objValue))

        End Sub

        Public Sub GetFromCommand(ByRef objCommand As SqlCommand)

            m_objValue = objCommand.Parameters(m_strName).Value

        End Sub

    End Class

End Namespace
