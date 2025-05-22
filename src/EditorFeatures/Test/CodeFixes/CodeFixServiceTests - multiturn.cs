using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Xunit;

public class CodeFixService_StreamFixesAsyncTests
{
    private readonly CodeFixService _codeFixService;
    private readonly TextDocument _document;
    private readonly ICodeActionRequestPriorityProvider _priorityProvider;

    public CodeFixService_StreamFixesAsyncTests()
    {
        // Create an in-memory solution and project to use real TextDocument
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        _document = workspace.AddDocument(project.Id, "TestDocument.cs", SourceText.From("// Test Code"));

        // Create a default priority provider
        _priorityProvider = new DefaultCodeActionRequestPriorityProvider();

        // Initialize the CodeFixService with real dependencies
        _codeFixService = new CodeFixService(
            diagnosticAnalyzerService: new TestDiagnosticAnalyzerService(),
            loggers: Enumerable.Empty<Lazy<IErrorLoggerService>>(),
            fixers: GetSampleFixers(),
            configurationProviders: Enumerable.Empty<Lazy<IConfigurationFixProvider, CodeChangeProviderMetadata>>()
        );
    }

    private static IEnumerable<Lazy<CodeFixProvider, CodeChangeProviderMetadata>> GetSampleFixers()
    {
        var fixer = new SampleCodeFixProvider();
        var metadata = new CodeChangeProviderMetadata(new Dictionary<string, object>
        {
            { nameof(CodeChangeProviderMetadata.Name), "SampleFixer" },
            { nameof(CodeChangeProviderMetadata.Languages), new[] { LanguageNames.CSharp } }
        });
        return new[] { new Lazy<CodeFixProvider, CodeChangeProviderMetadata>(() => fixer, metadata) };
    }

    [Fact]
    public async Task StreamFixesAsync_EmptyDiagnostics_ShouldYieldNoFixes()
    {
        // Arrange
        var range = new TextSpan(0, 10);
        var cancellationToken = CancellationToken.None;

        // Act
        var fixes = _codeFixService.StreamFixesAsync(_document, range, _priorityProvider, cancellationToken);
        var result = await fixes.ToListAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task StreamFixesAsync_WithValidDiagnostics_ShouldYieldFixes()
    {
        // Arrange
        var range = new TextSpan(0, 10);
        var cancellationToken = CancellationToken.None;

        var diagnosticData = new DiagnosticData(
            "TestId",
            "TestCategory",
            "Test Message",
            DiagnosticSeverity.Warning,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            warningLevel: 0,
            ImmutableArray<string>.Empty,
            ImmutableDictionary<string, string?>.Empty,
            _document.Project.Id,
            new DiagnosticDataLocation(
                new FileLinePositionSpan("TestFile", new LinePositionSpan(new LinePosition(0, 0), new LinePosition(1, 0))),
                _document.Id),
            ImmutableArray<DiagnosticDataLocation>.Empty);

        // Inject diagnostics into the analyzer service
        ((TestDiagnosticAnalyzerService)_codeFixService.DiagnosticAnalyzerService).SetDiagnostics(new[] { diagnosticData });

        // Act
        var fixes = _codeFixService.StreamFixesAsync(_document, range, _priorityProvider, cancellationToken);
        var result = await fixes.ToListAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("SampleFixer", result[0].Fixer.GetType().Name);
    }
}

internal class TestDiagnosticAnalyzerService : IDiagnosticAnalyzerService
{
    private ImmutableArray<DiagnosticData> _diagnostics = ImmutableArray<DiagnosticData>.Empty;

    public void SetDiagnostics(IEnumerable<DiagnosticData> diagnostics)
    {
        _diagnostics = diagnostics.ToImmutableArray();
    }

    public Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForSpanAsync(TextDocument document, TextSpan range, Func<string, bool>? shouldIncludeDiagnosticPredicate, bool includeCompilerDiagnostics, bool includeSuppressedDiagnostics, ICodeActionRequestPriorityProvider priorityProvider, DiagnosticKind diagnosticKind, bool isExplicit, CancellationToken cancellationToken)
    {
        return Task.FromResult(_diagnostics);
    }
}

internal class SampleCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("TestId");

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var codeAction = CodeAction.Create("Sample Fix", ct => Task.FromResult(context.Document));
        context.RegisterCodeFix(codeAction, context.Diagnostics.First());
        return Task.CompletedTask;
    }
}
