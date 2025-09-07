using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using DG.Tweening;
using Lumina.Essentials.Attributes;
using UnityEngine;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

public partial class KeyController : MonoBehaviour
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

    ComboController comboController;
    
    readonly List<List<Key>> keys = new ();

    public static KeyController Instance { get; private set; }

    /// <summary>
    /// The parent object containing all key objects.
    /// </summary>
    public GameObject Keyboard { get; private set; }

    public KeyCode KeyPressed { get; private set; }
    public float GlobalCooldown => globalCooldown;
    public float CurrentCooldown => currentCooldown;
    public bool OnCooldown => Instance.currentCooldown > 0;

    public List<List<Key>> Keys => keys;
    public List<Key> FlatKeys { get; private set; } = new ();
    public float[] Lanes { get; } = new float[3];
    
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

    #region Get Key Functions
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

    public (bool found, int row, int col) FindKey(KeyCode keycode)
    {
        for (int r = 0; r < keys.Count; r++)
        {
            for (int c = 0; c < keys[r].Count; c++)
            {
                if (keys[r][c].KeyboardLetter == keycode) return (true, r, c);
            }
        }

        return (false, -1, -1);
    }
    
    public Key GetKey(KeyCode keycode)
    {
        (bool found, int row, int col) = FindKey(keycode);
        return found ? keys[row][col] : null;
    }
    #endregion

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;
    }
    
    void Start()
    {
        comboController = ComboController.Instance;
        
        InitializeKeyboard();
        
        InitializeWordHighway();

        // get the first item in each row to determine lane positions
        if (SceneManagerExtended.ActiveSceneName == "Game")
        {
            for (int row = 0; row < keys.Count; row++)
            {
                if (keys[row].Count > 0)
                {
                    float lane = keys[row][0].transform.position.y;
                    Lanes[row] = lane;
                    Debug.DrawLine(new Vector3(-10f, lane, 0f), new Vector3(10f, lane, 0f), Color.green, 300f);
                }
            }
        }

        // Start intro animation sequence after all setup
        StartCoroutine(IntroSequence());

        #region Modifiers
        if (SceneManagerExtended.ActiveSceneName == "Game")
        {
            List<KeyCode> qweCombo = "QWE".ToKeyCodes();
            comboController.CreateCombo(qweCombo);
            
            List<KeyCode> asdfCombo = "ASDF".ToKeyCodes();
            //comboController.CreateCombo(asdfCombo);

            List<KeyCode> rtyCombo = "RTY".ToKeyCodes();
            //comboController.CreateCombo(rtyCombo);
            
            List<Key> oGCD_Keys = "PLM".ToKeyCodes().ToKeys();
            oGCD_Keys.SetModifier(Key.Modifiers.OffGlobalCooldown);

            const float cooldown = 10f;
            KeyCode.V.ToKey().SetModifier(Key.Modifiers.OffGlobalCooldown, true, cooldown);

            // set G key to be a mash key
            Key mashKey = GetKey(KeyCode.G);
            mashKey.SetModifier(Key.Modifiers.Mash);
            
            // make H shake
            Key shakeKey = Instance.GetKey(KeyCode.H);
            shakeKey.SetModifier(Key.Modifiers.Loose);
        }
        
        // chain J key
        Key chainKey = GetKey(KeyCode.J);
        chainKey.SetModifier(Key.Modifiers.Chained);
        #endregion
        
        return;
        IEnumerator IntroSequence()
        {
            // TODO: change this to a better solution
            if (SceneManagerExtended.ActiveSceneName != "Menu") yield break;
            
            GameManager.Instance.EnterTransition.gameObject.SetActive(true);
            
            // Wait one frame to ensure all keys are initialized
            yield return new WaitForSeconds(1f);
            
            // Center the title "TYPER" in the middle of the middle row
            string title = GameManager.Instance.GameName;
            List<KeyCode> titleKeyCodes = title.Select(c => (KeyCode) Enum.Parse(typeof(KeyCode), c.ToString().ToUpper())).ToList();
            List<Key> titleKeys = titleKeyCodes.Select(tc => FlatKeys.FirstOrDefault(k => k.KeyboardLetter == tc)).Where(k => k != null).ToList();

            Keyboard.transform.DOMove(new (3.5f, -2f), 1.5f).SetEase(Ease.OutCubic);
            yield return new WaitForSeconds(1.5f);

            #region Swap positions of title keys with random keys
            // Create combo for title keys
            comboController.CreateCombo(titleKeys);
            // Doesn't use the 'interactable' parameter since we want to animate the markers separately
            HighlightKeys(title, true, false);
            #endregion

            yield return new WaitForSeconds(0.75f);

            #region Wait for Player to Start
            // Animate combo markers on title keys
            foreach (Key titleKey in titleKeys)
            {
                titleKey.ComboMarker.SetActive(titleKey.Combo = true);
                yield return new WaitForSeconds(0.1f);
            }
            
            yield return new WaitUntil(() => GameManager.Instance.TyperEntered);
            #endregion

            #region Return to Original Positions
            // Removes combo for title keys as well
            HighlightKeys(title, false, false);

            foreach (var key in FlatKeys) 
                key.ComboMarker.SetActive(key.Combo = false);
            #endregion

            Keyboard.transform.DOMove(new (0.85f, -5f), 1.5f).SetEase(Ease.InOutCubic).OnComplete(() =>
            {
                var canvas = FindFirstObjectByType<MenuManager>(FindObjectsInactive.Include);
                canvas.gameObject.SetActive(true);
                
                var child = canvas.transform.GetChild(0);
                var rectTransform = child.GetComponent<RectTransform>();
                rectTransform.anchoredPosition = new (500, 0);
                rectTransform.DOAnchorPosX(0, 1f).SetEase(Ease.OutCubic);

                menuKeyPositions = FlatKeys.ToDictionary(k => k, k => k.transform.position);
                menuKeyboardParentPosition = Keyboard.transform.position; // store menu position
            });
        }
    }
    
    void InitializeKeyboard()
    {
        Keyboard = GameObject.Find("Keyboard");
        if (Keyboard != null) Destroy(Keyboard);
        Keyboard = new ("Keyboard");

        currentlyValidKeys.Clear();

        // create rows' parent objects
        GenerateRows();

        FlatKeys = GenerateKeys();

        // Set initial position for intro animation off-screen, unless in Game scene
        if (SceneManagerExtended.ActiveSceneName == "Game") 
            Keyboard.transform.position = new (3.5f, -2f);
        else 
            Keyboard.transform.position = new (3.5f, 8f);

        return;
        void GenerateRows()
        {
            for (int i = 0; i < 3; i++)
            {
                string rowName = i switch
                { 0 => "QWERTY Row (Q-P)",
                  1 => "ASDFG Row (A-L)",
                  2 => "ZXCVB Row (Z-M)",
                  _ => "Row name failed to initialize." };

                GameObject row = new (rowName);
                row.transform.parent = Keyboard.transform;
            }
        }
        
        List<Key> GenerateKeys()
        {
            for (int i = 0; i < CurrentlyValidKeys.Count; i++)
            {
                KeyCode keycode = CurrentlyValidKeys[i];

                // Declare start positions for each row
                Vector2 firstRow = new (-8.5f, 3.5f);
                Vector2 secondRow = new (-8f, 2.5f);
                Vector2 thirdRow = new (-7.5f, 1.5f);

                int row = Row(i);
                Vector2 pos = KeyPosition(i, row, firstRow, secondRow, thirdRow);
                Transform rowParent = Keyboard.transform.GetChild(row);
                Key key = Instantiate(keyPrefab, pos, Quaternion.identity, rowParent);

                // initialize key
                key.InitKey(keycode, Row(i), IndexInRow(i), i);

                // object setup
                key.name = keycode.ToString();
                key.gameObject.SetActive(true);

                // populate keys 2D list
                // ReSharper disable once ArrangeObjectCreationWhenTypeNotEvident
                if (keys.Count <= row) keys.Add(new List<Key>());
                keys[row].Add(key);
            }
            
            return keys.SelectMany(row => row).ToList();
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

        int IndexInRow(int index)
        {
            int i = index switch
            { >= 0 and < 10  => index,      // QWERTY row
              >= 10 and < 19 => index - 10, // ASDFG row
              >= 19 and < 26 => index - 19, // ZXCVB row
              _              => -1 };

            return i;
        }
    }

    GameObject wordHighway;
    
    void InitializeWordHighway() // Highlights the current keys pressed in the "Word Highway" area
    {
        wordHighway = GameObject.Find("Word Highway");
        if (wordHighway != null) Destroy(wordHighway);
        wordHighway = new ("Word Highway");

        wordHighway.transform.position = new (0, 3.5f);
        wordHighway.transform.localScale = Vector3.one * 0.75f;
    }

    public void StartGlobalCooldown()
    {
        foreach (Key key in FlatKeys.Where(k => !k.OffGlobalCooldown)) 
            key.StartLocalCooldown(globalCooldown);
    }

    Key highwayKey;
    List<Key> comboHighwayKeys = new(); // Track combo keys in wordHighway

    bool correctKey;
    
    void Update()
    {
        #region Input Handling
        if (!Input.anyKeyDown) return;

        var pressedKey = CurrentlyValidKeys.FirstOrDefault(Input.GetKeyDown);
        if (pressedKey == KeyCode.None) return;

        KeyPressed = pressedKey;

        Key keyObj = FlatKeys.FirstOrDefault(k => k.KeyboardLetter == KeyPressed);
        if (keyObj != null) keyObj.Activate();

        #endregion
        
        if (OnCooldown) return;

        #region Word Highway
        var prefab = Resources.Load<Key>("PREFABS/Highway Key");
        
        // Create a new Key object to pass to WordHighway
        if (wordHighway != null && keyObj != null)
        {
            // Check if key is part of any active combo
            bool isComboKey = comboController.Combos.Any(c => c.ContainsKey(keyObj));
            if (comboController.RecentKey != null) 
                correctKey = comboController.RecentKey.ToKeyCode() == keyObj.ToKeyCode();

            if (!isComboKey)
            {
                foreach (Key k in comboHighwayKeys.Where(k => k)) Destroy(k.gameObject);
                comboHighwayKeys.Clear();
                
                Debug.Log("Key not part of any combo");
                if (!highwayKey) highwayKey = Instantiate(prefab, wordHighway.transform.position, Quaternion.identity, wordHighway.transform);
                highwayKey.name = keyObj.KeyboardLetter.ToString();
                highwayKey.Letter.text = keyObj.KeyboardLetter.ToString();
                highwayKey.gameObject.SetActive(true);
            }
            else
            {
                if (!correctKey)
                {
                    Debug.Log("Key is part of a combo, but not the correct key in the sequence");
                    
                    foreach (Key k in comboHighwayKeys.Where(k => k)) Destroy(k.gameObject);
                    comboHighwayKeys.Clear();

                    if (!highwayKey) highwayKey = Instantiate(prefab, wordHighway.transform.position, Quaternion.identity, wordHighway.transform);
                    highwayKey.name = keyObj.KeyboardLetter.ToString();
                    highwayKey.Letter.text = keyObj.KeyboardLetter.ToString();
                    highwayKey.gameObject.SetActive(true);
                    return;
                }

                Debug.Log("Key is part of a combo");

                if (comboHighwayKeys.Count >= 0)
                {
                    if (highwayKey) Destroy(highwayKey.gameObject);
                }

                // Instantiate and position combo key in wordHighway
                int comboIndex = comboHighwayKeys.Count;
                Vector3 comboPos = new((comboIndex * keyOffset) - 1.5f, 0f, 0f);
                Key comboKey = Instantiate(prefab, wordHighway.transform.position, Quaternion.identity, wordHighway.transform);
                comboKey.transform.localPosition = comboPos;
                comboKey.name = keyObj.KeyboardLetter.ToString();
                comboKey.Letter.text = keyObj.KeyboardLetter.ToString();
                comboKey.gameObject.SetActive(true);
                comboHighwayKeys.Add(comboKey);

                if (!comboController.InProgress) // runs when the last key in the combo is pressed
                {
                    foreach (var key in comboHighwayKeys)
                    {
                        if (comboHighwayKeys.Count <= 1)
                        {
                            foreach (Key k in comboHighwayKeys.Where(k => k)) Destroy(k.gameObject);
                            comboHighwayKeys.Clear();

                            if (!highwayKey) highwayKey = Instantiate(prefab, wordHighway.transform.position, Quaternion.identity, wordHighway.transform);
                            highwayKey.name = keyObj.KeyboardLetter.ToString();
                            highwayKey.Letter.text = keyObj.KeyboardLetter.ToString();
                            highwayKey.gameObject.SetActive(true);
                            break;
                        }
                        
                        Debug.Log(comboHighwayKeys.Count);

                        key.transform.DOPunchPosition(new Vector3(0, 1f, 0), 0.3f, 10, 1f)
                           .SetDelay(0.5f)
                           .OnComplete
                            (() =>
                            {
                                var vfx = Instantiate(Resources.Load<ParticleSystem>("PREFABS/Combo Effect"), key.transform.position, Quaternion.identity);
                                var main = vfx.main;
                                main.maxParticles = 5;
                                main.startColor = Random.ColorHSV(0, 1, 1, 1, 1, 1);
                                vfx.Play();

                                key.transform.DOMoveY(10f, 0.5f).SetEase(Ease.InBack).SetLink(key.gameObject);
                                Destroy(key.gameObject, 0.5f);

                                comboHighwayKeys.Clear();
                                if (highwayKey) Destroy(highwayKey.gameObject);
                            });
                    }
                }
            }
        }
        #endregion
    }

    List<Key> GenerateKeys()
    {
        var rowKeys = new List<Key>();
        List<KeyCode> highwayKeys = "Isogram".ToKeyCodes();

        for (int i = 0; i < highwayKeys.Count; i++)
        {
            KeyCode keycode = highwayKeys[i];
            Key key = Instantiate(keyPrefab, Vector3.zero, Quaternion.identity, wordHighway.transform);
            Vector3 pos = new (i * keyOffset, 0f, 0f);
            key.transform.localPosition = pos;

            key.name = keycode.ToString();
            key.gameObject.SetActive(true);
        }

        return rowKeys;
    }
    
    Dictionary<Key, Vector3> menuKeyPositions = new ();
    Vector3 menuKeyboardParentPosition; // stores menu position of parent

    /// <summary>
    /// Moves the specified word to the center of the middle row, swapping with existing keys.
    /// </summary>
    /// <param name="word">The word to move (e.g., "PLAY", "MENU", "EXIT").</param>
    void MoveWordToCenterRow(string word)
    {
        const int middleRow = 1;
        List<Key> middleRowKeys = keys[middleRow];
        int startIdx = (middleRowKeys.Count - word.Length) / 2;
        List<KeyCode> wordKeyCodes = word.Select(c => (KeyCode) Enum.Parse(typeof(KeyCode), c.ToString().ToUpper())).ToList();
        
        // The keys corresponding to the letters in the word
        List<Key> wordKeys = wordKeyCodes.Select(tc => FlatKeys.FirstOrDefault(k => k.KeyboardLetter == tc)).Where(k => k != null).ToList();

        if (wordKeys.Count != word.Length)
        {
            Debug.LogWarning($"Not all letters in the word '{word}' are present on the keyboard.");
            return;
        }

        // save original positions of all keys
        menuKeyPositions = FlatKeys.ToDictionary(k => k, k => k.transform.position);

        for (int i = 0; i < wordKeys.Count; i++)
        {
            Key targetKey = middleRowKeys[startIdx + i];
            Key wordKey = wordKeys[i];

            if (wordKey == targetKey) continue; // skip if the key is already in the correct position

            Vector3 targetPosition = targetKey.transform.position;
            Vector3 wordPosition = wordKey.transform.position;

            // Swap positions
            wordKey.transform.DOMove(targetPosition, 0.5f).SetEase(Ease.InOutCubic);
            targetKey.transform.DOMove(wordPosition, 0.5f).SetEase(Ease.InOutCubic);
        }
    }

    public void HighlightKeys(string word, bool enable, bool interactable)
    {
        foreach (Key key in FlatKeys)
        {
            if (!key) continue;
            if (menuKeyPositions.TryGetValue(key, out Vector3 position)) 
                key.transform.position = position;
        }
        
        List<Key> keysToHighlight = word.Select(c => (KeyCode) Enum.Parse(typeof(KeyCode), c.ToString().ToUpper())).Select(tc => FlatKeys.FirstOrDefault(k => k.KeyboardLetter == tc)).Where(k => k != null).ToList();

        // List of keys to disable (all keys except the ones to highlight)
        List<Key> keysToDisable = FlatKeys.Except(keysToHighlight).ToList();

        foreach (Key key in keysToDisable)
        {
            if (enable)
            {
                key.Disable();
                MoveWordToCenterRow(word);

                if (interactable)
                {
                    comboController.CreateCombo(keysToHighlight);
                    // combo markers on highlighted keys
                    foreach (Key highlightKey in keysToHighlight) 
                        highlightKey.ComboMarker.SetActive(highlightKey.Combo = true);
                }
            }
            else
            {
                key.Enable();
                ResetKeyPositions();

                if (!interactable)
                {
                    comboController.RemoveCombo(keysToHighlight);
                    
                    foreach (Key highlightKey in keysToHighlight) 
                        highlightKey.ComboMarker.SetActive(highlightKey.Combo = false);
                }
            }
        }
    }

    public void ResetKeyPositions()
    {
        GameObject parent = GameObject.Find("Keyboard");
        if (!parent) return;
        
        if (parent != null && menuKeyboardParentPosition != Vector3.zero)
            parent.transform.DOMove(menuKeyboardParentPosition, 0.5f);
        
        foreach (Key key in FlatKeys)
        {
            if (menuKeyPositions.TryGetValue(key, out Vector3 position)) 
                key.transform.DOMove(position, 0.5f);
        }
    }
}
