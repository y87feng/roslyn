using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition.Hosting;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.CSharp.InlineRename;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Moq;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Rename
{
    public class CSharpInlineRenameServiceTests_GPT4o
    {
        private readonly CSharpEditorInlineRenameService _service;

        private static CSharpEditorInlineRenameService CreateCSharpEditorInlineRenameServiceThroughMEF()
        {
            // Create an MEF composition container
            var assemblies = MefHostServices.DefaultAssemblies
                .Add(typeof(CSharpEditorInlineRenameService).Assembly);

            var configuration = new ContainerConfiguration()
                .WithAssemblies(assemblies);

            using var container = configuration.CreateContainer();

            // Retrieve the service
            return container.GetExport<CSharpEditorInlineRenameService>();
        }

        public CSharpInlineRenameServiceTests_GPT4o()
        {
            _service = CreateCSharpEditorInlineRenameServiceThroughMEF();
        }

        [Fact]
        public async Task GetRenameContextAsync_ShouldReturnEmpty_WhenNoDefinitionsOrReferences()
        {
            // Arrange
            var mockRenameInfo = new Mock<VSTypeScriptInlineRenameInfo>();
            var mockLocationSet = new Mock<VSTypeScriptInlineRenameLocationSet>();

            mockRenameInfo.Setup(r => r.DefinitionLocations).Returns(ImmutableArray<VSTypeScriptDocumentSpan>.Empty);
            mockLocationSet.Setup(s => s.Locations).Returns(new List<VSTypeScriptInlineRenameLocationWrapper>());

            // Act
            var result = await _service.GetRenameContextAsync(
                mockRenameInfo.Object,
                mockLocationSet.Object,
                CancellationToken.None);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetRenameContextAsync_ShouldHandleDefinitions()
        {
            // Arrange
            var mockRenameInfo = new Mock<VSTypeScriptInlineRenameInfo>();
            var mockLocationSet = new Mock<VSTypeScriptInlineRenameLocationSet>();
            var mockDocument = CreateMockDocument("public class MyClass { }");
            var definitionLocation = new VSTypeScriptDocumentSpan(mockDocument, new TextSpan(0, 10));

            mockRenameInfo.Setup(r => r.DefinitionLocations).Returns(ImmutableArray.Create(definitionLocation));
            mockLocationSet.Setup(s => s.Locations).Returns(new List<VSTypeScriptInlineRenameLocationWrapper>());

            // Act
            var result = await _service.GetRenameContextAsync(
                mockRenameInfo.Object,
                mockLocationSet.Object,
                CancellationToken.None);

            // Assert
            Assert.Contains("definition", result.Keys);
            Assert.Single(result["definition"]);
        }

        [Fact]
        public async Task GetRenameContextAsync_ShouldHandleReferences()
        {
            // Arrange
            var mockRenameInfo = new Mock<VSTypeScriptInlineRenameInfo>();
            var mockLocationSet = new Mock<VSTypeScriptInlineRenameLocationSet>();
            var mockDocument = CreateMockDocument("var x = new MyClass();");
            var referenceLocation = new VSTypeScriptInlineRenameLocationWrapper(new InlineRenameLocation(mockDocument, new TextSpan(8, 7)));

            mockRenameInfo.Setup(r => r.DefinitionLocations).Returns(ImmutableArray<VSTypeScriptDocumentSpan>.Empty);
            mockLocationSet.Setup(s => s.Locations).Returns(new List<VSTypeScriptInlineRenameLocationWrapper> { referenceLocation });

            // Act
            var result = await _service.GetRenameContextAsync(
                mockRenameInfo.Object,
                mockLocationSet.Object,
                CancellationToken.None);

            // Assert
            Assert.Contains("reference", result.Keys);
            Assert.Single(result["reference"]);
        }

        [Fact]
        public async Task GetRenameContextAsync_ShouldRespectMaxDefinitionCount()
        {
            // Arrange
            var mockRenameInfo = new Mock<VSTypeScriptInlineRenameInfo>();
            var mockLocationSet = new Mock<VSTypeScriptInlineRenameLocationSet>();
            var mockDocument = CreateMockDocument("public class MyClass { }");
            var definitions = Enumerable.Range(0, 15)
                .Select(_ => new VSTypeScriptDocumentSpan(mockDocument, new TextSpan(0, 10)))
                .ToImmutableArray();

            mockRenameInfo.Setup(r => r.DefinitionLocations).Returns(definitions);
            mockLocationSet.Setup(s => s.Locations).Returns(new List<VSTypeScriptInlineRenameLocationWrapper>());

            // Act
            var result = await _service.GetRenameContextAsync(
                mockRenameInfo.Object,
                mockLocationSet.Object,
                CancellationToken.None);

            // Assert
            Assert.Contains("definition", result.Keys);
            Assert.Equal(10, result["definition"].Length);
        }

        [Fact]
        public async Task GetRenameContextAsync_ShouldRespectMaxReferenceCount()
        {
            // Arrange
            var mockRenameInfo = new Mock<VSTypeScriptInlineRenameInfo>();
            var mockLocationSet = new Mock<VSTypeScriptInlineRenameLocationSet>();
            var mockDocument = CreateMockDocument("var x = new MyClass();");
            var references = Enumerable.Range(0, 60)
                .Select(_ => new VSTypeScriptInlineRenameLocationWrapper(new InlineRenameLocation(mockDocument, new TextSpan(8, 7))))
                .ToList();

            mockRenameInfo.Setup(r => r.DefinitionLocations).Returns(ImmutableArray<VSTypeScriptDocumentSpan>.Empty);
            mockLocationSet.Setup(s => s.Locations).Returns(references);

            // Act
            var result = await _service.GetRenameContextAsync(
                mockRenameInfo.Object,
                mockLocationSet.Object,
                CancellationToken.None);

            // Assert
            Assert.Contains("reference", result.Keys);
            Assert.Equal(50, result["reference"].Length);
        }

        private static Document CreateMockDocument(string content)
        {
            var workspace = new AdhocWorkspace();
            var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
            return project.AddDocument("TestDocument.cs", content);
        }
    }
}
