using BSE.Modules.UserManagement.Models;
using BSE.Modules.UserManagement.Repositories;

namespace BSE.Modules.UserManagement.Services;

public sealed class UserManagementService : IUserManagementService
{
    private readonly IUserRepository _repository;

    public UserManagementService(IUserRepository repository)
    {
        _repository = repository;
    }

    public Task<IEnumerable<User>> GetAllUsersAsync() =>
        _repository.GetAllAsync();

    public Task<int> AddUserAsync(User user) =>
        _repository.AddAsync(user);

    public Task UpdateUserAsync(User user) =>
        _repository.UpdateAsync(user);
}
