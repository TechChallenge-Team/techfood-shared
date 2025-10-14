# Template de Configuração para Consumidores

## Para projetos que vão USAR os pacotes TechFood.Shared

### 1. Adicionar NuGet.config ao seu projeto

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
    <add key="github" value="https://nuget.pkg.github.com/TechChallenge-Team/index.json" />
  </packageSources>

  <packageSourceCredentials>
    <github>
      <add key="Username" value="%GITHUB_USERNAME%" />
      <add key="ClearTextPassword" value="%GITHUB_TOKEN%" />
    </github>
  </packageSourceCredentials>
</configuration>
```

### 2. Configurar variáveis de ambiente

**Windows (PowerShell):**

```powershell
# Criar arquivo .env.ps1
@"
`$env:GITHUB_USERNAME = "seu_usuario"
`$env:GITHUB_TOKEN = "seu_token_pat"
"@ | Out-File -FilePath .env.ps1

# Adicionar ao .gitignore
Add-Content -Path .gitignore -Value "`n# Environment`n.env.ps1`n.env"

# Carregar variáveis
. .\.env.ps1
```

**Linux/Mac (Bash):**

```bash
# Criar arquivo .env
cat > .env << EOF
export GITHUB_USERNAME="seu_usuario"
export GITHUB_TOKEN="seu_token_pat"
EOF

# Adicionar ao .gitignore
echo "" >> .gitignore
echo "# Environment" >> .gitignore
echo ".env" >> .gitignore

# Carregar variáveis
source .env
```

### 3. Script de configuração automática

**setup-nuget.ps1** (Windows):

```powershell
param(
    [Parameter(Mandatory=$true)]
    [string]$Username,

    [Parameter(Mandatory=$true)]
    [string]$Token
)

Write-Host "🔧 Configurando GitHub Packages..." -ForegroundColor Cyan

# Remover source existente (se houver)
dotnet nuget remove source github 2>$null

# Adicionar novo source
dotnet nuget add source https://nuget.pkg.github.com/TechChallenge-Team/index.json `
    --name github `
    --username $Username `
    --password $Token `
    --store-password-in-clear-text

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ GitHub Packages configurado com sucesso!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Agora você pode instalar os pacotes:" -ForegroundColor Yellow
    Write-Host "  dotnet add package TechFood.Shared.Domain" -ForegroundColor Gray
    Write-Host "  dotnet add package TechFood.Shared.Application" -ForegroundColor Gray
    Write-Host "  dotnet add package TechFood.Shared.Infra" -ForegroundColor Gray
    Write-Host "  dotnet add package TechFood.Shared.Presentation" -ForegroundColor Gray
} else {
    Write-Host "❌ Erro ao configurar GitHub Packages" -ForegroundColor Red
    exit 1
}
```

**setup-nuget.sh** (Linux/Mac):

```bash
#!/bin/bash

if [ $# -ne 2 ]; then
    echo "Uso: ./setup-nuget.sh <username> <token>"
    exit 1
fi

USERNAME=$1
TOKEN=$2

echo "🔧 Configurando GitHub Packages..."

# Remover source existente (se houver)
dotnet nuget remove source github 2>/dev/null

# Adicionar novo source
dotnet nuget add source https://nuget.pkg.github.com/TechChallenge-Team/index.json \
    --name github \
    --username "$USERNAME" \
    --password "$TOKEN" \
    --store-password-in-clear-text

if [ $? -eq 0 ]; then
    echo "✅ GitHub Packages configurado com sucesso!"
    echo ""
    echo "Agora você pode instalar os pacotes:"
    echo "  dotnet add package TechFood.Shared.Domain"
    echo "  dotnet add package TechFood.Shared.Application"
    echo "  dotnet add package TechFood.Shared.Infra"
    echo "  dotnet add package TechFood.Shared.Presentation"
else
    echo "❌ Erro ao configurar GitHub Packages"
    exit 1
fi
```

### 4. Configuração no Program.cs

