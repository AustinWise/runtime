// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime;
using System.Runtime.CompilerServices;

namespace System
{
    // TODO: consider have some fast path in C# for these.
    // The real functions are defined in SpanHelpers.ByteMemOps.cs but have been vectorized.
    // Rather than trying to sort through all the if-defs and gotos, P/Invoke to the C library.
    internal static class SpanHelpers
    {
        [RuntimeExport("RhSpanHelpers_MemCopy")]
        [Intrinsic] // Unrolled for small constant lengths
        internal static unsafe void Memmove(ref byte dest, ref byte src, nuint len)
        {
            fixed (byte* pDest = &dest)
            fixed (byte* pSrc = &src)
                RuntimeImports.memmove(pDest, pSrc, len);
        }

        [RuntimeExport("RhSpanHelpers_MemZero")]
        [Intrinsic] // Unrolled for small sizes
        public static unsafe void ClearWithoutReferences(ref byte dest, nuint len)
        {
            fixed (byte* pDest = &dest)
                RuntimeImports.memset(pDest, 0, len);
        }

        [RuntimeExport("RhSpanHelpers_MemSet")]
        internal static unsafe void Fill(ref byte dest, byte value, nuint len)
        {
            fixed (byte* pDest = &dest)
                RuntimeImports.memset(pDest, value, len);
        }
    }
}
