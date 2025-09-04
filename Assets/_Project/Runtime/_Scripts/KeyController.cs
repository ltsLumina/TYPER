using System;
using System.Collections.Generic;
using System.Linq;
using Lumina.Essentials.Attributes;
using MelenitasDev.SoundsGood;
using UnityEngine;
using VInspector;

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
    
    List<List<Key>> keys = new ();
    
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
                        return KeyboardData.Layouts.QWERTY.Alphabetic;

                    case KeyboardLayout.AZERTY:
                        return KeyboardData.Layouts.AZERTY.Alphabetic;

                    case KeyboardLayout.DVORAK:
                        return new List<KeyCode>(); // Placeholder
                }

                break;

            case KeySet.Numeric:
                switch (keyboardLayout)
                {
                    case KeyboardLayout.QWERTY:
                        return KeyboardData.Layouts.QWERTY.Numeric;

                    case KeyboardLayout.AZERTY:
                        return KeyboardData.Layouts.AZERTY.Numeric;

                    case KeyboardLayout.DVORAK:     // Placeholder
                        return new List<KeyCode>(); // Placeholder
                }

                break;

            case KeySet.Alphanumeric:
                switch (keyboardLayout)
                {
                    case KeyboardLayout.QWERTY:
                        return KeyboardData.Layouts.QWERTY.Alphanumeric;

                    case KeyboardLayout.AZERTY:
                        return KeyboardData.Layouts.AZERTY.Alphanumeric;

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

    (bool found, int row, int col) FindKey(KeyCode keycode, List<List<Key>> keys)
    {
        for (int r = 0; r < keys.Count; r++)
        {
            for (int c = 0; c < keys[r].Count; c++)
            {
                if (keys[r][c].KeyboardLetter == keycode)
                    return (true, r, c);
            }
        }

        return (false, -1, -1);
    }


    void Start()
    {
        GameObject parent = GameObject.Find("Keyboard") ?? new GameObject("Keyboard");
        CurrentlyValidKeys.Clear();

        // create rows' parent objects
        RowParents();
        
        for (int i = 0; i < CurrentlyValidKeys.Count; i++)
        {
            KeyCode keycode = CurrentlyValidKeys[i];
            
            // Declare start positions for each row
            Vector2 firstRow = new (-8.5f, 3.5f);
            Vector2 secondRow = new (-8f, 2.5f);
            Vector2 thirdRow = new (-7.5f, 1.5f);
            
            int row = Row(i);
            Vector2 pos = KeyPosition(i, row, firstRow, secondRow, thirdRow);
            Transform rowParent = parent.transform.GetChild(row);
            Key key = Instantiate(keyPrefab, pos, Quaternion.identity, rowParent);
            
            // initialize key
            key.InitKey(keycode, Row(i), IndexInRow(i), i);

            // object setup
            key.name = keycode.ToString();
            key.gameObject.SetActive(true);
            
            // TODO: will be replace by new 2D grid system
            allKeys.Add(key);
            
            // populate keys 2D list
            if (keys.Count <= row) keys.Add(new List<Key>());
            keys[row].Add(key);
            
            // populate lanes list
            Lanes.Add(allKeys[0].transform.position.y);
            Lanes.Add(allKeys[11].transform.position.y);
            Lanes.Add(allKeys[19].transform.position.y);
        }

        // Set parent position to center the keyboard on screen after all keys are instantiated.
        parent.transform.position = new (3.5f, -2f);

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

        return;
        void RowParents()
        {
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
        }

        int IndexInRow(int index)
        {
            int i = index switch
            { >= 0 and < 10  => index,      // QWERTY row
              >= 10 and < 19 => index - 10, // ASDFG row
              >= 19 and < 26 => index - 19, // ZXCVB row
              _              => -1 };

            return i;
        }

        int Row(int i)
        {
            int row = i switch
            { >= 0 and < 10 => 0 // QWERTY row
             ,
              >= 10 and < 19 => 1 // ASDFG row
             ,
              >= 19 and < 26 => 2 // ZXCVB row
             ,
              _ => -1 };

            return row;
        }

        Vector2 KeyPosition(int i, int row, Vector2 firstRowPos, Vector2 secondRowPos, Vector2 thirdRowPos)
        {
            Vector2 pos = i switch
            { >= 0 and < 10 => firstRowPos + new Vector2(i * keyOffset, rowOffsets[row]) // QWERTY row
             ,
              >= 10 and < 19 => secondRowPos + new Vector2((i - 10) * keyOffset, rowOffsets[row]) // ASDFG row
             ,
              >= 19 and < 26 => thirdRowPos + new Vector2((i - 19) * keyOffset, rowOffsets[row]) // ZXCVB row
             ,
              _ => Vector2.zero };

            return pos;
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
