using BSE.Modules.CaseWork.Commands;
using BSE.Modules.CaseWork.Models;
using BSE.Modules.CaseWork.Repositories;

namespace BSE.Modules.CaseWork.Services;

public sealed class CaseWorkService : ICaseWorkService
{
    private readonly ICaseWorkRepository _repository;

    public CaseWorkService(ICaseWorkRepository repository)
        => _repository = repository;

    public Task<CaseWorkRecord?> GetCaseWorkAsync(string rbse)
        => _repository.GetByRbseAsync(rbse);

    public Task<CaseWorkEntryRecord?> GetCaseWorkEntryAsync(string rbse)
        => _repository.GetEntryByRbseAsync(rbse);

    public Task<MinuteDetailsRecord?> GetMinuteDetailsAsync(string rbse, string minuteType)
        => _repository.GetMinuteDetailsAsync(rbse, minuteType);

    public Task SetMinuteSentDateAsync(string rbse, string minuteType)
        => _repository.SetMinuteSentDateAsync(rbse, minuteType);

    public Task EditCaseWorkEntryAsync(EditCaseWorkEntryCommand command)
        => _repository.EditEntryAsync(command);
}
