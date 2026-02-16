using DataAccessLayer.Entities;
using DataAccessLayer.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace BuissnessLogicLayer
{
    public class UserService : IUserService
    {
        private readonly IUserRepository userRepository;
        public UserService(IUserRepository userRepository)
        {
            this.userRepository = userRepository;
        }

        public async Task<IEnumerable<UserModel>> GetUsersAsync()
        {
            return await userRepository.GetUsersAsync();
        }

        public async Task<UserModel> CreateUserAsync(UserModel user)
        {
            user.Password = HashPassword(user.Password);
            return await userRepository.CreateUserAsync(user);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await userRepository.EmailExistsAsync(email);
        }

        public async Task<UserModel?> AuthenticateAsync(string email, string password)
        {
            var user = await userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                return null;
            }

            var hashedInput = HashPassword(password);
            return user.Password == hashedInput ? user : null;
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}