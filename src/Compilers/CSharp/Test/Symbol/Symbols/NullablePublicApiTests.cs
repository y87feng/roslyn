// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class NullablePublicApiTests : CSharpTestBase
    {
        [Fact]
        public void TestArrayNullability()
        {
            var source = @"
class C
{
    void M(C?[] arr1)
    {
        C[] arr2 = (C[])arr1;
        arr1[0].ToString();
        arr2[0].ToString();
    }
}
";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics(
                // (6,20): warning CS8619: Nullability of reference types in value of type 'C?[]' doesn't match target type 'C[]'.
                //         C[] arr2 = (C[])arr1;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "(C[])arr1").WithArguments("C?[]", "C[]").WithLocation(6, 20),
                // (7,9): warning CS8602: Possible dereference of a null reference.
                //         arr1[0].ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "arr1[0]").WithLocation(7, 9));

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var arrayAccesses = root.DescendantNodes().OfType<ElementAccessExpressionSyntax>().ToList();
            var arrayTypes = arrayAccesses.Select(arr => model.GetTypeInfo(arr.Expression).Type).Cast<IArrayTypeSymbol>().ToList();

            Assert.Equal(Nullability.MayBeNull, arrayTypes[0].ElementNullability);
            Assert.Equal(Nullability.NotNull, arrayTypes[1].ElementNullability);
        }

        [Fact]
        public void TestTypeArgumentNullability()
        {
            var source = @"
class B<T> {}
class C
{
    B<T> M<T>(T t) where T : C? { return default!; }

    void M1(C? c)
    {
        M(new C());
        M(c);
        if (c == null) return;
        M(c);
    }
}";

            var comp = CreateCompilation(source, options: WithNonNullTypesTrue());
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>().ToList();
            var expressionTypes = invocations.Select(inv => model.GetTypeInfo(inv).Type).Cast<INamedTypeSymbol>().ToList();

            Assert.Equal(Nullability.NotNull, expressionTypes[0].TypeArgumentsNullabilities.Single());
            Assert.Equal(Nullability.MayBeNull, expressionTypes[1].TypeArgumentsNullabilities.Single());
            Assert.Equal(Nullability.NotNull, expressionTypes[2].TypeArgumentsNullabilities.Single());
        }
    }
}
