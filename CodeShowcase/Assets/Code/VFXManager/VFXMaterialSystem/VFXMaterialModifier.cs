using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Pool;

[Serializable]
public abstract class VFXMaterialModifier
{
    public ValueOverriderSource source { get; private set; }
    public string propertyName;
    public int id { get; private set; }

    public abstract void Apply(VFXMaterialState state, float time);
    public abstract void Revert(List<MaterialWrapper> materials);
    public abstract VFXMaterialModifier Copy();
    
    public void Init(string assetName)
    {
        source = new ValueOverriderSource(assetName);
        id = ShaderLibrary.PropertyToID(propertyName);
    }

    public virtual void Clear()
    {
        source = default;
        propertyName = default;
        id = default;
    }
}

[Serializable]
public abstract class VFXMaterialModifier<T> : VFXMaterialModifier
{
    [SerializeField] protected T[] fixedValues = new T[3];
    [SerializeField] private uint _stateBitmask;
    private const uint FlagsUseCurve = 1 << 0;
    private List<ValueOverrider<T>> _overriders = new List<ValueOverrider<T>>();
    private bool _firstTimeApply;

    protected bool isCurveMode
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

    public sealed override void Apply(VFXMaterialState state, float time)
    {
        if (_overriders.Count == 0 && state.materials.Count != 0)
        {
            _overriders.Add(null);
        }
        for (int index = 0; index < state.materials.Count; index++)
        {
            MaterialWrapper material = state.materials[index];
            T value = EvaluateImpl(material, state.stage, time);
            // Counter +1 only when it's the first time apply of this modifier instance.
            ValueOverrider<T> overrider = _overriders[index];
            if (overrider == null)
            {
                overrider = _overriders[index] = GetOverriderImpl(material);
                overrider.Push(source, value, state.config.priority);
            }
            else
            {
                overrider.Set(source, value);
            }
        }
    }
    
    public sealed override void Revert(List<MaterialWrapper> materials)
    {
        for (int index = 0; index < materials.Count; index++)
        {
            _overriders[index]?.Pop(source, true);
            _overriders[index] = null;
        }
    }

    protected abstract ValueOverrider<T> GetOverriderImpl(MaterialWrapper material);
    protected abstract T EvaluateImpl(MaterialWrapper materials, VFXStage stage, float time);

    public override void Clear()
    {
        fixedValues = default;
        _stateBitmask = default;
    }
}

// In the source code of Unity engine, they force cast the int value to float which will lost precision if the number is too big.
// So here we transfer the int to float without change the value of this address by pointer. 
[Serializable]
public class VFXMaterialModifierInt : VFXMaterialModifier<int>
{

    [SerializeField] private AnimationCurve[] curves = new AnimationCurve[VFXSystem.VFXStageCount]
    {
        new AnimationCurve(),
        new AnimationCurve(),
        new AnimationCurve(),
    };

    protected override ValueOverrider<int> GetOverriderImpl(MaterialWrapper material)
    {
        return material.GetOverriderInt(this);
    }
    
    protected override int EvaluateImpl(MaterialWrapper material, VFXStage stage, float time)
    {
        return isCurveMode ? (int)curves[(int)stage].Evaluate(time) : fixedValues[(int)stage];
    }

    /// <summary>
    /// TODO: Separate config and runtime state classes, so we don't have to copy anymore.
    /// </summary>
    /// <returns></returns>
    public override VFXMaterialModifier Copy()
    {
        VFXMaterialModifierInt function = GenericPool<VFXMaterialModifierInt>.Get();
        function.fixedValues = fixedValues;
        function.isCurveMode = isCurveMode;
        function.curves = curves;
        return function;
    }
}

[Serializable]
public class VFXMaterialModifierFloat : VFXMaterialModifier<float>
{
    [SerializeField] private AnimationCurve[] curves = new AnimationCurve[VFXSystem.VFXStageCount]
    {
        new AnimationCurve(),
        new AnimationCurve(),
        new AnimationCurve(),
    };

    protected override ValueOverrider<float> GetOverriderImpl(MaterialWrapper material)
    {
        return material.GetOverriderFloat(this);
    }

    protected override float EvaluateImpl(MaterialWrapper material, VFXStage stage, float time)
    {
        return isCurveMode ? curves[(int)stage].Evaluate(time) : fixedValues[(int)stage];
    }

    public override VFXMaterialModifier Copy()
    {
        VFXMaterialModifierFloat function = GenericPool<VFXMaterialModifierFloat>.Get();
        function.fixedValues = fixedValues;
        function.isCurveMode = isCurveMode;
        function.curves = curves;
        return function;
    }
}

[Serializable]
public class VFXMaterialModifierVector : VFXMaterialModifier<Vector4>
{
    [Serializable]
    public class VectorCurves
    {
        // TODO: Save some memory and performance to use a dynamic size for Vector3 and Vector2.
        public AnimationCurve[] curves = new AnimationCurve[4]
        {
            new AnimationCurve(),
            new AnimationCurve(),
            new AnimationCurve(),
            new AnimationCurve(),
        };
        
