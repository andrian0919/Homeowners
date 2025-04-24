using HomeownersSubdivision.Data;
using HomeownersSubdivision.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace HomeownersSubdivision.Services
{
    public interface IUserService
    {
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(int id);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<User?> GetUserByEmailAsync(string email);
        Task<bool> CreateUserAsync(User user, string password);
        Task UpdateUserAsync(User user);
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
        Task<bool> AuthenticateAsync(string username, string password);
        Task<bool> DeleteUserAsync(int id);
        Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role);
    }

    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<bool> CreateUserAsync(User user, string password)
        {
            // Check if username or email already exists
            if (await _context.Users.AnyAsync(u => u.Username == user.Username || u.Email == user.Email))
            {
                return false;
            }

            // Hash the password
            user.Password = HashPassword(password);
            
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task UpdateUserAsync(User user)
        {
            _context.Entry(user).State = EntityState.Modified;
            // Don't update the password here
            _context.Entry(user).Property(x => x.Password).IsModified = false;
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return false;
            }

            // Verify current password
            if (user.Password != HashPassword(currentPassword))
            {
                return false;
            }

            // Update to new password
            user.Password = HashPassword(newPassword);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AuthenticateAsync(string username, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => 
                (u.Username == username || u.Email == username) && u.IsActive);
            
            if (user == null)
            {
                return false;
            }

            // Check password
            if (user.Password != HashPassword(password))
            {
                return false;
            }

            // Update last login date
            user.LastLoginDate = DateTime.Now;
            await _context.SaveChangesAsync();
            
            return true;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return false;
            }

            // Soft delete by setting IsActive to false
            user.IsActive = false;
            await _context.SaveChangesAsync();
            
            return true;
        }

        public async Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role)
        {
            return await _context.Users.Where(u => u.Role == role).ToListAsync();
        }

        private string HashPassword(string password)
        {
            // In a real application, use a more robust password hashing algorithm like BCrypt
            // This is a simple implementation for demonstration purposes
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
} 