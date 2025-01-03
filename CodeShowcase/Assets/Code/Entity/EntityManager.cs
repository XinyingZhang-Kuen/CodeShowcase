﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class EntityManager
{
    public static EntityManager instance { get; private set; } = new EntityManager();

    private Dictionary<GameObject, Entity> _entities = new Dictionary<GameObject, Entity>();

    public Entity CreateEntity(GameObject gameObject)
    {
        Entity entity = GenericPool<Entity>.Get();
        entity.Init(gameObject);
        if (gameObject)
        {
            _entities[gameObject] = entity;
        }
        return entity;
    }

    public Entity FindEntity(GameObject gameObject)
    {
        _entities.TryGetValue(gameObject, out Entity entity);
        return entity;
    }

    public void RemoveEntity(Entity entity)
    {
        _entities.Remove(entity.gameObject);
    }

    /// <summary>
    /// There may still be some logic running in late update, so it would be 
    /// The submit of material data when culling happens in SRP.
    /// </summary>
    public void BeforeRendering()
    {
        foreach (Entity entity in _entities.Values)
        {
            entity.BeforeRendering();
        }
    }
}