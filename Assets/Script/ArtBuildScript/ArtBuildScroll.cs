using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

public class ArtBuildScroll : MonoBehaviour
{
    [Tooltip("Input list for grabbing input actions from.")]
    [SerializeField] private InputActionAsset masterList;
    private InputAction tapInput; //Input for tapping
    private InputAction cameraMove; //Input for moving the camera

    private bool isPressed; //is the button for moving currently pressed

    [Tooltip("Is the scrolling locked on the Y-Axis?")]
    [SerializeField] private bool lockedY;

    [Tooltip("Multiplier for camera movement speed.")]
    [SerializeField] private float cameraMoveSpeed = 5; //Speed the cam can move at

    private Vector3 moveSlide; //Vector3 for the tiny slide that happens post-movement

    private void OnEnable()
    {
        if (tapInput == null)
        {
            if(masterList != null)
            {
                tapInput = masterList.FindAction("Tap"); //Assign tap input
            }
            else
            {
                Debug.LogError("MasterList not assigned"); //Throw an error if not possible
            }
        }

        if(cameraMove == null)
        {
            cameraMove = masterList.FindAction("Look"); //Assign camera move input
        }

        //Add tap inputs to their respective delegates
        if(tapInput != null)
        {
            tapInput.started += StartCam;
            tapInput.canceled += EndCam;
        }
    }

    private void OnDisable()
    {
        if (tapInput != null)
        {
            tapInput.started -= StartCam;
            tapInput.canceled -= EndCam;
        }
    }


    void LateUpdate()
    {
        //If the button is currently being held down move the camera
        if(isPressed == true)
        {
            moveSlide = cameraMove.ReadValue<Vector2>();

            transform.Translate(moveSlide * cameraMoveSpeed * Time.deltaTime);
        }
        else //If not slide the camera to a stop for a less sticky feeling camera
        {
            if(moveSlide != Vector3.zero)
            {
                moveSlide = Vector3.MoveTowards(moveSlide, Vector3.zero, 50 * Time.deltaTime);
                transform.Translate(moveSlide * Time.deltaTime);
            }
        }
    }

    void StartCam(InputAction.CallbackContext context) //Func for triggering the holding stuff
    {
        if(context.interaction is HoldInteraction)
        {
            isPressed = true;
        }
    }

    void EndCam(InputAction.CallbackContext context) //Func for stopping the holding stuff
    {
        isPressed = false;
    }

}
