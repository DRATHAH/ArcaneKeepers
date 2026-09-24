using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

public class ArtBuildScroll : MonoBehaviour
{
    [SerializeField] private InputActionAsset masterList;
    private InputAction tapInput;
    private InputAction cameraMove;

    private bool isPressed;

    [SerializeField] private bool lockedY;

    private Vector3 newDestination;

    [SerializeField] private float cameraMoveSpeed = 5;

    [SerializeField] private Rigidbody rb;

    public float heldTimer;

    private void OnEnable()
    {
        if (masterList != null)
        {
            tapInput = masterList.FindAction("Tap");
        }

        if (tapInput == null)
        {
            if(masterList == null)
            {
                tapInput = masterList.FindAction("Tap");
            }
            else
            {
                Debug.LogError("MasterList not assigned");
            }
        }

        if(cameraMove == null)
        {
            cameraMove = masterList.FindAction("Look");
        }

        if(tapInput != null)
        {
            tapInput.started += StartCam;
            tapInput.canceled += EndCam;
        }

    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void LateUpdate()
    {
        
        if(isPressed == true)
        {
            Vector3 moveInput = cameraMove.ReadValue<Vector2>();

            transform.Translate(moveInput * cameraMoveSpeed * Time.deltaTime);
        }


        
        transform.Translate(newDestination);
    }

    void StartCam(InputAction.CallbackContext context)
    {
        if(context.interaction is HoldInteraction)
        {
            heldTimer = Time.time;
            isPressed = true;
        }
    }

    void EndCam(InputAction.CallbackContext context)
    {
        isPressed = false;
        heldTimer = -1;
    }

}
