// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

class Program
{
    static void Main()
    {
        Console.WriteLine("Hello world");
        int res = MyImports.MyTestMethod(1, 2);
        Console.WriteLine($"result: {res}");
    }
}

class MyImports
{
    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern int MyTestMethod(int a, int b);
}
