using System;

namespace TechFood.Shared.Domain.Common.Dto;

public class PagingRequest<TSort> : PagingRequest where TSort : struct, IConvertible
{
    public TSort Sort { get; set; }
}
