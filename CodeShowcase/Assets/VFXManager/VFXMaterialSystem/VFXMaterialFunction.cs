using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Pool;

[Serializable]
public abstract class VFXMaterialFunction
{
    public abstract void Apply(List<Material> materials, float time);
    public abstract void Revert(List<Material> materials);
    public abstract VFXMaterialFunction Copy();
    public abstract void Clear();
}

[Serializable]
public abstract class VFXMaterialFunction<T> : VFXMaterialFunction
{
    [SerializeField] protected T fixedValue;
    [SerializeField] protected string name;
    [SerializeField] private uint _stateBitmask;
    [NonSerialized] private List<T> _recordedValue = new List<T>();
    private const uint FlagsUseCurve = 1 << 0;
    private const uint FlagsIsRecorded = 1 << 1;

    protected bool IsCurveMode
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (_stateBitmask & FlagsUseCurve) != 0u;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            if (value)
            {
                _stateBitmask |= FlagsUseCurve;
            }
            else
            {
                _stateBitmask &= ~FlagsUseCurve;
            }
        }
    }

    protected bool IsRecorded
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (_stateBitmask & FlagsIsRecorded) != 0u;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            if (value)
            {
                _stateBitmask |= FlagsIsRecorded;
            }
            else
            {
                _stateBitmask &= ~FlagsIsRecorded;
            }
        }
    }

    public sealed override void Apply(List<Material> materials, float time)
    {
        bool isRecorded = IsRecorded;
        for (int index = 0; index < materials.Count; index++)
        {
            Material material = materials[index];
            if (!isRecorded)
            {
                T recordedValue = GetValueFromMaterialImpl(material);
                _recordedValue.Add(recordedValue);
            }

            T value = EvaluateImpl(material, time);
            ApplyValueImpl(material, value);
        }
    }

    public sealed override void Revert(List<Material> materials)
    {
        if (IsRecorded)
        {
            for (int index = 0; index < materials.Count; index++)
            {
                Material material = materials[index];
                ApplyValueImpl(material, _recordedValue[index]);
            }
        }
    }

    protected abstract void ApplyValueImpl(Material materials, T value);
    protected abstract T GetValueFromMaterialImpl(Material materials);
    protected abstract T EvaluateImpl(Material materials, float time);

    public override void Clear()
    {
        fixedValue = default;
        name = default;
        _recordedValue.Clear();
        _stateBitmask = default;
    }
}

// In the source code of Unity engine, they force cast the int value to float which will lost precision if the number is too big.
// So here we transfer the int to float without change the value of this address by pointer.
// In shader, we should declared the valuable as float: 
//      float _IntParameter;
// When we are going to use the value transfer to int by pointer:
//      int value = asint(parameter);
[Serializable]
public class VFXMaterialInt : VFXMaterialFunction<int>
{
    [SerializeField] public AnimationCurve curve = new AnimationCurve();

    protected override unsafe void ApplyValueImpl(Material material, int value)
    {
        float perfectPrecisionInt = *(float*)(&value);
        material.SetFloat(name, perfectPrecisionInt);
    }

    protected override unsafe int GetValueFromMaterialImpl(Material material)
    {
        float floatValue = material.GetFloat(name);
        return *(int*)(&floatValue);
    }

    protected override int EvaluateImpl(Material material, float time)
    {
        return IsCurveMode ? (int)curve.Evaluate(time) : fixedValue;
    }

    public override VFXMaterialFunction Copy()
    {
        VFXMaterialInt function = GenericPool<VFXMaterialInt>.Get();
        function.fixedValue = fixedValue;
        function.IsCurveMode = IsCurveMode;
        function.curve = curve;
        return function;
    }
}

[Serializable]
public class VFXMaterialFloat : VFXMaterialFunction<float>
{
    [SerializeField] private AnimationCurve curve = new AnimationCurve();

    protected override void ApplyValueImpl(Material material, float value)
    {
        material.SetFloat(name, value);
    }

    protected override float GetValueFromMaterialImpl(Material material)
    {
        return material.GetFloat(name);
    }

    protected override float EvaluateImpl(Material material, float time)
    {
        return IsCurveMode ? curve.Evaluate(time) : fixedValue;
    }

