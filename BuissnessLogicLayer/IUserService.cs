using DataAccessLayer.Entities;

namespace BuissnessLogicLayer
{
    public interface IUserService
    {
        Task<IEnumerable<UserModel>> GetUsersAsync();
        Task<UserModel> CreateUserAsync(UserModel user);
        Task<bool> EmailExistsAsync(string email);
        Task<UserModel?> AuthenticateAsync(string email, string password);
        Task<UserModel?> GetUserByIdAsync(int id);
        Task<bool> UpdateUserAsync(UserModel user);
        Task<bool> DeleteUserAsync(int id);

    }
}