using BSE.Infrastructure;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.CaseWork.Repositories;

namespace BSE.Modules.CaseManagement.Services;

public sealed record CaseServiceDependencies(
    IDbConnectionFactory ConnectionFactory,
    ICaseRepository CaseRepository,
    IClinicalRepository ClinicalRepository,
    IBabRepository BabRepository,
    IFeedRepository FeedRepository,
    ITestRepository TestRepository,
    IOtherOwnerRepository OtherOwnerRepository,
    IPedigreeRepository PedigreeRepository,
    IBatchRepository BatchRepository,
    ICaseWorkRepository CaseWorkRepository);
