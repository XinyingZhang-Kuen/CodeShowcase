using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class VFXMaterialState : VFXState<VFXMaterialConfig>
{
    public List<Material> materials = new List<Material>();
    public readonly List<VFXMaterialFunction> fadeInFunctions = new List<VFXMaterialFunction>();
    public readonly List<VFXMaterialFunction> loopFunctions = new List<VFXMaterialFunction>(); 
    public readonly List<VFXMaterialFunction> fadeOutFunctions = new List<VFXMaterialFunction>(); 
}

/// <summary>
/// TODO:
/// - Add index redirect middleware to adapt to dynamic material count and indices if I have time.
///   We can dispatch an event with an material index remap dictionary to sync all data correctly when material list changed.
/// - We should also fetch data from renderer and material wrapper instead of get from component.
/// </summary>
public class VFXMaterialSystem : VFXSystem<VFXSystem, VFXMaterialConfig, VFXMaterialState>
{
    private List<Material> materials = new List<Material>();
    
    public override void Init()
    {
        using (GetComponentsInChildrenScope<Renderer> scope = new GetComponentsInChildrenScope<Renderer>())
        {
            if (scope.components.Count == 0)
                return;

            List<Material> rendererMaterials = ListPool<Material>.Get();
            foreach (Renderer renderer in scope.components)
            {
                renderer.GetSharedMaterials(rendererMaterials);
                materials.AddRange(rendererMaterials);
                rendererMaterials.Clear();
            }
            ListPool<Material>.Release(rendererMaterials);
        }
    }
    
    protected override void OnStateInit(VFXMaterialState state)
    {
        base.OnStateInit(state);
        state.materials = materials;
        
        for (int i = 0; i < state.config.fadeInFunctions.Count; i++)
        {
            state.fadeInFunctions.Add(state.config.fadeInFunctions[i].Copy());
        }
        
        if ((state.config.features & VFXSystemFeatures.Staging) != 0)
        {
            for (int i = 0; i < state.config.loopFunctions.Count; i++)
            {
                state.loopFunctions.Add(state.config.loopFunctions[i].Copy());
            }

            for (int i = 0; i < state.config.fadeOutFunctions.Count; i++)
            {
                state.fadeOutFunctions.Add(state.config.fadeOutFunctions[i].Copy());
            }
        }
    }

    protected override void OnPerform(VFXMaterialState state)
    {
        base.OnPerform(state);

        List<VFXMaterialFunction> functions = state.config.GetCurrentStageFunctions(state.stage);
        foreach (VFXMaterialFunction function in functions)
        {
            function.Apply(state.materials, state.timeInStage / state.StageDuration);
        }
    }

    protected override void OnClear(VFXMaterialState state)
    {
        base.OnClear(state);

        foreach (VFXMaterialFunction function in state.config.fadeInFunctions)
        {
            function.Revert(materials);
            state.config.fadeInFunctions.Clear();
        }

        if ((state.config.features & VFXSystemFeatures.Staging) != 0)
        {
            foreach (VFXMaterialFunction function in state.config.loopFunctions)
            {
                function.Revert(materials);
                state.config.loopFunctions.Clear();
            }
            foreach (VFXMaterialFunction function in state.config.fadeOutFunctions)
            {
                function.Revert(materials);
                state.config.fadeOutFunctions.Clear();
            }
        }
    }
}