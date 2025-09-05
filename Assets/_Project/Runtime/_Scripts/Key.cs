using System;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField, ReadOnly] int mashCount; 
    
    [Header("Cooldown")]
    [SerializeField] float cooldown = 2.5f;
    [SerializeField, ReadOnly] float remainingCooldown;
    [SerializeField, ReadOnly] float currentCooldown; 
    
    [Tab("References")]
    [SerializeField] GameObject comboIndicator;
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] TMP_Text letter;
    [SerializeField] SpriteRenderer cooldownSprite;
    [SerializeField] GameObject homingBar;
    [SerializeField] GameObject oGCDMarker;
    [SerializeField] GameObject comboMarker;
    [SerializeField] GameObject mashMarker;
    [SerializeField] TMP_Text damageText;
    
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
    ComboController comboController;

    public GameObject ComboIndicator => comboIndicator;
    public SpriteRenderer SpriteRenderer => spriteRenderer;
    public TMP_Text Letter => letter;
    public SpriteRenderer CooldownSprite => cooldownSprite;
    public GameObject HomingBar => homingBar;
    public GameObject offGCDMarker => oGCDMarker;
    public GameObject ComboMarker => comboMarker;
    public GameObject MashMarker => mashMarker;
    public TMP_Text DamageText => damageText;

    public bool IsActive => isActive;
    public void Disable()
    {
        isActive = false;
        SetColour(Color.grey);
    }

    public void Enable()
    {
        isActive = true;
        SetColour(Color.white);
    }

    public int ComboIndex
    {
        get => comboIndex;
        set => comboIndex = value;
    }

    void Awake()
    {
        // Ensure the cooldown sprite is fully opaque at start. In the prefab view the alpha is 0.5f;
        Color color = CooldownSprite.color;
        color.a = 1f;
        CooldownSprite.color = color;
        
        ComboIndicator.SetActive(false);
        HomingBar.SetActive(false);
        offGCDMarker.SetActive(false);
        ComboMarker.SetActive(false);
        MashMarker.SetActive(false);
    }

    void Start()
    {
        comboController = ComboController.Instance;
        
        damage = Mathf.Max(1, Mathf.RoundToInt(indexInRow / 2f));
        
        oGCDMarker.SetActive(offGlobalCooldown);
        ComboMarker.SetActive(combo);
        MashMarker.SetActive(mash);
        DamageText.text = Mash ? mashCount.ToString() : string.Empty;
        
        // reset all fills (hide)
        DrawCooldownFill();
        
        Assert();
    }
    
    void Assert()
    {
        Debug.Assert(ComboIndicator != null, $"{name} is missing a reference to its ComboIndicator!");
        Debug.Assert(SpriteRenderer != null, $"{name} is missing a reference to its SpriteRenderer!");
        Debug.Assert(Letter != null, $"{name} is missing a reference to its Letter TMP_Text!");
        Debug.Assert(CooldownSprite != null, $"{name} is missing a reference to its CooldownSprite!");
        Debug.Assert(HomingBar != null, $"{name} is missing a reference to its HomingBar!");
        Debug.Assert(offGCDMarker != null, $"{name} is missing a reference to its offGCDMarker!");
        Debug.Assert(ComboMarker != null, $"{name} is missing a reference to its ComboMarker!");
        Debug.Assert(MashMarker != null, $"{name} is missing a reference to its MashMarker!");
        Debug.Assert(DamageText != null, $"{name} is missing a reference to its DamageText!");
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

    Coroutine mashTimerCoroutine;

    /// <summary>
    /// Activates the key, dealing damage to the current enemy if one is present.
    /// </summary>
    /// <param name="overrideGlobalCooldown"> If true, the key will not trigger the global cooldown when pressed. (Overrides the OffGlobalCooldown property) </param>
    /// <returns> True if an enemy was hit, false otherwise. </returns>
    public void Activate(bool overrideGlobalCooldown = false, float cooldownOverride = -1f)
    {
        // If this key is inactive, do nothing.
        if (!isActive) return;

        // If this key is on cooldown, do nothing.
        // if this key is called again with a cooldown override, ignore the cooldown check.
        if (CooldownTime > 0f && !overrideGlobalCooldown) return;
        
        if (!comboController.CurrentComboKeys.Contains(this) && comboController.CurrentComboKeys.Count > 0)
        {
            // If the key pressed is not part of the current combo, break the combo.
            comboController.ResetCombo();
        }

        switch (Combo)
        {
            case false:
                // If this key is not part of a combo, but a combo is active, break the combo.
                comboController.ResetCombo();
                break;

            case true: {
                int nextKeyIndex = comboController.NextComboIndex;

                // When a combo is started, the nextKeyIndex is set to 1 (the second key in the combo).
                if (comboIndex == 0)
                {
                    comboController.BeginCombo(keyboardLetter);
                    Debug.Log($"Hit combo key {keyboardLetter} correctly!");
                    
                    StartLocalCooldown(cooldown);
                    ComboIndicator.gameObject.SetActive(false);
                    return;
                }

                // Combo Progression
                if (comboIndex == nextKeyIndex)
                {
                    // Combo Progressed
                    comboController.AdvanceCombo(keyboardLetter);
                    
                    // Combo Completed
                    if (comboIndex == comboController.ComboLength - 1)
                    {
                        var vfx = Resources.Load<ParticleSystem>("PREFABS/Combo Effect");

                        List<Key> surroundingKeys = KeyController.Instance.GetSurroundingKeys(keyboardLetter, true);

                        foreach (var key in surroundingKeys)
                        {
                            var instantiate = Instantiate(vfx, key.transform.position, Quaternion.identity);
                            ParticleSystem.MainModule instantiateMain = instantiate.main;
                            instantiateMain.startColor = Color.cyan;

                            key.Activate(true, 0.5f);
                        }
                        return;
                    }

                    StartLocalCooldown(cooldown);
                    ComboIndicator.gameObject.SetActive(false);
                    return;
                }
                
                // Non-sequential key in current combo pressed, break the combo.
                comboController.ResetCombo();
                break;
            }
        }

        // TODO: ALSO NOT FINAL!!!!! JUST TESTING
        if (Mash)
        {
            if (mashTimerCoroutine != null) StopCoroutine(mashTimerCoroutine);
            mashTimerCoroutine = StartCoroutine(MashTimer());

            SetColour(Color.orange, 0.25f);
            mashCount++;
            DamageText.text = mashCount > 0 ? mashCount.ToString() : string.Empty;

            // if divisible by 5 (5, 10, 15, etc), activate all adjacent keys
            if (mashCount % 5 == 0)
            {
                KeyController.Instance.GetAdjacentKey(keyboardLetter, KeyController.Direction.All, out List<Key> adjacentKeys);
                foreach (Key key in adjacentKeys)
                {
                    key.Activate(true, 0.5f);
                }
                
                StartLocalCooldown(5f);
                return;
            }
            
            DealDamage();
            
            // Flash the key orange to indicate a successful mash.
            SetColour(Color.orange, 0.25f);
            StartLocalCooldown(0.25f);
            return;
        }

        DealDamage();

        // If this key is off the global cooldown, start its own cooldown rather than letting the KeyController put it on global cooldown.
        if (overrideGlobalCooldown)
        {
            // If a cooldown override is provided, use it instead of the default cooldown.
            StartLocalCooldown(cooldownOverride > 0 ? cooldownOverride : cooldown);
            return;
        }

        if (OffGlobalCooldown) StartLocalCooldown(cooldown);
        else KeyController.Instance.StartGlobalCooldown();
    }

    bool DealDamage()
    {
        if (currentEnemy)
        {
            currentEnemy.TakeDamage(damage);
            Debug.Log($"Dealt 1 damage to {currentEnemy}!");

            // Flash the key green to indicate it was pressed.
            SetColour(Color.green, 0.75f);
            return true;
        }

        // Flash the key red to indicate a miss.
        SetColour(Color.crimson, 0.5f);
        return false;
    }

    IEnumerator MashTimer()
    {
        // if 3 seconds pass without a mash, reset the mash count
        yield return new WaitForSeconds(3f);
        mashCount = 0;
        DamageText.text = mashCount.ToString();
    }

    public void StartLocalCooldown(float cooldown)
    {
        // dont start a new cooldown if the current cooldown is longer than the new one
        if (CooldownTime > cooldown) return;
        
        CooldownTime = cooldown;
        currentCooldown = cooldown;
    }

    void SetColour(Color color)
    {
        if (SpriteRenderer.color == Color.green || SpriteRenderer.color == Color.crimson) return;
        SpriteRenderer.color = color;
    }

    void SetColour(Color color, float duration) => StartCoroutine(SwitchColour(color, duration));

    IEnumerator SwitchColour(Color colour, float duration)
    {
        SpriteRenderer.color = colour;
        yield return new WaitForSeconds(duration);
        SpriteRenderer.color = Color.white;
    }
}
