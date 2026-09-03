using BSE.Modules.UserManagement.Models;

namespace BSE.Modules.UserManagement.Repositories;

public interface IUserRepository
{
    /// <summary>Looks up a user by Email (primary Azure AD identity).</summary>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>Looks up a user by NT login (legacy fallback identity).</summary>
    Task<User?> GetByNtLoginAsync(string ntLogin);

    /// <summary>Returns all users ordered by display name.</summary>
    Task<IEnumerable<User>> GetAllAsync();

    /// <summary>Inserts a new user record and returns the generated UserId.</summary>
    Task<int> AddAsync(User user);

    /// <summary>Updates an existing user record.</summary>
    Task UpdateAsync(User user);
}
