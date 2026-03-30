using DataAccessLayer.Entities;

namespace DataAccessLayer.Interfaces
{
    public interface IUserRepository
    {
        Task<IEnumerable<UserModel>> GetUsersAsync();
        Task<UserModel> CreateUserAsync(UserModel user);
        Task<bool> EmailExistsAsync(string email);
        Task<UserModel?> GetByEmailAsync(string email);
        Task<UserModel?> GetUserByIdAsync(int id);
        Task<bool> UpdateUserAsync(UserModel user);
        Task<bool> DeleteUserAsync(int id);
    }
}