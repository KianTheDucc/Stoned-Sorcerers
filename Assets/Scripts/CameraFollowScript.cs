using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollowScript : MonoBehaviour
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private Camera virtcam;
    Vector3 cameraRotation;

    public Vector3 lerpMouse;
    // Start is called before the first frame update
    void Start()
    {
        cameraRotation = Camera.main.transform.localRotation.eulerAngles;
    }

    // Update is called once per frame
    void Update()
    {
        lerpMouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);



        //cameraRotation.y -= lerpMouse.x;

        //Camera.main.transform.localRotation = Quaternion.Euler(cameraRotation);

        Debug.Log(lerpMouse);
        
        lerpMouse = new Vector3(Mathf.Round(lerpMouse.x * 10.0f), Mathf.Round(lerpMouse.y * 10.0f), Mathf.Round(lerpMouse.z * 10.0f));
        Debug.Log(lerpMouse);
        gameObject.transform.position = Vector3.Lerp(transform.position, lerpMouse, speed * Time.deltaTime);
    }
}
