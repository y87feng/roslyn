// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class SolutionInfoTests
    {
        [Fact]
        public void Create_Errors()
        {
            var solutionId = SolutionId.CreateNewId();
            var version = VersionStamp.Default;
            var projectInfo = ProjectInfo.Create(ProjectId.CreateNewId(), version, "proj", "assembly", "C#");

            Assert.Throws<ArgumentNullException>(() => SolutionInfo.Create(null, version));
            Assert.Throws<ArgumentNullException>(() => SolutionInfo.Create(solutionId, version, projects: [projectInfo, null]));
        }

        [Fact]
        public void Create_Projects()
        {
            var solutionId = SolutionId.CreateNewId();
            var version = VersionStamp.Default;
            var projectInfo = ProjectInfo.Create(ProjectId.CreateNewId(), version, "proj", "assembly", "C#");

            var info1 = SolutionInfo.Create(solutionId, version, projects: [projectInfo]);
            Assert.Same(projectInfo, ((ImmutableArray<ProjectInfo>)info1.Projects).Single());

            var info2 = SolutionInfo.Create(solutionId, version);
            Assert.True(((ImmutableArray<ProjectInfo>)info2.Projects).IsEmpty);

            var info3 = SolutionInfo.Create(solutionId, version, projects: []);
            Assert.True(((ImmutableArray<ProjectInfo>)info3.Projects).IsEmpty);

            var info4 = SolutionInfo.Create(solutionId, version, projects: []);
            Assert.True(((ImmutableArray<ProjectInfo>)info4.Projects).IsEmpty);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("path")]
        public void Create_FilePath(string path)
        {
            var info = SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Default, filePath: path);
            Assert.Equal(path, info.FilePath);
        }

        [Fact]
        public void Create_WithNullId_ThrowsArgumentNullException()
        {
            var version = VersionStamp.Default;
            Assert.Throws<ArgumentNullException>(() => SolutionInfo.Create(null, version));
        }

        [Fact]
        public void Create_WithValidParameters_CreatesSolutionInfo()
        {
            var solutionId = SolutionId.CreateNewId();
            var version = VersionStamp.Default;
            var projectInfo = ProjectInfo.Create(ProjectId.CreateNewId(), version, "proj", "assembly", "C#");
            var analyzerReference = new TestAnalyzerReference();

            var solutionInfo = SolutionInfo.Create(solutionId, version, "path", new[] { projectInfo }, new[] { analyzerReference });

            Assert.Equal(solutionId, solutionInfo.Id);
            Assert.Equal(version, solutionInfo.Version);
            Assert.Equal("path", solutionInfo.FilePath);
            Assert.Single(solutionInfo.Projects);
            Assert.Single(solutionInfo.AnalyzerReferences);
        }

        [Fact]
        public void Create_WithNullFilePath_CreatesSolutionInfo()
        {
            var solutionId = SolutionId.CreateNewId();
            var version = VersionStamp.Default;

            var solutionInfo = SolutionInfo.Create(solutionId, version);

            Assert.Equal(solutionId, solutionInfo.Id);
            Assert.Equal(version, solutionInfo.Version);
            Assert.Null(solutionInfo.FilePath);
            Assert.Empty(solutionInfo.Projects);
            Assert.Empty(solutionInfo.AnalyzerReferences);
        }

        [Fact]
        public void Create_WithNullProjects_CreatesSolutionInfo()
        {
            var solutionId = SolutionId.CreateNewId();
            var version = VersionStamp.Default;

            var solutionInfo = SolutionInfo.Create(solutionId, version, projects: null);

            Assert.Equal(solutionId, solutionInfo.Id);
            Assert.Equal(version, solutionInfo.Version);
            Assert.Empty(solutionInfo.Projects);
        }

        [Fact]
        public void Create_WithNullAnalyzerReferences_CreatesSolutionInfo()
        {
            var solutionId = SolutionId.CreateNewId();
            var version = VersionStamp.Default;

            var solutionInfo = SolutionInfo.Create(solutionId, version, analyzerReferences: null);

            Assert.Equal(solutionId, solutionInfo.Id);
            Assert.Equal(version, solutionInfo.Version);
            Assert.Empty(solutionInfo.AnalyzerReferences);
        }
    }
}
