using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Remote;
using Moq;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

public class FindAllDeclarationsTests_Sonnet_3_5
{
    [Fact]
    public async Task ThrowsArgumentNullException_WhenProjectIsNull()
    {
        // Arrange
        Project? project = null;
        var query = SearchQuery.Create("test", SearchKind.Exact);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => DeclarationFinder.FindAllDeclarationsWithNormalQueryAsync(
                project!, query, SymbolFilter.All, CancellationToken.None));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public async Task ReturnsEmptyArray_WhenSearchNameIsWhitespace(string searchName)
    {
        // Arrange
        var mockProject = CreateMockProject();
        var query = SearchQuery.Create(searchName, SearchKind.Exact);

        // Act
        var result = await DeclarationFinder.FindAllDeclarationsWithNormalQueryAsync(
            mockProject, query, SymbolFilter.All, CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ThrowsInvalidOperationException_WhenQueryKindIsCustom()
    {
        // Arrange
        var mockProject = CreateMockProject();
        var query = SearchQuery.CreateCustom(name => true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => DeclarationFinder.FindAllDeclarationsWithNormalQueryAsync(
                mockProject, query, SymbolFilter.All, CancellationToken.None));
    }

    [Fact]
    public async Task UsesRemoteService_WhenAvailable()
    {
        // Arrange
        var expectedSymbols = ImmutableArray.Create<ISymbol>();
        var mockProject = CreateMockProject();
        var mockRemoteClient = new Mock<RemoteHostClient>();
        var mockRemoteService = new Mock<IRemoteSymbolFinderService>();

        mockRemoteService
            .Setup(s => s.FindAllDeclarationsWithNormalQueryAsync(
                It.IsAny<Checksum>(),
                It.IsAny<ProjectId>(),
                It.IsAny<string>(),
                It.IsAny<SearchKind>(),
                It.IsAny<SymbolFilter>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ImmutableArray<SerializableSymbolAndProjectId>.Empty);

        mockRemoteClient
            .Setup(c => c.TryInvokeAsync(
                It.IsAny<Solution>(),
                It.IsAny<Func<IRemoteSymbolFinderService, SolutionInfo, CancellationToken, Task<ImmutableArray<SerializableSymbolAndProjectId>>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSymbols);

        SetupMockRemoteHostClient(mockProject, mockRemoteClient.Object);

        var query = SearchQuery.Create("TestSymbol", SearchKind.Exact);

        // Act
        var result = await DeclarationFinder.FindAllDeclarationsWithNormalQueryAsync(
            mockProject, query, SymbolFilter.All, CancellationToken.None);

        // Assert
        Assert.Same(expectedSymbols, result);
        mockRemoteClient.Verify(
            c => c.TryInvokeAsync(
                It.IsAny<Solution>(),
                It.IsAny<Func<IRemoteSymbolFinderService, SolutionInfo, CancellationToken, Task<ImmutableArray<SerializableSymbolAndProjectId>>>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(SearchKind.Exact)]
    [InlineData(SearchKind.ExactIgnoreCase)]
    [InlineData(SearchKind.Fuzzy)]
    public async Task HandlesAllNonCustomSearchKinds(SearchKind searchKind)
    {
        // Arrange
        var mockProject = CreateMockProject();
        var query = SearchQuery.Create("TestSymbol", searchKind);

        // Act
        var result = await DeclarationFinder.FindAllDeclarationsWithNormalQueryAsync(
            mockProject, query, SymbolFilter.All, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task HandlesCancellation()
    {
        // Arrange
        var mockProject = CreateMockProject();
        var query = SearchQuery.Create("TestSymbol", SearchKind.Exact);
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => DeclarationFinder.FindAllDeclarationsWithNormalQueryAsync(
                mockProject, query, SymbolFilter.All, cancellationTokenSource.Token));
    }

    [Theory]
    [InlineData(SymbolFilter.None)]
    [InlineData(SymbolFilter.Namespace)]
    [InlineData(SymbolFilter.Type)]
    [InlineData(SymbolFilter.Member)]
    [InlineData(SymbolFilter.TypeAndMember)]
    [InlineData(SymbolFilter.All)]
    public async Task HandlesAllSymbolFilterTypes(SymbolFilter filter)
    {
        // Arrange
        var mockProject = CreateMockProject();
        var query = SearchQuery.Create("TestSymbol", SearchKind.Exact);

        // Act
        var result = await DeclarationFinder.FindAllDeclarationsWithNormalQueryAsync(
            mockProject, query, filter, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task FallsBackToLocalSearch_WhenRemoteServiceFails()
    {
        // Arrange
        var mockProject = CreateMockProject();
        var mockRemoteClient = new Mock<RemoteHostClient>();

        mockRemoteClient
            .Setup(c => c.TryInvokeAsync(
                It.IsAny<Solution>(),
                It.IsAny<Func<IRemoteSymbolFinderService, , CancellationToken, Task<ImmutableArray<SerializableSymbolAndProjectId>>>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Remote service failure"));

        SetupMockRemoteHostClient(mockProject, mockRemoteClient.Object);

        var query = SearchQuery.Create("TestSymbol", SearchKind.Exact);

        // Act
        var result = await DeclarationFinder.FindAllDeclarationsWithNormalQueryAsync(
            mockProject, query, SymbolFilter.All, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    private static Project CreateMockProject()
    {
        var mockSolution = new Mock<Solution>();
        var mockProjectState = new Mock<ProjectState>();
        var mockProject = new Mock<Project>(mockSolution.Object, mockProjectState.Object);

        mockProject.Setup(p => p.Solution).Returns(mockSolution.Object);
        mockProject.Setup(p => p.SupportsCompilation).Returns(true);

        return mockProject.Object;
    }

    private static void SetupMockRemoteHostClient(Project project, IRemoteHostClient remoteClient)
    {
        var mockRemoteHostClientFactory = new Mock<IRemoteHostClientFactory>();
        mockRemoteHostClientFactory
            .Setup(f => f.TryGetClient(project, It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteClient);

        // Set up services using RemoteHostClient's static method
        var solution = project.Solution;
        var remoteClient = await RemoteHostClient.TryGetClientAsync(project, CancellationToken.None);

        var mockProject = Mock.Get(project);
        mockProject.Setup(p => p.Solution).Returns(solution);
    }
}
