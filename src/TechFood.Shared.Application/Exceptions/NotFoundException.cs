namespace TechFood.Shared.Application.Exceptions;

public class NotFoundException : ApplicationException
{
    public NotFoundException(string message)
        : base(message)
    { }

    public NotFoundException(string message, System.Exception innerException)
        : base(message, innerException)
    { }
}
