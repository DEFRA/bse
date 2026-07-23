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
    Task UpdateAsync(UpdateFarmCommand command, int userId);
    Task<ChangeCphhResult> ChangeCphhAsync(string oldCphh, string newCphh, int userId);
    Task<int> GetConfirmedCaseCountAsync(string cphh);
    Task<int> GetCaseCountByCphhAsync(string cphh);
}