        public AnimationCurve this[int index]
        {
            get
            {
                return curves[index];
            }    
        }
    }
    
    [SerializeField] private VectorCurves[] curves = new VectorCurves[VFXSystem.VFXStageCount]
    {
        new VectorCurves(),
        new VectorCurves(),
        new VectorCurves(),
    };

    protected override ValueOverrider<Vector4> GetOverriderImpl(MaterialWrapper material)
    {
        return material.GetOverriderVector(this);
    }

    protected override Vector4 EvaluateImpl(MaterialWrapper material, VFXStage stage, float time)
    {
        int stageIndex = (int)stage;
        return isCurveMode
            ? new Vector4(
                curves[stageIndex][0].Evaluate(time),
                curves[stageIndex][1].Evaluate(time),
                curves[stageIndex][2].Evaluate(time),
                curves[stageIndex][3].Evaluate(time)
            )
            : fixedValues[stageIndex];
    }

    public override VFXMaterialModifier Copy()
    {
        VFXMaterialModifierVector function = GenericPool<VFXMaterialModifierVector>.Get();
        function.fixedValues = fixedValues;
        function.isCurveMode = isCurveMode;
        function.curves = curves;
        return function;
    }
}

[Serializable]
public class VFXMaterialModifierColor : VFXMaterialModifier<Color>
{
    [SerializeField] private Gradient[] curves = new Gradient[VFXSystem.VFXStageCount]
    {
        new Gradient(),
        new Gradient(),
        new Gradient(),
    };

    protected override ValueOverrider<Color> GetOverriderImpl(MaterialWrapper material)
    {
        return material.GetOverriderColor(this);
    }

    protected override Color EvaluateImpl(MaterialWrapper material, VFXStage stage, float time)
    {
        return isCurveMode ? curves[(int)stage].Evaluate(time) : fixedValues[(int)stage];
    }

    public override VFXMaterialModifier Copy()
    {
        VFXMaterialModifierColor function = GenericPool<VFXMaterialModifierColor>.Get();
        function.fixedValues = fixedValues;
        function.isCurveMode = isCurveMode;
        function.curves = curves;
        return function;
    }
}

[Serializable]
public class VFXMaterialModifierTexture : VFXMaterialModifier<Texture>
{
    protected override ValueOverrider<Texture> GetOverriderImpl(MaterialWrapper material)
    {
        return material.GetOverriderTexture(this);
    }

    protected override Texture EvaluateImpl(MaterialWrapper material, VFXStage stage, float time)
    {
        return fixedValues[(int)stage];
    }

    public override VFXMaterialModifier Copy()
    {
        VFXMaterialModifierTexture function = GenericPool<VFXMaterialModifierTexture>.Get();
        function.fixedValues = fixedValues;
        function.isCurveMode = isCurveMode;
        return function;
    }
}
[Serializable]
public class VFXMaterialModifierKeyword : VFXMaterialModifier<bool>
{
    [SerializeField] private AnimationCurve[] curves = new AnimationCurve[VFXSystem.VFXStageCount]
    {
        new AnimationCurve(),
        new AnimationCurve(),
        new AnimationCurve(),
    };

    protected override ValueOverrider<bool> GetOverriderImpl(MaterialWrapper material)
    {
        return material.GetOverriderKeyword(this);
    }

    protected override bool EvaluateImpl(MaterialWrapper material, VFXStage stage, float time)
    {
        return isCurveMode ? curves[(int)stage].Evaluate(time) > 0 : fixedValues[(int)stage];
    }

    public override VFXMaterialModifier Copy()
    {
        VFXMaterialModifierKeyword function = GenericPool<VFXMaterialModifierKeyword>.Get();
        function.fixedValues = fixedValues;
        function.isCurveMode = isCurveMode;
        function.curves = curves;
        return function;
    }
}

[Serializable]
public class VFXMaterialModifierShaderPass : VFXMaterialModifier<bool>
{
    [SerializeField] private AnimationCurve[] curves = new AnimationCurve[VFXSystem.VFXStageCount]
    {
        new AnimationCurve(),
        new AnimationCurve(),
        new AnimationCurve(),
    };

    protected override ValueOverrider<bool> GetOverriderImpl(MaterialWrapper material)
    {
        return material.GetOverriderPass(this);
    }

    protected override bool EvaluateImpl(MaterialWrapper material, VFXStage stage, float time)
    {
        return isCurveMode ? curves[(int)stage].Evaluate(time) > 0 : fixedValues[(int)stage];
    }

    public override VFXMaterialModifier Copy()
    {
        VFXMaterialModifierShaderPass function = GenericPool<VFXMaterialModifierShaderPass>.Get();
        function.fixedValues = fixedValues;
        function.isCurveMode = isCurveMode;
        function.curves = curves;
        return function;
    }
}