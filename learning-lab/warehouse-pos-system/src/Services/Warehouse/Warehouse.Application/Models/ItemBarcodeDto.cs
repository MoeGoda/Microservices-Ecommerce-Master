using Warehouse.Domain.Entities;

namespace Warehouse.Application.Models
{
    public class ItemBarcodeDto
    {
        public int Id { get; set; }
        public string Barcode { get; set; } = null!;
        public string BarcodeType { get; set; } = null!;
        public bool IsPrimary { get; set; }

        public static ItemBarcodeDto FromEntity(ItemBarcode itemBarcode)
        {
            return new ItemBarcodeDto
            {
                Id = itemBarcode.Id,
                Barcode = itemBarcode.Barcode,
                BarcodeType = itemBarcode.BarcodeType.ToString(),
                IsPrimary = itemBarcode.IsPrimary,
            };
        }
    }
}
