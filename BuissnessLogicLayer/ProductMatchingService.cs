using DataAccessLayer.Entities;

namespace BuissnessLogicLayer
{
    public class ProductMatchingService : IProductMatchingService
    {
        public Task<IReadOnlyList<ProductModel>> MatchIngredientsToProductsAsync(
            IReadOnlyList<string> ingredientNames,
            IReadOnlyList<ProductModel> allProducts)
        {
            var matched = new List<ProductModel>();

            foreach (var ingredient in ingredientNames)
            {
                var cleanIngredient = ingredient.Trim().ToLowerInvariant();

                // Skip if we already matched this product
                var alreadyMatched = matched.Select(m => m.ProductId).ToHashSet();

                // Try exact match first
                var exactMatch = allProducts.FirstOrDefault(p =>
                    !alreadyMatched.Contains(p.ProductId) &&
                    p.Name.Equals(cleanIngredient, StringComparison.OrdinalIgnoreCase) &&
                    p.IsForSale &&
                    p.InventoryQuantity > 0);

                if (exactMatch != null)
                {
                    matched.Add(exactMatch);
                    continue;
                }

                // Try partial match - ingredient word appears in product name
                var words = cleanIngredient.Split(new[] { ' ', ',', '-' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in words)
                {
                    if (word.Length < 3) continue; // Skip very short words

                    var partialMatch = allProducts.FirstOrDefault(p =>
                        !alreadyMatched.Contains(p.ProductId) &&
                        p.IsForSale &&
                        p.InventoryQuantity > 0 &&
                        p.Name.Contains(word, StringComparison.OrdinalIgnoreCase));

                    if (partialMatch != null)
                    {
                        matched.Add(partialMatch);
                        alreadyMatched.Add(partialMatch.ProductId);
                        break;
                    }
                }
            }

            // If we found very few matches, add some popular products as suggestions
            if (matched.Count < 3)
            {
                var popularProducts = allProducts
                    .Where(p => p.IsForSale && p.InventoryQuantity > 0 && !matched.Any(m => m.ProductId == p.ProductId))
                    .OrderByDescending(p => p.InventoryQuantity) // Assume higher inventory = more popular
                    .Take(5 - matched.Count)
                    .ToList();

                matched.AddRange(popularProducts);
            }

            return Task.FromResult<IReadOnlyList<ProductModel>>(matched);
        }
    }
}