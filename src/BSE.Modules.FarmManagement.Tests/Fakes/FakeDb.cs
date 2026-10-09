using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace BSE.Modules.FarmManagement.Tests.Fakes;

/// <summary>Minimal in-memory ADO.NET provider for exercising Dapper-backed repositories without a
/// real database. Queued <see cref="DataTable"/>s are returned in FIFO order to successive
/// QueryAsync/QuerySingleOrDefaultAsync calls; <see cref="ReturnValues"/> lets a test configure the
/// value an output/return-value parameter should receive for a given stored procedure name, mirroring
/// what the real SP would set.</summary>
internal sealed class FakeDbConnection : DbConnection
{
    private readonly Queue<DataTable> _resultSets;
    private ConnectionState _state = ConnectionState.Closed;

    public FakeDbConnection(params DataTable[] resultSets)
    {
        _resultSets = new Queue<DataTable>(resultSets);
    }

    public List<string> CommandTexts { get; } = [];
    public Dictionary<string, int> ReturnValues { get; } = [];
    public FakeDbCommand? LastCommand { get; private set; }

    [AllowNull]
    public override string ConnectionString { get; set; } = "Server=localhost;Database=BSETest;Trusted_Connection=True;";
    public override string Database => "BSETest";
    public override string DataSource => "localhost";
    public override string ServerVersion => "1.0";
    public override ConnectionState State => _state;

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
    public override void ChangeDatabase(string databaseName) { }
    public override void Close() => _state = ConnectionState.Closed;
    public override void Open() => _state = ConnectionState.Open;

    protected override DbCommand CreateDbCommand()
    {
        var command = new FakeDbCommand(this, _resultSets.Count > 0 ? _resultSets.Peek() : new DataTable());
        LastCommand = command;
        return command;
    }

    public DataTable TakeResultSet() => _resultSets.Count > 0 ? _resultSets.Dequeue() : new DataTable();
}

internal sealed class FakeDbCommand : DbCommand
{
    private readonly FakeDbConnection _connection;
    private readonly DataTable _resultSet;

    public FakeDbCommand(FakeDbConnection connection, DataTable resultSet)
    {
        _connection = connection;
        _resultSet = resultSet;
    }

    [AllowNull]
    public override string CommandText { get; set; } = string.Empty;
    public override int CommandTimeout { get; set; }
    public override CommandType CommandType { get; set; }
    public override bool DesignTimeVisible { get; set; }
    public override UpdateRowSource UpdatedRowSource { get; set; }
    protected override DbConnection? DbConnection { get; set; }
    protected override DbParameterCollection DbParameterCollection { get; } = new FakeDbParameterCollection();
    protected override DbTransaction? DbTransaction { get; set; }

    public override void Cancel() { }

    public override int ExecuteNonQuery()
    {
        _connection.CommandTexts.Add(CommandText);
        ApplyConfiguredReturnValue();
        return 1;
    }

    public override object? ExecuteScalar()
    {
        _connection.CommandTexts.Add(CommandText);
        ApplyConfiguredReturnValue();
        return null;
    }

    public override void Prepare() { }

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        _connection.CommandTexts.Add(CommandText);
        ApplyConfiguredReturnValue();
        var resultSet = CommandText == "SET ARITHABORT ON" ? new DataTable() : _connection.TakeResultSet();
        return new DataTableReader(resultSet);
    }

    protected override DbParameter CreateDbParameter() => new FakeDbParameter();

    private void ApplyConfiguredReturnValue()
    {
        if (!_connection.ReturnValues.TryGetValue(CommandText, out var value))
            return;

        foreach (FakeDbParameter parameter in Parameters)
        {
            if (parameter.Direction is ParameterDirection.ReturnValue or ParameterDirection.Output or ParameterDirection.InputOutput)
                parameter.Value = value;
        }
    }
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
    [AllowNull]
    public override string ParameterName { get; set; } = string.Empty;
    [AllowNull]
    public override string SourceColumn { get; set; } = string.Empty;
    public override object? Value { get; set; }
    public override bool SourceColumnNullMapping { get; set; }
    public override DataRowVersion SourceVersion { get; set; }
    public override int Size { get; set; }
    public override byte Precision { get; set; }
    public override byte Scale { get; set; }
    public override void ResetDbType() => DbType = DbType.String;
}
