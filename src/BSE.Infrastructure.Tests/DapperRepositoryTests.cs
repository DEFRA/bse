using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using BSE.Infrastructure;
using Dapper;
using FluentAssertions;
using NSubstitute;

namespace BSE.Infrastructure.Tests;

public sealed class DapperRepositoryTests
{
    [Fact]
    public async Task QueryAsync_WhenStoredProcedureReturnsRows_ReturnsMappedResultSet()
    {
        var connection = new FakeDbConnection(CreateSingleRowTable("Id", 7, "Name", "Alpha"));
        var factory = Substitute.For<IDbConnectionFactory>();
        factory.CreateConnection().Returns(connection);

        var sut = new DapperRepository(factory);

        var result = (await sut.QueryAsync<TestRow>("GetThing", new { Id = 7 })).ToList();

        result.Should().ContainSingle();
        result[0].Id.Should().Be(7);
        result[0].Name.Should().Be("Alpha");
        connection.CommandTexts.Should().ContainInOrder("SET ARITHABORT ON", "GetThing");
    }

    [Fact]
    public async Task QuerySingleOrDefaultAsync_WhenNoRowsExist_ReturnsNull()
    {
        var connection = new FakeDbConnection(new DataTable());
        var factory = Substitute.For<IDbConnectionFactory>();
        factory.CreateConnection().Returns(connection);
        var sut = new DapperRepository(factory);

        var result = await sut.QuerySingleOrDefaultAsync<TestRow>("GetThing", new { Id = 99 });

        result.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WhenStoredProcedureIsInvoked_ExecutesCommand()
    {
        var connection = new FakeDbConnection();
        var factory = Substitute.For<IDbConnectionFactory>();
        factory.CreateConnection().Returns(connection);
        var sut = new DapperRepository(factory);

        await sut.ExecuteAsync("DeleteThing", new { Id = 3 });

        connection.CommandTexts.Should().ContainInOrder("SET ARITHABORT ON", "DeleteThing");
    }

    [Fact]
    public async Task QueryAsync_WithExplicitTimeout_UsesConfiguredCommandTimeout()
    {
        var connection = new FakeDbConnection(CreateSingleRowTable("Id", 11, "Name", "Beta"));
        var factory = Substitute.For<IDbConnectionFactory>();
        factory.CreateConnection().Returns(connection);
        var sut = new DapperRepository(factory);

        var result = (await sut.QueryAsync<TestRow>("GetLongRunningThing", new { Id = 11 }, 15)).ToList();

        result.Should().ContainSingle().Which.Name.Should().Be("Beta");
        connection.LastCommand.Should().NotBeNull();
        connection.LastCommand!.CommandTimeout.Should().Be(15);
    }

    [Fact]
    public async Task ExecuteWithOutputAsync_WithDynamicParameters_ExecutesStoredProcedure()
    {
        var connection = new FakeDbConnection();
        var factory = Substitute.For<IDbConnectionFactory>();
        factory.CreateConnection().Returns(connection);
        var sut = new DapperRepository(factory);
        var parameters = new DynamicParameters();
        parameters.Add("@ReturnValue", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await sut.ExecuteWithOutputAsync("UpdateThing", parameters);

        connection.CommandTexts.Should().ContainInOrder("SET ARITHABORT ON", "UpdateThing");
        connection.LastCommand!.Parameters.Count.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteWithRowCountAsync_OnProvidedConnection_UsesCallerTransactionAndReturnsRowCount()
    {
        var connection = new FakeDbConnection();
        var factory = Substitute.For<IDbConnectionFactory>();
        var sut = new DapperRepository(factory);
        IDbTransaction? transaction = null;

        var rowCount = await sut.ExecuteWithRowCountAsync("DeleteThing", new { Id = 21 }, connection, transaction);

        rowCount.Should().Be(1);
        connection.CommandTexts.Should().ContainSingle().Which.Should().Be("DeleteThing");
    }

    private static DataTable CreateSingleRowTable(string firstColumnName, object firstValue, string secondColumnName, object secondValue)
    {
        var table = new DataTable();
        table.Columns.Add(firstColumnName, firstValue.GetType());
        table.Columns.Add(secondColumnName, secondValue.GetType());

        var row = table.NewRow();
        row[firstColumnName] = firstValue;
        row[secondColumnName] = secondValue;
        table.Rows.Add(row);
        return table;
    }

    private sealed class TestRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class FakeDbConnection : DbConnection
    {
        private readonly Queue<DataTable> _resultSets;
        private ConnectionState _state = ConnectionState.Closed;

        public FakeDbConnection(params DataTable[] resultSets)
        {
            _resultSets = new Queue<DataTable>(resultSets);
        }

        public List<string> CommandTexts { get; } = [];
        public FakeDbCommand? LastCommand { get; private set; }

        [AllowNull]
        public override string ConnectionString { get; set; } = "Server=localhost;Database=BSETest;Trusted_Connection=True;";
        public override string Database => "BSETest";
        public override string DataSource => "localhost";
        public override string ServerVersion => "1.0";
        public override ConnectionState State => _state;

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();

        public override void ChangeDatabase(string databaseName)
        {
        }

        public override void Close() => _state = ConnectionState.Closed;

        public override void Open() => _state = ConnectionState.Open;

        protected override DbCommand CreateDbCommand()
        {
            var command = new FakeDbCommand(this, _resultSets.Count > 0 ? _resultSets.Peek() : new DataTable());
            LastCommand = command;
            return command;
        }

        public DataTable TakeResultSet()
        {
            return _resultSets.Count > 0 ? _resultSets.Dequeue() : new DataTable();
        }
    }

    private sealed class FakeDbCommand : DbCommand
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

        public override void Cancel()
        {
        }

        public override int ExecuteNonQuery()
        {
            _connection.CommandTexts.Add(CommandText);
            return 1;
        }

        public override object? ExecuteScalar()
        {
            _connection.CommandTexts.Add(CommandText);
            return null;
        }

        public override void Prepare()
        {
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            _connection.CommandTexts.Add(CommandText);
            var resultSet = CommandText == "SET ARITHABORT ON" ? new DataTable() : _connection.TakeResultSet();
            return new DataTableReader(resultSet);
        }

        protected override DbParameter CreateDbParameter() => new FakeDbParameter();
    }

    private sealed class FakeDbParameterCollection : DbParameterCollection
    {
        private readonly List<DbParameter> _parameters = [];

        public override int Count => _parameters.Count;
        public override object SyncRoot => this;
        public override int Add(object value)
        {
            _parameters.Add((DbParameter)value);
            return _parameters.Count - 1;
        }

        public override void AddRange(Array values)
        {
            foreach (var item in values)
            {
                _parameters.Add((DbParameter)item);
            }
        }

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

        public override void RemoveAt(string parameterName)
        {
            var index = IndexOf(parameterName);
            if (index >= 0)
            {
                _parameters.RemoveAt(index);
            }
        }

        public override void RemoveAt(int index) => _parameters.RemoveAt(index);

        protected override void SetParameter(int index, DbParameter value) => _parameters[index] = value;

        protected override void SetParameter(string parameterName, DbParameter value)
        {
            var index = IndexOf(parameterName);
            if (index >= 0)
            {
                _parameters[index] = value;
            }
            else
            {
                Add(value);
            }
        }
    }

    private sealed class FakeDbParameter : DbParameter
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

        public override void ResetDbType()
        {
            DbType = DbType.String;
        }
    }
}
