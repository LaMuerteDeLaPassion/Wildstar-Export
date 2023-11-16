using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class WorldCamera : MonoBehaviour
{
    public float cameraMoveSpeed = 0.5f;
    public float shiftCameraMoveSpeed = 2;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None; // set to Confined to block mouse inside game
    }

    void Update()
    {
        // Ignore if over UI
        if(EventSystem.current.IsPointerOverGameObject()){
            return;
        }
        if (Input.GetMouseButtonDown(2)){
            Cursor.lockState = CursorLockMode.Locked;
        }
        if(Input.GetMouseButton(2)){
            var movement = this.gameObject.transform.parent.parent.position;
            var fwd = this.gameObject.transform.parent.parent.forward;
            var rht = this.gameObject.transform.parent.parent.right;
            movement += rht * 1.0f * Input.GetAxis("Mouse X");
            movement += fwd * 1.0f * Input.GetAxis("Mouse Y");
            this.gameObject.transform.parent.parent.position = movement;
        }
        if(Input.GetMouseButton(1)){
            this.gameObject.transform.parent.parent.Rotate(new Vector3(0,1,0), 10.0f * Input.GetAxis("Mouse X"));
            this.gameObject.transform.parent.Rotate(new Vector3(1,0,0), -5.0f * Input.GetAxis("Mouse Y"));
        }
        if (Input.GetMouseButtonUp(2)){
            Cursor.lockState = CursorLockMode.None;
        }
        this.ApplyScroll();
    }
    public void ApplyScroll(){
        float deadZone = 0.01f;
        float easeFactor = 10f;
        float ScrollWheelValue = Input.GetAxis("Mouse ScrollWheel") * easeFactor;

        if((ScrollWheelValue > -deadZone && ScrollWheelValue<deadZone) || ScrollWheelValue == 0f){
            return;
        }

        var scrollVector = new Vector3(0,0,ScrollWheelValue);
        this.transform.Translate(scrollVector);
    }
}
