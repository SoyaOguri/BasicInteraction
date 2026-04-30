using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Camera shifting script
/// </summary>
/// -View shifting with key input- 
/// 
public class CameraShifter : MonoBehaviour
{

    private GameObject cameraShifterObject;

    private Vector3 initLocalPosition;
    private Quaternion initLocalRotation;

    private Vector3 offsetLocalPosition;
    private Quaternion offsetLocalRotation;
    private Vector3 offsetLocalAngles;

    // Use this for initialization
    void Start()
    {
        cameraShifterObject = this.gameObject;
        initLocalPosition = cameraShifterObject.transform.localPosition;
        initLocalRotation = cameraShifterObject.transform.localRotation;
        offsetLocalPosition = Vector3.zero;
        offsetLocalRotation = Quaternion.identity;
    }

    // Use to set the camera base position 
    void SetCameraShift(Vector3 position, Quaternion rotation)
    {
        initLocalPosition = position;
        initLocalRotation = rotation;
    }

    // Reset offset and return camera to base position.
    void ResetCameraOffset()
    {
        offsetLocalAngles = Vector3.zero;
        offsetLocalPosition = Vector3.zero;
        offsetLocalRotation = Quaternion.identity;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float movement = 0f;
        float rotation = 0f;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            movement = 0.1f;
            rotation = 1.0f;
            if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter))
            {
                ResetCameraOffset();
            }
        }
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            movement = 0.5f;
            rotation = 5.0f;
            if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter))
            {
                ResetCameraOffset();
            }
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            offsetLocalPosition.z -= movement;
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            offsetLocalPosition.z += movement;
        }
        if (Input.GetKey(KeyCode.UpArrow))
        {
            offsetLocalPosition.x += movement;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            offsetLocalPosition.x -= movement;
        }
        if (Input.GetKey(KeyCode.KeypadPlus))
        {
            offsetLocalPosition.y += movement;
        }
        if (Input.GetKey(KeyCode.KeypadMinus))
        {
            offsetLocalPosition.y -= movement;
        }
        if (Input.GetKey(KeyCode.PageUp))
        {
            offsetLocalAngles.x -= rotation;
        }
        if (Input.GetKey(KeyCode.PageDown))
        {
            offsetLocalAngles.x += rotation;
        }
        if (Input.GetKey(KeyCode.Period))
        {
            offsetLocalAngles.y += rotation;
        }
        if (Input.GetKey(KeyCode.Comma))
        {
            offsetLocalAngles.y -= rotation;
        }

        offsetLocalRotation.eulerAngles = offsetLocalAngles;

        cameraShifterObject.transform.localPosition =
            initLocalPosition + offsetLocalPosition;
        cameraShifterObject.transform.localRotation =
            initLocalRotation * offsetLocalRotation;
    }

}

