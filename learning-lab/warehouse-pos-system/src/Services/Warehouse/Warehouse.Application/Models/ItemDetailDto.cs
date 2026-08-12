using Warehouse.Application.Features.Items;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Models
{
    // The full shape for "here's everything about one item" — a detail
    // screen or a barcode scan lookup. Deliberately a separate type from
    // ItemSummaryDto rather than the same type with Barcodes/Units/Variants
    // left empty for list rows: fetching those three collections is a
    // per-item cost that a list endpoint returning many items shouldn't pay
    // for every row it's not going to show.
    public class ItemDetailDto
    {
        public int Id { get; set; }
        public string Sku { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Description { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public bool IsActive { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public int BaseUnitOfMeasureId { get; set; }
        public string BaseUnitOfMeasureCode { get; set; } = null!;
        public int? ParentItemId { get; set; }

        // Null unless a Promotion (C5) is active for this item right now.
        // UnitPrice above is always the price to actually CHARGE — the
        // discounted one, when a promotion applies — so a caller that only
        // reads UnitPrice (every call site that existed before C5) keeps
        // working unchanged; these two are purely for callers that want to
        // show "was X, now Y" (the POS receipt, the admin item panel).
        public decimal? OriginalUnitPrice { get; set; }
        public int? ActivePromotionId { get; set; }

        public IReadOnlyList<ItemBarcodeDto> Barcodes { get; set; } = Array.Empty<ItemBarcodeDto>();
        public IReadOnlyList<ItemUnitDto> Units { get; set; } = Array.Empty<ItemUnitDto>();
        public IReadOnlyList<ItemSummaryDto> Variants { get; set; } = Array.Empty<ItemSummaryDto>();

        // effectivePrice defaults to null — every call site that predates
        // C5 (CreateItemCommandHandler, AddItemBarcode/AddItemUnit
        // handlers) has no promotion to consider and just gets the item's
        // raw UnitPrice, unchanged. Only ResolveBarcodeQueryHandler and
        // GetItemByIdQueryHandler pass one in.
        public static ItemDetailDto FromEntity(
            Item item,
            IEnumerable<ItemBarcode> barcodes,
            IEnumerable<ItemUnit> units,
            IEnumerable<Item> variants,
            EffectivePrice? effectivePrice = null)
        {
            return new ItemDetailDto
            {
                Id = item.Id,
                Sku = item.Sku,
                Name = item.Name,
                Description = item.Description,
                UnitPrice = effectivePrice?.UnitPrice ?? item.UnitPrice,
                OriginalUnitPrice = effectivePrice?.OriginalUnitPrice,
                ActivePromotionId = effectivePrice?.PromotionId,
                IsActive = item.IsActive,
                CategoryId = item.CategoryId,
                CategoryName = item.Category.Name,
                BaseUnitOfMeasureId = item.BaseUnitOfMeasureId,
                BaseUnitOfMeasureCode = item.BaseUnitOfMeasure.Code,
                ParentItemId = item.ParentItemId,
                Barcodes = barcodes.Select(ItemBarcodeDto.FromEntity).ToList(),
                Units = units.Select(ItemUnitDto.FromEntity).ToList(),
                Variants = variants.Select(ItemSummaryDto.FromEntity).ToList(),
            };
        }
    }
}
