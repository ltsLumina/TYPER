using System;
using System.Collections.Generic;
using System.Linq;
using Lumina.Essentials.Attributes;
using MelenitasDev.SoundsGood;
using UnityEngine;

public class KeyController : MonoBehaviour
{
    enum KeyboardLayout
    {
        QWERTY,
        AZERTY,
        DVORAK
    }

    enum KeySet
    {
        Alphabetic,
        Numeric,
        Alphanumeric
    }
    
    [Header("References")]
    [SerializeField] Key keyPrefab;
    [SerializeField] List<Key> allKeys = new ();
    
    [Header("Keyboard Settings")]
    [Tooltip("List of keys to instantiate on the keyboard. If empty, defaults to all alphabetic keys.")]
    [SerializeField] List<KeyCode> overrideKeys = new ();
    [SerializeField] List<KeyCode> currentlyValidKeys = new ();
    [SerializeField] KeyboardLayout keyboardLayout = KeyboardLayout.QWERTY;
    [SerializeField] KeySet keySet = KeySet.Alphabetic;
    
    [Header("Key Settings")]
    [Tooltip("Determines the spacing between each key object")]
    [SerializeField] float keyOffset = 1.0f;
    [SerializeField] List<float> rowOffsets = new () { 0f, -0.2f, -0.4f };
    
    [Header("Global Cooldown Settings")]
    [SerializeField] float globalCooldown = 1f;
    [SerializeField, ReadOnly] float currentCooldown;

    public static KeyController Instance { get; private set; }

    public KeyCode KeyPressed { get; private set; }
    public float GlobalCooldown => globalCooldown;
    public float CurrentCooldown => currentCooldown;
    public bool OnCooldown => Instance.currentCooldown > 0;

    public List<Key> AllKeys => allKeys;

    public List<float> Lanes { get; } = new ();
    
    /// <summary>
    ///     List of keys that are currently valid to press.
    ///     <remarks> By default, this contains all alphabetic keyboard keys.</remarks>
    /// </summary>
    List<KeyCode> CurrentlyValidKeys
    {
        get
        {
            if (currentlyValidKeys.Count > 0) return currentlyValidKeys;
            
            // if override keys are set, use those
            if (overrideKeys.Count > 0) return overrideKeys;

            // Uses the selected layout and key set to determine valid keys. E.g., QWERTY + Alphabetic = A-Z keys.
            currentlyValidKeys = GetKeySetByLayout();

            return currentlyValidKeys;
        }
    }

    List<KeyCode> GetKeySetByLayout()
    {
        switch (keySet)
        {
            case KeySet.Alphabetic:
                switch (keyboardLayout)
                {
                    case KeyboardLayout.QWERTY:
                        return Keys.Layouts.QWERTY.Alphabetic;

                    case KeyboardLayout.AZERTY:
                        return Keys.Layouts.AZERTY.Alphabetic;

                    case KeyboardLayout.DVORAK:
                        return new List<KeyCode>(); // Placeholder
                }

                break;

            case KeySet.Numeric:
                switch (keyboardLayout)
                {
                    case KeyboardLayout.QWERTY:
                        return Keys.Layouts.QWERTY.Numeric;

                    case KeyboardLayout.AZERTY:
                        return Keys.Layouts.AZERTY.Numeric;

                    case KeyboardLayout.DVORAK:     // Placeholder
                        return new List<KeyCode>(); // Placeholder
                }

                break;

            case KeySet.Alphanumeric:
                switch (keyboardLayout)
                {
                    case KeyboardLayout.QWERTY:
                        return Keys.Layouts.QWERTY.Alphanumeric;

                    case KeyboardLayout.AZERTY:
                        return Keys.Layouts.AZERTY.Alphanumeric;

                    case KeyboardLayout.DVORAK:     // Placeholder
                        return new List<KeyCode>(); // Placeholder
                }

                break;
        }

        return null;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;
    }

