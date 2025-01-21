
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.TextFormatting;
using Microsoft.CodeAnalysis.Editor.CSharp.InlineRename;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Moq;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.InlineRename
{
    [UseExportProvider]
    public class CSharpEditorInlineRenameServiceTests
    {
        // ORIGINAL GENERATED CODE by GPT4o
        //private async Task<(TestWorkspace workspace, CSharpEditorInlineRenameService service, Document document)> CreateTestEnvironmentAsync(string code)
        //{
        //    var composition = TestComposition.Empty
        //        .AddParts(typeof(CSharpEditorInlineRenameService))
        //        .AddParts(typeof(TestWorkspace));

        //    var workspace = TestWorkspace.CreateCSharp(code, composition: composition);
        //    var service = workspace.ExportProvider.GetExportedValue<CSharpEditorInlineRenameService>();
        //    var document = workspace.CurrentSolution.Projects.First().Documents.First();
        //    return (workspace, service, document);
        //}

        private async Task<(TestWorkspace workspace, IEditorInlineRenameService service, Document document)> CreateTestEnvironmentAsync(string code)
        {
            var workspace = TestWorkspace.CreateCSharp(code, composition: EditorTestCompositions.EditorFeatures);
            var documentId = workspace.Documents.Single().Id;
            var document = workspace.CurrentSolution.GetRequiredDocument(documentId);
            var inlineRenameService = document.GetRequiredLanguageService<IEditorInlineRenameService>();
            return (workspace, inlineRenameService, document);
        }

        [Fact]
        public async Task GetRenameContextAsync_SingleDefinition_NoReferences()
        {
            // Arrange
            var (workspace, service, document) = await CreateTestEnvironmentAsync("class C { void M() {} }");

            var definitionSpan = new TextSpan(10, 5); // Span for 'M'
            var mockRenameInfo = new Mock<IInlineRenameInfo>();
            var mockLocationSet = new Mock<IInlineRenameLocationSet>();

            mockRenameInfo.Setup(i => i.DefinitionLocations)
                .Returns(ImmutableArray.Create(new DocumentSpan(document, definitionSpan)));
            mockLocationSet.Setup(l => l.Locations).Returns(new List<InlineRenameLocation>());

            // Act
            var context = await service.GetRenameContextAsync(mockRenameInfo.Object, mockLocationSet.Object, CancellationToken.None);

            // Assert
            Assert.True(context.ContainsKey("definition"));
            Assert.False(context.ContainsKey("reference"));
            Assert.False(context.ContainsKey("documentation"));

            var definitions = context["definition"];
            Assert.Single(definitions);
            Assert.Equal(document.FilePath, definitions[0].filePath);
            Assert.Contains("void M()", definitions[0].content);
        }

        [Fact]
        public async Task GetRenameContextAsync_SingleDefinition_WithDocumentation()
        {
            // Arrange
            var (workspace, service, document) = await CreateTestEnvironmentAsync(
                @"
            /// <summary>Method M</summary>
            class C { void M() {} }");

            var definitionSpan = new TextSpan(50, 5); // Span for 'M'
            var mockRenameInfo = new Mock<IInlineRenameInfo>();
            var mockLocationSet = new Mock<IInlineRenameLocationSet>();

            mockRenameInfo.Setup(i => i.DefinitionLocations)
                .Returns(ImmutableArray.Create(new DocumentSpan(document, definitionSpan)));
            mockLocationSet.Setup(l => l.Locations).Returns(new List<InlineRenameLocation>());

            // Act
            var context = await service.GetRenameContextAsync(mockRenameInfo.Object, mockLocationSet.Object, CancellationToken.None);

            // Assert
            Assert.True(context.ContainsKey("definition"));
            Assert.True(context.ContainsKey("documentation"));
            Assert.False(context.ContainsKey("reference"));

            var documentation = context["documentation"];
            Assert.Single(documentation);
            Assert.Contains("Method M", documentation[0].content);
        }

        [Fact]
        public async Task GetRenameContextAsync_MultipleDefinitions_MultipleReferences()
        {
            // Arrange
            var (workspace, service, document) = await CreateTestEnvironmentAsync(
                @"
            class C 
            { 
                void M() {} 
                void N() { M(); }
                void O() { M(); }
            }");

            var definitionSpan = new TextSpan(10, 5); // Span for 'M'
            var referenceSpans = new[]
            {
            new TextSpan(40, 3), // Span for 'M()' in N
            new TextSpan(60, 3)  // Span for 'M()' in O
        };

            var mockRenameInfo = new Mock<IInlineRenameInfo>();
            var mockLocationSet = new Mock<IInlineRenameLocationSet>();

            mockRenameInfo.Setup(i => i.DefinitionLocations)
                .Returns(ImmutableArray.Create(new DocumentSpan(document, definitionSpan)));

            mockLocationSet.Setup(l => l.Locations)
                .Returns(referenceSpans.Select(span => new InlineRenameLocation(document, span)).ToList());

            // Act
            var context = await service.GetRenameContextAsync(mockRenameInfo.Object, mockLocationSet.Object, CancellationToken.None);

            // Assert
            Assert.True(context.ContainsKey("definition"));
            Assert.True(context.ContainsKey("reference"));
            Assert.False(context.ContainsKey("documentation"));

            var references = context["reference"];
            Assert.Equal(2, references.Length);
            Assert.Contains("M()", references[0].content);
            Assert.Contains("M()", references[1].content);
        }
    }

}
