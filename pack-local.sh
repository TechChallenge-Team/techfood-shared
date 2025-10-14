#!/bin/bash

# Script para fazer build e pack dos pacotes NuGet localmente
# Útil para testes antes de publicar

VERSION="${1:-1.0.0-local}"
OUTPUT_DIR="${2:-./local-packages}"

echo "🏗️  Building and packing TechFood.Shared packages..."
echo "Version: $VERSION"
echo "Output: $OUTPUT_DIR"
echo ""

# Criar diretório de output
mkdir -p "$OUTPUT_DIR"

# Limpar diretório de output
echo "🧹 Cleaning output directory..."
rm -f "$OUTPUT_DIR"/*.nupkg

# Array com os projetos
projects=(
    "src/TechFood.Shared.Domain/TechFood.Shared.Domain.csproj"
    "src/TechFood.Shared.Application/TechFood.Shared.Application.csproj"
    "src/TechFood.Shared.Infra/TechFood.Shared.Infra.csproj"
    "src/TechFood.Shared.Presentation/TechFood.Shared.Presentation.csproj"
)

success=true

# Build e pack cada projeto
for project in "${projects[@]}"; do
    project_name=$(basename "$project" .csproj)
    echo "📦 Packing $project_name..."
    
    dotnet pack "$project" \
        --configuration Release \
        --output "$OUTPUT_DIR" \
        /p:Version="$VERSION" \
        /p:PackageVersion="$VERSION" \
        --verbosity minimal
    
    if [ $? -ne 0 ]; then
        echo "❌ Failed to pack $project_name"
        success=false
    else
        echo "✅ $project_name packed successfully"
    fi
    echo ""
done

if [ "$success" = true ]; then
    echo "🎉 All packages created successfully!"
    echo ""
    echo "📋 Generated packages:"
    ls -lh "$OUTPUT_DIR"/*.nupkg | awk '{print "  - " $9}'
    echo ""
    echo "💡 To use these packages locally, run:"
    echo "   dotnet nuget add source $(realpath $OUTPUT_DIR) --name local"
    echo "   dotnet add package TechFood.Shared.Domain --version $VERSION --source local"
else
    echo "❌ Some packages failed to build"
    exit 1
fi
