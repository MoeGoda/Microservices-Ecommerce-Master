namespace Common.Exceptions
{
    public class NotFoundException : Exception, IHasStatusCode
    {
        public int StatusCode => 404;

        public NotFoundException(string entityName, object key)
            : base($"Entity \"{entityName}\" ({key}) was not found.")
        {
        }
    }
}
