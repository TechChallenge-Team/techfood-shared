using System;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace TechFood.Shared.Infra.Extensions;

public class InfraOptions
{
    public Action<IServiceProvider, DbContextOptionsBuilder>? DbContext { get; set; }

    public Assembly? AssemblyLoad { get; set; }
}
