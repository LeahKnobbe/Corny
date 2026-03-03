using BuissnessLogicLayer;
using CORNY.Models.ViewModels;
using DataAccessLayer.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace CORNY.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductService productService;
        private readonly IWebHostEnvironment webHostEnvironment;

        public ProductController(IProductService productService, IWebHostEnvironment webHostEnvironment)
        {
            this.productService = productService;
            this.webHostEnvironment = webHostEnvironment;
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

            var images = await productService.GetProductImagesAsync(id);
            var imageUrls = new List<string>();

            if (!string.IsNullOrWhiteSpace(product.ImageUrl))
            {
                imageUrls.Add(product.ImageUrl);
            }

            imageUrls.AddRange(images.Select(image => image.ImageUrl));
            imageUrls = imageUrls.Distinct().ToList();

            var viewModel = new ProductDetailsViewModel
            {
                Product = product,
                ImageUrls = imageUrls
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new ProductFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var imageUrls = await SaveImagesAsync(model.ImageFiles);

            var product = new ProductModel
            {
                Name = model.Name,
                Description = model.Description,
                Sizing = model.Sizing,
                InventoryQuantity = model.InventoryQuantity,
                IsForSale = model.IsForSale,
                Pricing = model.Pricing,
                FarmId = model.FarmId,
                CategoryId = model.CategoryId,
                ImageUrl = imageUrls.FirstOrDefault()
            };

            await productService.CreateProductAsync(product);

            if (imageUrls.Count > 1)
            {
                await productService.AddProductImagesAsync(product.ProductId, imageUrls.Skip(1).ToList());
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await productService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            var model = new ProductFormViewModel
            {
                ProductId = product.ProductId,
                Name = product.Name,
                Description = product.Description,
                Sizing = product.Sizing,
                InventoryQuantity = product.InventoryQuantity,
                IsForSale = product.IsForSale,
                Pricing = product.Pricing,
                FarmId = product.FarmId,
                CategoryId = product.CategoryId,
                ImageUrl = product.ImageUrl
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductFormViewModel model)
        {
            if (id != model.ProductId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingProduct = await productService.GetProductByIdAsync(id);
            if (existingProduct == null)
            {
                return NotFound();
            }

            var imageUrls = await SaveImagesAsync(model.ImageFiles);
            var imageUrl = string.IsNullOrWhiteSpace(model.ImageUrl)
                ? imageUrls.FirstOrDefault()
                : model.ImageUrl;

            var product = new ProductModel
            {
                ProductId = model.ProductId,
                Name = model.Name,
                Description = model.Description,
                Sizing = model.Sizing,
                InventoryQuantity = model.InventoryQuantity,
                IsForSale = model.IsForSale,
                Pricing = model.Pricing,
                FarmId = model.FarmId,
                CategoryId = model.CategoryId,
                ImageUrl = imageUrl
            };

            var success = await productService.UpdateProductAsync(product);
            if (!success)
            {
                return NotFound();
            }

            if (imageUrls.Count > 0)
            {
                await productService.AddProductImagesAsync(product.ProductId, imageUrls);
            }

            return RedirectToAction(nameof(Index));
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

        private async Task<IReadOnlyList<string>> SaveImagesAsync(IList<IFormFile> files)
        {
            if (files.Count == 0)
            {
                return Array.Empty<string>();
            }

            var uploadsFolder = Path.Combine(webHostEnvironment.WebRootPath, "images", "products");
            Directory.CreateDirectory(uploadsFolder);

            var savedUrls = new List<string>();

            foreach (var file in files.Where(file => file.Length > 0))
            {
                var extension = Path.GetExtension(file.FileName);
                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                await using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);

                savedUrls.Add($"/images/products/{fileName}");
            }

            return savedUrls;
        }
    }
}