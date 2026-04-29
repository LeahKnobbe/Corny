namespace BuissnessLogicLayer.Models
{
    public class SpoonacularRecipeSearchResponse
    {
        public SpoonacularRecipe[] Results { get; set; } = Array.Empty<SpoonacularRecipe>();
        public int Offset { get; set; }
        public int Number { get; set; }
        public int TotalResults { get; set; }
    }

    // Response from Spoonacular's findByIngredients endpoint
    public class SpoonacularRecipe
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public int UsedIngredientCount { get; set; }
        public int MissedIngredientCount { get; set; }
        // These are arrays of OBJECTS, not strings
        public SpoonacularIngredient[] UsedIngredients { get; set; } = Array.Empty<SpoonacularIngredient>();
        public SpoonacularIngredient[] MissedIngredients { get; set; } = Array.Empty<SpoonacularIngredient>();
    }

    // Ingredient object from Spoonacular
    public class SpoonacularIngredient
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Original { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
    }

    // Full recipe information with instructions
    public class SpoonacularRecipeInformation
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public int ReadyInMinutes { get; set; }
        public int Servings { get; set; }
        public string Summary { get; set; } = string.Empty;
        public bool Vegetarian { get; set; }
        public bool Vegan { get; set; }
        public bool GlutenFree { get; set; }
        public bool DairyFree { get; set; }
        public bool VeryHealthy { get; set; }
        public bool Cheap { get; set; }
        public bool VeryPopular { get; set; }
        public ExtendedIngredient[] ExtendedIngredients { get; set; } = Array.Empty<ExtendedIngredient>();
        public AnalyzedInstruction[] AnalyzedInstructions { get; set; } = Array.Empty<AnalyzedInstruction>();
        public string Instructions { get; set; } = string.Empty;
    }

    public class ExtendedIngredient
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Original { get; set; } = string.Empty;
        public string OriginalName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Unit { get; set; } = string.Empty;
    }

    public class AnalyzedInstruction
    {
        public string Name { get; set; } = string.Empty;
        public InstructionStep[] Steps { get; set; } = Array.Empty<InstructionStep>();
    }

    public class InstructionStep
    {
        public int Number { get; set; }
        public string Step { get; set; } = string.Empty;
    }
}