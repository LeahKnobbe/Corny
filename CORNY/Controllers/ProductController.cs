using BuissnessLogicLayer;
using DataAccessLayer.Entities;
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

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var product = await productService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductModel product)
        {
            if (ModelState.IsValid)
            {
                await productService.CreateProductAsync(product);
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await productService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductModel product)
        {
            if (id != product.ProductId)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                var success = await productService.UpdateProductAsync(product);
                if (!success)
                {
                    return NotFound();
                }
                return RedirectToAction(nameof(Index));
            }

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await productService.DeleteProductAsync(id);
            if (!success)
            {
                return NotFound();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}