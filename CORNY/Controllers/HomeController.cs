using System;
using System.Diagnostics;
using System.Linq;
using BuissnessLogicLayer;
using CORNY.Models;
using Microsoft.AspNetCore.Mvc;

namespace CORNY.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IProductService productService;

        public HomeController(ILogger<HomeController> logger, IProductService productService)
        {
            _logger = logger;
            this.productService = productService;
        }

        public async Task<IActionResult> Index(string? search)
        {
            var products = await productService.GetProductsAsync();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                products = products.Where(product =>
                    product.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || (product.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            ViewData["Search"] = search;

            return View(products);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
