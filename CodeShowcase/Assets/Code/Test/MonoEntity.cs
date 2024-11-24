using UnityEngine;

/// <summary>
/// It is not proper to register an entity by a MonoBehaviour in game, so this only is a test tool.
/// </summary>
public class MonoEntity : MonoBehaviour
{
    private Entity _entity;
    
    private void OnEnable()
    {
        _entity = EntityManager.instance.CreateEntity(gameObject);
    }

    private void OnDisable()
    {
        EntityManager.instance.RemoveEntity(_entity);
    }
}