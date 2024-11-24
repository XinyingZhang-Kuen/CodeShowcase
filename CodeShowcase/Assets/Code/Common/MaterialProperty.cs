using System.Collections.Generic;

public struct MaterialProperty
{
    private static HashSet<int> pointerCastInts = new HashSet<int>()
    {
        ShaderLibrary.PropertyToID("_RenderingLayer"),
        ShaderLibrary.PropertyToID("_Layer"),
    };
    
    public readonly int propertyID;
    private uint bitmask;
    public bool IsPointerCastInt => (bitmask & (1 << 0)) > 0u;
    
    public MaterialProperty(int propertyID)
    {
        this.propertyID = propertyID;
        
        bitmask = 0u;
        
        if (pointerCastInts.Contains(propertyID))
        {
            bitmask |= 1 << 0;
        }
    }
}