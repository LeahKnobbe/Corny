using BuissnessLogicLayer;
using CORNY.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CORNY.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ICartService cartService;
        private readonly IProductService productService;
        private readonly IUserService userService;

        public CartController(ICartService cartService, IProductService productService, IUserService userService)
        {
            this.cartService = cartService;
            this.productService = productService;
            this.userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Challenge();
            }

            var cartItems = await cartService.GetCartItemsAsync(userId.Value);

            var products = await productService.GetProductsByIdsAsync(cartItems.Select(item => item.ProductId));
            var productLookup = products.ToDictionary(product => product.ProductId);

            var viewModel = new CartViewModel
            {
                Items = cartItems
                    .Where(item => productLookup.ContainsKey(item.ProductId))
                    .Select(item => new CartItemViewModel
                    {
                        Product = productLookup[item.ProductId],
                        Quantity = item.Quantity,
                        ImageUrl = productLookup[item.ProductId].ImageUrl
                    })
                    .ToList()
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Challenge();
            }

            var cartItems = await cartService.GetCartItemsAsync(userId.Value);
            if (!cartItems.Any())
            {
                return RedirectToAction(nameof(Index));
            }

            var products = await productService.GetProductsByIdsAsync(cartItems.Select(item => item.ProductId));
            var productLookup = products.ToDictionary(product => product.ProductId);

            var user = await userService.GetUserByIdAsync(userId.Value);
            var fullName = user != null ? $"{user.FirstName} {user.LastName}" : "Customer";

            var viewModel = new CheckoutViewModel
            {
                UserFullName = fullName,
                Items = cartItems
                    .Where(item => productLookup.ContainsKey(item.ProductId))
                    .Select(item => new CartItemViewModel
                    {
                        Product = productLookup[item.ProductId],
                        Quantity = item.Quantity,
                        ImageUrl = productLookup[item.ProductId].ImageUrl
                    })
                    .ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId, int quantity = 1, string? returnUrl = null)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Challenge();
            }

            if (quantity < 1)
            {
                quantity = 1;
            }

            await cartService.AddToCartAsync(userId.Value, productId, quantity);

            var product = await productService.GetProductByIdAsync(productId);
            TempData["CartMessage"] = product == null
                ? "Item added to cart."
                : $"{product.Name} added to cart.";

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int productId, int quantity)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Challenge();
            }

            await cartService.UpdateQuantityAsync(userId.Value, productId, quantity);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int productId)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Challenge();
            }

            await cartService.RemoveFromCartAsync(userId.Value, productId);
            return RedirectToAction(nameof(Index));
        }

        private int? GetUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var userId) ? userId : null;
        }
    }
}