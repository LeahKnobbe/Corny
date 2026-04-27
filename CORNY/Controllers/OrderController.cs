using BuissnessLogicLayer;
using CORNY.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CORNY.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IOrderService orderService;
        private readonly IProductService productService;

        public OrderController(IOrderService orderService, IProductService productService)
        {
            this.orderService = orderService;
            this.productService = productService;
        }

        [Authorize(Roles = "1")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var orders = await orderService.GetOrdersAsync();
            var viewModel = new OrderIndexViewModel
            {
                Orders = await BuildOrderListAsync(orders),
                Summary = await BuildOrderSummaryAsync(orders)
            };

            return View(viewModel);
        }

        [Authorize(Roles = "1")]
        [HttpGet]
        public async Task<IActionResult> Summary()
        {
            var orders = await orderService.GetOrdersAsync();
            var summary = await BuildOrderSummaryAsync(orders);

            return Json(new
            {
                totalAmount = summary.TotalAmount,
                totalOrders = summary.TotalOrders,
                bestSellingItem = summary.BestSellingItem,
                bestSellingQuantity = summary.BestSellingQuantity
            });
        }

        [HttpGet]
        public async Task<IActionResult> MyOrders()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(value, out var userId))
            {
                return Challenge();
            }

            var orders = await orderService.GetOrdersForUserAsync(userId);
            var viewModel = await BuildOrderListAsync(orders);
            return View(viewModel);
        }

        private async Task<IReadOnlyList<OrderDisplayViewModel>> BuildOrderListAsync(IReadOnlyList<DataAccessLayer.Entities.OrderModel> orders)
        {
            var productIds = orders
                .SelectMany(order => order.OrderItems.Select(item => item.ProductId))
                .Distinct()
                .ToList();

            var products = await productService.GetProductsByIdsAsync(productIds);
            var productLookup = products.ToDictionary(product => product.ProductId, product => product.Name);

            return orders.Select(order => new OrderDisplayViewModel
            {
                OrderId = order.OrderId,
                OrderDate = order.OrderDate,
                UserId = order.UserId,
                Status = order.Status,
                TotalOrderCost = order.TotalOrderCost,
                ShippingAddress = order.ShippingAddress,
                Items = order.OrderItems.Select(item => new OrderItemDisplayViewModel
                {
                    ProductId = item.ProductId,
                    ProductName = productLookup.TryGetValue(item.ProductId, out var name) ? name : "Unknown",
                    Quantity = item.Quantity,
                    PriceWhenPlaced = item.PriceWhenPlaced
                }).ToList()
            }).ToList();
        }

        private async Task<OrderSummaryViewModel> BuildOrderSummaryAsync(IReadOnlyList<DataAccessLayer.Entities.OrderModel> orders)
        {
            var summary = new OrderSummaryViewModel
            {
                TotalOrders = orders.Count,
                TotalAmount = orders.Sum(order => order.TotalOrderCost)
            };

            var bestSeller = orders
                .SelectMany(order => order.OrderItems)
                .GroupBy(item => item.ProductId)
                .Select(group => new { ProductId = group.Key, Quantity = group.Sum(item => item.Quantity) })
                .OrderByDescending(group => group.Quantity)
                .FirstOrDefault();

            if (bestSeller == null)
            {
                return summary;
            }

            var products = await productService.GetProductsByIdsAsync(new[] { bestSeller.ProductId });
            summary.BestSellingItem = products.FirstOrDefault()?.Name ?? "Unknown";
            summary.BestSellingQuantity = bestSeller.Quantity;

            return summary;
        }
    }
}