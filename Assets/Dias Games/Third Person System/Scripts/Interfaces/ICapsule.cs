using UnityEngine;

namespace DiasGames.Components
{
    public interface ICapsule 
    {
        void SetCapsuleSize(float newHeight, float newRadius);
        void ResetCapsuleSize();
        float GetCapsuleHeight();
        float GetCapsuleRadius();

        void EnableCollision();
        void DisableCollision();
    }
}