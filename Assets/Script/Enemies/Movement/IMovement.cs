using UnityEngine;

public interface IMovement
{
    [HideInInspector] public bool pushed { get; }   
    public void PauseMovement();
    public void Push(Vector3 pushForce);

    public void Push(Vector3 pushForce, bool invincible);
}
