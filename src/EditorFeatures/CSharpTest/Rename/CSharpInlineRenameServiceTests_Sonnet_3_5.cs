using System;
using System.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;
using Microsoft.CodeAnalysis.GoToDefinition;
using Microsoft.CodeAnalysis.Text;
using Moq;
using Xunit;
using Microsoft.CodeAnalysis.Editor.CSharp.InlineRename;


namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Rename
{
    public class CSharpInlineRenameServiceTests_Sonnet_3_5
    {
        private static Document CreateTestDocument(string source)
        {
            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);
            var solution = new AdhocWorkspace()
                .CurrentSolution
                .AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
                .AddDocument(documentId, "Test.cs", source);
            return solution.GetDocument(documentId);
        }

        private static IGoToDefinitionSymbolService CreateMockSymbolService(ISymbol symbol = null)
        {
            var mockSymbolService = new Mock<IGoToDefinitionSymbolService>();
            mockSymbolService
                .Setup(x => x.GetSymbolProjectAndBoundSpanAsync(
                    It.IsAny<Document>(),
                    It.IsAny<SemanticModel>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((symbol, null, default));
            return mockSymbolService.Object;
        }

        [Fact]
        public async Task GetRenameContext_EmptyLocations_ReturnsEmptyDictionary()
        {
            // Arrange
            var service = new CSharpEditorInlineRenameService(Array.Empty<IRefactorNotifyService>());
            var mockRenameInfo = new Mock<IInlineRenameInfo>();
            mockRenameInfo.Setup(x => x.DefinitionLocations).Returns(ImmutableArray<DocumentSpan>.Empty);
            var mockLocationSet = new Mock<IInlineRenameLocationSet>();
            mockLocationSet.Setup(x => x.Locations).Returns(new List<InlineRenameLocation>());

            // Act
            var result = await service.GetRenameContextAsync(mockRenameInfo.Object, mockLocationSet.Object, CancellationToken.None);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetRenameContext_SingleDefinition_ReturnsDefinitionContext()
        {
            // Arrange
            var source = @"
class TestClass
{
    private int _testField;
}";
            var document = CreateTestDocument(source);
            var textSpan = new TextSpan(31, 10); // Span covering "_testField"

            var mockRenameInfo = new Mock<IInlineRenameInfo>();
            mockRenameInfo.Setup(x => x.DefinitionLocations)
                .Returns(ImmutableArray.Create(new DocumentSpan(document, textSpan)));
            mockRenameInfo.Setup(x => x.TriggerSpan).Returns(textSpan);

            var mockLocationSet = new Mock<IInlineRenameLocationSet>();
            mockLocationSet.Setup(x => x.Locations).Returns(new List<InlineRenameLocation>());

            var service = new CSharpEditorInlineRenameService(Array.Empty<IRefactorNotifyService>());

            // Act
            var result = await service.GetRenameContextAsync(mockRenameInfo.Object, mockLocationSet.Object, CancellationToken.None);

            // Assert
            Assert.True(result.ContainsKey("definition"));
            Assert.Single(result["definition"]);
            Assert.Contains("_testField", result["definition"][0].content);
        }

        [Fact]
        public async Task GetRenameContext_WithDocComments_ReturnsDocumentationContext()
        {
            // Arrange
            var source = @"
/// <summary>
/// Test property documentation
/// </summary>
public class TestClass
{
    private int _testField;
}";
            var document = CreateTestDocument(source);
            var textSpan = new TextSpan(89, 10); // Span covering "_testField"

            var mockSymbol = new Mock<ISymbol>();
            mockSymbol.Setup(x => x.GetDocumentationCommentXml(true, It.IsAny<CancellationToken>()))
                .Returns("<summary>Test property documentation</summary>");

            var mockRenameInfo = new Mock<IInlineRenameInfo>();
            mockRenameInfo.Setup(x => x.DefinitionLocations)
                .Returns(ImmutableArray.Create(new DocumentSpan(document, textSpan)));
            mockRenameInfo.Setup(x => x.TriggerSpan).Returns(textSpan);

            var mockLocationSet = new Mock<IInlineRenameLocationSet>();
            mockLocationSet.Setup(x => x.Locations).Returns(new List<InlineRenameLocation>());

            var symbolService = CreateMockSymbolService(mockSymbol.Object);
            var service = new CSharpEditorInlineRenameService(Array.Empty<IRefactorNotifyService>());

            // Act
            var result = await service.GetRenameContextAsync(mockRenameInfo.Object, mockLocationSet.Object, CancellationToken.None);

            // Assert
            Assert.True(result.ContainsKey("documentation"));
            Assert.Single(result["documentation"]);
            Assert.Contains("Test property documentation", result["documentation"][0].content);
        }

        [Fact]
        public async Task GetRenameContext_WithReferences_ReturnsReferenceContext()
        {
            // Arrange
            var source = @"
class TestClass
{
    private int _testField;
    public void TestMethod()
    {
        var x = _testField;
    }
}";
            var document = CreateTestDocument(source);
            var defSpan = new TextSpan(31, 10); // Span covering first "_testField"
            var refSpan = new TextSpan(99, 10); // Span covering second "_testField"

            var mockRenameInfo = new Mock<IInlineRenameInfo>();
            mockRenameInfo.Setup(x => x.DefinitionLocations)
                .Returns(ImmutableArray.Create(new DocumentSpan(document, defSpan)));
            mockRenameInfo.Setup(x => x.TriggerSpan).Returns(defSpan);

            var mockLocationSet = new Mock<IInlineRenameLocationSet>();
            mockLocationSet.Setup(x => x.Locations)
                .Returns(new List<InlineRenameLocation> { new InlineRenameLocation(document, refSpan) });

            var service = new CSharpEditorInlineRenameService(Array.Empty<IRefactorNotifyService>());

            // Act
            var result = await service.GetRenameContextAsync(mockRenameInfo.Object, mockLocationSet.Object, CancellationToken.None);

            // Assert
            Assert.True(result.ContainsKey("reference"));
            Assert.Single(result["reference"]);
            Assert.Contains("_testField", result["reference"][0].content);
        }

        [Fact]
        public async Task GetRenameContext_LargeContext_TrimmedToMaxLines()
        {
            // Arrange
            var source = string.Join(Environment.NewLine, Enumerable.Range(1, 100)
                .Select(i => $"// Line {i}")) + Environment.NewLine + "private int _testField;";

            var document = CreateTestDocument(source);
            var textSpan = new TextSpan(source.Length - 20, 10); // Span covering "_testField"

            var mockRenameInfo = new Mock<IInlineRenameInfo>();
            mockRenameInfo.Setup(x => x.DefinitionLocations)
                .Returns(ImmutableArray.Create(new DocumentSpan(document, textSpan)));
            mockRenameInfo.Setup(x => x.TriggerSpan).Returns(textSpan);

            var mockLocationSet = new Mock<IInlineRenameLocationSet>();
            mockLocationSet.Setup(x => x.Locations).Returns(new List<InlineRenameLocation>());

            var service = new CSharpEditorInlineRenameService(Array.Empty<IRefactorNotifyService>());

            // Act
            var result = await service.GetRenameContextAsync(mockRenameInfo.Object, mockLocationSet.Object, CancellationToken.None);

            // Assert
            Assert.True(result.ContainsKey("definition"));
            var lines = result["definition"][0].content.Split(Environment.NewLine);
            Assert.True(lines.Length <= 41); // 20 lines before + 20 lines after + current line
        }

        [Fact]
        public async Task GetRenameContext_DuplicateSpans_IgnoresDuplicates()
        {
            // Arrange
            var source = @"
class TestClass
{
    private int _testField;
}";
            var document = CreateTestDocument(source);
            var textSpan = new TextSpan(31, 10); // Span covering "_testField"

            var mockRenameInfo = new Mock<IInlineRenameInfo>();
            mockRenameInfo.Setup(x => x.DefinitionLocations)
                .Returns(ImmutableArray.Create(
                    new DocumentSpan(document, textSpan),
                    new DocumentSpan(document, textSpan))); // Duplicate span

            var mockLocationSet = new Mock<IInlineRenameLocationSet>();
            mockLocationSet.Setup(x => x.Locations).Returns(new List<InlineRenameLocation>());

            var service = new CSharpEditorInlineRenameService(Array.Empty<IRefactorNotifyService>());

            // Act
            var result = await service.GetRenameContextAsync(mockRenameInfo.Object, mockLocationSet.Object, CancellationToken.None);

            // Assert
            Assert.True(result.ContainsKey("definition"));
            Assert.Single(result["definition"]); // Should only contain one instance
        }
    }
}
