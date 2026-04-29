using BuissnessLogicLayer;
using BuissnessLogicLayer.Models;
using CORNY.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CORNY.Controllers
{
    [Authorize]
    public class RecipeSuggestionController : Controller
    {
        private readonly ICartService cartService;
        private readonly IProductService productService;
        private readonly IRecipeSuggestionService recipeSuggestionService;
        private readonly ISpoonacularService spoonacularService;
        private readonly IProductMatchingService productMatchingService;
        private readonly ILogger<RecipeSuggestionController> logger;

        public RecipeSuggestionController(
            ICartService cartService,
            IProductService productService,
            IRecipeSuggestionService recipeSuggestionService,
            ISpoonacularService spoonacularService,
            IProductMatchingService productMatchingService,
            ILogger<RecipeSuggestionController> logger)
        {
            this.cartService = cartService;
            this.productService = productService;
            this.recipeSuggestionService = recipeSuggestionService;
            this.spoonacularService = spoonacularService;
            this.productMatchingService = productMatchingService;
            this.logger = logger;
        }

        [HttpGet]
        [Route("RecipeSuggestions")]
        public async Task<IActionResult> Index(string? filter, int offset = 0)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Challenge();
            }

            try
            {
                var cartItems = await cartService.GetCartItemsAsync(userId.Value);
                var products = await productService.GetProductsByIdsAsync(cartItems.Select(item => item.ProductId));
                var productLookup = products.ToDictionary(product => product.ProductId);

                var cartProducts = cartItems
                    .Where(item => productLookup.ContainsKey(item.ProductId))
                    .Select(item => new CartItemViewModel
                    {
                        Product = productLookup[item.ProductId],
                        Quantity = item.Quantity,
                        ImageUrl = productLookup[item.ProductId].ImageUrl
                    })
                    .ToList();

                // Convert CartItemViewModel to CartItemInfo for the service
                var cartItemsInfo = cartProducts
                    .Select(item => new CartItemInfo
                    {
                        ProductId = item.Product.ProductId,
                        ProductName = item.Product.Name,
                        Quantity = item.Quantity,
                        Sizing = item.Product.Sizing,
                        Price = item.Product.Pricing,
                        ImageUrl = item.ImageUrl
                    })
                    .ToList();

                var allProducts = await productService.GetProductsAsync();

                // Get recipe suggestions from the service with offset
                var suggestions = await recipeSuggestionService.GetRecipeSuggestionsAsync(
                    cartItemsInfo,
                    allProducts.ToList(),
                    filter,
                    offset);

                var viewModel = new RecipeSuggestionViewModel
                {
                    CartItems = cartProducts,
                    Recipes = suggestions.Recipes,
                    SuggestedAddons = suggestions.SuggestedAddons,
                    SelectedFilter = filter,
                    CurrentOffset = offset
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error loading recipe suggestions page");
                
                // Return empty view model on error
                var emptyViewModel = new RecipeSuggestionViewModel
                {
                    CartItems = Array.Empty<CartItemViewModel>(),
                    Recipes = Array.Empty<RecipeCardViewModel>(),
                    SuggestedAddons = Array.Empty<AddonProductViewModel>(),
                    SelectedFilter = filter,
                    CurrentOffset = offset
                };
                
                return View(emptyViewModel);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GenerateNew(string? filter, int currentOffset = 0)
        {
            // Increment offset by 3 to get different recipes
            var newOffset = currentOffset + 3;
            
            // Regenerate suggestions with the selected filter and new offset
            return RedirectToAction(nameof(Index), new { filter, offset = newOffset });
        }

        [HttpGet]
        [Route("RecipeSuggestions/Details")]
        public async Task<IActionResult> Details(int? spoonacularId, string? title)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Challenge();
            }

            try
            {
                RecipeDetailViewModel? viewModel = null;

                // Try to get recipe details from Spoonacular if we have an ID
                if (spoonacularId.HasValue)
                {
                    var spoonacularRecipe = await spoonacularService.GetRecipeInformationAsync(spoonacularId.Value);
                    if (spoonacularRecipe != null)
                    {
                        viewModel = await MapSpoonacularToDetailViewModelAsync(spoonacularRecipe, userId.Value);
                    }
                }

                // Fallback: If no Spoonacular data, show a simple page
                if (viewModel == null)
                {
                    viewModel = new RecipeDetailViewModel
                    {
                        Title = title ?? "Recipe Details",
                        Description = "Recipe details are currently unavailable. Please try generating new suggestions.",
                        ImageUrl = "/images/placeholder-recipe.jpg",
                        TimeMinutes = null,
                        Difficulty = "Easy",
                        Tags = Array.Empty<string>(),
                        Ingredients = Array.Empty<RecipeIngredientViewModel>(),
                        Instructions = Array.Empty<RecipeInstructionStepViewModel>(),
                        MissingDatabaseProducts = Array.Empty<DataAccessLayer.Entities.ProductModel>()
                    };
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error loading recipe details");
                return RedirectToAction(nameof(Index));
            }
        }

        private async Task<RecipeDetailViewModel> MapSpoonacularToDetailViewModelAsync(
            SpoonacularRecipeInformation recipe,
            int userId)
        {
            // Get user's cart items to check what they already have
            var cartItems = await cartService.GetCartItemsAsync(userId);
            var products = await productService.GetProductsByIdsAsync(cartItems.Select(item => item.ProductId));
            var cartProductNames = products.Select(p => p.Name.ToLowerInvariant()).ToHashSet();

            // Get all available products for matching
            var allProducts = (await productService.GetProductsAsync()).ToList();

            // Map ingredients
            var ingredientViewModels = recipe.ExtendedIngredients.Select(ing =>
            {
                var isInCart = cartProductNames.Contains(ing.Name.ToLowerInvariant());
                var matchedProduct = allProducts.FirstOrDefault(p =>
                    p.IsForSale &&
                    p.InventoryQuantity > 0 &&
                    (p.Name.Equals(ing.Name, StringComparison.OrdinalIgnoreCase) ||
                     p.Name.Contains(ing.Name, StringComparison.OrdinalIgnoreCase) ||
                     ing.Name.Contains(p.Name, StringComparison.OrdinalIgnoreCase)));

                return new RecipeIngredientViewModel
                {
                    Name = ing.OriginalName,
                    Amount = $"{ing.Amount} {ing.Unit}".Trim(),
                    IsInCart = isInCart,
                    IsAvailableInStore = matchedProduct != null,
                    ProductId = matchedProduct?.ProductId
                };
            }).ToList();

            // Map instructions
            var instructions = new List<RecipeInstructionStepViewModel>();
            if (recipe.AnalyzedInstructions?.Any() == true)
            {
                foreach (var instruction in recipe.AnalyzedInstructions)
                {
                    foreach (var step in instruction.Steps)
                    {
                        instructions.Add(new RecipeInstructionStepViewModel
                        {
                            StepNumber = step.Number,
                            Instruction = step.Step
                        });
                    }
                }
            }
            else if (!string.IsNullOrWhiteSpace(recipe.Instructions))
            {
                // Fallback: Parse plain text instructions
                var lines = recipe.Instructions.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < lines.Length; i++)
                {
                    instructions.Add(new RecipeInstructionStepViewModel
                    {
                        StepNumber = i + 1,
                        Instruction = lines[i].Trim()
                    });
                }
            }

            // Find missing ingredients that are available in store
            var missingIngredientNames = ingredientViewModels
                .Where(ing => !ing.IsInCart)
                .Select(ing => ing.Name)
                .ToList();

            var missingProducts = await productMatchingService.MatchIngredientsToProductsAsync(
                missingIngredientNames,
                allProducts);

            // Build tags
            var tags = new List<string>();
            if (recipe.Vegetarian) tags.Add("Vegetarian");
            if (recipe.Vegan) tags.Add("Vegan");
            if (recipe.ReadyInMinutes <= 30) tags.Add("Quick Meal");
            if (recipe.ReadyInMinutes > 0) tags.Add($"{recipe.ReadyInMinutes} min");

            return new RecipeDetailViewModel
            {
                Title = recipe.Title,
                Description = StripHtmlTags(recipe.Summary),
                ImageUrl = recipe.Image,
                TimeMinutes = recipe.ReadyInMinutes,
                Servings = recipe.Servings,
                Difficulty = recipe.ReadyInMinutes <= 30 ? "Easy" : "Medium",
                Tags = tags,
                Ingredients = ingredientViewModels,
                Instructions = instructions,
                MissingDatabaseProducts = missingProducts
            };
        }

        private string StripHtmlTags(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            // Simple HTML tag removal
            return System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty).Trim();
        }

        private int? GetUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var userId) ? userId : null;
        }
    }
}