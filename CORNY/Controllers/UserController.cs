using BuissnessLogicLayer;
using Microsoft.AspNetCore.Mvc;

namespace CORNY.Controllers
{

    /// Handles user-related views.
    public class UserController : Controller
    {
        // Provides user data and operations.
        private readonly IUserService userService;


        /// Initializes a new instance of the iuserservice class.

        public UserController(IUserService userService)
        {
            this.userService = userService;
        }

 
        /// Displays the list of users
        public async Task<IActionResult> Index()
        {
            var users = await userService.GetUsersAsync();
            return View(users);
        }
    }
}
