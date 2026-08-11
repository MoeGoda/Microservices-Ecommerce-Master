using Common.Exceptions;

namespace Warehouse.Application.Exceptions
{
    // This is the exact case Common.Exceptions.IHasStatusCode's own comment
    // anticipated back in B1: a Warehouse-specific business rule ("a manual
    // stock adjustment can't take QuantityOnHand negative") opting into
    // GlobalExceptionHandler's shared ProblemDetails handling without
    // needing any change to the shared Common.Exceptions library.
    public class InsufficientStockException : Exception, IHasStatusCode
    {
        public int StatusCode => 409;

        public InsufficientStockException(string itemName, string locationName, int quantityOnHand, int requestedChange)
            : base($"Cannot apply a change of {requestedChange} to '{itemName}' at '{locationName}': only {quantityOnHand} on hand.")
        {
        }
    }
}
