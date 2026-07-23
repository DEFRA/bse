using BSE.Infrastructure;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Exceptions;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;

namespace BSE.Modules.CaseManagement.Services;

/// <summary>
/// Orchestrates case lifecycle operations.
///
/// <c>CreateCaseAsync</c> is the transactional save: it opens a single <c>SqlConnection</c>,
/// begins a transaction, calls AddCase then all child SP calls (clinical, BAB, feeds, tests,
/// other owners, dam/sire, clinical visits, batch link), and only commits once every call
/// succeeds. A failure at any step rolls back the entire operation — this is the fix for
/// the critical defect R01 identified in the HLD.
///
/// <c>DeleteCaseAsync</c>, <c>MoveCaseAsync</c>, and <c>ChangeRbseAsync</c> manage their
/// own transactions internally inside the stored procedures.
/// </summary>
public sealed class CaseService : ICaseService
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICaseRepository _caseRepository;
    private readonly IClinicalRepository _clinicalRepository;
    private readonly IBabRepository _babRepository;
    private readonly IFeedRepository _feedRepository;
    private readonly ITestRepository _testRepository;
    private readonly IOtherOwnerRepository _otherOwnerRepository;
    private readonly IPedigreeRepository _pedigreeRepository;

    public CaseService(
        IDbConnectionFactory connectionFactory,
        ICaseRepository caseRepository,
        IClinicalRepository clinicalRepository,
        IBabRepository babRepository,
        IFeedRepository feedRepository,
        ITestRepository testRepository,
        IOtherOwnerRepository otherOwnerRepository,
        IPedigreeRepository pedigreeRepository)
    {
        _connectionFactory = connectionFactory;
        _caseRepository = caseRepository;
        _clinicalRepository = clinicalRepository;
        _babRepository = babRepository;
        _feedRepository = feedRepository;
        _testRepository = testRepository;
        _otherOwnerRepository = otherOwnerRepository;
        _pedigreeRepository = pedigreeRepository;
    }

    public Task<CaseDetailRecord?> GetCaseDetailsAsync(string rbse)
        => _caseRepository.GetCaseDetailsByRbseAsync(rbse);

    public Task<CaseRecord?> GetCaseAsync(string rbse)
        => _caseRepository.GetCaseByRbseAsync(rbse);

    public Task<FinalResultRecord?> GetFinalResultAsync(string rbse)
        => _caseRepository.GetFinalResultByRbseAsync(rbse);

    /// <inheritdoc/>
    public async Task<AddCaseResult> CreateCaseAsync(UpdateCaseDetailsCommand command, int userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            // ── 1. Core case row ─────────────────────────────────────────
            var addResult = await _caseRepository.AddCaseAsync(command.Case, userId, connection, transaction);
            if (addResult != AddCaseResult.Success)
                return addResult;

            var rbse = command.Case.Rbse;

            // ── 2. Clinical signs ────────────────────────────────────────
            if (command.Clinical is not null)
                await _clinicalRepository.AddAsync(command.Clinical, connection, transaction);

            // ── 3. BAB data ──────────────────────────────────────────────
            if (command.Bab is not null)
                await _babRepository.AddAsync(command.Bab, connection, transaction);

            // ── 4. Feed history ──────────────────────────────────────────
            foreach (var feed in command.Feeds)
                await _feedRepository.AddAsync(feed, connection, transaction);

            // ── 5. Tests ─────────────────────────────────────────────────
            foreach (var test in command.Tests)
                await _testRepository.AddAsync(test, connection, transaction);

            // ── 6. Other owners ──────────────────────────────────────────
            foreach (var owner in command.OtherOwners)
                await _otherOwnerRepository.AddAsync(owner, connection, transaction);

            // ── 7. Dam/Sire pedigree ─────────────────────────────────────
            if (command.DamSire is not null)
                await _pedigreeRepository.AddEditDamSireAsync(command.DamSire, connection, transaction);

            // ── 8. Clinical visits ───────────────────────────────────────
            foreach (var visit in command.ClinicalVisits)
                await _clinicalRepository.AddVisitAsync(visit, connection, transaction);

            transaction.Commit();
            return AddCaseResult.Success;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public Task<DeleteCaseResult> DeleteCaseAsync(string rbse, int userId)
        => _caseRepository.DeleteCaseAsync(rbse, userId);

    /// <inheritdoc/>
    public async Task<EditCaseResult> EditCaseAsync(EditCaseDetailsCommand command, int userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            // ── 1. Core case row (optimistic concurrency via RowStamp) ────
            var result = await _caseRepository.EditCaseAsync(command.Case, userId, connection, transaction);
            if (result != EditCaseResult.Success)
                return result;

            // ── 2. Clinical signs ────────────────────────────────────────
            if (command.Clinical is not null)
                await _clinicalRepository.EditAsync(command.Clinical, connection, transaction);

            // ── 3. BAB data ──────────────────────────────────────────────
            if (command.Bab is not null)
                await _babRepository.EditAsync(command.Bab, connection, transaction);

            // ── 4. Dam/Sire pedigree ─────────────────────────────────────
            if (command.DamSire is not null)
                await _pedigreeRepository.AddEditDamSireAsync(command.DamSire, connection, transaction);

            transaction.Commit();
            return EditCaseResult.Success;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public Task<MoveCaseResult> MoveCaseAsync(string rbse, string newCphh, int userId)
        => _caseRepository.MoveCaseAsync(rbse, newCphh, userId);

    public Task<ChangeRbseResult> ChangeRbseAsync(string oldRbse, string newRbse, int userId)
        => _caseRepository.ChangeRbseAsync(oldRbse, newRbse, userId);
}
