using UnityEngine;
using UnityEngine.Assertions;

/// <summary>
/// All unsafe code should be implemented here.
/// </summary>
public unsafe class Unsafe
{
    public static TTo PointerCast<TFrom, TTo>(TFrom value) where TTo : unmanaged where TFrom : unmanaged
    {
        Assert.IsTrue(sizeof(TFrom) == sizeof(TTo), "Size of these two types are different!");
        return *(TTo*)(&value);
    }
}