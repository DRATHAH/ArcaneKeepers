using UnityEngine;

public class CameraClamp : MonoBehaviour
{
    [Tooltip("Determines the upper bounds of screen scrolling.")]
    [SerializeField] private Vector3 upperRightBorder = Vector3.zero;

    [Tooltip("Determines the lower bounds of screens scrolling")]
    [SerializeField] private Vector3 lowerLeftBorder = Vector3.zero;

    void Update()
    {
        AdjustPos();
    }

    void AdjustPos()
    {
        Vector3 resetPosition = transform.position;

        //Check if the position is in conflict with any borders for x
        if (transform.position.x > upperRightBorder.x)
        {
            resetPosition.x = upperRightBorder.x; //if greater than uprightborder set it to the border's pos
        }
        else if (transform.position.x < lowerLeftBorder.x)
        {

            resetPosition.x = lowerLeftBorder.x; //if less than lowerleft set it to the border's pos
        }

        //Check if position is in conflict with any borders for y
        if (transform.position.y > upperRightBorder.y)
        {
            resetPosition.y = upperRightBorder.y; //if greater than uprightborder set it to the border's pos
        }
        else if (transform.position.y < lowerLeftBorder.y)
        {

            resetPosition.y = lowerLeftBorder.y; //if less than lowerleft set it to the border's pos
        }

        transform.position = resetPosition; //Set position
    }
}
