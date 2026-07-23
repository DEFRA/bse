namespace BSE.Host.Services;

public interface ICurrentUserService
{
    /// <summary>
    /// Resolves the database integer UserId for the current authenticated user.
    /// Throws <see cref="InvalidOperationException"/> if the user is not found in the database.
    /// </summary>
    Task<int> GetUserIdAsync();
}
