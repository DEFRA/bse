using System.Collections;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace BSE.Modules.UserManagement.Tests.Db;

internal sealed record RecordedCommand(string Text, IReadOnlyDictionary<string, object?> Parameters);

/// <summary>In-memory ADO.NET connection so Dapper-based repositories can run without SQL Server.</summary>
internal sealed class FakeDbConnection : DbConnection
{
    private readonly Queue<DataTable[]> _results = new();
    private ConnectionState _state = ConnectionState.Closed;

    public List<RecordedCommand> Commands { get; } = [];

    /// <summary>Queues the result sets returned to the next stored-procedure call (one table per grid result).</summary>
    public void Enqueue(params DataTable[] tables) => _results.Enqueue(tables);

    public DataTable[] Dequeue() => _results.Count > 0 ? _results.Dequeue() : [];

    [System.Diagnostics.CodeAnalysis.AllowNull]
    public override string ConnectionString { get; set; } = "Server=fake;Database=BSETest;";
    public override string Database => "BSETest";
    public override string DataSource => "fake";
    public override string ServerVersion => "1.0";
    public override ConnectionState State => _state;

    public override void ChangeDatabase(string databaseName) { }
    public override void Close() => _state = ConnectionState.Closed;
    public override void Open() => _state = ConnectionState.Open;

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => new FakeDbTransaction(this, isolationLevel);
    protected override DbCommand CreateDbCommand() => new FakeDbCommand(this);

    /// <summary>Builds a table from the public readable properties of <typeparamref name="T"/>.</summary>
    public static DataTable Table<T>(params T[] rows)
    {
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .ToArray();
        var table = new DataTable();
        foreach (var property in properties)
        {
            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            table.Columns.Add(property.Name, type).AllowDBNull = true;
        }

        foreach (var row in rows)
            table.Rows.Add(properties.Select(p => p.GetValue(row) ?? DBNull.Value).ToArray());
        return table;
    }
}

internal sealed class FakeDbTransaction(FakeDbConnection connection, IsolationLevel isolationLevel) : DbTransaction
{
    public override IsolationLevel IsolationLevel { get; } = isolationLevel;
    protected override DbConnection DbConnection { get; } = connection;
    public override void Commit() { }
    public override void Rollback() { }
}

internal sealed class FakeDbCommand(FakeDbConnection connection) : DbCommand
{
    private readonly FakeDbParameterCollection _parameters = new();

    [System.Diagnostics.CodeAnalysis.AllowNull] public override string CommandText { get; set; } = string.Empty;
    public override int CommandTimeout { get; set; }
    public override CommandType CommandType { get; set; }
    public override bool DesignTimeVisible { get; set; }
    public override UpdateRowSource UpdatedRowSource { get; set; }
    protected override DbConnection? DbConnection { get; set; } = connection;
    protected override DbParameterCollection DbParameterCollection => _parameters;
    protected override DbTransaction? DbTransaction { get; set; }

    public override void Cancel() { }
    public override void Prepare() { }

    public override int ExecuteNonQuery()
    {
        Record();
        return 1;
    }

    public override object? ExecuteScalar()
    {
        Record();
        return null;
    }

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        Record();
        if (CommandText == "SET ARITHABORT ON")
            return new DataTableReader(new DataTable());

        var tables = connection.Dequeue();
        return new DataTableReader(tables.Length == 0 ? EmptyGrids() : tables);
    }

    // A zero-column grid is valid for single-result queries; typed multi-grid reads queue real tables instead.
    private static DataTable[] EmptyGrids() => Enumerable.Range(0, 12).Select(_ => new DataTable()).ToArray();

    protected override DbParameter CreateDbParameter() => new FakeDbParameter();

    private void Record()
    {
        var inputs = _parameters.Cast<DbParameter>()
            .ToDictionary(p => p.ParameterName, p => p.Value, StringComparer.OrdinalIgnoreCase);
        connection.Commands.Add(new RecordedCommand(CommandText, inputs));

        foreach (DbParameter parameter in _parameters)
        {
            if (parameter.Direction == ParameterDirection.Input || parameter.Value is not null and not DBNull)
                continue;

            parameter.Value = parameter.Direction == ParameterDirection.ReturnValue ? 0 : DefaultOutput(parameter.DbType);
        }
    }

    private static object DefaultOutput(DbType type) => type switch
    {
        DbType.String or DbType.AnsiString or DbType.AnsiStringFixedLength or DbType.StringFixedLength => string.Empty,
        DbType.Int32 => 0,
        DbType.Int16 => (short)0,
        DbType.Int64 => 0L,
        DbType.Byte => (byte)0,
        DbType.Boolean => false,
        DbType.DateTime => DateTime.Now,
        _ => DBNull.Value
    };
}

internal sealed class FakeDbParameterCollection : DbParameterCollection
{
    private readonly List<DbParameter> _parameters = [];

    public override int Count => _parameters.Count;
    public override object SyncRoot => this;
    public override int Add(object value) { _parameters.Add((DbParameter)value); return _parameters.Count - 1; }
    public override void AddRange(Array values) { foreach (var item in values) _parameters.Add((DbParameter)item); }
    public override void Clear() => _parameters.Clear();
    public override bool Contains(string value) => _parameters.Any(p => p.ParameterName == value);
    public override bool Contains(object value) => _parameters.Contains((DbParameter)value);
    public override void CopyTo(Array array, int index) => _parameters.ToArray().CopyTo(array, index);
    public override IEnumerator GetEnumerator() => _parameters.GetEnumerator();
    protected override DbParameter GetParameter(int index) => _parameters[index];
    protected override DbParameter GetParameter(string parameterName) => _parameters.First(p => p.ParameterName == parameterName);
    public override int IndexOf(string parameterName) => _parameters.FindIndex(p => p.ParameterName == parameterName);
    public override int IndexOf(object value) => _parameters.IndexOf((DbParameter)value);
    public override void Insert(int index, object value) => _parameters.Insert(index, (DbParameter)value);
    public override void Remove(object value) => _parameters.Remove((DbParameter)value);
    public override void RemoveAt(string parameterName) { var i = IndexOf(parameterName); if (i >= 0) _parameters.RemoveAt(i); }
    public override void RemoveAt(int index) => _parameters.RemoveAt(index);
    protected override void SetParameter(int index, DbParameter value) => _parameters[index] = value;

    protected override void SetParameter(string parameterName, DbParameter value)
    {
        var i = IndexOf(parameterName);
        if (i >= 0) _parameters[i] = value; else Add(value);
    }
}

internal sealed class FakeDbParameter : DbParameter
{
    public override DbType DbType { get; set; }
    public override ParameterDirection Direction { get; set; }
    public override bool IsNullable { get; set; }
    [System.Diagnostics.CodeAnalysis.AllowNull] public override string ParameterName { get; set; } = string.Empty;
    [System.Diagnostics.CodeAnalysis.AllowNull] public override string SourceColumn { get; set; } = string.Empty;
    public override object? Value { get; set; }
    public override bool SourceColumnNullMapping { get; set; }
    public override DataRowVersion SourceVersion { get; set; }
    public override int Size { get; set; }
    public override byte Precision { get; set; }
    public override byte Scale { get; set; }
    public override void ResetDbType() => DbType = DbType.String;
}
