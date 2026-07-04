using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Abstractions.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EnhancementHub.Infrastructure.Services;

public sealed class RoslynRepositoryScanner : IGitRepositoryScanner
{
    private static readonly string[] ExcludedDirectories =
    [
        "bin", "obj", ".git", "node_modules", ".vs", "packages", "TestResults"
    ];

    public async Task<RepositoryScanResult> ScanAsync(string rootPath, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(rootPath))
        {
            throw new DirectoryNotFoundException($"Repository path not found: {rootPath}");
        }

        var csharpFiles = Directory
            .EnumerateFiles(rootPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !IsExcluded(f, rootPath))
            .ToList();

        var namespaces = new List<ScannedNamespace>();
        var classes = new List<ScannedClass>();
        var controllers = new List<ScannedController>();
        var dbContexts = new HashSet<string>(StringComparer.Ordinal);

        foreach (var file in csharpFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var source = await File.ReadAllTextAsync(file, cancellationToken);
            var tree = CSharpSyntaxTree.ParseText(source, cancellationToken: cancellationToken);
            var root = await tree.GetRootAsync(cancellationToken);
            var semanticModel = CSharpCompilation
                .Create("Scan")
                .AddSyntaxTrees(tree)
                .GetSemanticModel(tree);

            foreach (var ns in root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>())
            {
                var name = ns.Name.ToString();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    namespaces.Add(new ScannedNamespace
                    {
                        Name = name,
                        FilePath = GetRelativePath(rootPath, file)
                    });
                }
            }

            foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                var className = classDecl.Identifier.Text;
                var nsName = GetNamespace(classDecl);
                var methods = classDecl.Members
                    .OfType<MethodDeclarationSyntax>()
                    .Select(m => m.Identifier.Text)
                    .Distinct()
                    .ToList();

                var baseTypes = classDecl.BaseList?.Types
                    .Select(t => t.ToString())
                    .ToList() ?? [];

                var scanned = new ScannedClass
                {
                    Name = className,
                    Namespace = nsName,
                    FilePath = GetRelativePath(rootPath, file),
                    IsStatic = classDecl.Modifiers.Any(SyntaxKind.StaticKeyword),
                    IsPartial = classDecl.Modifiers.Any(SyntaxKind.PartialKeyword),
                    Methods = methods,
                    BaseTypes = baseTypes
                };
                classes.Add(scanned);

                if (InheritsFromDbContext(classDecl, semanticModel))
                {
                    dbContexts.Add($"{nsName}.{className}");
                }

                if (IsApiController(classDecl, semanticModel))
                {
                    var actions = classDecl.Members
                        .OfType<MethodDeclarationSyntax>()
                        .Where(m => m.Modifiers.Any(SyntaxKind.PublicKeyword))
                        .Select(m => m.Identifier.Text)
                        .ToList();

                    controllers.Add(new ScannedController
                    {
                        Name = className,
                        Namespace = nsName,
                        FilePath = GetRelativePath(rootPath, file),
                        Actions = actions
                    });
                }
            }
        }

        return new RepositoryScanResult
        {
            RootPath = rootPath,
            Namespaces = namespaces
                .GroupBy(n => n.Name)
                .Select(g => g.First())
                .OrderBy(n => n.Name)
                .ToList(),
            Classes = classes.OrderBy(c => c.Namespace).ThenBy(c => c.Name).ToList(),
            Controllers = controllers.OrderBy(c => c.Name).ToList(),
            DbContextTypes = dbContexts.OrderBy(x => x).ToList(),
            TotalFilesScanned = csharpFiles.Count
        };
    }

    private static bool IsExcluded(string filePath, string rootPath)
    {
        var relative = Path.GetRelativePath(rootPath, filePath);
        var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return parts.Any(p => ExcludedDirectories.Contains(p, StringComparer.OrdinalIgnoreCase));
    }

    private static string GetRelativePath(string root, string file) =>
        Path.GetRelativePath(root, file).Replace('\\', '/');

    private static string GetNamespace(SyntaxNode node)
    {
        var ns = node.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
        return ns?.Name.ToString() ?? string.Empty;
    }

    private static bool InheritsFromDbContext(ClassDeclarationSyntax classDecl, SemanticModel semanticModel)
    {
        if (classDecl.BaseList is null)
        {
            return false;
        }

        foreach (var baseType in classDecl.BaseList.Types)
        {
            var symbol = semanticModel.GetSymbolInfo(baseType.Type).Symbol as INamedTypeSymbol;
            var name = symbol?.ToDisplayString() ?? baseType.Type.ToString();
            if (name.Contains("DbContext", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return classDecl.Identifier.Text.EndsWith("DbContext", StringComparison.Ordinal);
    }

    private static bool IsApiController(ClassDeclarationSyntax classDecl, SemanticModel semanticModel)
    {
        foreach (var attrList in classDecl.AttributeLists)
        {
            foreach (var attr in attrList.Attributes)
            {
                var name = attr.Name.ToString();
                if (name is "ApiController" or "ApiControllerAttribute"
                    || name.EndsWith(".ApiController", StringComparison.Ordinal)
                    || name.EndsWith(".ApiControllerAttribute", StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return classDecl.Identifier.Text.EndsWith("Controller", StringComparison.Ordinal)
            && classDecl.Modifiers.Any(SyntaxKind.PublicKeyword);
    }
}
