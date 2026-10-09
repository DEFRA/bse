using System.Data;
using BSE.Infrastructure;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Models;
using Dapper;

namespace BSE.Modules.CaseManagement.Repositories;

public interface ITestRepository
{
    Task<IReadOnlyList<CaseTestRecord>> GetByRbseAsync(string rbse);
    Task AddAsync(AddTestCommand command, IDbConnection connection, IDbTransaction transaction);
    // Standalone variants for direct Razor Page CRUD handlers (no external transaction)
    Task AddAsync(AddTestCommand command);
    Task EditAsync(EditTestCommand command, IDbConnection connection, IDbTransaction transaction);
    Task EditAsync(EditTestCommand command);
    Task DeleteAsync(int id, IDbConnection connection, IDbTransaction transaction);
    Task DeleteAsync(int id, byte[] rowStamp);
}

public sealed class TestRepository : DapperRepository, ITestRepository
{
    public TestRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<IReadOnlyList<CaseTestRecord>> GetByRbseAsync(string rbse)
        => (await QueryAsync<CaseTestRecord>("GetTestByRBSE", new { RBSE = rbse })).ToList();

    public Task AddAsync(AddTestCommand command, IDbConnection connection, IDbTransaction transaction)
        => ExecuteAsync("AddTest", new { RBSE = command.Rbse, TestType = command.TestType, TestResult = command.TestResult }, connection, transaction);

    public Task AddAsync(AddTestCommand command)
        => ExecuteAsync("AddTest", new { RBSE = command.Rbse, TestType = command.TestType, TestResult = command.TestResult });

    public Task EditAsync(EditTestCommand command, IDbConnection connection, IDbTransaction transaction)
        => ExecuteAsync("EditTest", new { ID = command.Id, TestType = command.TestType, TestResult = command.TestResult, RowStamp = command.RowStamp }, connection, transaction);

    public Task EditAsync(EditTestCommand command)
        => ExecuteAsync("EditTest", new { ID = command.Id, TestType = command.TestType, TestResult = command.TestResult, RowStamp = command.RowStamp });

    public Task DeleteAsync(int id, IDbConnection connection, IDbTransaction transaction)
        => ExecuteAsync("DeleteTest", new { ID = id }, connection, transaction);

    public Task DeleteAsync(int id, byte[] rowStamp)
        => ExecuteAsync("DeleteTest", new { ID = id, RowStamp = rowStamp });
}

public interface IOtherOwnerRepository
{
    Task<IReadOnlyList<OtherOwnerRecord>> GetByRbseAsync(string rbse);
    Task AddAsync(AddOtherOwnerCommand command, IDbConnection connection, IDbTransaction transaction);
    Task EditAsync(EditOtherOwnerCommand command, IDbConnection connection, IDbTransaction transaction);
    Task DeleteAsync(int id, byte[] rowStamp, IDbConnection connection, IDbTransaction transaction);
}

public sealed class OtherOwnerRepository : DapperRepository, IOtherOwnerRepository
{
    public OtherOwnerRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<IReadOnlyList<OtherOwnerRecord>> GetByRbseAsync(string rbse)
        => (await QueryAsync<OtherOwnerRecord>("GetOtherOwnerByRBSE", new { RBSE = rbse })).ToList();

    public Task AddAsync(AddOtherOwnerCommand command, IDbConnection connection, IDbTransaction transaction)
        => ExecuteAsync("AddOtherOwner", new { RBSE = command.Rbse, Type = command.Type, Name = command.Name, CPHH = command.Cphh }, connection, transaction);

    public Task EditAsync(EditOtherOwnerCommand command, IDbConnection connection, IDbTransaction transaction)
        => ExecuteAsync("EditOtherOwner", new { ID = command.Id, Type = command.Type, Name = command.Name, CPHH = command.Cphh, RowStamp = command.RowStamp }, connection, transaction);

    public Task DeleteAsync(int id, byte[] rowStamp, IDbConnection connection, IDbTransaction transaction)
        => ExecuteAsync("DeleteOtherOwner", new { ID = id, RowStamp = rowStamp }, connection, transaction);
}

public interface IPedigreeRepository
{
    Task<DamSireDetailRecord?> GetDamByRbseAsync(string rbse);
    Task<DamSireDetailRecord?> GetSireByRbseAsync(string rbse);
    /// <summary>Returns null on success, or a legacy-parity warning message if the
    /// AddEditDamSireDetails SP reports the dam, sire, or case pedigree row couldn't be updated
    /// (RowStamp mismatch) — soft/non-fatal, matching legacy's <c>UpdateDamSireRecords</c> exactly.</summary>
    Task<string?> AddEditDamSireAsync(AddEditDamSireCommand command, IDbConnection connection, IDbTransaction transaction);
}

public sealed class PedigreeRepository : DapperRepository, IPedigreeRepository
{
    public PedigreeRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

    public Task<DamSireDetailRecord?> GetDamByRbseAsync(string rbse)
        => QuerySingleOrDefaultAsync<DamSireDetailRecord>("GetDamDetailsByRBSE", new { RBSE = rbse });

    public Task<DamSireDetailRecord?> GetSireByRbseAsync(string rbse)
        => QuerySingleOrDefaultAsync<DamSireDetailRecord>("GetSireDetailsByRBSE", new { RBSE = rbse });

    public async Task<string?> AddEditDamSireAsync(AddEditDamSireCommand command, IDbConnection connection, IDbTransaction transaction)
    {
        var p = new DynamicParameters(new
        {
            RBSE = command.Rbse,
            DamID = command.DamId, DamRBSE = command.DamRbse,
            DamEartag = command.DamEartag, DamName = command.DamName, DamHerdbook = command.DamHerdbook,
            DamBirthDay = command.DamBirthDay, DamBirthMonth = command.DamBirthMonth, DamBirthYear = command.DamBirthYear,
            DamRowStamp = command.DamRowStamp,
            SireID = command.SireId, SireRBSE = command.SireRbse,
            SireEartag = command.SireEartag, SireName = command.SireName, SireHerdbook = command.SireHerdbook,
            SireBirthDay = command.SireBirthDay, SireBirthMonth = command.SireBirthMonth, SireBirthYear = command.SireBirthYear,
            SireRowStamp = command.SireRowStamp,
            CaseHerdbook = command.CaseHerdbook, CaseRowStamp = command.CaseRowStamp
        });
        p.Add("ReturnValue", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

        await ExecuteAsync("AddEditDamSireDetails", p, connection, transaction);

        // Legacy parity: clsCase.UpdateDamSireRecords treats every one of these codes as
        // soft/non-fatal (objErrorList.Add), never rolling back the rest of the save.
        return p.Get<int>("ReturnValue") switch
        {
            0 => null,
            1 => "Failed to create or update a dam record.  The record may have been changed by another user",
            2 => "Failed to create or update a sire record.  The record may have been changed by another user",
            3 => "Failed to create a pedigree record for the case.",
            4 => "Failed to update the case's pedigree record with pointers to the dam and sire information.  The record may have been changed by another user.",
            var code => $"AddEditDamSireDetails returned unexpected code {code}."
        };
    }
}
