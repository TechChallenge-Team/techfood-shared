# TechFood Shared - Pacotes NuGet

Este repositório contém pacotes compartilhados do projeto TechFood que são publicados no GitHub Packages.

## 📦 Pacotes Disponíveis

### TechFood.Shared.Domain

Contém entidades, validações, enums e objetos de valor compartilhados.

**Instalação:**

```bash
dotnet add package TechFood.Shared.Domain --version 1.0.0
```

### TechFood.Shared.Application

Contém exceções customizadas, extensões e recursos de aplicação compartilhados.

**Instalação:**

```bash
dotnet add package TechFood.Shared.Application --version 1.0.0
```

### TechFood.Shared.Infra

Contém configurações de persistência, Entity Framework, consistência eventual e extensões de infraestrutura.

**Instalação:**

```bash
dotnet add package TechFood.Shared.Infra --version 1.0.0
```

### TechFood.Shared.Presentation

Contém extensões, filtros, configurações de Swagger e recursos para camada de apresentação ASP.NET Core.

**Instalação:**

```bash
dotnet add package TechFood.Shared.Presentation --version 1.0.0
```

## 🔧 Configuração

### 1. Configurar autenticação no GitHub Packages

Para usar os pacotes, você precisa configurar a autenticação com o GitHub Packages.

#### Criar um Personal Access Token (PAT)

1. Acesse: https://github.com/settings/tokens
2. Clique em "Generate new token" → "Generate new token (classic)"
3. Selecione o escopo `read:packages`
4. Copie o token gerado

#### Configurar o NuGet.config

Crie ou edite o arquivo `NuGet.config` na raiz do seu projeto:

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
      <add key="Username" value="SEU_USUARIO_GITHUB" />
      <add key="ClearTextPassword" value="SEU_TOKEN_PAT" />
    </github>
  </packageSourceCredentials>
</configuration>
```

**Importante:** Não commite o arquivo `NuGet.config` com credenciais! Use variáveis de ambiente ou configuração local.

#### Alternativa: Usar variáveis de ambiente

No seu terminal ou CI/CD:

```bash
# Linux/macOS
export NUGET_AUTH_TOKEN=seu_token_aqui

# Windows PowerShell
$env:NUGET_AUTH_TOKEN="seu_token_aqui"

# Adicionar source
dotnet nuget add source https://nuget.pkg.github.com/TechChallenge-Team/index.json \
  --name github \
  --username SEU_USUARIO \
  --password $NUGET_AUTH_TOKEN \
  --store-password-in-clear-text
```

### 2. Instalar os pacotes

```bash
# Instalar todos os pacotes
dotnet add package TechFood.Shared.Domain
dotnet add package TechFood.Shared.Application
dotnet add package TechFood.Shared.Infra
dotnet add package TechFood.Shared.Presentation
```

## 🚀 Pipeline de Publicação

A pipeline automatiza o processo de build, test e publicação dos pacotes:

### Triggers

- **Push para `main`**: Publica versão beta (1.0.X-beta)
- **Tags `v*.*.*`**: Publica versão estável (ex: v1.0.0)
- **Pull Requests**: Executa build e testes com coverage

### Versionamento

#### Versão Beta (Push para main)

```bash
git add .
git commit -m "feat: nova funcionalidade"
git push origin main
# Gera: 1.0.{run_number}-beta
```

#### Versão Estável (Tag)

```bash
git tag v1.0.0
git push origin v1.0.0
# Gera: 1.0.0
```

### Jobs da Pipeline

1. **build-and-test**: Compila a solution e executa testes
2. **pack-nuget**: Cria os pacotes .nupkg
3. **publish-nuget**: Publica no GitHub Packages e cria release

## 📖 Uso nos Projetos

### Exemplo: Usar Domain e Application

```csharp
using TechFood.Shared.Domain.Enums;
using TechFood.Shared.Domain.Common.Exceptions;
using TechFood.Shared.Application.Exceptions;

public class MeuServico
{
    public void ProcessarPedido(int pedidoId)
    {
        if (pedidoId <= 0)
            throw new ApplicationException("ID inválido");

        var status = OrderStatusType.Preparando;
        // ... sua lógica
    }
}
```

### Exemplo: Usar Presentation (Startup/Program.cs)

```csharp
using TechFood.Shared.Presentation.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Adicionar extensões do TechFood.Shared.Presentation
builder.Services.AddTechFoodSwagger();
builder.Services.AddTechFoodFilters();

var app = builder.Build();

app.UseTechFoodRequestPipeline();
app.Run();
```

## 🔄 Atualizando Pacotes

```bash
# Atualizar um pacote específico
dotnet add package TechFood.Shared.Domain --version 1.0.5

# Atualizar todos os pacotes TechFood
dotnet list package | grep TechFood | awk '{print $2}' | xargs -I {} dotnet add package {}
```

## 🛠️ Desenvolvimento Local

Para desenvolver e testar localmente antes de publicar:

```bash
# Build dos pacotes localmente
dotnet pack src/TechFood.Shared.Domain/TechFood.Shared.Domain.csproj -o ./local-packages
dotnet pack src/TechFood.Shared.Application/TechFood.Shared.Application.csproj -o ./local-packages
dotnet pack src/TechFood.Shared.Infra/TechFood.Shared.Infra.csproj -o ./local-packages
dotnet pack src/TechFood.Shared.Presentation/TechFood.Shared.Presentation.csproj -o ./local-packages

# Adicionar source local
dotnet nuget add source ./local-packages --name local

# Instalar do source local
dotnet add package TechFood.Shared.Domain --source local
```

## 📝 Notas

- Os pacotes são privados e requerem autenticação
- Versões beta são geradas automaticamente no push para `main`
- Versões estáveis são criadas via tags Git
- A pipeline requer permissão `packages: write` no GitHub
- Coverage mínimo: 50%

## 🤝 Contribuindo

1. Crie uma branch: `git checkout -b feature/minha-feature`
2. Commit suas mudanças: `git commit -am 'feat: minha feature'`
3. Push para a branch: `git push origin feature/minha-feature`
4. Abra um Pull Request
5. Após merge na `main`, será publicada uma versão beta
6. Para release estável, crie uma tag: `git tag v1.0.1 && git push origin v1.0.1`

## 📚 Links Úteis

- [GitHub Packages Documentation](https://docs.github.com/en/packages)
- [NuGet Package Documentation](https://docs.microsoft.com/en-us/nuget/)
- [Semantic Versioning](https://semver.org/)
