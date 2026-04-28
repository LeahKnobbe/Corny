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
    /// AI-powered recipe suggestion service using OpenAI and Spoonacular.
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

            // Read API key from User Secrets or Environment Variables
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
            string? filter = null)
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
                logger.LogInformation("Starting recipe suggestion generation with OpenAI and Spoonacular");

                // Step 1: Get recipe ideas from Spoonacular
                var ingredientNames = cartItems.Select(item => item.ProductName).ToList();
                logger.LogInformation("Searching Spoonacular for recipes with ingredients: {Ingredients}", string.Join(", ", ingredientNames));
                
                var spoonacularRecipes = await spoonacularService.SearchRecipesByIngredientsAsync(ingredientNames, 5);

                if (spoonacularRecipes?.Any() == true)
                {
                    logger.LogInformation("Found {Count} recipes from Spoonacular", spoonacularRecipes.Length);
                    foreach (var recipe in spoonacularRecipes.Take(3))
                    {
                        logger.LogInformation("Spoonacular Recipe: {Title}, ID: {Id}, Image: {Image}", recipe.Title, recipe.Id, recipe.Image);
                    }
                }
                else
                {
                    logger.LogWarning("No recipes found from Spoonacular");
                }

                // Step 2: Build OpenAI prompt
                var prompt = BuildOpenAIPrompt(cartItems, spoonacularRecipes, filter);

                // Step 3: Call OpenAI
                logger.LogInformation("Calling OpenAI for recipe suggestions");
                var openAiResponse = await CallOpenAIAsync(prompt);

                if (openAiResponse == null)
                {
                    logger.LogWarning("OpenAI API call failed. Using fallback.");
                    return await fallbackService.GetRecipeSuggestionsAsync(cartItems, availableProducts, filter);
                }

                // Step 4: Parse OpenAI response and fetch detailed Spoonacular data
                logger.LogInformation("Parsing OpenAI response and fetching detailed Spoonacular data for accurate times");
                var parsedResult = await ParseOpenAIResponseAsync(openAiResponse, spoonacularRecipes);

                logger.LogInformation("Generated {Count} recipe suggestions with accurate Spoonacular data", parsedResult.recipes.Count);
                foreach (var recipe in parsedResult.recipes)
                {
                    logger.LogInformation("FINAL RECIPE CARD: Title={Title}, PrepTime={Time}, Difficulty={Difficulty}, SpoonacularId={Id}", 
                        recipe.Title, recipe.PrepTime, recipe.Difficulty, recipe.SpoonacularRecipeId);
                }

                // Step 5: Match suggested add-ons to actual database products
                logger.LogInformation("Matching {Count} suggested ingredients to database products", parsedResult.suggestedIngredients.Count);
                var matchedProducts = await productMatchingService.MatchIngredientsToProductsAsync(
                    parsedResult.suggestedIngredients,
                    availableProducts);

                logger.LogInformation("Matched {Count} products from database", matchedProducts.Count);

                // Step 6: Convert matched products to AddonProductViewModel
                var addonViewModels = matchedProducts.Select(p => new AddonProductViewModel
                {
                    ProductId = p.ProductId,
                    Name = p.Name,
                    Price = p.Pricing,
                    ImageUrl = !string.IsNullOrWhiteSpace(p.ImageUrl) 
                        ? p.ImageUrl 
                        : "/images/placeholder-product.jpg"
                }).ToList();

                return new RecipeSuggestionResult
                {
                    Recipes = parsedResult.recipes,
                    SuggestedAddons = addonViewModels
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error generating AI recipe suggestions. Using fallback.");
                return await fallbackService.GetRecipeSuggestionsAsync(cartItems, availableProducts, filter);
            }
        }

        private string BuildOpenAIPrompt(
            IReadOnlyList<CartItemInfo> cartItems,
            SpoonacularRecipe[]? spoonacularRecipes,
            string? filter)
        {
            var cartItemsList = string.Join(", ", cartItems.Select(item => item.ProductName));
            
            var filterInstruction = filter switch
            {
                "high-protein" => "Focus on high-protein recipes.",
                "vegetarian" => "Only suggest vegetarian recipes.",
                "quick-meals" => "Only suggest recipes that take 30 minutes or less.",
                _ => "Suggest a variety of easy, healthy recipes."
            };

            var spoonacularInfo = spoonacularRecipes?.Any() == true
                ? $"\n\nAvailable recipes from Spoonacular:\n{JsonSerializer.Serialize(spoonacularRecipes.Take(3), new JsonSerializerOptions { WriteIndented = true })}"
                : "";

            return $@"You are a helpful recipe assistant for a farmer's market website called Corny.

The user has the following items in their cart:
{cartItemsList}

{filterInstruction}

{spoonacularInfo}

Please suggest 3 simple, beginner-friendly recipes that use these cart items. For each recipe, provide:
- A catchy title
- A short description (1-2 sentences)
- Estimated cooking time in minutes
- Difficulty level (Easy, Medium, or Hard)
- Which cart items are used
- A few additional ingredients that might be needed (suggest INGREDIENT NAMES ONLY like garlic, lemon, olive oil, lettuce, cheese, etc.)
- Tags like ""Uses 2 cart items"", ""Easy"", ""25 min"", etc.

IMPORTANT: 
- For suggested additional ingredients, only suggest BASIC INGREDIENT NAMES (garlic, lemon, etc.), NOT specific product brands
- Do NOT include imageUrl in your response - we will use Spoonacular images automatically

Return ONLY a valid JSON object in this exact format (no markdown, no code blocks, just pure JSON):
{{
  ""recipes"": [
    {{
      ""title"": ""Recipe Name"",
      ""description"": ""Brief description"",
      ""timeMinutes"": 25,
      ""difficulty"": ""Easy"",
      ""cartItemsUsed"": [""item1"", ""item2""],
      ""tags"": [""Uses 2 cart items"", ""Easy"", ""25 min""]
    }}
  ],
  ""addOns"": [""garlic"", ""lemon"", ""olive oil"", ""lettuce""]
}}";
        }

        private async Task<OpenAIResponse?> CallOpenAIAsync(string prompt)
        {
            try
            {
                var requestBody = new
                {
                    model = "gpt-3.5-turbo",
                    messages = new[]
                    {
                        new { role = "system", content = "You are a helpful recipe assistant. Always return valid JSON only. Do not include markdown code blocks." },
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.7,
                    max_tokens = 1500
                };

                var response = await httpClient.PostAsJsonAsync("chat/completions", requestBody);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    logger.LogError("OpenAI API error: {StatusCode}, Response: {Response}", response.StatusCode, errorContent);
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<OpenAIResponse>();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error calling OpenAI API");
                return null;
            }
        }

        private async Task<(IReadOnlyList<RecipeCardViewModel> recipes, IReadOnlyList<string> suggestedIngredients)> ParseOpenAIResponseAsync(
            OpenAIResponse? openAiResponse,
            SpoonacularRecipe[]? spoonacularRecipes)
        {
            if (openAiResponse?.Choices == null || !openAiResponse.Choices.Any())
            {
                logger.LogWarning("OpenAI response is empty or invalid");
                return (Array.Empty<RecipeCardViewModel>(), Array.Empty<string>());
            }

            try
            {
                var content = openAiResponse.Choices[0].Message.Content.Trim();
                logger.LogInformation("OpenAI raw response: {Content}", content.Substring(0, Math.Min(200, content.Length)));
                
                // Remove markdown code blocks if present
                if (content.StartsWith("```json"))
                {
                    content = content.Substring(7);
                }
                if (content.StartsWith("```"))
                {
                    content = content.Substring(3);
                }
                if (content.EndsWith("```"))
                {
                    content = content.Substring(0, content.Length - 3);
                }
                content = content.Trim();

                var aiResult = JsonSerializer.Deserialize<AIRecipeResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (aiResult == null)
                {
                    logger.LogWarning("Failed to deserialize OpenAI response");
                    return (Array.Empty<RecipeCardViewModel>(), Array.Empty<string>());
                }

                logger.LogInformation("OpenAI suggested {Count} recipes", aiResult.Recipes.Length);

                // Map to our view models and fetch detailed info from Spoonacular
                var recipeViewModels = new List<RecipeCardViewModel>();

                for (int index = 0; index < aiResult.Recipes.Length; index++)
                {
                    var recipe = aiResult.Recipes[index];
                    SpoonacularRecipe? spoonacularRecipe = null;
                    
                    // Try to find matching Spoonacular recipe
                    if (spoonacularRecipes != null && spoonacularRecipes.Any())
                    {
                        spoonacularRecipe = spoonacularRecipes.ElementAtOrDefault(index);
                        if (spoonacularRecipe != null)
                        {
                            logger.LogInformation("Matched OpenAI recipe '{AITitle}' with Spoonacular recipe '{SpoonTitle}' (ID: {Id})",
                                recipe.Title, spoonacularRecipe.Title, spoonacularRecipe.Id);
                        }
                    }
                    
                    // Use Spoonacular image if available
                    var imageUrl = !string.IsNullOrWhiteSpace(spoonacularRecipe?.Image) 
                        ? spoonacularRecipe.Image 
                        : "/images/placeholder-recipe.jpg";

                    // Fetch detailed recipe information to get ACCURATE time and difficulty
                    SpoonacularRecipeInformation? detailedRecipe = null;
                    if (spoonacularRecipe?.Id != null && spoonacularRecipe.Id > 0)
                    {
                        try
                        {
                            logger.LogInformation("🔍 Fetching DETAILED INFO for Spoonacular recipe ID {RecipeId}...", spoonacularRecipe.Id);
                            detailedRecipe = await spoonacularService.GetRecipeInformationAsync(spoonacularRecipe.Id);
                            
                            if (detailedRecipe != null)
                            {
                                logger.LogInformation("✅ GOT DETAILED INFO: Title='{Title}', ReadyInMinutes={Time}, Servings={Servings}",
                                    detailedRecipe.Title, detailedRecipe.ReadyInMinutes, detailedRecipe.Servings);
                            }
                            else
                            {
                                logger.LogWarning("⚠️ Detailed recipe info returned NULL for ID {RecipeId}", spoonacularRecipe.Id);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "❌ FAILED to fetch detailed recipe info for {RecipeId}", spoonacularRecipe.Id);
                        }
                    }
                    else
                    {
                        logger.LogWarning("⚠️ No Spoonacular ID available for recipe '{Title}', using OpenAI estimates", recipe.Title);
                    }

                    // Use Spoonacular's ACTUAL time if available, otherwise use OpenAI's estimate
                    string prepTime;
                    string difficulty;
                    
                    if (detailedRecipe != null && detailedRecipe.ReadyInMinutes > 0)
                    {
                        prepTime = $"{detailedRecipe.ReadyInMinutes} min";
                        difficulty = detailedRecipe.ReadyInMinutes <= 20 ? "Easy" : 
                                   detailedRecipe.ReadyInMinutes <= 45 ? "Medium" : "Hard";
                        logger.LogInformation("✅ USING SPOONACULAR DATA: PrepTime={PrepTime}, Difficulty={Difficulty}", prepTime, difficulty);
                    }
                    else
                    {
                        prepTime = $"{recipe.TimeMinutes} min";
                        difficulty = recipe.Difficulty;
                        logger.LogWarning("⚠️ USING OPENAI ESTIMATES: PrepTime={PrepTime}, Difficulty={Difficulty}", prepTime, difficulty);
                    }

                    logger.LogInformation("📝 Creating recipe card: Title={Title}, PrepTime={PrepTime}, Difficulty={Difficulty}, SpoonacularId={Id}",
                        recipe.Title, prepTime, difficulty, spoonacularRecipe?.Id);
                    
                    recipeViewModels.Add(new RecipeCardViewModel
                    {
                        SpoonacularRecipeId = spoonacularRecipe?.Id,
                        Title = recipe.Title,
                        Description = recipe.Description,
                        ImageUrl = imageUrl,
                        UsesCartItems = recipe.CartItemsUsed?.Length ?? 0,
                        Difficulty = difficulty,
                        PrepTime = prepTime,
                        Tags = recipe.Tags ?? Array.Empty<string>()
                    });
                }

                logger.LogInformation("✅ Successfully created {Count} recipe cards", recipeViewModels.Count);
                return (recipeViewModels, aiResult.AddOns ?? Array.Empty<string>());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error parsing OpenAI response");
                return (Array.Empty<RecipeCardViewModel>(), Array.Empty<string>());
            }
        }

        // OpenAI API response models
        private class OpenAIResponse
        {
            public OpenAIChoice[] Choices { get; set; } = Array.Empty<OpenAIChoice>();
        }

        private class OpenAIChoice
        {
            public OpenAIMessage Message { get; set; } = new();
        }

        private class OpenAIMessage
        {
            public string Content { get; set; } = string.Empty;
        }

        // AI response structure - matches what OpenAI returns
        private class AIRecipeResponse
        {
            public AIRecipe[] Recipes { get; set; } = Array.Empty<AIRecipe>();
            public string[] AddOns { get; set; } = Array.Empty<string>();
        }

        private class AIRecipe
        {
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public int TimeMinutes { get; set; }
            public string Difficulty { get; set; } = "Easy";
            public string[] CartItemsUsed { get; set; } = Array.Empty<string>();
            public string[] Tags { get; set; } = Array.Empty<string>();
        }
    }
}