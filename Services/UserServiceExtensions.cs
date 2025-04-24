using HomeownersSubdivision.Models;
using Microsoft.AspNetCore.Http;

namespace HomeownersSubdivision.Services
{
    public static class UserServiceExtensions
    {
        public static User? GetCurrentUser(this IUserService userService, HttpContext httpContext)
        {
            try
            {
                var userId = httpContext.Session.GetInt32("UserId");
                if (userId == null || userId <= 0)
                {
                    // Try to get user from claims if session is not available
                    var userIdClaim = httpContext.User?.FindFirst("UserId");
                    if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int claimUserId))
                    {
                        userId = claimUserId;
                    }
                    else
                    {
                        return null;
                    }
                }

                // Since we're in an async method but not using await, we need to get the result this way
                return userService.GetUserByIdAsync(userId.Value).GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                // Log the exception if needed
                return null;
            }
        }
    }
} 