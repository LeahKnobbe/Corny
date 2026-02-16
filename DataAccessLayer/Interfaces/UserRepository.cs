using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Interfaces
{
    public class UserRepository : IUserRepository
    {
        private readonly UserDbContext userDbContext;

        public UserRepository(UserDbContext userDbContext)
        {
            this.userDbContext = userDbContext;
        }

        public async Task<IEnumerable<UserModel>> GetUsersAsync()
        {
            return await userDbContext.Users.ToListAsync();
        }

        public async Task<UserModel> CreateUserAsync(UserModel user)
        {
            userDbContext.Users.Add(user);
            await userDbContext.SaveChangesAsync();
            return user;
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await userDbContext.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<UserModel?> GetByEmailAsync(string email)
        {
            return await userDbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        }
    }
}