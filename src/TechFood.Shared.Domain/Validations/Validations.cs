using System;
using TechFood.Shared.Domain.Exceptions;

namespace TechFood.Shared.Domain.Validations;

public class Validations
{
    public static void ThrowIfEmpty(string value, string message)
    {
        if (value == null || value.Trim().Length == 0)
        {
            throw new DomainException(message);
        }
    }

    public static void ThrowValidGuid(Guid guid, string message)
    {
        if (guid == new Guid())
        {
            throw new DomainException(message);
        }
    }

    public static void ThrowIsGreaterThanZero(decimal value, string message)
    {
        if (value < 0)
        {
            throw new DomainException(message);
        }
    }

    public static void ThrowIsGreaterThanZero(int value, string message)
    {
        if (value < 0)
        {
            throw new DomainException(message);
        }
    }

    public static void ThrowObjectIsNull(object? value, string message)
    {
        if (value == null)
        {
            throw new DomainException(message);
        }
    }

    public static void ThrowIfLessThan(int value, int value1, string message)
    {
        if (value < value1)
        {
            throw new DomainException(message);
        }
    }
}
