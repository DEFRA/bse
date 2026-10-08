using System.Data;
using BSE.Modules.FarmManagement.Models;
using BSE.SharedKernel;

namespace BSE.Modules.FarmManagement.Repositories;

/// <summary>
/// Data access contract for core farm operations. One method per stored procedure.
/// SP names match filenames in src/BSE.Database/StoredProcedures/FarmManagement/ exactly,
/// except <c>GetNumberOfConfirmedCases</c> and <c>GetNumberOfCasesByCPHH</c> which live in
/// CaseManagement/ but are read here for farm-context display.
/// </summary>
public interface IFarmRepository
{
    Task<FarmRecord?> GetByCphhAsync(string cphh);
    Task<FarmDetailRecord> GetDetailsByCphhAsync(string cphh);
    Task<IEnumerable<FarmSummaryRecord>> GetByCphAsync(string cph);
    Task AddAsync(AddFarmCommand command, int userId);
    /// <summary>Transactional variant — enlisted in a caller-supplied connection/transaction so a
    /// brand-new farm can be committed atomically alongside new-case creation.</summary>
    Task AddAsync(AddFarmCommand command, int userId, IDbConnection connection, IDbTransaction transaction);
    Task UpdateAsync(UpdateFarmCommand command, int userId);
    /// <summary>Transactional variant — enlisted in a caller-supplied connection/transaction
    /// so the farm row can be committed atomically alongside the case row and other case-save work.
    /// Returns null on success, or a legacy-parity warning message if the EditFarm SP reports the row
    /// was modified by another user (RowStamp mismatch) — a soft, non-fatal result that does not roll
    /// back the transaction, matching legacy's <c>UpdateFarmDetails</c> behaviour exactly. A genuinely
    /// hard SP failure (deleted by another user, audit log error, table update error) throws instead.</summary>
    Task<string?> UpdateAsync(UpdateFarmCommand command, int userId, IDbConnection connection, IDbTransaction transaction);
    Task<ChangeCphhResult> ChangeCphhAsync(string oldCphh, string newCphh, int userId);
    Task<int> GetConfirmedCaseCountAsync(string cphh);
    Task<int> GetCaseCountByCphhAsync(string cphh);
}
