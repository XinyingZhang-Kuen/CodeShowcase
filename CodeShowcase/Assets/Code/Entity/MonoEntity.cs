using UnityEngine;

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