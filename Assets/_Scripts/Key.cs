using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class Key : MonoBehaviour
{
    [SerializeField] KeyCode keyboardLetter = KeyCode.Q;
    
    Enemy currentEnemy;
    
    /// <summary>
    /// Action invoked when the key is pressed.
    /// The bool indicates whether an enemy was hit.
    /// The Enemy parameter is the enemy that was hit (null if none).
    /// </summary>
    public event Action<bool, Enemy> OnPressed;

    public SpriteRenderer SpriteRenderer { get; private set; }
    public TMP_Text Letter { get; private set; }

    public KeyCode KeyboardLetter
    {
        get => keyboardLetter;
        set => keyboardLetter = value;
    }

    void Awake()
    {
        SpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        Letter = GetComponentInChildren<TMP_Text>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"Collided with: {other.name}!");
        if (other.TryGetComponent(out Enemy enemy))
        {
            currentEnemy = enemy;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent(out Enemy enemy))
        {
            if (currentEnemy == enemy)
            {
                currentEnemy = null;
            }
        }
    }

    public bool Activate()
    {
        // If the global cooldown is active, do nothing.
        if (KeyController.Instance.OnCooldown) return false;
        
        //Debug.Log($"I was just pressed! (Key:  {keyboardLetter})");

        SetColour(Color.green, KeyController.Instance.GlobalCooldown);
        
        if (currentEnemy != null)
        {
            currentEnemy.TakeDamage(1);
            Debug.Log($"Dealt 1 damage to {currentEnemy}!");
            OnPressed?.Invoke(true, currentEnemy);
            return true;
        }
        
        OnPressed?.Invoke(false, null);
        return false;
    }

    public void SetColour(Color colour)
    {
        SpriteRenderer.color = colour;
    }
    
    public void SetColour(Color colour, float duration)
    {
        // Prevent overlapping colour changes.
        if (SpriteRenderer.color == Color.green) return;
        
        StartCoroutine(ColourSwitch(colour, duration));
    }

    IEnumerator ColourSwitch(Color colour, float duration = 1f)
    {
        SpriteRenderer.color = colour;

        yield return new WaitForSeconds(duration);

        SpriteRenderer.color = Color.white;
    }
}
