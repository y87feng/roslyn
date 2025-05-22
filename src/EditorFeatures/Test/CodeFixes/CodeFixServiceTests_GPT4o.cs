extern alias WORKSPACES;
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Moq;
using WORKSPACES::Microsoft.CodeAnalysis.ErrorLogger;
using Xunit;
namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeFixes;

[UseExportProvider]
public class CodeFixServiceTests_GPT4o
{
    private CodeFixService CreateCodeFixService()
    {
        var diagnosticAnalyzerService = new Mock<IDiagnosticAnalyzerService>();
        var errorLoggers = Enumerable.Empty<Lazy<IErrorLoggerService>>();
        var fixers = Enumerable.Empty<Lazy<CodeFixProvider, CodeChangeProviderMetadata>>();
        var configurationProviders = Enumerable.Empty<Lazy<IConfigurationFixProvider, CodeChangeProviderMetadata>>();

        return new CodeFixService(
            diagnosticAnalyzerService.Object,
            errorLoggers,
            fixers,
            configurationProviders);
    }

    [Fact]
    public async Task StreamFixesAsync_EmptyDiagnostics_ReturnsNoResults()
    {
        // Arrange
        var codeFixService = CreateCodeFixService();
        var document = CreateTestDocument();
        var range = new TextSpan(0, 10);
        var priorityProvider = new Mock<ICodeActionRequestPriorityProvider>();
        priorityProvider.Setup(p => p.Priority).Returns(CodeActionRequestPriority.Default);

        // Act
        var result = codeFixService.StreamFixesAsync(document, range, priorityProvider.Object, CancellationToken.None);
        var collections = new List<CodeFixCollection>();
        await foreach (var codeFixCollection in result)
        {
            collections.Add(codeFixCollection);
        }

        // Assert
        Assert.Empty(collections);
    }

    [Fact]
    public async Task StreamFixesAsync_WithDiagnostics_ReturnsFixes()
    {
        // Arrange
        var codeFixService = CreateCodeFixService();
        var document = CreateTestDocument();
        var range = new TextSpan(0, 10);
        var diagnostics = new List<DiagnosticData>
        {
            new DiagnosticData(
                id: "TestDiagnosticId",
                category: "TestCategory",
                message: "TestMessage",
                severity: DiagnosticSeverity.Warning,
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                warningLevel: 1,
                customTags: ImmutableArray<string>.Empty,
                properties: ImmutableDictionary<string, string?>.Empty,
                projectId: null,
                location: new DiagnosticDataLocation(
                    unmappedFileSpan: new FileLinePositionSpan(
                        "TestFile.cs",
                        new LinePosition(0, 0),
                        new LinePosition(0, 10)
                    )
                ),
                additionalLocations: ImmutableArray<DiagnosticDataLocation>.Empty,
                description: null,
                helpLink: null,
                title: "TestTitle",
                isSuppressed: false)
        };

        var diagnosticService = new Mock<IDiagnosticAnalyzerService>();
        diagnosticService
            .Setup(d => d.GetDiagnosticsForSpanAsync(document, range, It.IsAny<Func<string, bool>>(), true, false, It.IsAny<ICodeActionRequestPriorityProvider>(), DiagnosticKind.All, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(diagnostics.ToImmutableArray());

        var priorityProvider = new Mock<ICodeActionRequestPriorityProvider>();
        priorityProvider.Setup(p => p.Priority).Returns(CodeActionRequestPriority.Default);

        // Act
        var result = codeFixService.StreamFixesAsync(document, range, priorityProvider.Object, CancellationToken.None);
        var collections = new List<CodeFixCollection>();
        await foreach (var codeFixCollection in result)
        {
            collections.Add(codeFixCollection);
        }

        // Assert
        Assert.NotEmpty(collections);
        Assert.Equal("TestDiagnosticId", collections.First().FirstDiagnostic.Id);
    }

    [Fact]
    public async Task StreamFixesAsync_Cancellation_ThrowsException()
    {
        // Arrange
        var codeFixService = CreateCodeFixService();
        var document = CreateTestDocument();
        var range = new TextSpan(0, 10);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var priorityProvider = new Mock<ICodeActionRequestPriorityProvider>();
        priorityProvider.Setup(p => p.Priority).Returns(CodeActionRequestPriority.Default);

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
        {
            await foreach (var _ in codeFixService.StreamFixesAsync(document, range, priorityProvider.Object, cts.Token))
            {
                // Intentionally left empty
            }
        });
    }

    private TextDocument CreateTestDocument()
    {
        // Create a concrete Project object
        var projectId = ProjectId.CreateNewId();
        var solution = new AdhocWorkspace().CurrentSolution.AddProject(projectId, "TestProject", "TestAssembly", LanguageNames.CSharp);
        var project = solution.GetProject(projectId)!;

        var textDocumentState = new Mock<TextDocumentState>();
        // Return a new TextDocument
        return new TextDocument(project, textDocumentState.Object, TextDocumentKind.Document);
    }
}
