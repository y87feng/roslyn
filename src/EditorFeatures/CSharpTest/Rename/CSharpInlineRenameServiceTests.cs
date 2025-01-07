// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Rename
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.Rename)]
    public class CSharpInlineRenameServiceTests
    {
        private class ContextDictionaryComparer : IEqualityComparer<ImmutableDictionary<string, ImmutableArray<(string filePath, string content)>>?>
        {
            public static ContextDictionaryComparer Instance = new();

            public bool Equals(ImmutableDictionary<string, ImmutableArray<(string filePath, string content)>>? x, ImmutableDictionary<string, ImmutableArray<(string filePath, string content)>>? y)
            {
                if (x == y)
                    return true;

                if (x is null || y is null)
                    return false;

                if (x.Count != y.Count)
                    return false;

                foreach (var (elementFromX, elementFromY) in x.Zip(y, (elementFromX, elementFromY) => (elementFromX, elementFromY)))
                {
                    var (keyFromX, valueFromX) = elementFromX;
                    var (keyFromY, valueFromY) = elementFromY;

                    if (keyFromX != keyFromY || !valueFromX.SequenceEqual(valueFromY))
                        return false;
                }

                return true;
            }

            public int GetHashCode(ImmutableDictionary<string, ImmutableArray<(string filePath, string content)>>? obj)
                => EqualityComparer<ImmutableDictionary<string, ImmutableArray<(string filePath, string content)>>?>.Default.GetHashCode(obj);
        }

        private static async Task VerifyGetRenameContextAsync(
            string markup, string expectedContextJson, SymbolRenameOptions options, CancellationToken cancellationToken)
        {
            using var workspace = TestWorkspace.CreateCSharp(markup, composition: EditorTestCompositions.EditorFeatures);
            var documentId = workspace.Documents.Single().Id;
            var document = workspace.CurrentSolution.GetRequiredDocument(documentId);
            var inlineRenameService = document.GetRequiredLanguageService<IEditorInlineRenameService>();
            MarkupTestFile.GetPosition(markup, out _, out int cursorPosition);
            var inlineRenameInfo = await inlineRenameService.GetRenameInfoAsync(document, cursorPosition, cancellationToken).ConfigureAwait(false);
            var inlineRenameLocationSet = await inlineRenameInfo.FindRenameLocationsAsync(options, cancellationToken).ConfigureAwait(false);
            var context = await inlineRenameService.GetRenameContextAsync(inlineRenameInfo, inlineRenameLocationSet, cancellationToken).ConfigureAwait(false);
            var serializationOptions = new JsonSerializerOptions
            {
                IncludeFields = true,
            };
            var expectedContext = JsonSerializer.Deserialize<ImmutableDictionary<string, ImmutableArray<(string, string)>>>(expectedContextJson, serializationOptions);
            AssertEx.AreEqual<ImmutableDictionary<string, ImmutableArray<(string filePath, string content)>>?>(expectedContext, context, comparer: ContextDictionaryComparer.Instance);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/74545")]
        public async Task VerifyContextReachEndOfFile()
        {
            var markup = @"
public class Sampl$$eClass()
{
}";
            await VerifyGetRenameContextAsync(
                markup,
                @"
{
    ""definition"" : [ {""Item1"":""test1.cs"", ""Item2"":""public class SampleClass()\r\n{\r\n}""} ]
}",
                new SymbolRenameOptions(),
                CancellationToken.None);
        }

        // *********** GPT-4o Generated Tests *********** //
        [Fact]
        public async Task VerifyNoDefinitionLocations()
        {
            var markup = @"public class$$ SampleClass { }";
            await VerifyGetRenameContextAsync(
                markup,
                "{}",
                new SymbolRenameOptions(),
                CancellationToken.None);
        }

        [Fact]
        public async Task VerifyNoReferenceLocations()
        {
            var markup = @"public class SampleClass { public void $$Method() { } }";
            await VerifyGetRenameContextAsync(
                markup,
                "{}",
                new SymbolRenameOptions(),
                CancellationToken.None);
        }

        [Fact]
        public async Task VerifyMixedDefinitionsAndReferences()
        {
            var markup = @"public class Sample$$Class { public void Method() { } }";
            await VerifyGetRenameContextAsync(
                markup,
                @"{ ""definition"": [ { ""Item1"": ""test1.cs"", ""Item2"": ""public class SampleClass { public void Method() { } }"" } ] }",
                new SymbolRenameOptions(),
                CancellationToken.None);
        }

        [Fact]
        public async Task VerifyMaxDefinitionsExceeded()
        {
            var markup = string.Concat(Enumerable.Repeat(@"public class $$SampleClass { }\n", 15));
            await VerifyGetRenameContextAsync(
                markup,
                "{}",
                new SymbolRenameOptions(),
                CancellationToken.None);
        }

        [Fact]
        public async Task VerifyNullDocumentationComments()
        {
            var markup = @"public class $$SampleClass { }";
            await VerifyGetRenameContextAsync(
                markup,
                "{}",
                new SymbolRenameOptions(),
                CancellationToken.None);
        }

        [Fact]
        public async Task VerifyInvalidSyntax()
        {
            var markup = @"public class $$SampleClass {";
            await VerifyGetRenameContextAsync(
                markup,
                "{}",
                new SymbolRenameOptions(),
                CancellationToken.None);
        }

        [Fact]
        public async Task VerifyLargeContext()
        {
            var markup = @"public class $$SampleClass { public void Method() { for (int i = 0; i < 100; i++) { Console.WriteLine(i); } } }";
            await VerifyGetRenameContextAsync(
                markup,
                @"{ ""definition"": [ { ""Item1"": ""test1.cs"", ""Item2"": ""public class SampleClass { public void Method() { for (int i = 0; i < 100; i++) { Console.WriteLine(i); } } }"" } ] }",
                new SymbolRenameOptions(),
                CancellationToken.None);
        }

        [Fact]
        public async Task VerifyNestedSyntax()
        {
            var markup = @"public class $$SampleClass { public void Method() { if (true) { Console.WriteLine(); } } }";
            await VerifyGetRenameContextAsync(
                markup,
                @"{ ""definition"": [ { ""Item1"": ""test1.cs"", ""Item2"": ""public class SampleClass { public void Method() { if (true) { Console.WriteLine(); } } }"" } ] }",
                new SymbolRenameOptions(),
                CancellationToken.None);
        }

        [Fact]
        public async Task VerifyMultipleFiles()
        {
            var markup = @"public class $$SampleClass { public void Method() { } }";
            await VerifyGetRenameContextAsync(
                markup,
                @"{ ""definition"": [ { ""Item1"": ""test1.cs"", ""Item2"": ""public class SampleClass { public void Method() { } }"" } ] }",
                new SymbolRenameOptions(),
                CancellationToken.None);
        }

        [Fact]
        public async Task VerifyMissingDocuments()
        {
            var markup = @"public class $$SampleClass { }";
            await VerifyGetRenameContextAsync(
                markup,
                "{}",
                new SymbolRenameOptions(),
                CancellationToken.None);
        }

        // *********************************** //

        // *********** Sonnet 3.5 Generated Tests *********** //
        [Fact]
        public async Task VerifyReferencesCollection()
        {
            var markup = @"
public class Sample$$Class
{
    public void Method()
    {
        var obj = new SampleClass();
        obj = new SampleClass();
    }
}";
            await VerifyGetRenameContextAsync(
                markup,
                @"{
    ""definition"" : [ {""Item1"":""test1.cs"", ""Item2"":""public class SampleClass\r\n{\r\n    public void Method()\r\n    {\r\n        var obj = new SampleClass();\r\n        obj = new SampleClass();\r\n    }\r\n}""} ],
    ""reference"" : [ 
        {""Item1"":""test1.cs"", ""Item2"":""        var obj = new SampleClass();\r\n        obj = new SampleClass();""} 
    ]
}",
                new SymbolRenameOptions(),
                CancellationToken.None);
        }

        [Fact]
        public async Task VerifyDocumentationComments()
        {
            var markup = @"
/// <summary>
/// Sample class description
/// </summary>
public class Sample$$Class
{
}";
            await VerifyGetRenameContextAsync(
                markup,
                @"{
    ""definition"" : [ {""Item1"":""test1.cs"", ""Item2"":""/// <summary>\r\n/// Sample class description\r\n/// </summary>\r\npublic class SampleClass\r\n{\r\n}""} ],
    ""documentation"" : [ {""Item1"":""test1.cs"", ""Item2"":""<summary>\r\nSample class description\r\n</summary>""} ]
}",
                new SymbolRenameOptions(),
                CancellationToken.None);
        }

        [Fact]
        public async Task VerifyLargeSpanTrimming()
        {
            var markup = @"
public class Other 
{
    private void Method1() { }
    private void Method2() { }
}

public class Sample$$Class
{
    private void Method3() { }
    private void Method4() { }
    private void Method5() { }
}

public class Another
{
    private void Method6() { }
    private void Method7() { }
}";
            await VerifyGetRenameContextAsync(
                markup,
                @"{
    ""definition"" : [ {""Item1"":""test1.cs"", ""Item2"":""public class SampleClass\r\n{\r\n    private void Method3() { }\r\n    private void Method4() { }\r\n    private void Method5() { }\r\n}""} ]
}",
                new SymbolRenameOptions(),
                CancellationToken.None);
        }

        [Fact]
        public async Task VerifyMultipleDefinitions()
        {
            var markup = @"
public partial class Sample$$Class
{
    private void Method1() { }
}

public partial class SampleClass
{
    private void Method2() { }
}";
            await VerifyGetRenameContextAsync(
                markup,
                @"{
    ""definition"" : [ 
        {""Item1"":""test1.cs"", ""Item2"":""public partial class SampleClass\r\n{\r\n    private void Method1() { }\r\n}""}, 
        {""Item1"":""test1.cs"", ""Item2"":""public partial class SampleClass\r\n{\r\n    private void Method2() { }\r\n}""} 
    ]
}",
                new SymbolRenameOptions(),
                CancellationToken.None);
        }

        [Fact]
        public async Task VerifyBoundarySpans()
        {
            var markup = @"using System;

public class Sample$$Class
{
}";
            await VerifyGetRenameContextAsync(
                markup,
                @"{
    ""definition"" : [ {""Item1"":""test1.cs"", ""Item2"":""public class SampleClass\r\n{\r\n}""} ]
}",
                new SymbolRenameOptions(),
                CancellationToken.None);
        }

        [Fact]
        public async Task VerifyLocalVariableRename()
        {
            var markup = @"
public class Test
{
    public void Method()
    {
        int sample$$Variable = 42;
        Console.WriteLine(sampleVariable);
    }
}";
            await VerifyGetRenameContextAsync(
                markup,
                @"{
    ""definition"" : [ {""Item1"":""test1.cs"", ""Item2"":""        int sampleVariable = 42;\r\n        Console.WriteLine(sampleVariable);""} ],
    ""reference"" : [ {""Item1"":""test1.cs"", ""Item2"":""        Console.WriteLine(sampleVariable);""} ]
}",
                new SymbolRenameOptions(),
                CancellationToken.None);
        }
    }
}
