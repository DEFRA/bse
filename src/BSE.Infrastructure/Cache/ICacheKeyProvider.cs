namespace BSE.Infrastructure.Cache;

public interface ICacheKeyProvider
{
    /// <summary>Returns a user-scoped cache key for case wizard state.</summary>
    string CaseWizard(string userId);

    /// <summary>Returns a user-scoped cache key for farm wizard state.</summary>
    string FarmWizard(string userId);
}
