using System;
using UnityEngine;

public class DebugPlayer : MonoBehaviour
{
    [SerializeField] float speed;
    
    Vector2 moveInput;

    void Update()
    {
        moveInput = new (Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        
        transform.Translate(moveInput * (speed * Time.deltaTime));
    }
}
