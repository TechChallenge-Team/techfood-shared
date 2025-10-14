# Script para fazer build e pack dos pacotes NuGet localmente
# Útil para testes antes de publicar

param(
    [Parameter(Mandatory=$false)]
    [string]$Version = "1.0.0-local",
    
    [Parameter(Mandatory=$false)]
    [string]$OutputDir = "./local-packages"
)

Write-Host "🏗️  Building and packing TechFood.Shared packages..." -ForegroundColor Cyan
Write-Host "Version: $Version" -ForegroundColor Yellow
Write-Host "Output: $OutputDir" -ForegroundColor Yellow
Write-Host ""

# Criar diretório de output
if (!(Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
}

# Limpar diretório de output
Write-Host "🧹 Cleaning output directory..." -ForegroundColor Gray
Remove-Item "$OutputDir\*.nupkg" -ErrorAction SilentlyContinue

# Array com os projetos
$projects = @(
    "src/TechFood.Shared.Domain/TechFood.Shared.Domain.csproj",
    "src/TechFood.Shared.Application/TechFood.Shared.Application.csproj",
    "src/TechFood.Shared.Infra/TechFood.Shared.Infra.csproj",
    "src/TechFood.Shared.Presentation/TechFood.Shared.Presentation.csproj"
)

$success = $true

# Build e pack cada projeto
foreach ($project in $projects) {
    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($project)
    Write-Host "📦 Packing $projectName..." -ForegroundColor Green
    
    dotnet pack $project `
        --configuration Release `
        --output $OutputDir `
        /p:Version=$Version `
        /p:PackageVersion=$Version `
        --verbosity minimal
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Failed to pack $projectName" -ForegroundColor Red
        $success = $false
    } else {
        Write-Host "✅ $projectName packed successfully" -ForegroundColor Green
    }
    Write-Host ""
}

if ($success) {
    Write-Host "🎉 All packages created successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📋 Generated packages:" -ForegroundColor Cyan
    Get-ChildItem "$OutputDir\*.nupkg" | ForEach-Object {
        Write-Host "  - $($_.Name)" -ForegroundColor White
    }
    Write-Host ""
    Write-Host "💡 To use these packages locally, run:" -ForegroundColor Yellow
    Write-Host "   dotnet nuget add source $(Resolve-Path $OutputDir) --name local" -ForegroundColor Gray
    Write-Host "   dotnet add package TechFood.Shared.Domain --version $Version --source local" -ForegroundColor Gray
} else {
    Write-Host "❌ Some packages failed to build" -ForegroundColor Red
    exit 1
}
