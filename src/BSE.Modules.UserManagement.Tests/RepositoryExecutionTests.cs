using System.Data;
using System.Reflection;
using BSE.Infrastructure;
using BSE.Modules.UserManagement.Tests.Db;
using FluentAssertions;
using NSubstitute;

namespace BSE.Modules.UserManagement.Tests;

/// <summary>Runs every public repository method against an in-memory connection so each stored-procedure call and parameter map is exercised.</summary>
public sealed class RepositoryExecutionTests
{
    public static TheoryData<string> RepositoryNames() =>
    [
        "AdnsRepository", "AnimalRelationsRepository", "AuditLogRepository", "BatchRepository", "BsessRepository",
        "BabRepository", "CaseRepository", "TestRepository", "OtherOwnerRepository", "PedigreeRepository",
        "ClinicalRepository", "FeedRepository", "CaseWorkRepository", "FarmRelationRepository", "FarmRepository",
        "HerdSizeRepository", "VetnetRepository", "OssExportRepository", "LookupRepository", "SearchRepository",
        "UserRepository"
    ];

    // Multi-grid reads need typed result sets, so RepositoryGridTests covers these.
    private static readonly HashSet<string> GridMethods =
        ["GetCaseDetailsByRbseAsync", "GetRelationsDetailsByRbseAsync", "GetCasesForGbAsync"];

    private static Type FindRepository(string name) =>
        typeof(BSE.Host.Services.ICurrentUserService).Assembly.GetReferencedAssemblies()
            .Where(a => a.Name!.StartsWith("BSE.Modules."))
            .Select(Assembly.Load)
            .SelectMany(a => a.GetTypes())
            .Single(t => t.Name == name && t.IsClass && !t.IsAbstract && typeof(DapperRepository).IsAssignableFrom(t));

    [Theory]
    [MemberData(nameof(RepositoryNames))]
    public async Task EveryPublicMethod_RunsAgainstTheFakeConnection(string repositoryName)
    {
        var repositoryType = FindRepository(repositoryName);
        var connection = new FakeDbConnection();
        var factory = Substitute.For<IDbConnectionFactory>();
        factory.CreateConnection().Returns(connection);
        var repository = Activator.CreateInstance(repositoryType, factory)!;
        var failures = new List<string>();

        foreach (var method in repositoryType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            if (method.IsSpecialName || method.ContainsGenericParameters || GridMethods.Contains(method.Name))
                continue;

            try
            {
                var arguments = method.GetParameters().Select(p => ArgumentFactory.Create(p.ParameterType, connection)).ToArray();
                if (method.Invoke(repository, arguments) is Task task)
                    await task;
            }
            catch (Exception ex)
            {
                var inner = ex is TargetInvocationException { InnerException: { } i } ? i : ex;
                failures.Add($"{method.Name}: {inner.GetType().Name}: {inner.Message}");
            }
        }

        failures.Should().BeEmpty();
        connection.Commands.Should().NotBeEmpty();
    }
}

internal static class ArgumentFactory
{
    public static object? Create(Type type, FakeDbConnection connection, int depth = 0)
    {
        if (type == typeof(string)) return "002600001";
        if (type == typeof(IDbConnection) || type == typeof(System.Data.Common.DbConnection)) return connection;
        if (type == typeof(IDbTransaction)) return connection.BeginTransaction();
        if (type == typeof(byte[])) return new byte[] { 1 };
        if (type == typeof(CancellationToken)) return CancellationToken.None;
        if (type == typeof(DateTime)) return DateTime.Today;
        if (type == typeof(int) || type == typeof(short) || type == typeof(long)) return Convert.ChangeType(1, type);
        if (Nullable.GetUnderlyingType(type) is not null) return null;
        if (type.IsValueType) return Activator.CreateInstance(type);
        if (type == typeof(Dapper.DynamicParameters)) return new Dapper.DynamicParameters();
        if (type.IsArray) return Array.CreateInstance(type.GetElementType()!, 0);

        if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();
            if (definition == typeof(IEnumerable<>) || definition == typeof(IReadOnlyList<>) || definition == typeof(IList<>)
                || definition == typeof(IReadOnlyCollection<>) || definition == typeof(ICollection<>))
                return Activator.CreateInstance(typeof(List<>).MakeGenericType(type.GetGenericArguments()[0]));
            if (definition == typeof(List<>))
                return Activator.CreateInstance(type);
        }

        if (type.IsInterface || type.IsAbstract || depth > 3)
            return null;

        var constructor = type.GetConstructors().OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();
        if (constructor is null)
            return null;

        return constructor.Invoke(constructor.GetParameters().Select(p => Create(p.ParameterType, connection, depth + 1)).ToArray());
    }
}
