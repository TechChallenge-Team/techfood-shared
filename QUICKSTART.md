# 🚀 Guia Rápido - Publicação de Pacotes NuGet

## ✅ Checklist antes de publicar

- [ ] Todos os testes estão passando
- [ ] Coverage acima de 50%
- [ ] Código revisado e sem warnings
- [ ] Documentação atualizada
- [ ] CHANGELOG.md atualizado (se aplicável)
- [ ] Versão incrementada corretamente

## 📦 Fluxo de Publicação

### 1️⃣ Desenvolvimento e Testes Locais

```powershell
# Build local
dotnet build TechFood.Shared.sln --configuration Release

# Rodar testes
dotnet test TechFood.Shared.sln --configuration Release

# Criar pacotes locais para testar
.\pack-local.ps1 -Version "1.0.0-dev"

# Ou no Linux/Mac
./pack-local.sh 1.0.0-dev
```

### 2️⃣ Testar Pacotes em Outro Projeto

```powershell
# Adicionar source local
dotnet nuget add source C:\path\to\techfood-shared\local-packages --name local

# Instalar pacote local
cd C:\path\to\seu-projeto
dotnet add package TechFood.Shared.Domain --version 1.0.0-dev --source local
```

### 3️⃣ Publicar Versão Beta (Development)

```bash
# Commitar mudanças
git add .
git commit -m "feat: adiciona nova funcionalidade X"

# Push para main (gera versão beta automaticamente)
git push origin main
```

**Resultado:** Pipeline gera e publica `1.0.{run_number}-beta`

### 4️⃣ Publicar Versão Estável (Production)

```bash
# Certifique-se que está na branch main e atualizada
git checkout main
git pull origin main

# Criar e enviar tag de versão
git tag v1.0.0
git push origin v1.0.0
```

**Resultado:** Pipeline gera e publica `1.0.0` + cria GitHub Release

## 🔢 Versionamento (Semantic Versioning)

```
MAJOR.MINOR.PATCH
  1  .  0  .  0
```

- **MAJOR**: Mudanças incompatíveis na API
- **MINOR**: Nova funcionalidade compatível
- **PATCH**: Correção de bugs compatível

### Exemplos

```bash
# Correção de bug
git tag v1.0.1
git push origin v1.0.1

# Nova funcionalidade
git tag v1.1.0
git push origin v1.1.0

# Breaking change
git tag v2.0.0
git push origin v2.0.0
```

## 🔐 Configuração de Credenciais (Uma única vez)

### Criar Personal Access Token (PAT)

1. Acesse: https://github.com/settings/tokens
2. Click "Generate new token" → "Generate new token (classic)"
3. Marque: `read:packages` e `write:packages`
4. Copie o token

### Configurar localmente

```powershell
# PowerShell
$env:GITHUB_TOKEN = "seu_token_aqui"

dotnet nuget add source https://nuget.pkg.github.com/TechChallenge-Team/index.json `
  --name github `
  --username seu_usuario `
  --password $env:GITHUB_TOKEN `
  --store-password-in-clear-text
```

```bash
# Bash
export GITHUB_TOKEN="seu_token_aqui"

dotnet nuget add source https://nuget.pkg.github.com/TechChallenge-Team/index.json \
  --name github \
  --username seu_usuario \
  --password $GITHUB_TOKEN \
  --store-password-in-clear-text
```

## 📥 Consumindo os Pacotes

### Em projetos da organização

```bash
# Instalar pacote específico
dotnet add package TechFood.Shared.Domain --version 1.0.0

# Instalar todos
dotnet add package TechFood.Shared.Domain
dotnet add package TechFood.Shared.Application
dotnet add package TechFood.Shared.Infra
dotnet add package TechFood.Shared.Presentation
```

### Atualizar para versão mais recente

```bash
dotnet add package TechFood.Shared.Domain --version 1.1.0
```

## 🔍 Verificar Pacotes Publicados

### Via GitHub

https://github.com/orgs/TechChallenge-Team/packages

### Via CLI

```bash
# Listar versões disponíveis
dotnet nuget list source
dotnet nuget search TechFood --source github
```

## 🐛 Troubleshooting

### Erro: 401 Unauthorized

```bash
# Verificar se o token está configurado
dotnet nuget list source

# Reconfigurar credenciais
dotnet nuget remove source github
dotnet nuget add source https://nuget.pkg.github.com/TechChallenge-Team/index.json \
  --name github \
  --username seu_usuario \
  --password $GITHUB_TOKEN \
  --store-password-in-clear-text
```

### Erro: Package already exists

Você está tentando publicar uma versão que já existe. Incremente a versão:

```bash
# Ao invés de v1.0.0, use:
git tag v1.0.1
git push origin v1.0.1
```

### Pipeline falhou no teste de coverage

```bash
# Rodar testes localmente para ver o coverage
dotnet test --collect:"XPlat Code Coverage"

# Instalar reportgenerator
dotnet tool install --global dotnet-reportgenerator-globaltool

# Gerar relatório
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage-report -reporttypes:Html

# Abrir relatório
start coverage-report/index.html  # Windows
open coverage-report/index.html   # Mac
xdg-open coverage-report/index.html  # Linux
```

## 📚 Referências

- [GitHub Packages Docs](https://docs.github.com/en/packages)
- [Semantic Versioning](https://semver.org/)
- [NuGet Package Documentation](https://docs.microsoft.com/en-us/nuget/)
- [GitHub Actions](https://docs.github.com/en/actions)

## 💬 Contato

Para questões ou problemas, abra uma issue no repositório.
