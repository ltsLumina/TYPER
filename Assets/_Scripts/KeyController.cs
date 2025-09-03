using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public struct Keys
{
    public struct Layouts
    {
        public struct QWERTY
        {
            public static List<KeyCode> Alphabetic { get; } = new ()
            { KeyCode.Q,
              KeyCode.W,
              KeyCode.E,
              KeyCode.R,
              KeyCode.T,
              KeyCode.Y,
              KeyCode.U,
              KeyCode.I,
              KeyCode.O,
              KeyCode.P,
              KeyCode.A,
              KeyCode.S,
              KeyCode.D,
              KeyCode.F,
              KeyCode.G,
              KeyCode.H,
              KeyCode.J,
              KeyCode.K,
              KeyCode.L,
              KeyCode.Z,
              KeyCode.X,
              KeyCode.C,
              KeyCode.V,
              KeyCode.B,
              KeyCode.N,
              KeyCode.M, };

            public static List<KeyCode> Numeric { get; } = new ()
            { KeyCode.Alpha1,
              KeyCode.Alpha2,
              KeyCode.Alpha3,
              KeyCode.Alpha4,
              KeyCode.Alpha5,
              KeyCode.Alpha6,
              KeyCode.Alpha7,
              KeyCode.Alpha8,
              KeyCode.Alpha9,
              KeyCode.Alpha0 };

            public static List<KeyCode> Alphanumeric { get; } = new ()
            { KeyCode.Alpha1,
              KeyCode.Alpha2,
              KeyCode.Alpha3,
              KeyCode.Alpha4,
              KeyCode.Alpha5,
              KeyCode.Alpha6,
              KeyCode.Alpha7,
              KeyCode.Alpha8,
              KeyCode.Alpha9,
              KeyCode.Alpha0,
              KeyCode.Q,
              KeyCode.W,
              KeyCode.E,
              KeyCode.R,
              KeyCode.T,
              KeyCode.Y,
              KeyCode.U,
              KeyCode.I,
              KeyCode.O,
              KeyCode.P,
              KeyCode.A,
              KeyCode.S,
              KeyCode.D,
              KeyCode.F,
              KeyCode.G,
              KeyCode.H,
              KeyCode.J,
              KeyCode.K,
              KeyCode.L,
              KeyCode.Z,
              KeyCode.X,
              KeyCode.C,
              KeyCode.V,
              KeyCode.B,
              KeyCode.N,
              KeyCode.M, };
        }
        
        public struct AZERTY
        {
            public static List<KeyCode> Alphabetic { get; } = new ()
            { KeyCode.A,
              KeyCode.Z,
              KeyCode.E,
              KeyCode.R,
              KeyCode.T,
              KeyCode.Y,
              KeyCode.U,
              KeyCode.I,
              KeyCode.O,
              KeyCode.P,
              KeyCode.Q,
              KeyCode.S,
              KeyCode.D,
              KeyCode.F,
              KeyCode.G,
              KeyCode.H,
              KeyCode.J,
              KeyCode.K,
              KeyCode.L,
              KeyCode.M,
              KeyCode.W,
              KeyCode.X,
              KeyCode.C,
              KeyCode.V,
              KeyCode.B,
              KeyCode.N, };
        
            public static List<KeyCode> Numeric { get; } = new ()
            { KeyCode.Alpha1,
              KeyCode.Alpha2,
              KeyCode.Alpha3,
              KeyCode.Alpha4,
              KeyCode.Alpha5,
              KeyCode.Alpha6,
              KeyCode.Alpha7,
              KeyCode.Alpha8,
              KeyCode.Alpha9,
              KeyCode.Alpha0 };
        
            public static List<KeyCode> Alphanumeric { get; } = new ()
            { KeyCode.Alpha1,
              KeyCode.Alpha2,
              KeyCode.Alpha3,
              KeyCode.Alpha4,
              KeyCode.Alpha5,
              KeyCode.Alpha6,
              KeyCode.Alpha7,
              KeyCode.Alpha8,
              KeyCode.Alpha9,
              KeyCode.Alpha0,
              KeyCode.A,
              KeyCode.Z,
              KeyCode.E,
              KeyCode.R,
              KeyCode.T,
              KeyCode.Y,
              KeyCode.U,
              KeyCode.I,
              KeyCode.O,
              KeyCode.P,
              KeyCode.Q,
              KeyCode.S,
              KeyCode.D,
              KeyCode.F,
              KeyCode.G,
              KeyCode.H,
              KeyCode.J,
              KeyCode.K,
              KeyCode.L,
              KeyCode.M,
              KeyCode.W,
              KeyCode.X,
              KeyCode.C,
              KeyCode.V,
              KeyCode.B,
              KeyCode.N, };
        }
        
        // TODO: Add DVORAK layout
    }
}

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
    [SerializeField] List<Key> keys = new ();
    
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
    [SerializeField] float currentCooldown;
    
    KeyCode keyPressed;
    
    public static KeyController Instance { get; private set; }

    public float GlobalCooldown => globalCooldown;
    public float CurrentCooldown => currentCooldown;
    public bool OnCooldown => Instance.currentCooldown > 0;
    
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
        keys.Clear();
        currentlyValidKeys.Clear();

        for (int index = 0; index < CurrentlyValidKeys.Count; index++)
        {
            KeyCode key = CurrentlyValidKeys[index];
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
            {
                >= 0 and < 10  => firstRow + new Vector2(index * keyOffset, rowOffsets[rowIndex]),         // QWERTY row
                >= 10 and < 19 => secondRow + new Vector2((index - 10) * keyOffset, rowOffsets[rowIndex]), // ASDFG row
                >= 19 and < 26 => thirdRow + new Vector2((index - 19) * keyOffset, rowOffsets[rowIndex]),   // ZXCVB row
                _              => Vector2.zero
            };

            var newKey = Instantiate(keyPrefab, pos, Quaternion.identity, parent.transform);
            keys.Add(newKey);

            // key setup
            newKey.KeyboardLetter = key;
            newKey.Letter.text = key.ToString();
            newKey.Letter.text = newKey.Letter.text.Replace("Alpha", ""); // remove "Alpha" from numeric keys
            
            // object setup
            newKey.name = key.ToString();
            newKey.gameObject.SetActive(true);
        }

        foreach (Key key in keys)
        {
            key.OnPressed += (hit, enemy) =>
            {
                currentCooldown = globalCooldown;

                foreach (var key in keys)
                {
                    key.SetColour(Color.grey, globalCooldown);
                }
            };
        }

        // Set parent position to center the keyboard on screen after all keys are instantiated.
        parent.transform.position = new (3.5f, -2f);
    }

    void Update()
    {
        #region Global Cooldown
        if (currentCooldown > 0) currentCooldown -= Time.deltaTime;
        else currentCooldown = 0;
        #endregion
        
        #region Input Handling
        if (!Input.anyKeyDown) return;

        var pressedKey = CurrentlyValidKeys.FirstOrDefault(Input.GetKeyDown);
        if (pressedKey == KeyCode.None) return;
    
        keyPressed = pressedKey;
    
        var keyObj = keys.FirstOrDefault(k => k.KeyboardLetter == keyPressed);
        if (keyObj != null)
        {
            keyObj.Activate();
        }
        #endregion
    }
}
