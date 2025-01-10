using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Remote;
using Moq;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests; 

public class FindAllDeclarationsTests_GPT4o
{
    private readonly Mock<RemoteHostClient> _mockRemoteHostClient;

    public FindAllDeclarationsTests_GPT4o()
    {
        _mockRemoteHostClient = new Mock<RemoteHostClient>(MockBehavior.Strict);
    }

    [Fact]
    public async Task FindAllDeclarationsWithNormalQueryAsync_ShouldThrowArgumentNullException_WhenProjectIsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            DeclarationFinder.FindAllDeclarationsWithNormalQueryAsync(null, SearchQuery.Create("Test", SearchKind.Exact), SymbolFilter.All, CancellationToken.None));
    }

    [Fact]
    public async Task FindAllDeclarationsWithNormalQueryAsync_ShouldThrowContractException_WhenCustomSearchQueryIsUsed()
    {
        var project = CreateSampleProject();
        var query = SearchQuery.CreateCustom(s => s.Contains("test"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            DeclarationFinder.FindAllDeclarationsWithNormalQueryAsync(project, query, SymbolFilter.All, CancellationToken.None));
    }

    [Fact]
    public async Task FindAllDeclarationsWithNormalQueryAsync_ShouldReturnEmpty_WhenQueryNameIsNullOrWhitespace()
    {
        var project = CreateSampleProject();

        var result = await DeclarationFinder.FindAllDeclarationsWithNormalQueryAsync(project, SearchQuery.Create(string.Empty, SearchKind.Exact), SymbolFilter.All, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task FindAllDeclarationsWithNormalQueryAsync_ShouldCallRemoteService_WhenRemoteClientExists()
    {
        var project = CreateSampleProject();

        _mockRemoteHostClient.Setup(client => client.TryInvokeAsync<IRemoteSymbolFinderService, ImmutableArray<SerializableSymbolAndProjectId>>(
            It.IsAny<Func<IRemoteSymbolFinderService, CancellationToken, ValueTask<ImmutableArray<SerializableSymbolAndProjectId>>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Optional<ImmutableArray<SerializableSymbolAndProjectId>>());

        var result = await DeclarationFinder.FindAllDeclarationsWithNormalQueryAsync(
            project, SearchQuery.Create("Test", SearchKind.Exact), SymbolFilter.All, CancellationToken.None);

        Assert.Empty(result); // Assuming the service returns no symbols

        _mockRemoteHostClient.Verify(client => client.TryInvokeAsync<IRemoteSymbolFinderService, ImmutableArray<SerializableSymbolAndProjectId>>(
            It.IsAny<Func<IRemoteSymbolFinderService, CancellationToken, ValueTask<ImmutableArray<SerializableSymbolAndProjectId>>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FindAllDeclarationsWithNormalQueryAsync_ShouldCallLocalSearch_WhenRemoteClientDoesNotExist()
    {
        var project = CreateSampleProject();

        var result = await DeclarationFinder.FindAllDeclarationsWithNormalQueryAsync(
            project, SearchQuery.Create("Test", SearchKind.Exact), SymbolFilter.All, CancellationToken.None);

        Assert.Empty(result); // Assuming no symbols are found in the local search
    }

    [Fact]
    public async Task FindAllDeclarationsWithNormalQueryAsync_ShouldReturnSymbols_FromRemoteService()
    {
        var project = CreateSampleProject();

        // Simulate remote symbols
        var fakeSymbol = CreateFakeSymbol("TestSymbol");
        var remoteResults = ImmutableArray.Create(new SerializableSymbolAndProjectId(SymbolKey.CreateString(fakeSymbol, CancellationToken.None), project.Id));

        _mockRemoteHostClient.Setup(client => client.TryInvokeAsync<IRemoteSymbolFinderService, ImmutableArray<SerializableSymbolAndProjectId>>(
            It.IsAny<Func<IRemoteSymbolFinderService, CancellationToken, ValueTask<ImmutableArray<SerializableSymbolAndProjectId>>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteResults);

        var result = await DeclarationFinder.FindAllDeclarationsWithNormalQueryAsync(
            project, SearchQuery.Create("TestSymbol", SearchKind.Exact), SymbolFilter.All, CancellationToken.None);

        Assert.NotEmpty(result);
        Assert.Contains(result, symbol => symbol.Name == "TestSymbol");
    }

    [Fact]
    public async Task FindAllDeclarationsWithNormalQueryAsync_ShouldReturnSymbols_FromLocalSearch()
    {
        var project = CreateSampleProjectWithSymbols();

        var result = await DeclarationFinder.FindAllDeclarationsWithNormalQueryAsync(
            project, SearchQuery.Create("LocalSymbol", SearchKind.Exact), SymbolFilter.All, CancellationToken.None);

        Assert.NotEmpty(result);
        Assert.Contains(result, symbol => symbol.Name == "LocalSymbol");
    }

    // Helper to create a project with symbols
    private Project CreateSampleProjectWithSymbols()
    {
        var solution = new AdhocWorkspace().CurrentSolution;
        var projectId = ProjectId.CreateNewId();

        var projectInfo = ProjectInfo.Create(
            projectId,
            VersionStamp.Default,
            "TestProject",
            "TestAssembly",
            LanguageNames.CSharp);

        var project = solution.AddProject(projectInfo).GetProject(projectId);

        var code = @"
        namespace TestNamespace
        {
            public class LocalSymbol { }
        }
    ";
        var documentId = DocumentId.CreateNewId(projectId);
        solution = solution.AddDocument(documentId, "TestDocument.cs", code);
        return solution.GetProject(projectId);
    }

    // Helper to create a fake symbol
    private ISymbol CreateFakeSymbol(string name)
    {
        var mockSymbol = new Mock<ISymbol>();
        mockSymbol.Setup(s => s.Name).Returns(name);
        return mockSymbol.Object;
    }

    private Project CreateSampleProject()
    {
        var solution = new AdhocWorkspace().CurrentSolution;
        var projectId = ProjectId.CreateNewId();

        var projectInfo = ProjectInfo.Create(
            projectId,
            VersionStamp.Default,
            "TestProject",
            "TestAssembly",
            LanguageNames.CSharp);

        return solution.AddProject(projectInfo).GetProject(projectId);
    }
}
