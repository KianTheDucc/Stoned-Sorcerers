using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class charController : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform cam;
    [SerializeField] private float speed;
    [SerializeField] private float jumpForce;
    [SerializeField] private GameObject GroundCheckObject;
    [SerializeField] private LayerMask GroundLayer;
    public Vector2 lerpMouse;

    void Start()
    {
        cam = Camera.main.transform;
    }

    void Update()
    {

    }

    private void FixedUpdate()
    {
        Movement();
        Jump();
    }
    void Movement()
    {
        Vector3 forwardMovement = cam.forward * Input.GetAxis("Vertical");
        Vector3 horizontalMovement = cam.right * Input.GetAxis("Horizontal");
        Vector3 movement = Vector3.ClampMagnitude(forwardMovement + horizontalMovement, 1);

        rb.AddForce(movement * speed * Time.deltaTime, ForceMode.Impulse);
        if (forwardMovement == new Vector3(0, 0, 0) && horizontalMovement == new Vector3(0, 0, 0))
        {
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
        }
    }

    void Jump()
    {
        if (GroundCheck() && Input.GetKey(KeyCode.Space))
        {
            rb.AddForce(new Vector3(0, jumpForce * Time.deltaTime, 0), ForceMode.Impulse);
        }
    }

    public bool GroundCheck()
    {
        return Physics.CheckSphere(GroundCheckObject.transform.position, 0.3f, GroundLayer);  
    }
}
