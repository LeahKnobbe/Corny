using BuissnessLogicLayer.Models;
using DataAccessLayer.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace BuissnessLogicLayer
{
    /// <summary>
    /// AI-powered recipe suggestion service using OpenAI and Spoonacular with semantic filtering.
    /// Falls back to MockRecipeSuggestionService if APIs are unavailable.
    /// </summary>
    public class OpenAIRecipeSuggestionService : IRecipeSuggestionService
    {
        private readonly HttpClient httpClient;
        private readonly IConfiguration configuration;
        private readonly ILogger<OpenAIRecipeSuggestionService> logger;
        private readonly ISpoonacularService spoonacularService;
        private readonly IProductMatchingService productMatchingService;
        private readonly MockRecipeSuggestionService fallbackService;
        private readonly string? openAiApiKey;

        public OpenAIRecipeSuggestionService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<OpenAIRecipeSuggestionService> logger,
            ISpoonacularService spoonacularService,
            IProductMatchingService productMatchingService)
        {
            this.httpClient = httpClient;
            this.configuration = configuration;
            this.logger = logger;
            this.spoonacularService = spoonacularService;
            this.productMatchingService = productMatchingService;
            this.fallbackService = new MockRecipeSuggestionService();

            openAiApiKey = configuration["OPENAI_API_KEY"]
                           ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");

            httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
            if (!string.IsNullOrWhiteSpace(openAiApiKey))
            {
                httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", openAiApiKey);
            }
        }

        public async Task<RecipeSuggestionResult> GetRecipeSuggestionsAsync(
            IReadOnlyList<CartItemInfo> cartItems,
            IReadOnlyList<ProductModel> availableProducts,
            string? filter = null,
            int offset = 0)
        {
            // If no API keys configured, use fallback
            if (string.IsNullOrWhiteSpace(openAiApiKey))
            {
                logger.LogWarning("OpenAI API key not configured. Using mock data.");
                return await fallbackService.GetRecipeSuggestionsAsync(cartItems, availableProducts, filter);
            }

            if (!cartItems.Any())
            {
                logger.LogInformation("No cart items provided. Returning empty suggestions.");
                return new RecipeSuggestionResult();
            }

            try
            {
                logger.LogInformation("Starting recipe suggestion with HYBRID semantic + keyword filtering");
                logger.LogInformation("Filter: {Filter}, Offset: {Offset}", filter ?? "None", offset);

                // Step 1: Get recipes from Spoonacular with the provided offset
                var ingredientNames = cartItems.Select(item => item.ProductName).ToList();
                logger.LogInformation("Cart items: {Ingredients}, Offset: {Offset}", 
                    string.Join(", ", ingredientNames), offset);
                
                var spoonacularRecipes = await spoonacularService.SearchRecipesByIngredientsAsync(
                    ingredientNames, 
                    30,
                    offset: offset);

                if (spoonacularRecipes?.Any() != true)
                {
                    logger.LogWarning("No recipes found from Spoonacular");
                    offset = 0;
                    return await fallbackService.GetRecipeSuggestionsAsync(cartItems, availableProducts, filter);
                }

                logger.LogInformation("Found {Count} recipes from Spoonacular", spoonacularRecipes.Length);

                // Step 2: Build WEIGHTED semantic queries
                var (cartQuery, filterQuery) = BuildWeightedSemanticQueries(cartItems, filter);
                logger.LogInformation("Cart-based query: {CartQuery}", cartQuery);
                logger.LogInformation("Filter-based query: {FilterQuery}", filterQuery);

                // Step 3: Get embeddings for both queries
                var cartEmbedding = await GetEmbeddingAsync(cartQuery);
                var filterEmbedding = !string.IsNullOrWhiteSpace(filter) 
                    ? await GetEmbeddingAsync(filterQuery) 
                    : null;

                if (cartEmbedding == null)
                {
                    logger.LogWarning("Failed to get cart embedding, using fallback");
                    return await fallbackService.GetRecipeSuggestionsAsync(cartItems, availableProducts, filter);
                }

                // Step 4: Score recipes using HYBRID approach (semantic + keyword + hard filters)
                var scoredRecipes = new List<(SpoonacularRecipe recipe, SpoonacularRecipeInformation? details, double finalScore)>();
                
                foreach (var spoonacularRecipe in spoonacularRecipes.Take(20))
                {
                    try
                    {
                        logger.LogInformation("🔍 Evaluating recipe: {Title} (ID: {Id})", 
                            spoonacularRecipe.Title, spoonacularRecipe.Id);
                        
                        var detailedRecipe = await spoonacularService.GetRecipeInformationAsync(spoonacularRecipe.Id);
                        if (detailedRecipe == null) continue;

                        // HARD FILTER: Must pass dietary requirements
                        if (!PassesHardFilter(detailedRecipe, filter))
                        {
                            logger.LogInformation("❌ REJECTED by hard filter: {Title}", detailedRecipe.Title);
                            continue;
                        }

                        // Build recipe text for semantic comparison
                        var recipeText = BuildRecipeText(detailedRecipe);
                        var recipeEmbedding = await GetEmbeddingAsync(recipeText);
                        if (recipeEmbedding == null) continue;

                        // Calculate semantic similarities
                        var cartSimilarity = CosineSimilarity(cartEmbedding, recipeEmbedding);
                        var filterSimilarity = filterEmbedding != null 
                            ? CosineSimilarity(filterEmbedding, recipeEmbedding) 
                            : 0.0;

                        // Calculate keyword boost for filter compliance
                        var keywordBoost = CalculateKeywordBoost(detailedRecipe, filter);

                        // WEIGHTED SCORING:
                        // - If filter is set: 40% cart match + 40% filter match + 20% keyword boost
                        // - If no filter: 80% cart match + 20% general quality
                        double finalScore;
                        if (!string.IsNullOrWhiteSpace(filter) && filterEmbedding != null)
                        {
                            finalScore = (0.30 * cartSimilarity) + (0.50 * filterSimilarity) + (0.20 * keywordBoost);
                            logger.LogInformation("📊 {Title}: Cart={Cart:F3}, Filter={Filter:F3}, Keyword={Keyword:F3} => FINAL={Final:F3}",
                                detailedRecipe.Title, cartSimilarity, filterSimilarity, keywordBoost, finalScore);
                        }
                        else
                        {
                            finalScore = (0.80 * cartSimilarity) + (0.20 * keywordBoost);
                            logger.LogInformation("📊 {Title}: Cart={Cart:F3}, Quality={Quality:F3} => FINAL={Final:F3}",
                                detailedRecipe.Title, cartSimilarity, keywordBoost, finalScore);
                        }

                        scoredRecipes.Add((spoonacularRecipe, detailedRecipe, finalScore));
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing recipe {RecipeId}", spoonacularRecipe.Id);
                    }
                }

                // Step 5: Sort by final score and take top 3
                var topRecipes = scoredRecipes
                    .OrderByDescending(r => r.finalScore)
                    .Take(3)
                    .ToList();

                if (!topRecipes.Any())
                {
                    logger.LogWarning("No recipes passed filtering. Using fallback.");
                    return await fallbackService.GetRecipeSuggestionsAsync(cartItems, availableProducts, filter);
                }

                logger.LogInformation("✅ TOP 3 RECIPES:");
                foreach (var (recipe, details, score) in topRecipes)
                {
                    logger.LogInformation("  🏆 {Title} (Final Score: {Score:F3})", details!.Title, score);
                }

                // Step 6: Build view models
                var recipeViewModels = BuildRecipeViewModels(topRecipes, BuildCartProductNameSet(cartItems));
                var addonViewModels = await BuildAddonViewModels(recipeViewModels, cartItems, availableProducts);

                logger.LogInformation("✅ Generated {RecipeCount} recipes with {AddonCount} addons", 
                    recipeViewModels.Count, addonViewModels.Count);

                return new RecipeSuggestionResult
                {
                    Recipes = recipeViewModels,
                    SuggestedAddons = addonViewModels
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in recipe suggestion. Using fallback.");
                return await fallbackService.GetRecipeSuggestionsAsync(cartItems, availableProducts, filter);
            }
        }

        private (string cartQuery, string filterQuery) BuildWeightedSemanticQueries(
            IReadOnlyList<CartItemInfo> cartItems, 
            string? filter)
        {
            var ingredients = string.Join(", ", cartItems.Select(c => c.ProductName));
            
            // Cart-focused query
            var cartQuery = $"Recipe using these ingredients: {ingredients}";

            // Filter-focused query with STRONG intent
            var filterQuery = filter?.ToLower() switch
            {
                "high-protein" => $"High-protein main course dish with meat, fish, eggs, or tofu. Protein-rich meal using {ingredients}. NOT a dessert or sweet dish.",
                "vegetarian" => $"Vegetarian recipe with vegetables and plant-based ingredients using {ingredients}. No meat or fish.",
                "quick-meals" => $"Quick and easy recipe ready in 30 minutes or less using {ingredients}.",
                "gluten-free" => $"Gluten-free recipe without wheat, flour, or bread using {ingredients}.",
                "dairy-free" => $"Dairy-free recipe without milk, cheese, cream, or butter using {ingredients}.",
                _ => cartQuery
            };

            return (cartQuery, filterQuery);
        }

        private double CalculateKeywordBoost(SpoonacularRecipeInformation recipe, string? filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return 0.5; // Neutral for no filter

            var title = recipe.Title.ToLowerInvariant();
            var summary = recipe.Summary.ToLowerInvariant();
            var ingredientNames = string.Join(" ", recipe.ExtendedIngredients.Select(i => i.Name.ToLowerInvariant()));

            var boost = filter.ToLower() switch
            {
                "high-protein" => CalculateProteinBoost(title, summary, ingredientNames),
                "vegetarian" => recipe.Vegetarian ? 1.0 : 0.0,
                "quick-meals" => recipe.ReadyInMinutes <= 30 ? 1.0 : 0.3,
                "gluten-free" => recipe.GlutenFree ? 1.0 : 0.0,
                "dairy-free" => recipe.DairyFree ? 1.0 : 0.0,
                _ => 0.5
            };

            return boost;
        }

        private double CalculateProteinBoost(string title, string summary, string ingredients)
        {
            var meatKeywords = new[] { "beef", "steak", "chicken", "turkey", "pork", "lamb", "duck", "venison", "bison" };
            var fishKeywords = new[] { "fish", "salmon", "tuna", "cod", "shrimp", "prawn", "lobster", "crab", "scallop" };
            var proteinKeywords = new[] { "egg", "tofu", "tempeh", "lentil", "chickpea", "bean", "quinoa", "protein" };

            var dessertKeywords = new[] { "dessert", "cake", "cookie", "sweet", "candy", "chocolate", "ice cream", 
                "mousse", "pudding", "shortcake", "pastry", "tart", "pie" };

            // STRONG penalty for desserts
            var hasDessertKeyword = dessertKeywords.Any(k => title.Contains(k) || summary.Contains(k));
            if (hasDessertKeyword)
            {
                logger.LogInformation("⚠️ Dessert keyword detected - applying penalty");
                return 0.0; // Zero boost for desserts
            }

            // Check for protein sources
            var hasMeat = meatKeywords.Any(k => title.Contains(k) || ingredients.Contains(k));
            var hasFish = fishKeywords.Any(k => title.Contains(k) || ingredients.Contains(k));
            var hasProtein = proteinKeywords.Any(k => title.Contains(k) || ingredients.Contains(k));

            if (hasMeat || hasFish)
                return 1.0; // Strong boost for meat/fish
            if (hasProtein)
                return 0.7; // Moderate boost for other protein
            
            return 0.2; // Low boost otherwise
        }

        private string BuildRecipeText(SpoonacularRecipeInformation recipe)
        {
            var ingredientList = string.Join(", ", recipe.ExtendedIngredients.Select(i => i.Name));
            var summary = StripHtmlTags(recipe.Summary);
            
            var dietaryTags = new List<string>();
            if (recipe.Vegetarian) dietaryTags.Add("vegetarian");
            if (recipe.Vegan) dietaryTags.Add("vegan");
            if (recipe.GlutenFree) dietaryTags.Add("gluten-free");
            if (recipe.DairyFree) dietaryTags.Add("dairy-free");
            
            var dietary = dietaryTags.Any() ? $" Tags: {string.Join(", ", dietaryTags)}." : "";
            
            return $"{recipe.Title}.{dietary} Main ingredients: {ingredientList}. Description: {summary}";
        }

        private async Task<double[]?> GetEmbeddingAsync(string text)
        {
            try
            {
                if (text.Length > 8000)
                    text = text.Substring(0, 8000);

                var requestBody = new
                {
                    model = "text-embedding-3-small",
                    input = text
                };

                var response = await httpClient.PostAsJsonAsync("embeddings", requestBody);
                
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("Embedding API failed: {Status}", response.StatusCode);
                    return null;
                }

                var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>();
                return result?.Data?.FirstOrDefault()?.Embedding;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting embedding");
                return null;
            }
        }

        private double CosineSimilarity(double[] vec1, double[] vec2)
        {
            if (vec1.Length != vec2.Length)
                return 0;

            double dotProduct = 0;
            double magnitude1 = 0;
            double magnitude2 = 0;

            for (int i = 0; i < vec1.Length; i++)
            {
                dotProduct += vec1[i] * vec2[i];
                magnitude1 += vec1[i] * vec1[i];
                magnitude2 += vec2[i] * vec2[i];
            }

            magnitude1 = Math.Sqrt(magnitude1);
            magnitude2 = Math.Sqrt(magnitude2);

            if (magnitude1 == 0 || magnitude2 == 0)
                return 0;

            return dotProduct / (magnitude1 * magnitude2);
        }

        private bool PassesHardFilter(SpoonacularRecipeInformation recipe, string? filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return true;

            return filter.ToLower() switch
            {
                "vegetarian" => recipe.Vegetarian,
                "quick-meals" => recipe.ReadyInMinutes > 0 && recipe.ReadyInMinutes <= 30,
                "gluten-free" => recipe.GlutenFree,
                "dairy-free" => recipe.DairyFree,
                _ => true
            };
        }

        private HashSet<string> BuildCartProductNameSet(IReadOnlyList<CartItemInfo> cartItems)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in cartItems)
            {
                names.Add(item.ProductName);
                var words = item.ProductName.Split(new[] { ' ', ',', '-' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in words.Where(w => w.Length >= 3))
                {
                    names.Add(word);
                }
            }
            return names;
        }

        private List<RecipeCardViewModel> BuildRecipeViewModels(
            List<(SpoonacularRecipe recipe, SpoonacularRecipeInformation? details, double finalScore)> topRecipes,
            HashSet<string> cartProductNames)
        {
            var recipeViewModels = new List<RecipeCardViewModel>();

            foreach (var (spoonacularRecipe, detailedRecipe, score) in topRecipes)
            {
                if (detailedRecipe == null) continue;

                var prepTime = detailedRecipe.ReadyInMinutes > 0 
                    ? $"{detailedRecipe.ReadyInMinutes} min" 
                    : "30 min";
                
                var difficulty = detailedRecipe.ReadyInMinutes <= 20 ? "Easy" : 
                               detailedRecipe.ReadyInMinutes <= 45 ? "Medium" : "Hard";

                var tags = new List<string>();
                tags.Add($"Uses {spoonacularRecipe.UsedIngredientCount} cart item{(spoonacularRecipe.UsedIngredientCount != 1 ? "s" : "")}");
                tags.Add(difficulty);
                tags.Add(prepTime);
                if (detailedRecipe.Vegetarian) tags.Add("Vegetarian");
                if (detailedRecipe.Vegan) tags.Add("Vegan");
                if (detailedRecipe.GlutenFree) tags.Add("Gluten-Free");
                if (detailedRecipe.DairyFree) tags.Add("Dairy-Free");
                tags.Add($"{(int)(score * 100)}% Match");

                var description = StripHtmlTags(detailedRecipe.Summary);
                if (description.Length > 150)
                    description = description.Substring(0, 147) + "...";

                recipeViewModels.Add(new RecipeCardViewModel
                {
                    SpoonacularRecipeId = spoonacularRecipe.Id,
                    Title = detailedRecipe.Title,
                    Description = description,
                    ImageUrl = !string.IsNullOrWhiteSpace(detailedRecipe.Image) 
                        ? detailedRecipe.Image 
                        : "/images/placeholder-recipe.jpg",
                    UsesCartItems = spoonacularRecipe.UsedIngredientCount,
                    Difficulty = difficulty,
                    PrepTime = prepTime,
                    Tags = tags.ToArray()
                });
            }

            return recipeViewModels;
        }

        private async Task<List<AddonProductViewModel>> BuildAddonViewModels(
            List<RecipeCardViewModel> recipes,
            IReadOnlyList<CartItemInfo> cartItems,
            IReadOnlyList<ProductModel> availableProducts)
        {
            var allIngredients = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var cartProductNames = BuildCartProductNameSet(cartItems);

            // Note: We'd need to fetch detailed recipes again here to get ingredients
            // For now, using empty set - you may want to cache the detailed recipes
            
            var suggestedIngredients = allIngredients.Take(10).ToList();
            var matchedProducts = await productMatchingService.MatchIngredientsToProductsAsync(
                suggestedIngredients,
                availableProducts);

            var cartProductIds = cartItems.Select(c => c.ProductId).ToHashSet();
            matchedProducts = matchedProducts.Where(p => !cartProductIds.Contains(p.ProductId)).ToList();

            return matchedProducts.Select(p => new AddonProductViewModel
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Price = p.Pricing,
                ImageUrl = !string.IsNullOrWhiteSpace(p.ImageUrl) ? p.ImageUrl : "/images/placeholder-product.jpg"
            }).ToList();
        }

        private string StripHtmlTags(string? html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            return System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty).Trim();
        }

        private class EmbeddingResponse
        {
            public EmbeddingData[]? Data { get; set; }
        }

        private class EmbeddingData
        {
            public double[]? Embedding { get; set; }
        }
    }
}