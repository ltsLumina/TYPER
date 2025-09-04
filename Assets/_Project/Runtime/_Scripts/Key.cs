using System;
using System.Collections;
using System.Linq;
using Lumina.Essentials.Attributes;
using TMPro;
using UnityEngine;
using VInspector;

public partial class Key : MonoBehaviour
{
    [Tab("Attributes")]
    [Header("Attributes")]
    [SerializeField, ReadOnly] KeyCode keyboardLetter = KeyCode.Q;
    [SerializeField] int damage;
    [SerializeField] bool offGlobalCooldown;
    [SerializeField] bool combo;
    [SerializeField] bool mash;
    [SerializeField, ReadOnly] int comboIndex; 
    
    [Header("Cooldown")]
    [SerializeField] float cooldown = 2.5f;
    [SerializeField, ReadOnly] float remainingCooldown;
    [SerializeField, ReadOnly] float currentCooldown; 
    
    [Tab("Settings")]
    [Header("Settings")]
    [SerializeField] bool isActive = true;
    [Header("Debug Info")]
    [Tooltip("The index of the row this key is in. 1-based.")]
    [SerializeField, ReadOnly] int row; 
    [Tooltip("The index of this key within its row. 0-based.")]
    [SerializeField, ReadOnly] int indexInRow;
    [Tooltip("The index of this key in the entire keyboard. 0-based.")]
    [SerializeField, ReadOnly] int indexGlobal; 
    
    Enemy currentEnemy;
    
    /// <summary>
    /// Action invoked when the key is pressed.
    /// The bool indicates whether an enemy was hit.
    /// The Enemy parameter is the enemy that was hit (null if none).
    /// </summary>
    public event Action<bool, Enemy> OnPressed;

    public SpriteRenderer SpriteRenderer { get; private set; }
    public TMP_Text Letter { get; private set; }
    public SpriteRenderer CooldownSprite { get; private set; }
    public GameObject HomingBar { get; private set; }
    public GameObject oGCDIndicator { get; private set; }
    public GameObject ComboIndicator { get; private set; }
    public TMP_Text DamageText { get; private set; }

    public bool IsActive => isActive;
    public void Disable() => isActive = false;
    public void Enable() => isActive = true;

    void Awake()
    {
        SpriteRenderer = transform.Find("Sprite").GetComponent<SpriteRenderer>();
        Letter = SpriteRenderer.GetComponentInChildren<TMP_Text>();
        CooldownSprite = transform.Find("Cooldown").GetComponent<SpriteRenderer>();
        HomingBar = transform.Find("Homing Bar").gameObject;
        oGCDIndicator = transform.Find("oGCD Indicator").gameObject;
        ComboIndicator = transform.Find("Combo Indicator").gameObject;
        DamageText = transform.Find("Text (Damage)").GetComponent<TMP_Text>();
        
        // Ensure the cooldown sprite is fully opaque at start. In the prefab view the alpha is 0.5f;
        Color color = CooldownSprite.color;
        color.a = 1f;
        CooldownSprite.color = color;
        
        // Hide the homing bar at start.
        HomingBar.SetActive(false);
        
        // Hide the oGCD indicator at start.
        oGCDIndicator.SetActive(false);
        
        // Hide the combo indicator at start.
        ComboIndicator.SetActive(false);
    }

    void Start()
    {
        damage = Mathf.Max(1, Mathf.RoundToInt(indexInRow / 2f));
        DamageText.text = GameManager.Instance.ShowDamageNumbers ? damage.ToString() : string.Empty;
        
        oGCDIndicator.SetActive(offGlobalCooldown);
        ComboIndicator.SetActive(combo);
        
        // reset all fills (hide)
        DrawCooldownFill();
    }
    
    public void InitKey(KeyCode keycode, int row, int indexInRow, int indexGlobal)
    {
        keyboardLetter = keycode;
        this.row = row;
        this.indexInRow = indexInRow;
        this.indexGlobal = indexGlobal;
        
        Letter.text = keycode.ToString();
        Letter.text = Letter.text.Replace("Alpha", ""); // remove "Alpha" from numeric keys

        HomingBar.SetActive(keycode is KeyCode.F or KeyCode.J);
    }
    
