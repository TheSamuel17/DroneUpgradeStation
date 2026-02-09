using UnityEngine;

namespace DroneUpgradeStation
{
    public static class Utils
    {
        public static void RemoveComponent<T>(this GameObject go) where T : Component
        {
            if (go.TryGetComponent<T>(out var component))
            {
                Component.Destroy(component);
            }
        }
    }
}
