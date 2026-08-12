using MediatR;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.Items.Queries.ResolveBarcode
{
    // What a POS scan (Phase C) or an admin "look up by barcode" box
    // actually calls. Returns null rather than throwing NotFoundException
    // on an unknown barcode — a shopper scanning something that isn't in
    // the catalog is an expected, everyday outcome, not an exceptional one.
    public class ResolveBarcodeQuery : IRequest<ItemDetailDto?>
    {
        public string Barcode { get; set; } = null!;
    }
}
