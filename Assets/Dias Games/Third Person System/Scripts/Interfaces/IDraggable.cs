using UnityEngine;

public interface IDraggable : IHandIKTarget, ICharacterTargetPos
{
    void StartDrag();
    void StopDrag();

    bool Move(Vector3 velocity);
}