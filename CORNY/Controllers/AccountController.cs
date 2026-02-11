using Microsoft.AspNetCore.Mvc;
using CORNY.Models.ViewModels;
using BuissnessLogicLayer;
using DataAccessLayer.Entities;

namespace CORNY.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;

        public AccountController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // TODO (Leah later): SignIn logic with Identity
            // For now: just pretend login succeeded
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (model.AcceptTerms != true)
            {
                ModelState.AddModelError(nameof(model.AcceptTerms),
                    "You must accept the Terms and Privacy Policy.");
            }

            if (!ModelState.IsValid)
                return View(model);

            // Check if email already exists
            if (await _userService.EmailExistsAsync(model.Email))
            {
                ModelState.AddModelError(nameof(model.Email), 
                    "An account with this email already exists.");
                return View(model);
            }

            // Create new user entity
            var newUser = new UserModel
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                Password = model.Password, // Will be hashed in service layer
                Bday = DateOnly.FromDateTime(model.Birthday!.Value)
            };

            try
            {
                await _userService.CreateUserAsync(newUser);
                TempData["SuccessMessage"] = "Registration successful! Please log in.";
                return RedirectToAction("Login");
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, 
                    "An error occurred during registration. Please try again.");
                return View(model);
            }
        }
    }
}