    void Start()
    {
        GameObject parent = GameObject.Find("Keyboard") ?? new GameObject("Keyboard");
        allKeys.Clear();
        currentlyValidKeys.Clear();

        // create rows parent objects
        for (int i = 0; i < 3; i++)
        {
            string rowName = i switch
            {
                0 => "QWERTY Row (Q-P)",
                1 => "ASDFG Row (A-L)",
                2 => "ZXCVB Row (Z-M)",
                _ => "Row name failed to initialize."
            };
            
            GameObject row = new (rowName);
            row.transform.parent = parent.transform;
        }
        
        for (int index = 0; index < CurrentlyValidKeys.Count; index++)
        {
            KeyCode keycode = CurrentlyValidKeys[index];
            Vector2 firstRow, secondRow, thirdRow;
            firstRow = new (-8.5f, 3.5f);
            secondRow = new (-8f, 2.5f);
            thirdRow = new (-7.5f, 1.5f);
            
            int rowIndex = index switch
            {
                >= 0 and < 10  => 0, // QWERTY row
                >= 10 and < 19 => 1, // ASDFG row
                >= 19 and < 26 => 2, // ZXCVB row
                _              => -1
            };

            Vector2 pos = index switch
            { >= 0 and < 10  => firstRow + new Vector2(index * keyOffset, rowOffsets[rowIndex]),         // QWERTY row
              >= 10 and < 19 => secondRow + new Vector2((index - 10) * keyOffset, rowOffsets[rowIndex]), // ASDFG row
              >= 19 and < 26 => thirdRow + new Vector2((index - 19) * keyOffset, rowOffsets[rowIndex]),  // ZXCVB row
              _          => Vector2.zero };
           
            var row = parent.transform.GetChild(rowIndex);
            var newKey = Instantiate(keyPrefab, pos, Quaternion.identity, row);
            allKeys.Add(newKey);

            // key setup
            newKey.KeyboardLetter = keycode;
            newKey.Letter.text = keycode.ToString();
            newKey.Letter.text = newKey.Letter.text.Replace("Alpha", ""); // remove "Alpha" from numeric keys
            // set to index in row (0-9, 0-8, 0-6)
            int i = index switch
            { >= 0 and < 10  => index,      // QWERTY row
              >= 10 and < 19 => index - 10, // ASDFG row
              >= 19 and < 26 => index - 19, // ZXCVB row
              _              => -1 };

            newKey.RowIndex = i + 1;
            // get the index for F and J keys to add homing bars
            if (keycode is KeyCode.F or KeyCode.J) newKey.HomingBar.SetActive(true);

            // object setup
            newKey.name = keycode.ToString();
            newKey.gameObject.SetActive(true);
        }

        // Set parent position to center the keyboard on screen after all keys are instantiated.
        parent.transform.position = new (3.5f, -2f);
        
        // TODO: refactor to not use hardcoded indices
        Lanes.Add(allKeys[0].transform.position.y);
        Lanes.Add(allKeys[10].transform.position.y);
        Lanes.Add(allKeys[19].transform.position.y);

        foreach (float lane in Lanes)
        {
            Debug.DrawLine(new Vector3(-10f, lane, 0f), new Vector3(10f, lane, 0f), Color.green, 300f);
        }
        
        // select three random keys to be oGCD actions
        var oGCD_Keys = allKeys.OrderBy(x => Guid.NewGuid()).Take(3).ToList();
        foreach (var key in oGCD_Keys)
        {
            key.OffGlobalCooldown = true;
        }
        
        // select three keys to be combo keys (none of which are oGCD)
        var comboKeys = allKeys.Except(oGCD_Keys).OrderBy(x => Guid.NewGuid()).Take(3).ToList();
        foreach (var key in comboKeys)
        {
            key.Combo = true;
        }

        // select one random basic key to be a mash key (not oGCD, not combo)
        var mashKey = allKeys.Except(oGCD_Keys).Except(comboKeys).OrderBy(x => Guid.NewGuid()).FirstOrDefault();
        if (mashKey != null) mashKey.Mash = true;

        // select three random basic keys to be inactive at start
        foreach (var key in allKeys.Except(oGCD_Keys).Except(comboKeys).OrderBy(x => Guid.NewGuid()).Take(3))
        {
            var color = key.SpriteRenderer.color;
            color.a = 0.25f;
            key.SpriteRenderer.color = color;
            key.Disable();
        }
    }
    
    public void StartGlobalCooldown()
    {
        foreach (Key key in allKeys.Where(k => !k.OffGlobalCooldown))
        {
            key.StartLocalCooldown(globalCooldown);
        }
    }
    
    void Update()
    {
        #region Input Handling
        if (!Input.anyKeyDown) return;

        var pressedKey = CurrentlyValidKeys.FirstOrDefault(Input.GetKeyDown);
        if (pressedKey == KeyCode.None) return;
    
        KeyPressed = pressedKey;
    
        var keyObj = allKeys.FirstOrDefault(k => k.KeyboardLetter == KeyPressed);
        if (keyObj != null)
        {
            keyObj.Activate();
        }
        #endregion
    }
}
