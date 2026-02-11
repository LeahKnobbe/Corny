using BuissnessLogicLayer;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace CORNY.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductService productService;
        public ProductController(IProductService productService)
        {
            this.productService = productService;
        }

        public async Task<IActionResult> Index()
        {
            var products = await productService.GetProductsAsync();
            return View(products);
        }

        // DB-backed Fruit page
        [HttpGet]
        [Route("Produce/Fruit")]
        public async Task<IActionResult> Fruit()
        {
            var products = await productService.GetProductsAsync();
            var fruits = products
                .Where(product => product.CategoryId == 1 && product.IsForSale)
                .ToList();

            return View(fruits);
        }

        // Hardcoded demo page (easy delete later)
        [HttpGet]
        [Route("Produce/FruitCoded")]
        public IActionResult FruitCoded()
        {
            return View();
        }
    }
}