    public override VFXMaterialFunction Copy()
    {
        VFXMaterialFloat function = GenericPool<VFXMaterialFloat>.Get();
        function.fixedValue = fixedValue;
        function.IsCurveMode = IsCurveMode;
        function.curve = curve;
        return function;
    }
}

[Serializable]
public class VFXMaterialVector : VFXMaterialFunction<Vector4>
{
    // TODO: Save some memory and performance to use a dynamic size for Vector3 and Vector2.
    [SerializeField] private AnimationCurve[] curves =
    {
        new AnimationCurve(),
        new AnimationCurve(),
        new AnimationCurve(),
        new AnimationCurve(),
    };

    protected override void ApplyValueImpl(Material material, Vector4 value)
    {
        material.SetVector(name, value);
    }

    protected override Vector4 GetValueFromMaterialImpl(Material material)
    {
        return material.GetVector(name);
    }

    protected override Vector4 EvaluateImpl(Material material, float time)
    {
        return IsCurveMode
            ? new Vector4(
                curves[0].Evaluate(time),
                curves[1].Evaluate(time),
                curves[2].Evaluate(time),
                curves[3].Evaluate(time)
            )
            : fixedValue;
    }

    public override VFXMaterialFunction Copy()
    {
        VFXMaterialVector function = GenericPool<VFXMaterialVector>.Get();
        function.fixedValue = fixedValue;
        function.IsCurveMode = IsCurveMode;
        function.curves = curves;
        return function;
    }
}

[Serializable]
public class VFXMaterialColor : VFXMaterialFunction<Vector4>
{
    [SerializeField] private Gradient gradient = new Gradient();

    protected override void ApplyValueImpl(Material material, Vector4 value)
    {
        material.SetVector(name, value);
    }

    protected override Vector4 GetValueFromMaterialImpl(Material material)
    {
        return material.GetVector(name);
    }

    protected override Vector4 EvaluateImpl(Material material, float time)
    {
        return IsCurveMode ? gradient.Evaluate(time) : fixedValue;
    }

    public override VFXMaterialFunction Copy()
    {
        VFXMaterialColor function = GenericPool<VFXMaterialColor>.Get();
        function.fixedValue = fixedValue;
        function.IsCurveMode = IsCurveMode;
        function.gradient = gradient;
        return function;
    }
}

[Serializable]
public class VFXMaterialKeyword : VFXMaterialFunction<bool>
{
    [SerializeField] private AnimationCurve curve = new AnimationCurve();

    protected override void ApplyValueImpl(Material material, bool value)
    {
        if (value)
        {
            material.EnableKeyword(name);
        }
        else
        {
            material.DisableKeyword(name);
        }
    }

    protected override bool GetValueFromMaterialImpl(Material material)
    {
        return material.IsKeywordEnabled(name);
    }

    protected override bool EvaluateImpl(Material material, float time)
    {
        // Explain the float to bool by greater or less than zero instead, it would be easier to be understood for artists.
        return IsCurveMode ? curve.Evaluate(time) > 0 : fixedValue;
    }

    public override VFXMaterialFunction Copy()
    {
        VFXMaterialKeyword function = GenericPool<VFXMaterialKeyword>.Get();
        function.fixedValue = fixedValue;
        function.IsCurveMode = IsCurveMode;
        function.curve = curve;
        return function;
    }
}

[Serializable]
public class VFXMaterialShaderPass : VFXMaterialFunction<bool>
{
    [SerializeField] private AnimationCurve curve = new AnimationCurve();

    protected override void ApplyValueImpl(Material material, bool value)
    {
        material.SetShaderPassEnabled(name, value);
    }

    protected override bool GetValueFromMaterialImpl(Material material)
    {
        return material.GetShaderPassEnabled(name);
    }

    protected override bool EvaluateImpl(Material material, float time)
    {
        // Explain the float to bool by greater or less than zero instead, it would be easier to be understood for artists.
        return IsCurveMode ? curve.Evaluate(time) > 0 : fixedValue;
    }

    public override VFXMaterialFunction Copy()
    {
        VFXMaterialShaderPass function = GenericPool<VFXMaterialShaderPass>.Get();
        function.fixedValue = fixedValue;
        function.IsCurveMode = IsCurveMode;
        function.curve = curve;
        return function;
    }
}