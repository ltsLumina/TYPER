using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    [SerializeField] GameObject cursor;

    void Update()
    {
        cursor.transform.SetParent(EventSystem.current.currentSelectedGameObject?.transform);
        cursor.transform.localPosition = new Vector3(-200, 0, 0);
    }
}
