using System.Data;
using BSE.Infrastructure;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Models;

namespace BSE.Modules.CaseManagement.Repositories;

public interface ITestRepository
{
    Task<IReadOnlyList<CaseTestRecord>> GetByRbseAsync(string rbse);
    Task AddAsync(AddTestCommand command, IDbConnection connection, IDbTransaction transaction);
    Task EditAsync(EditTestCommand command, IDbConnection connection, IDbTransaction transaction);
    Task DeleteAsync(int id, IDbConnection connection, IDbTransaction transaction);
}

public sealed class TestRepository : DapperRepository, ITestRepository
{
    public TestRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<IReadOnlyList<CaseTestRecord>> GetByRbseAsync(string rbse)
        => (await QueryAsync<CaseTestRecord>("GetTestByRBSE", new { RBSE = rbse })).ToList();

    public Task AddAsync(AddTestCommand c, IDbConnection conn, IDbTransaction tx)
        => ExecuteAsync("AddTest", new { RBSE = c.Rbse, TestType = c.TestType, TestResult = c.TestResult }, conn, tx);

    public Task EditAsync(EditTestCommand c, IDbConnection conn, IDbTransaction tx)
        => ExecuteAsync("EditTest", new { ID = c.Id, RBSE = c.Rbse, TestType = c.TestType, TestResult = c.TestResult, RowStamp = c.RowStamp }, conn, tx);

    public Task DeleteAsync(int id, IDbConnection conn, IDbTransaction tx)
        => ExecuteAsync("DeleteTest", new { ID = id }, conn, tx);
}

public interface IOtherOwnerRepository
{
    Task<IReadOnlyList<OtherOwnerRecord>> GetByRbseAsync(string rbse);
    Task AddAsync(AddOtherOwnerCommand command, IDbConnection connection, IDbTransaction transaction);
    Task EditAsync(EditOtherOwnerCommand command, IDbConnection connection, IDbTransaction transaction);
    Task DeleteAsync(int id, IDbConnection connection, IDbTransaction transaction);
}

public sealed class OtherOwnerRepository : DapperRepository, IOtherOwnerRepository
{
    public OtherOwnerRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<IReadOnlyList<OtherOwnerRecord>> GetByRbseAsync(string rbse)
        => (await QueryAsync<OtherOwnerRecord>("GetOtherOwnerByRBSE", new { RBSE = rbse })).ToList();

    public Task AddAsync(AddOtherOwnerCommand c, IDbConnection conn, IDbTransaction tx)
        => ExecuteAsync("AddOtherOwner", new { RBSE = c.Rbse, Type = c.Type, Name = c.Name, CPHH = c.Cphh }, conn, tx);

    public Task EditAsync(EditOtherOwnerCommand c, IDbConnection conn, IDbTransaction tx)
        => ExecuteAsync("EditOtherOwner", new { ID = c.Id, RBSE = c.Rbse, Type = c.Type, Name = c.Name, CPHH = c.Cphh, RowStamp = c.RowStamp }, conn, tx);

    public Task DeleteAsync(int id, IDbConnection conn, IDbTransaction tx)
        => ExecuteAsync("DeleteOtherOwner", new { ID = id }, conn, tx);
}

public interface IPedigreeRepository
{
    Task<DamSireDetailRecord?> GetDamByRbseAsync(string rbse);
    Task<DamSireDetailRecord?> GetSireByRbseAsync(string rbse);
    Task AddEditDamSireAsync(AddEditDamSireCommand command, IDbConnection connection, IDbTransaction transaction);
}

public sealed class PedigreeRepository : DapperRepository, IPedigreeRepository
{
    public PedigreeRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

    public Task<DamSireDetailRecord?> GetDamByRbseAsync(string rbse)
        => QuerySingleOrDefaultAsync<DamSireDetailRecord>("GetDamDetailsByRBSE", new { RBSE = rbse });

    public Task<DamSireDetailRecord?> GetSireByRbseAsync(string rbse)
        => QuerySingleOrDefaultAsync<DamSireDetailRecord>("GetSireDetailsByRBSE", new { RBSE = rbse });

    public Task AddEditDamSireAsync(AddEditDamSireCommand c, IDbConnection conn, IDbTransaction tx)
        => ExecuteAsync("AddEditDamSireDetails", new
        {
            RBSE = c.Rbse,
            DamEartag = c.DamEartag, DamName = c.DamName, DamHerdbook = c.DamHerdbook,
            DamBirthDay = c.DamBirthDay, DamBirthMonth = c.DamBirthMonth, DamBirthYear = c.DamBirthYear,
            SireEartag = c.SireEartag, SireName = c.SireName, SireHerdbook = c.SireHerdbook,
            SireBirthDay = c.SireBirthDay, SireBirthMonth = c.SireBirthMonth, SireBirthYear = c.SireBirthYear
        }, conn, tx);
}
