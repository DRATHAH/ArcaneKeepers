using UnityEngine;

public interface IMovement
{
    
    public void PauseMovement();
    public void Push(Vector3 pushForce);

    public void Push(Vector3 pushForce, bool invincible);
}
