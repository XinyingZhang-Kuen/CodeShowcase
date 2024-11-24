using System.Collections.Generic;
using UnityEngine;

public class VFXMaterialState : VFXState<VFXMaterialConfig>
{
    public List<MaterialWrapper> materials = new();
    public readonly List<VFXMaterialModifier> modifiers = new();
}

/// <summary>
/// TODO:
/// - Add index redirect middleware to adapt to dynamic material count and indices if I have time.
///   We can dispatch an event with an material index remap dictionary to sync all data correctly when material list changed.
/// - We should also fetch data from renderer and material wrapper instead of get from component.
/// </summary>
public class VFXMaterialSystem : VFXSystem<VFXMaterialConfig, VFXMaterialState>
{
    private List<MaterialWrapper> materials = new();
    private EntityRendering _entityRendering;

    public override void Init()
    {
        if (!trigger.gameObject)
        {
            Debug.LogError($"GameObject is null or destroyed!");
            return;
        }
        
        Entity entity = EntityManager.instance.FindEntity(trigger.gameObject);
        if (entity == null)
        {
            GameObject gameObject = trigger.gameObject;
            Debug.LogError($"Can't find entity of gameObject {gameObject.name}", gameObject);
            return;
        }

        _entityRendering = entity.entityRendering;
    }

    protected override void OnStateInit(VFXMaterialState state)
    {
        base.OnStateInit(state);
        state.materials = materials;

        for (int i = 0; i < state.config.modifiers.Count; i++)
        {
            state.modifiers.Add(state.config.modifiers[i].Copy());
            state.modifiers[i].Init(state.config.name);
        }
    }

    protected override void OnPerform(VFXMaterialState state)
    {
        base.OnPerform(state);

        float timeInStage = state.timeInStage / state.StageDuration;
        foreach (VFXMaterialModifier function in state.config.modifiers)
        {
            function.Apply(state, timeInStage);
        }
    }

    protected override void OnClear(VFXMaterialState state)
    {
        base.OnClear(state);

        foreach (VFXMaterialModifier function in state.config.modifiers)
        {
            function.Revert(materials);
        }
    }
}