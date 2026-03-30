using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CORNY.Models.ViewModels
{
    public class ProductFormViewModel
    {
        public int ProductId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? Sizing { get; set; }

        public int InventoryQuantity { get; set; }

        public bool IsForSale { get; set; }

        public decimal Pricing { get; set; }

        public int FarmId { get; set; }

        public int CategoryId { get; set; }

        public string? ImageUrl { get; set; }

        [ValidateNever]
        public IList<IFormFile>? ImageFiles { get; set; }

        public IList<ProductImageItemViewModel> ExistingImages { get; set; } = new List<ProductImageItemViewModel>();
    }
}