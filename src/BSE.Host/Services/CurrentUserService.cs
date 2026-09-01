using BSE.Modules.UserManagement.Repositories;
using BSE.SharedKernel;

namespace BSE.Host.Services;

/// <summary>
/// Resolves the database integer UserId for the currently authenticated user.
/// Used by Razor Page models that need to pass a UserId to domain service write operations.
/// Resolution uses UPN (Azure AD) with NTLogin fallback, matching the legacy ASP.NET WebForms pattern.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IUserContext _context;
    private readonly IUserRepository _repository;

    public CurrentUserService(IUserContext context, IUserRepository repository)
    {
        _context = context;
        _repository = repository;
    }

    public async Task<int> GetUserIdAsync()
    {
        var upn = _context.Upn;
        var ntLogin = upn.IndexOf('@') > 0 ? upn[..upn.IndexOf('@')] : upn;

        var user = await _repository.GetByEmailAsync(upn)
                ?? await _repository.GetByNtLoginAsync(ntLogin);

        if (user is null)
            throw new InvalidOperationException(
                $"Authenticated user '{upn}' does not have a record in the BSE database. " +
                "Ensure the user account has been added via User Maintenance.");

        return user.UserId;
    }
}
