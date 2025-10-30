using System.Collections.Generic;
using UnityEngine;

// Starkie, M.
[CreateAssetMenu(fileName = "ShipCoordinateRegistry", menuName = "Ship/ShipCoordinateRegistry")]
public class ShipCoordinateRegistry : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public ulong tagId;          // AprilTag ID
        public Vector3 shipPosition; // meters in ship frame
        public Vector3 shipEuler;    // degrees in ship frame (Z-up or Y-up per your convention)
    }

    public List<Entry> entries = new();
    Dictionary<ulong, Pose> _cache;

    void OnEnable()
    {
        Debug.Log("[ARAccuracy]->OnEnable");
        _cache = new();
        foreach (var e in entries)
        {
            _cache[e.tagId] = new Pose(e.shipPosition, Quaternion.Euler(e.shipEuler));
        }
    }

    public bool TryGetShipPose(ulong tagId, out Pose shipPose)
    {
        Debug.Log("[ARAccuracy]->TryGetShipPose");
        return _cache.TryGetValue(tagId, out shipPose);
    }
}
