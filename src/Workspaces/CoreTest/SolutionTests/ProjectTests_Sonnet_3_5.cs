using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class ProjectTests_Sonnet_3_5
    {
        [Fact]
        public void Create_WithRequiredParameters_CreatesValidProjectInfo()
        {
            // Arrange
            var id = ProjectId.CreateNewId();
            var version = VersionStamp.Create();
            var name = "TestProject";
            var assemblyName = "TestAssembly";
            var language = "C#";

            // Act
            var projectInfo = ProjectInfo.Create(
                id: id,
                version: version,
                name: name,
                assemblyName: assemblyName,
                language: language);

            // Assert
            Assert.NotNull(projectInfo);
            Assert.Equal(id, projectInfo.Id);
            Assert.Equal(version, projectInfo.Version);
            Assert.Equal(name, projectInfo.Name);
            Assert.Equal(assemblyName, projectInfo.AssemblyName);
            Assert.Equal(language, projectInfo.Language);
            Assert.True(projectInfo.HasAllInformation);
            Assert.True(projectInfo.RunAnalyzers);
            Assert.False(projectInfo.IsSubmission);
            Assert.Null(projectInfo.FilePath);
            Assert.Null(projectInfo.OutputFilePath);
            Assert.Null(projectInfo.OutputRefFilePath);
        }

        [Theory]
        [InlineData(null, "version", "name", "assembly", "C#")]
        [InlineData("id", "version", null, "assembly", "C#")]
        [InlineData("id", "version", "name", null, "C#")]
        [InlineData("id", "version", "name", "assembly", null)]
        public void Create_WithNullRequiredParameters_ThrowsArgumentNullException(
            string? idValue, string versionValue, string? name, string? assemblyName, string? language)
        {
            // Arrange
            var id = idValue != null ? ProjectId.CreateNewId() : null;
            var version = VersionStamp.Create();

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => ProjectInfo.Create(
                id: id!,
                version: version,
                name: name!,
                assemblyName: assemblyName!,
                language: language!));

            // Verify the correct parameter name is indicated in the exception
            if (id == null) Assert.Equal("id", exception.ParamName);
            else if (name == null) Assert.Equal("name", exception.ParamName);
            else if (assemblyName == null) Assert.Equal("assemblyName", exception.ParamName);
            else Assert.Equal("language", exception.ParamName);
        }

        [Fact]
        public void Create_WithAllOptionalParameters_CreatesValidProjectInfo()
        {
            // Arrange
            var id = ProjectId.CreateNewId();
            var version = VersionStamp.Create();
            var documents = new List<DocumentInfo> { DocumentInfo.Create(DocumentId.CreateNewId(id), "test.cs") };
            var projectReferences = new List<ProjectReference> { new ProjectReference(ProjectId.CreateNewId()) };
            //var metadataReferences = new List<MetadataReference> { MetadataReference.CreateFromFile("System.dll") }; // Original Sonnet 3.5 code, but System.dll doesn't exist
            var metadataReferences = new List<MetadataReference> {
                MetadataReference.CreateFromStream(new System.IO.MemoryStream())
            };
            var analyzerReferences = new List<AnalyzerReference>();
            var additionalDocuments = new List<DocumentInfo> { DocumentInfo.Create(DocumentId.CreateNewId(id), "additional.txt") };

            // Act
            var projectInfo = ProjectInfo.Create(
                id: id,
                version: version,
                name: "TestProject",
                assemblyName: "TestAssembly",
                language: "C#",
                filePath: "test.csproj",
                outputFilePath: "bin/test.dll",
                compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                parseOptions: new CSharpParseOptions(),
                documents: documents,
                projectReferences: projectReferences,
                metadataReferences: metadataReferences,
                analyzerReferences: analyzerReferences,
                additionalDocuments: additionalDocuments,
                isSubmission: true,
                hostObjectType: typeof(ProjectInfoTests),
                outputRefFilePath: "ref/test.dll");

            // Assert
            Assert.NotNull(projectInfo);
            Assert.Equal("test.csproj", projectInfo.FilePath);
            Assert.Equal("bin/test.dll", projectInfo.OutputFilePath);
            Assert.Equal("ref/test.dll", projectInfo.OutputRefFilePath);
            Assert.NotNull(projectInfo.CompilationOptions);
            Assert.NotNull(projectInfo.ParseOptions);
            Assert.Equal(documents.Count, projectInfo.Documents.Count);
            Assert.Equal(projectReferences.Count, projectInfo.ProjectReferences.Count);
            Assert.Equal(metadataReferences.Count, projectInfo.MetadataReferences.Count);
            Assert.Equal(analyzerReferences.Count, projectInfo.AnalyzerReferences.Count);
            Assert.Equal(additionalDocuments.Count, projectInfo.AdditionalDocuments.Count);
            Assert.True(projectInfo.IsSubmission);
            Assert.Equal(typeof(ProjectInfoTests), projectInfo.HostObjectType);
        }

        [Fact]
        public void Create_WithEmptyCollections_CreatesValidProjectInfo()
        {
            // Arrange
            var id = ProjectId.CreateNewId();
            var version = VersionStamp.Create();

            // Act
            var projectInfo = ProjectInfo.Create(
                id: id,
                version: version,
                name: "TestProject",
                assemblyName: "TestAssembly",
                language: "C#",
                documents: new List<DocumentInfo>(),
                projectReferences: new List<ProjectReference>(),
                metadataReferences: new List<MetadataReference>(),
                analyzerReferences: new List<AnalyzerReference>(),
                additionalDocuments: new List<DocumentInfo>());

            // Assert
            Assert.NotNull(projectInfo);
            Assert.Empty(projectInfo.Documents);
            Assert.Empty(projectInfo.ProjectReferences);
            Assert.Empty(projectInfo.MetadataReferences);
            Assert.Empty(projectInfo.AnalyzerReferences);
            Assert.Empty(projectInfo.AdditionalDocuments);
        }

        [Fact]
        public void Create_WithNullCollections_CreatesValidProjectInfo()
        {
            // Arrange
            var id = ProjectId.CreateNewId();
            var version = VersionStamp.Create();

            // Act
            var projectInfo = ProjectInfo.Create(
                id: id,
                version: version,
                name: "TestProject",
                assemblyName: "TestAssembly",
                language: "C#",
                documents: null,
                projectReferences: null,
                metadataReferences: null,
                analyzerReferences: null,
                additionalDocuments: null);

            // Assert
            Assert.NotNull(projectInfo);
            Assert.Empty(projectInfo.Documents);
            Assert.Empty(projectInfo.ProjectReferences);
            Assert.Empty(projectInfo.MetadataReferences);
            Assert.Empty(projectInfo.AnalyzerReferences);
            Assert.Empty(projectInfo.AdditionalDocuments);
        }

        [Fact]
        public void Create_WithProjectNameAndFlavor_ParsesCorrectly()
        {
            // Arrange
            var id = ProjectId.CreateNewId();
            var version = VersionStamp.Create();
            var name = "TestProject (netcoreapp3.1)";

            // Act
            var projectInfo = ProjectInfo.Create(
                id: id,
                version: version,
                name: name,
                assemblyName: "TestAssembly",
                language: "C#");

            // Assert
            Assert.NotNull(projectInfo);
            var (parsedName, flavor) = projectInfo.NameAndFlavor;
            Assert.Equal("TestProject", parsedName);
            Assert.Equal("netcoreapp3.1", flavor);
        }

        [Fact]
        public void Create_WithProjectNameWithoutFlavor_ParsesCorrectly()
        {
            // Arrange
            var id = ProjectId.CreateNewId();
            var version = VersionStamp.Create();
            var name = "TestProject";

            // Act
            var projectInfo = ProjectInfo.Create(
                id: id,
                version: version,
                name: name,
                assemblyName: "TestAssembly",
                language: "C#");

            // Assert
            Assert.NotNull(projectInfo);
            var (parsedName, flavor) = projectInfo.NameAndFlavor;
            Assert.Null(parsedName);
            Assert.Null(flavor);
        }
    }
}
