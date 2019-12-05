using System;
using System.Runtime.CompilerServices;
using UnityEngine;

/// <summary>
/// using static MathExtension;
/// </summary>
public static class MathExtension
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Sigmoid(float x)
    {
        return (1.0f / (1.0f + Mathf.Exp(-x)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Sigmoid(double x)
    {
        return (1.0f / (1.0f + Math.Exp(-x)));
    }
}
