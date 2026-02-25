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

        public async Task<UserModel?> GetUserByIdAsync(int id)
        {
            return await userDbContext.Users.FindAsync(id);
        }

        public async Task<bool> UpdateUserAsync(UserModel user)
        {
            var existingUser = await userDbContext.Users.FindAsync(user.UserId);
            if (existingUser == null)
            {
                return false;
            }
            existingUser.FirstName = user.FirstName;
            existingUser.LastName = user.LastName;
            existingUser.Email = user.Email;
            existingUser.Password = user.Password;
            existingUser.Bday = user.Bday;
            await userDbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await userDbContext.Users.FindAsync(id);
            if (user == null)
            {
                return false;
            }
            userDbContext.Users.Remove(user);
            await userDbContext.SaveChangesAsync();
            return true;
        }
    }
}