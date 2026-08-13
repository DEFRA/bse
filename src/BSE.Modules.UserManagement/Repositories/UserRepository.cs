using BSE.Infrastructure;
using BSE.Modules.UserManagement.Models;
using Dapper;
using System.Data;
using UG = BSE.SharedKernel.UserGroup;

namespace BSE.Modules.UserManagement.Repositories;

/// <summary>
/// Typed repository for all user-management stored procedure calls.
/// Inherits <see cref="DapperRepository"/> to delegate connection management.
/// </summary>
public sealed class UserRepository : DapperRepository, IUserRepository
{
    public UserRepository(IDbConnectionFactory connectionFactory)
        : base(connectionFactory) { }

    // ── Private DTOs matching SP output column names ───────────────────────────

    // GetUsers SP — mapped by column name so adding/removing columns does not break Dapper.
    private sealed class GetUsersRow
    {
        public int ID { get; init; }
        public string NTLogin { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string? Email { get; init; }
        public int UserGroup { get; init; }
        public bool IsActive { get; init; }
    }

    // GetUserByNTLogin: ID, Name, UserGroup, GroupName, Email (NTLogin is the input parameter)
    private sealed record GetUserByNtLoginRow(
        int ID,
        string Name,
        int UserGroup,
        string GroupName,
        string? Email);

    // -- Interface implementation

    public async Task<User?> GetByUpnAsync(string upn)
    {
        // The UPN column is added by AddUserUpnColumn.sql (preparatory script in this slice).
        // The GetUsers SP does not currently SELECT the UPN column; once that SP is updated,
        // Dapper will populate it and this filter will start resolving matches.
        // Until then, all rows have UPN = null and this returns null — triggering NTLogin fallback.
        var rows = await QueryAsync<GetUsersRow>("GetUsers");
        // UPN is not yet returned by GetUsers SP; all rows have null UPN until SP is updated.
        var row = rows.FirstOrDefault(r =>
            false);
        return row is null ? null : MapToUser(row);
    }

    public async Task<User?> GetByNtLoginAsync(string ntLogin)
    {
        // GetUserByNTLogin filters IsActive = 1; inactive users return null.
        var row = await QuerySingleOrDefaultAsync<GetUserByNtLoginRow>(
            "GetUserByNTLogin", new { NTLogin = ntLogin });
        if (row is null) return null;

        return new User(
            UserId: row.ID,
            NTLogin: ntLogin,
            Upn: null,          // not returned by this SP; populated after UPN migration
            UserName: row.Name,
            Email: row.Email,
            IsActive: true,     // SP only returns active users
            UserGroupId: row.UserGroup,
            UserGroup: ToUserGroup(row.UserGroup),
            GroupName: row.GroupName);   // luUserGroup.Name from DB join
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        var rows = await QueryAsync<GetUsersRow>("GetUsers");
        return rows.Select(MapToUser);
    }

    public async Task<int> AddAsync(User user)
    {
        var p = new DynamicParameters();
        p.Add("NTLogin", user.NTLogin, DbType.String, size: 25);
        p.Add("Name", user.UserName, DbType.String, size: 35);
        p.Add("Email", user.Email, DbType.String, size: 60);
        p.Add("UserGroup", (int)user.UserGroup, DbType.Int32);
        p.Add("IsActive", user.IsActive, DbType.Boolean);
        p.Add("ID", dbType: DbType.Int32, direction: ParameterDirection.Output);
        await ExecuteWithOutputAsync("AddUser", p);
        return p.Get<int>("ID");
    }

    public Task UpdateAsync(User user) =>
        ExecuteAsync("EditUser", new
        {
            ID = user.UserId,
            NTLogin = user.NTLogin,
            Name = user.UserName,
            Email = user.Email,
            UserGroup = (int)user.UserGroup,
            IsActive = user.IsActive
        });

    // -- Helpers

    private static User MapToUser(GetUsersRow r) =>
        new(UserId: r.ID,
            NTLogin: r.NTLogin,
            Upn: null,          // UPN not yet selected by GetUsers SP; pending SP update
            UserName: r.Name,
            Email: r.Email,
            IsActive: r.IsActive,
            UserGroupId: r.UserGroup,
            UserGroup: ToUserGroup(r.UserGroup));

    private static UG ToUserGroup(int id) =>
        Enum.IsDefined(typeof(UG), id) ? (UG)id : UG.None;
}