```csharp
using TechFood.Shared.Domain.Common.Interfaces;
using TechFood.Shared.Application.Exceptions;
using TechFood.Shared.Infra.Persistence;
using TechFood.Shared.Infra.UoW;
using TechFood.Shared.Presentation.Extensions;
using TechFood.Shared.Presentation.Filters;

var builder = WebApplication.CreateBuilder(args);

// ===== Controllers =====
builder.Services.AddControllers(options =>
{
    // Adicionar filtros do TechFood
    options.Filters.Add<ValidationFilter>();
    options.Filters.Add<ExceptionFilter>();
})
.AddTechFoodJsonOptions(); // Naming policy e configurações JSON

// ===== Swagger =====
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddTechFoodSwagger(options =>
{
    options.Title = "Minha API";
    options.Version = "v1";
    options.Description = "API usando TechFood.Shared";
});

// ===== Database =====
builder.Services.AddDbContext<MeuDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// ===== TechFood Infrastructure =====
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ===== CORS =====
builder.Services.AddTechFoodCors(options =>
{
    options.AllowedOrigins = new[] { "http://localhost:3000" };
});

// ===== Authentication =====
builder.Services.AddTechFoodAuthentication(builder.Configuration);

// ===== Seus serviços =====
builder.Services.AddScoped<IMeuServico, MeuServico>();

var app = builder.Build();

// ===== Pipeline =====
if (app.Environment.IsDevelopment())
{
    app.UseTechFoodSwagger();
}

app.UseTechFoodRequestPipeline(); // HTTPS, Auth, CORS, etc.
app.MapControllers();

app.Run();
```

### 5. Configuração no appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MeuDb;Trusted_Connection=true;"
  },
  "JwtSettings": {
    "SecretKey": "sua_chave_secreta_aqui_minimo_32_caracteres",
    "Issuer": "MeuApp",
    "Audience": "MeuApp",
    "ExpirationInMinutes": 60
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### 6. Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar NuGet.config para autenticação
COPY ["NuGet.config", "."]

# Copiar csproj e restaurar
COPY ["MeuProjeto/MeuProjeto.csproj", "MeuProjeto/"]
RUN dotnet restore "MeuProjeto/MeuProjeto.csproj"

# Copiar código fonte
COPY . .
WORKDIR "/src/MeuProjeto"
RUN dotnet build "MeuProjeto.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MeuProjeto.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MeuProjeto.dll"]
```

### 7. GitHub Actions CI/CD

```yaml
name: CI/CD

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  build:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "8.0.x"

      - name: Configure GitHub Packages
        run: |
          dotnet nuget add source https://nuget.pkg.github.com/TechChallenge-Team/index.json \
            --name github \
            --username ${{ github.actor }} \
            --password ${{ secrets.GITHUB_TOKEN }} \
            --store-password-in-clear-text

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --configuration Release --no-restore

      - name: Test
        run: dotnet test --no-build --verbosity normal
```

### 8. .gitignore essencial

```gitignore
# Build
bin/
obj/
*.user
*.suo

# NuGet
*.nupkg
local-packages/
NuGet.config

# Environment
.env
.env.ps1
.env.local

# IDE
.vs/
.vscode/
.idea/
*.swp
```

## Checklist de Setup

- [ ] Criar Personal Access Token no GitHub com escopo `read:packages`
- [ ] Executar script de setup (setup-nuget.ps1 ou setup-nuget.sh)
- [ ] Adicionar NuGet.config ao .gitignore
- [ ] Instalar pacotes TechFood.Shared
- [ ] Configurar Program.cs com extensions do TechFood
- [ ] Configurar appsettings.json
- [ ] Testar build local
- [ ] Configurar CI/CD se necessário

## Troubleshooting

### Erro 401 Unauthorized

```bash
# Verificar configuração
dotnet nuget list source

# Reconfigurar
dotnet nuget remove source github
./setup-nuget.sh <seu_usuario> <seu_token>
```

### Pacote não encontrado

```bash
# Limpar cache
dotnet nuget locals all --clear

# Tentar novamente
dotnet restore --force
```

### Build falha no Docker

```dockerfile
# Passar token como build arg
ARG GITHUB_TOKEN
RUN dotnet nuget add source https://nuget.pkg.github.com/TechChallenge-Team/index.json \
    --name github \
    --username github \
    --password ${GITHUB_TOKEN} \
    --store-password-in-clear-text
```

## Links Úteis

- [Documentação Completa](./NUGET.md)
- [Exemplos de Uso](./EXAMPLES.md)
- [GitHub Packages Docs](https://docs.github.com/en/packages)
