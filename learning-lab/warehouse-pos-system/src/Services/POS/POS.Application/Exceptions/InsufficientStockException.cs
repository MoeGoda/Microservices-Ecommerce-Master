using Common.Exceptions;

namespace POS.Application.Exceptions
{
    // POS's own version of the exact case Warehouse's InsufficientStockException
    // (B2) already made real once: a business rule specific to this
    // service ("can't sell more of an item than Warehouse says is on
    // hand at this register's location") opting into GlobalExceptionHandler's
    // shared ProblemDetails handling via IHasStatusCode, without any change
    // to Common.Exceptions. Deliberately not a shared type — Warehouse's
    // version is about a stock adjustment going negative; this one is
    // about a sale line exceeding what's available. Different callers,
    // different messages, same interface.
    public class InsufficientStockException : Exception, IHasStatusCode
    {
        public int StatusCode => 409;

        public InsufficientStockException(string sku, int requestedQuantity, int availableQuantity)
            : base($"Cannot sell {requestedQuantity} of '{sku}': only {availableQuantity} available at this location.")
        {
        }
    }
}
