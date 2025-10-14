using System.Collections.Generic;

namespace TechFood.Shared.Domain.Common.Dto;

public class PagingReponse<TEntity> where TEntity : class
{
    public int Page { get; set; }

    public int Size { get; set; }

    public int Total { get; set; }

    public IEnumerable<TEntity>? Items { get; set; }
}
