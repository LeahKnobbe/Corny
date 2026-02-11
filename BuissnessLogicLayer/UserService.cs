using DataAccessLayer.Entities;
using DataAccessLayer.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

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
            // Hash the password before storing
            user.Password = HashPassword(user.Password);
            return await userRepository.CreateUserAsync(user);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await userRepository.EmailExistsAsync(email);
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
