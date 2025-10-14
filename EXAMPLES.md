# 📖 Exemplos de Uso dos Pacotes TechFood.Shared

## TechFood.Shared.Domain

### Usando Enums

```csharp
using TechFood.Shared.Domain.Enums;

public class PedidoService
{
    public void ProcessarPedido(Guid pedidoId)
    {
        var pedido = _repository.GetById(pedidoId);

        // Usar enums do domínio
        if (pedido.Status == OrderStatusType.Recebido)
        {
            pedido.Status = OrderStatusType.EmPreparacao;
        }

        // Tipo de pagamento
        if (pedido.Pagamento.Tipo == PaymentType.Pix)
        {
            ProcessarPagamentoPix(pedido);
        }
    }
}
```

### Usando ValueObjects e Validações

```csharp
using TechFood.Shared.Domain.Common.ValueObjects;
using TechFood.Shared.Domain.Common.Validations;

public class Cliente
{
    public Cpf Cpf { get; private set; }
    public Email Email { get; private set; }

    public Cliente(string cpf, string email)
    {
        // ValueObjects já fazem validação automática
        Cpf = new Cpf(cpf);
        Email = new Email(email);
    }
}

// Uso
try
{
    var cliente = new Cliente("123.456.789-00", "email@example.com");
}
catch (DomainException ex)
{
    Console.WriteLine($"Erro de domínio: {ex.Message}");
}
```

### Usando Entidades Base

```csharp
using TechFood.Shared.Domain.Common.Entities;

public class Produto : Entity
{
    public string Nome { get; private set; }
    public decimal Preco { get; private set; }
    public bool Ativo { get; private set; }

    public Produto(string nome, decimal preco)
    {
        Id = Guid.NewGuid();
        Nome = nome;
        Preco = preco;
        Ativo = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Desativar()
    {
        Ativo = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

## TechFood.Shared.Application

### Usando Exceções Customizadas

```csharp
using TechFood.Shared.Application.Exceptions;

public class ProdutoService
{
    public async Task<ProdutoDto> GetProdutoAsync(Guid id)
    {
        var produto = await _repository.GetByIdAsync(id);

        if (produto == null)
            throw new NotFoundException($"Produto com ID {id} não encontrado");

        if (!produto.Ativo)
            throw new ApplicationException("Produto inativo não pode ser consultado");

        return _mapper.Map<ProdutoDto>(produto);
    }
}
```

### Usando Extensions

```csharp
using TechFood.Shared.Application.Extensions;
using Microsoft.AspNetCore.Http;

public class UsuarioService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public string GetCurrentUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user?.GetUserId(); // Extension method
    }

    public string GetCurrentUserEmail()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user?.GetUserEmail(); // Extension method
    }
}
```

### Usando LINQ Extensions

```csharp
using TechFood.Shared.Application.Extensions;

public class ProdutoQuery
{
    public async Task<List<Produto>> GetProdutosPaginadosAsync(int page, int pageSize)
    {
        var query = _context.Produtos
            .Where(p => p.Ativo)
            .OrderBy(p => p.Nome);

        // Extension de paginação
        var produtos = await query
            .Paginate(page, pageSize)
            .ToListAsync();

        return produtos;
    }
}
```

## TechFood.Shared.Infra

### Configurando UnitOfWork

```csharp
using TechFood.Shared.Infra.UoW;
using Microsoft.EntityFrameworkCore;

// Startup.cs ou Program.cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddDbContext<MeuDbContext>(options =>
        options.UseSqlServer(Configuration.GetConnectionString("Default")));

    // Registrar UnitOfWork
    services.AddScoped<IUnitOfWork, UnitOfWork>();
}

// Uso em Service
public class PedidoService
{
    private readonly IUnitOfWork _uow;

    public PedidoService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> CriarPedidoAsync(PedidoDto dto)
    {
        var pedido = new Pedido(dto);
        await _repository.AddAsync(pedido);

        // Commit transacional
        return await _uow.CommitAsync();
    }
}
```

### Usando Consistência Eventual

```csharp
using TechFood.Shared.Infra.EventualConsistency;

public class PedidoCriadoHandler : INotificationHandler<PedidoCriadoEvent>
{
    private readonly IEventualConsistencyService _eventService;

    public async Task Handle(PedidoCriadoEvent notification, CancellationToken cancellationToken)
    {
        // Publicar evento para processamento assíncrono
        await _eventService.PublishAsync(notification);
    }
}
```

### Configurando Persistence

```csharp
using TechFood.Shared.Infra.Persistence;
using Microsoft.Extensions.DependencyInjection;

// Extension methods para configuração
public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Adicionar configurações de persistência
        services.AddTechFoodPersistence(configuration);

        // Adicionar repositórios
        services.AddScoped<IProdutoRepository, ProdutoRepository>();
        services.AddScoped<IPedidoRepository, PedidoRepository>();

        return services;
    }
}
```

## TechFood.Shared.Presentation

### Configurando Swagger

```csharp
using TechFood.Shared.Presentation.Extensions;

// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Adicionar Swagger com configurações padrão TechFood
builder.Services.AddTechFoodSwagger(options =>
{
    options.Title = "TechFood API";
    options.Version = "v1";
    options.Description = "API do sistema TechFood";
});

var app = builder.Build();

// Usar Swagger em desenvolvimento
if (app.Environment.IsDevelopment())
{
    app.UseTechFoodSwagger();
}

app.Run();
```

### Usando Filtros Globais

```csharp
using TechFood.Shared.Presentation.Filters;

// Program.cs
builder.Services.AddControllers(options =>
{
    // Adicionar filtros customizados do TechFood
    options.Filters.Add<ValidationFilter>();
    options.Filters.Add<ExceptionFilter>();
    options.Filters.Add<LoggingFilter>();
});
```

### Usando Naming Policy Customizado

```csharp
using TechFood.Shared.Presentation.NamingPolicy;

// Program.cs
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Usar naming policy customizado
        options.JsonSerializerOptions.PropertyNamingPolicy =
            new SnakeCaseNamingPolicy();
    });
```

### Configurando Pipeline de Request

```csharp
using TechFood.Shared.Presentation.Extensions;

// Program.cs
var app = builder.Build();

// Usar pipeline padrão do TechFood
app.UseTechFoodRequestPipeline();

// Equivalente a:
// app.UseHttpsRedirection();
// app.UseAuthentication();
// app.UseAuthorization();
// app.UseExceptionHandler();
// app.UseCors();

app.Run();
```

### Controller com Features do TechFood

```csharp
using Microsoft.AspNetCore.Mvc;
using TechFood.Shared.Application.Exceptions;
using TechFood.Shared.Domain.Common.Interfaces;

[ApiController]
[Route("api/[controller]")]
public class ProdutosController : ControllerBase
{
    private readonly IProdutoService _service;

    public ProdutosController(IProdutoService service)
    {
        _service = service;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProdutoDto>> Get(Guid id)
    {
        try
        {
            var produto = await _service.GetByIdAsync(id);
            return Ok(produto);
        }
        catch (NotFoundException ex)
        {
            // Filtro global trata automaticamente
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<ProdutoDto>> Create([FromBody] CreateProdutoDto dto)
    {
        // Validação automática pelo ValidationFilter
        var produto = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(Get), new { id = produto.Id }, produto);
    }
}
```

## Exemplo Completo: API Mínima

```csharp
// Program.cs
using TechFood.Shared.Domain.Enums;
using TechFood.Shared.Application.Exceptions;
using TechFood.Shared.Infra.Persistence;
using TechFood.Shared.Presentation.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configurar serviços
builder.Services.AddControllers()
    .AddTechFoodJsonOptions();

builder.Services.AddTechFoodSwagger();
builder.Services.AddTechFoodAuthentication(builder.Configuration);
builder.Services.AddTechFoodCors();

// Infraestrutura
builder.Services.AddTechFoodPersistence(builder.Configuration);
builder.Services.AddScoped<IProdutoService, ProdutoService>();

var app = builder.Build();

// Configurar pipeline
if (app.Environment.IsDevelopment())
{
    app.UseTechFoodSwagger();
}

app.UseTechFoodRequestPipeline();
app.MapControllers();

app.Run();
```

## Testando com os Pacotes

```csharp
using Xunit;
using TechFood.Shared.Domain.Enums;
using TechFood.Shared.Domain.Common.ValueObjects;
using TechFood.Shared.Application.Exceptions;

public class ProdutoServiceTests
{
    [Fact]
    public async Task DeveRetornarNotFoundException_QuandoProdutoNaoExiste()
    {
        // Arrange
        var service = CreateService();
        var produtoId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => service.GetByIdAsync(produtoId)
        );
    }

    [Fact]
    public void DeveCriarCpfValido()
    {
        // Arrange
        var cpfString = "123.456.789-00";

        // Act
        var cpf = new Cpf(cpfString);

        // Assert
        Assert.NotNull(cpf);
        Assert.Equal(cpfString, cpf.ToString());
    }
}
```

## 🎯 Boas Práticas

1. **Sempre use as exceções do Application** ao invés de Exception genérica
2. **Use ValueObjects** para validação automática de dados
3. **Implemente IUnitOfWork** para transações complexas
4. **Configure os filtros globais** para tratamento consistente de erros
5. **Use os extensions methods** para código mais limpo
6. **Mantenha consistência** usando os enums do domínio

## 📚 Mais Exemplos

Veja os projetos de referência:

- techfood-backend
- techfood-self-order
- techfood-admin

Cada um deles usa esses pacotes compartilhados.
