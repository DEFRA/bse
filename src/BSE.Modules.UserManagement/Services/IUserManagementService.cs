using BSE.Modules.UserManagement.Models;

namespace BSE.Modules.UserManagement.Services;

public interface IUserManagementService
{
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task<int> AddUserAsync(User user);
    Task UpdateUserAsync(User user);
}
