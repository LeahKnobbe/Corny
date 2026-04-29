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
        private readonly IOrderService orderService;

        public CartController(ICartService cartService, IProductService productService, IUserService userService, IOrderService orderService)
        {
            this.cartService = cartService;
            this.productService = productService;
            this.userService = userService;
            this.orderService = orderService;
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

            var product = await productService.GetProductByIdAsync(productId);
            if (product == null || product.InventoryQuantity <= 0)
            {
                TempData["CartMessage"] = "This item is currently out of stock.";
                return RedirectToAction(nameof(Index));
            }

            var cartItems = await cartService.GetCartItemsAsync(userId.Value);
            var existingQuantity = cartItems
                .Where(item => item.ProductId == productId)
                .Select(item => item.Quantity)
                .FirstOrDefault();

            var available = product.InventoryQuantity;
            var remaining = Math.Max(0, available - existingQuantity);

            if (remaining <= 0)
            {
                TempData["CartMessage"] = $"{product.Name} is already at the maximum available quantity.";
                return RedirectToAction(nameof(Index));
            }

            if (quantity > remaining)
            {
                quantity = remaining;
                TempData["CartMessage"] = $"Only {available} of {product.Name} are available. Cart updated to max.";
            }

            await cartService.AddToCartAsync(userId.Value, productId, quantity);

            if (TempData["CartMessage"] == null)
            {
                TempData["CartMessage"] = $"{product.Name} added to cart.";
            }

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

            var product = await productService.GetProductByIdAsync(productId);
            if (product == null || product.InventoryQuantity <= 0)
            {
                await cartService.UpdateQuantityAsync(userId.Value, productId, 0);
                TempData["CartMessage"] = "This item is currently out of stock.";
                return RedirectToAction(nameof(Index));
            }

            if (quantity > product.InventoryQuantity)
            {
                quantity = product.InventoryQuantity;
                TempData["CartMessage"] = $"Only {product.InventoryQuantity} of {product.Name} are available. Cart updated to max.";
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(string? shippingAddress)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Challenge();
            }

            var orderId = await orderService.PlaceOrderAsync(userId.Value, shippingAddress);
            if (orderId == null)
            {
                return RedirectToAction(nameof(Index));
            }

            TempData["CartMessage"] = "Order placed.";
            return RedirectToAction(nameof(Index));
        }

        private int? GetUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var userId) ? userId : null;
        }
    }
}