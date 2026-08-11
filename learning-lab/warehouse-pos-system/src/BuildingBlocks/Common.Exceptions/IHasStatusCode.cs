namespace Common.Exceptions
{
    // Lets GlobalExceptionHandler (a separate package, Common.ExceptionHandling)
    // map an exception to an HTTP status code without knowing about every
    // exception type in every microservice. A future service (Warehouse's
    // "InsufficientStockException", say) can opt into the same handling
    // just by implementing this interface — it never has to be added to
    // this shared library. That's the Open/Closed Principle in practice:
    // the handler is closed for modification, but open for any service to extend.
    //
    // StatusCode is a plain int, not Microsoft.AspNetCore.Http.StatusCodes,
    // deliberately: this project has zero ASP.NET Core dependency, so an
    // Application layer (which should never reference a web framework) can
    // safely reference it for shared exception *types* without dragging a
    // FrameworkReference into its build graph. The ASP.NET-specific piece
    // that turns these into ProblemDetails responses lives in the sibling
    // Common.ExceptionHandling package instead.
    public interface IHasStatusCode
    {
        int StatusCode { get; }
    }
}
