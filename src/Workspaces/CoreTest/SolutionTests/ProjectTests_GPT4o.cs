using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class ProjectTests_GPT4o
    {
        [Fact]
        public void Create_ValidParameters_ShouldReturnProjectInfo()
        {
            // Arrange
            var id = ProjectId.CreateNewId();
            var version = VersionStamp.Create();
            var name = "TestProject";
            var assemblyName = "TestAssembly";
            var language = "C#";

            // Act
            var projectInfo = ProjectInfo.Create(
                id,
                version,
                name,
                assemblyName,
                language
            );

            // Assert
            Assert.NotNull(projectInfo);
            Assert.Equal(id, projectInfo.Id);
            Assert.Equal(version, projectInfo.Version);
            Assert.Equal(name, projectInfo.Name);
            Assert.Equal(assemblyName, projectInfo.AssemblyName);
            Assert.Equal(language, projectInfo.Language);
        }

        [Fact]
        public void Create_NullId_ShouldThrowArgumentNullException()
        {
            // Arrange
            var version = VersionStamp.Create();
            var name = "TestProject";
            var assemblyName = "TestAssembly";
            var language = "C#";

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                ProjectInfo.Create(
                    null!,
                    version,
                    name,
                    assemblyName,
                    language
                ));
        }

        [Fact]
        public void Create_NullName_ShouldThrowArgumentNullException()
        {
            // Arrange
            var id = ProjectId.CreateNewId();
            var version = VersionStamp.Create();
            var assemblyName = "TestAssembly";
            var language = "C#";

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                ProjectInfo.Create(
                    id,
                    version,
                    null!,
                    assemblyName,
                    language
                ));
        }

        [Fact]
        public void Create_WithOptionalParameters_ShouldReturnProjectInfo()
        {
            // Arrange
            var id = ProjectId.CreateNewId();
            var version = VersionStamp.Create();
            var name = "TestProject";
            var assemblyName = "TestAssembly";
            var language = "C#";
            var filePath = "path/to/project.csproj";
            var outputFilePath = "path/to/output.dll";

            // Act
            var projectInfo = ProjectInfo.Create(
                id,
                version,
                name,
                assemblyName,
                language,
                filePath,
                outputFilePath
            );

            // Assert
            Assert.NotNull(projectInfo);
            Assert.Equal(filePath, projectInfo.FilePath);
            Assert.Equal(outputFilePath, projectInfo.OutputFilePath);
        }

        [Fact]
        public void Create_WithNullEnumerableParameters_ShouldReturnProjectInfo()
        {
            // Arrange
            var id = ProjectId.CreateNewId();
            var version = VersionStamp.Create();
            var name = "TestProject";
            var assemblyName = "TestAssembly";
            var language = "C#";

            // Act
            var projectInfo = ProjectInfo.Create(
                id,
                version,
                name,
                assemblyName,
                language,
                documents: null,
                projectReferences: null,
                metadataReferences: null,
                analyzerReferences: null,
                additionalDocuments: null
            );

            // Assert
            Assert.NotNull(projectInfo);
            Assert.Empty(projectInfo.Documents);
            Assert.Empty(projectInfo.ProjectReferences);
            Assert.Empty(projectInfo.MetadataReferences);
            Assert.Empty(projectInfo.AnalyzerReferences);
            Assert.Empty(projectInfo.AdditionalDocuments);
        }

        [Fact]
        public void Create_WithFullParameters_ShouldReturnProjectInfo()
        {
            // Arrange
            var id = ProjectId.CreateNewId();
            var version = VersionStamp.Create();
            var name = "TestProject";
            var assemblyName = "TestAssembly";
            var language = "C#";
            var filePath = "path/to/project.csproj";
            var outputFilePath = "path/to/output.dll";
            var outputRefFilePath = "path/to/output.ref.dll";
            var compilationOptions = new CSharpCompilationOptions(OutputKind.ConsoleApplication);
            var parseOptions = new CSharpParseOptions();
            //var compilationOptions = new CompilationOptions(OutputKind.ConsoleApplication);
            //var parseOptions = new CSharpParseOptions();
            var documents = new List<DocumentInfo>();
            var projectReferences = new List<ProjectReference>();
            var metadataReferences = new List<MetadataReference>();
            var analyzerReferences = new List<AnalyzerReference>();
            var additionalDocuments = new List<DocumentInfo>();
            var isSubmission = true;
            var hostObjectType = typeof(object);

            // Act
            var projectInfo = ProjectInfo.Create(
                id,
                version,
                name,
                assemblyName,
                language,
                filePath,
                outputFilePath,
                compilationOptions,
                parseOptions,
                documents,
                projectReferences,
                metadataReferences,
                analyzerReferences,
                additionalDocuments,
                isSubmission,
                hostObjectType,
                outputRefFilePath
            );

            // Assert
            Assert.NotNull(projectInfo);
            Assert.Equal(filePath, projectInfo.FilePath);
            Assert.Equal(outputFilePath, projectInfo.OutputFilePath);
            Assert.Equal(outputRefFilePath, projectInfo.OutputRefFilePath);
            Assert.Equal(compilationOptions, projectInfo.CompilationOptions);
            Assert.Equal(parseOptions, projectInfo.ParseOptions);
            Assert.Same(documents, projectInfo.Documents);
            Assert.Same(projectReferences, projectInfo.ProjectReferences);
            Assert.Same(metadataReferences, projectInfo.MetadataReferences);
            Assert.Same(analyzerReferences, projectInfo.AnalyzerReferences);
            Assert.Same(additionalDocuments, projectInfo.AdditionalDocuments);
            Assert.Equal(isSubmission, projectInfo.IsSubmission);
            Assert.Equal(hostObjectType, projectInfo.HostObjectType);
        }
    }
}
