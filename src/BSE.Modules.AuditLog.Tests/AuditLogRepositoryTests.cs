using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using BSE.Infrastructure;
using BSE.Modules.AuditLog.Models;
using BSE.Modules.AuditLog.Repositories;
using BSE.SharedKernel;
using Dapper;
using FluentAssertions;
using NSubstitute;

namespace BSE.Modules.AuditLog.Tests;

public sealed class AuditLogRepositoryTests
{
    [Fact]
    public async Task GetByCaseAsync_ConvertsUtcAuditTimestampsToUkTime()
    {
        var connection = new FakeDbConnection(CreateRows(
            new AuditLogEntry { Id = 1, TableName = "Case", UserName = "alice", DateTime = new DateTime(2026, 01, 15, 12, 00, 00, DateTimeKind.Utc) }));
        var factory = Substitute.For<IDbConnectionFactory>();
        factory.CreateConnection().Returns(connection);

        var sut = new AuditLogRepository(factory);

        var result = (await sut.GetByCaseAsync("AB12345AB")).ToList();

        result.Should().ContainSingle();
        result[0].DateTime.Should().Be(UkTimeZoneHelper.ToUkTime(new DateTime(2026, 01, 15, 12, 00, 00, DateTimeKind.Utc)));
        connection.CommandTexts.Should().ContainInOrder("SET ARITHABORT ON", "GetAuditLogByCase");
    }

    [Fact]
    public async Task GetByUserAsync_ConvertsUtcAuditTimestampsToUkTime()
    {
        var utc = new DateTime(2026, 02, 01, 18, 30, 00, DateTimeKind.Utc);
        var connection = new FakeDbConnection(CreateRows(
            new AuditLogEntry { Id = 2, TableName = "Farm", UserName = "bob", DateTime = utc }));
        var factory = Substitute.For<IDbConnectionFactory>();
        factory.CreateConnection().Returns(connection);

        var sut = new AuditLogRepository(factory);

        var result = (await sut.GetByUserAsync(new DateTime(2026, 01, 01), new DateTime(2026, 02, 28), 42)).ToList();

        result.Should().ContainSingle();
        result[0].DateTime.Should().Be(UkTimeZoneHelper.ToUkTime(utc));
        connection.CommandTexts.Should().ContainInOrder("SET ARITHABORT ON", "GetAuditLogByUser");
    }

    private static DataTable CreateRows<T>(params T[] rows)
    {
        var table = new DataTable();
        table.Columns.Add(nameof(AuditLogEntry.Id), typeof(int));
        table.Columns.Add(nameof(AuditLogEntry.TableName), typeof(string));
        table.Columns.Add(nameof(AuditLogEntry.FieldName), typeof(string));
        table.Columns.Add(nameof(AuditLogEntry.DateTime), typeof(DateTime));
        table.Columns.Add(nameof(AuditLogEntry.UserName), typeof(string));
        table.Columns.Add(nameof(AuditLogEntry.BeforeValue), typeof(string));
        table.Columns.Add(nameof(AuditLogEntry.AfterValue), typeof(string));
        table.Columns.Add(nameof(AuditLogEntry.Reason), typeof(string));
        table.Columns.Add(nameof(AuditLogEntry.Key), typeof(string));

        foreach (var row in rows)
        {
            var dataRow = table.NewRow();
            var properties = typeof(T).GetProperties();
            foreach (var property in properties)
            {
                if (property.Name == nameof(AuditLogEntry.Id) || property.Name == nameof(AuditLogEntry.TableName)
                    || property.Name == nameof(AuditLogEntry.FieldName) || property.Name == nameof(AuditLogEntry.DateTime)
                    || property.Name == nameof(AuditLogEntry.UserName) || property.Name == nameof(AuditLogEntry.BeforeValue)
                    || property.Name == nameof(AuditLogEntry.AfterValue) || property.Name == nameof(AuditLogEntry.Reason)
                    || property.Name == nameof(AuditLogEntry.Key))
                {
                    dataRow[property.Name] = property.GetValue(row) ?? DBNull.Value;
                }
            }
            table.Rows.Add(dataRow);
        }

        return table;
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

        public override void Cancel() { }
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

        public override void Prepare() { }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            _connection.CommandTexts.Add(CommandText);
            var resultSet = CommandText == "SET ARITHABORT ON" ? new DataTable() : _connection.TakeResultSet();
            return new DataTableReader(resultSet);
        }

        protected override DbParameter CreateDbParameter() => new FakeDbParameter();
    }

#pragma warning disable CS8765
    private sealed class FakeDbParameterCollection : DbParameterCollection
    {
        private readonly List<DbParameter> _parameters = [];

        public override int Count => _parameters.Count;
        public override object SyncRoot => this;
        public override int Add(object? value)
        {
            _parameters.Add((DbParameter)value!);
            return _parameters.Count - 1;
        }

        public override void AddRange(Array values) => throw new NotSupportedException();
        public override void Clear() => _parameters.Clear();
        public override bool Contains(string value) => _parameters.Any(p => p.ParameterName == value);
        public override bool Contains(object? value) => _parameters.Contains((DbParameter)value!);
        public override void CopyTo(Array array, int index) => _parameters.ToArray().CopyTo(array, index);
        public override IEnumerator GetEnumerator() => _parameters.GetEnumerator();
        public override int IndexOf(string parameterName) => _parameters.FindIndex(p => p.ParameterName == parameterName);
        public override int IndexOf(object? value) => _parameters.IndexOf((DbParameter)value!);
        public override void Insert(int index, object? value) => _parameters.Insert(index, (DbParameter)value!);
        public override void Remove(object? value) => _parameters.Remove((DbParameter)value!);
        public override void RemoveAt(string parameterName) => RemoveAt(IndexOf(parameterName));
        public override void RemoveAt(int index) => _parameters.RemoveAt(index);
        protected override DbParameter GetParameter(int index) => _parameters[index];
        protected override DbParameter GetParameter(string parameterName) => _parameters[IndexOf(parameterName)];
        protected override void SetParameter(int index, DbParameter value) => _parameters[index] = value;
        protected override void SetParameter(string parameterName, DbParameter value) => _parameters[IndexOf(parameterName)] = value;
    }

    private sealed class FakeDbParameter : DbParameter
    {
        public override DbType DbType { get; set; }
        public override ParameterDirection Direction { get; set; }
        public override bool IsNullable { get; set; }
        public override string ParameterName { get; set; } = string.Empty;
        public override string SourceColumn { get; set; } = string.Empty;
        public override object? Value { get; set; }
        public override bool SourceColumnNullMapping { get; set; }
        public override int Size { get; set; }
        public override void ResetDbType() { }
    }
#pragma warning restore CS8765
}
