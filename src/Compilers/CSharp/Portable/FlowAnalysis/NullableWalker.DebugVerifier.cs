// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class NullableWalker
    {
#if DEBUG
        private sealed class DebugVerifier : BoundTreeWalker, IDisposable
        {
            private static ImmutableArray<BoundKind> s_skippedExpression = ImmutableArray.Create(BoundKind.ArrayInitialization, BoundKind.Conversion);
            private readonly PooledDictionary<BoundExpression, TypeSymbolWithAnnotations> _topLevelNullabilityMap;

            public DebugVerifier(NullableWalker walker)
            {
                _topLevelNullabilityMap = PooledDictionary<BoundExpression, TypeSymbolWithAnnotations>.GetInstance();
                foreach (var (key, value) in walker._topLevelNullabilityMap)
                {
                    _topLevelNullabilityMap[key] = value;
                }
            }

            public static void Verify(NullableWalker walker, BoundNode node)
            {
                var verifier = new DebugVerifier(walker);
                verifier.Visit(node);
                Debug.Assert(verifier._topLevelNullabilityMap.Count == 0);
                verifier.Dispose();
            }

            protected override BoundExpression VisitExpressionWithoutStackGuard(BoundExpression node)
            {
                return (BoundExpression)base.Visit(node);
            }

            public override BoundNode Visit(BoundNode node)
            {
                if (node is BoundExpression expression && !s_skippedExpression.Contains(expression.Kind))
                {
                    Debug.Assert(_topLevelNullabilityMap.ContainsKey(expression), $"Did not find {expression} in the map.");
                    _topLevelNullabilityMap.Remove(expression);
                }
                return base.Visit(node);
            }

            private bool _disposedValue = false;

            void Dispose(bool disposing)
            {
                if (!_disposedValue)
                {
                    if (disposing)
                    {
                        _topLevelNullabilityMap.Free();
                    }

                    _disposedValue = true;
                }
            }

            public void Dispose()
            {
                Dispose(true);
            }
        }
#endif
    }
}