    void Update()
    {
        if (!isActive) return;
        
        // Handle per-key cooldown timer
        if (CooldownTime > 0f)
        {
            CooldownTime -= Time.deltaTime;
            if (CooldownTime <= 0f)
            {
                CooldownTime = 0f;
                SetColour(Color.white);
            }
            else // not finished cooldown yet
            {
                SetColour(Color.grey);
                DrawCooldownFill();
            }
        }
    }

    readonly static int Arc2 = Shader.PropertyToID("_Arc2");
    void DrawCooldownFill() => CooldownSprite.material.SetFloat(Arc2, Mathf.Lerp(360f, 0f, CooldownTime / currentCooldown));

    void OnTriggerEnter2D(Collider2D other)
    {
        //Debug.Log($"{name} overlapping with: {other.name}!", other.gameObject);
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
                currentEnemy = null;
        }
    }

    int mashCount;

    /// <summary>
    /// Activates the key, dealing damage to the current enemy if one is present.
    /// </summary>
    /// <returns> True if an enemy was hit, false otherwise. </returns>
    public bool Activate()
    {
        // If this key is inactive, do nothing.
        if (!isActive) return false;
        
        // If this key is on cooldown, do nothing.
        if (CooldownTime > 0f) return false;

        if (Combo)
        {
            //TODO: pseudo-code. NOT FINAL!!!!
            
            StartLocalCooldown(0.25f);
            comboIndex++;

            if (comboIndex >= 3)
            {
                FindObjectsByType<Enemy>(FindObjectsSortMode.None).ToList().ForEach(e =>
                {
                    // get the 8 surrounding keys
                    var surroundingKeys = KeyController.Instance.AllKeys.Where(k =>
                        Math.Abs(k.indexInRow - indexInRow) <= 1 &&
                        Math.Abs(Array.IndexOf(KeyController.Instance.AllKeys.ToArray(), k) - Array.IndexOf(KeyController.Instance.AllKeys.ToArray(), this)) <= 1 && k != this).ToList();
                    
                    Debug.Log($"Combo finished! Dealing 3 damage to all enemies in surrounding keys: {string.Join(", ", surroundingKeys.Select(k => k.name))}");

                    if (surroundingKeys.Any(k => k.currentEnemy == e))
                    {
                        e.TakeDamage(3);
                        Debug.Log($"Dealt 3 damage to {e}!");
                    }
                });
            }

            return false;
        }

        // TODO: ALSO NOT FINAL!!!!! JUST TESTING
        if (Mash)
        {
            // does nothing until its been mashed 5 times
            mashCount++;
            
            if (mashCount < 5)
            {
                SetColour(Color.yellow, 0.25f);
                return false;
            }
            else
            {
                mashCount = 0;
                Debug.Log("Mash action activated!");
            }
        }
        
        // If this key is off the global cooldown, start its own cooldown rather than letting the KeyController put it on global cooldown.
        if (OffGlobalCooldown) StartLocalCooldown(cooldown);
        else KeyController.Instance.StartGlobalCooldown();

        if (currentEnemy)
        {
            currentEnemy.TakeDamage(damage);
            Debug.Log($"Dealt 1 damage to {currentEnemy}!");

            // If this is a combo action, do not invoke the OnPressed event yet. (don't invoke the global cooldown)
            if (!offGlobalCooldown) OnPressed?.Invoke(true, currentEnemy);

            // Flash the key green to indicate it was pressed.
            SetColour(Color.green, 0.75f);
            return true;
        }

        // Flash the key red to indicate a miss.
        SetColour(Color.crimson, 0.5f);
        if (!offGlobalCooldown) OnPressed?.Invoke(false, null);
        return false;
    }

    public void StartLocalCooldown(float cooldown)
    {
        CooldownTime = cooldown;
        currentCooldown = cooldown;
    }

    void SetColour(Color color)
    {
        if (SpriteRenderer.color == Color.green || SpriteRenderer.color == Color.crimson) return;
        SpriteRenderer.color = color;
    }

    void SetColour(Color color, float duration)
    {
        StartCoroutine(SwitchColour(color, duration));
    }

    IEnumerator SwitchColour(Color colour, float duration)
    {
        SpriteRenderer.color = colour;
        yield return new WaitForSeconds(duration);
        SpriteRenderer.color = Color.clear;
    }
}
