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

    readonly List<List<Key>> keys = new ();

    public static KeyController Instance { get; private set; }

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
    
    /// <param name="row"> 0-based index of the row to retrieve. </param>
    /// <returns> List of keys in the specified row. </returns>
    public List<Key> GetRow(int row) => keys[row]; 

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

    public enum Direction
    {
        Up,
        Down,
        Left,
        Right,
        All
    }

    /// <param name="direction"> Direction to look for an adjacent key. </param>
    /// <param name="adjacentKeys">
    ///     If direction is All, this will be populated with all adjacent keys found. Otherwise, it
    ///     will be null.
    /// </param>
    /// <returns>
    ///     The adjacent key in the specified direction, or null if none exists. If direction is All, returns null and
    ///     populates adjacentKeys with all found adjacent keys.
    /// </returns>
    public Key GetAdjacentKey(KeyCode keycode, Direction direction, out List<Key> adjacentKeys)
    {
        (bool found, int row, int col) = FindKey(keycode);
        if (!found)
        {
            adjacentKeys = null;
            return null;
        }

        switch (direction) // super fancy math or something
        {
            case Direction.Up:
                adjacentKeys = null;
                return row > 0 ? keys[row - 1][Mathf.Min(col, keys[row - 1].Count - 1)] : null;

            case Direction.Down:
                adjacentKeys = null;
                return row < keys.Count - 1 ? keys[row + 1][Mathf.Min(col, keys[row + 1].Count - 1)] : null;

            case Direction.Left:
                adjacentKeys = null;
                return col > 0 ? keys[row][col - 1] : null;

            case Direction.Right:
                adjacentKeys = null;
                return col < keys[row].Count - 1 ? keys[row][col + 1] : null;

            case Direction.All: // return the first adjacent key found in every direction
                var directions = new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right };
                adjacentKeys = directions.Select(dir => GetAdjacentKey(keycode, dir, out _)).Where(adjacent => adjacent != null).ToList();
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(direction));
        }
        
        return null;
    }
    
    /// <param name="includeSelf"> Whether to include the specified key in the returned list. </param>
    /// <returns> Returns a list of all keys surrounding the specified key (up to 8 keys). </returns>
    public List<Key> GetSurroundingKeys(KeyCode keycode, bool includeSelf = false)
    {
        (bool found, int row, int col) = FindKey(keycode);
        if (!found) return null;

        List<Key> surroundingKeys = new ();
        
        for (int r = row - 1; r <= row + 1; r++)
        {
            for (int c = col - 1; c <= col + 1; c++)
            {
                if (r >= 0 && r < keys.Count && c >= 0 && c < keys[r].Count && (r != row || c != col))
                {
                    surroundingKeys.Add(keys[r][c]);
                }
            }
        }
        
        if (includeSelf) surroundingKeys.Add(keys[row][col]);

        return surroundingKeys;
    }
    
    public List<List<Key>> Wave()
    {
        List<List<Key>> wave = new ();
        int maxCols = keys.Max(row => row.Count);

        for (int col = 0; col < maxCols; col++)
        {
            List<Key> waveRow = (from t in keys where col < t.Count select t[col]).ToList();
            wave.Add(waveRow);
        }

        return wave;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;
    }

    ComboController comboController;
    
    void Start()
    {
        comboController = ComboController.Instance;
        
        GameObject parent = GameObject.Find("Keyboard") ?? new GameObject("Keyboard");
        currentlyValidKeys.Clear();

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

            // populate keys 2D list
            // ReSharper disable once ArrangeObjectCreationWhenTypeNotEvident
            if (keys.Count <= row) keys.Add(new List<Key>());
            keys[row].Add(key);
        }

        FlatKeys = keys.SelectMany(row => row).ToList();
        
        // Set parent position to center the keyboard on screen after all keys are instantiated.
        parent.transform.position = new (3.5f, 8f);
        gameKeyboardParentPosition = parent.transform.position; // store gameplay position

        // get the first item in each row to determine lane positions
        // for (int row = 0; row < keys.Count; row++)
        // {
        //     if (keys[row].Count > 0)
        //     {
        //         float lane = keys[row][0].transform.position.y;
        //         Lanes[row] = lane;
        //         Debug.DrawLine(new Vector3(-10f, lane, 0f), new Vector3(10f, lane, 0f), Color.green, 300f);
        //     }
        // }

        // Start intro animation sequence after all setup
        StartCoroutine(IntroSequence());
        
        return;
        void RowParents()
        {
            for (int i = 0; i < 3; i++)
            {
                string rowName = i switch
                { 0 => "QWERTY Row (Q-P)",
                  1 => "ASDFG Row (A-L)",
                  2 => "ZXCVB Row (Z-M)",
                  _ => "Row name failed to initialize." };

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

        IEnumerator IntroSequence()
        {
            // Wait one frame to ensure all keys are initialized
            yield return null;
            
            // Center the title "TYPER" in the middle of the middle row
            string title = GameManager.Instance.GameName;
            List<KeyCode> titleKeyCodes = title.Select(c => (KeyCode) Enum.Parse(typeof(KeyCode), c.ToString().ToUpper())).ToList();
            List<Key> titleKeys = titleKeyCodes.Select(tc => FlatKeys.FirstOrDefault(k => k.KeyboardLetter == tc)).Where(k => k != null).ToList();
            
            parent.transform.DOMove(new (3.5f, -2f), 1.5f).SetEase(Ease.OutCubic).OnComplete(() => {
                gameKeyPositions = FlatKeys.ToDictionary(k => k, k => k.transform.position);
                gameKeyboardParentPosition = parent.transform.position; // update gameplay position after animation
            });
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

            parent.transform.DOMove(new (0.85f, -5f), 1.5f).SetEase(Ease.InOutCubic).OnComplete(() =>
            {
                var canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
                canvas.gameObject.SetActive(true);

                menuKeyPositions = FlatKeys.ToDictionary(k => k, k => k.transform.position);
                menuKeyboardParentPosition = parent.transform.position; // store menu position
            });
        }
    }

    public void StartGlobalCooldown()
    {
        foreach (Key key in FlatKeys.Where(k => !k.OffGlobalCooldown)) 
            key.StartLocalCooldown(globalCooldown);
    }
    
    void Update()
    {
        #region Input Handling
        if (!Input.anyKeyDown) return;

        var pressedKey = CurrentlyValidKeys.FirstOrDefault(Input.GetKeyDown);
        if (pressedKey == KeyCode.None) return;
    
        KeyPressed = pressedKey;
    
        var keyObj = FlatKeys.FirstOrDefault(k => k.KeyboardLetter == KeyPressed);
        if (keyObj != null)
        {
            keyObj.Activate();
        }
        #endregion
    }

    Dictionary<Key, Vector3> gameKeyPositions = new ();
    Dictionary<Key, Vector3> menuKeyPositions = new ();
    Vector3 gameKeyboardParentPosition; // stores gameplay position of parent
    Vector3 menuKeyboardParentPosition; // stores menu position of parent

    /// <summary>
    /// Moves the specified word to the center of the middle row, swapping with existing keys.
    /// </summary>
    /// <param name="word">The word to move (e.g., "PLAY", "MENU", "EXIT").</param>
    public void MoveWordToCenterRow(string word)
    {
        const int middleRow = 1;
        List<Key> middleRowKeys = keys[middleRow];
        int startIdx = (middleRowKeys.Count - word.Length) / 2;
        List<KeyCode> wordKeyCodes = word.Select(c => (KeyCode) Enum.Parse(typeof(KeyCode), c.ToString().ToUpper())).ToList();
        
        // The keys corresponding to the letters in the word
        List<Key> wordKeys = wordKeyCodes.Select(tc => FlatKeys.FirstOrDefault(k => k.KeyboardLetter == tc)).Where(k => k != null).ToList();
        // The keys that are not part of the word (to be disabled if highlight is true)
        List<Key> unavailableKeys = FlatKeys.Except(wordKeys).ToList();

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

                if (interactable) comboController.CreateCombo(keysToHighlight);
            }
            else
            {
                key.Enable();
                ResetKeyPositions();

                if (!interactable) comboController.RemoveCombo(keysToHighlight);
            }
        }
    }

    public void ResetKeyPositions()
    {
        GameObject parent = GameObject.Find("Keyboard");
        if (parent != null && menuKeyboardParentPosition != Vector3.zero)
            parent.transform.DOMove(menuKeyboardParentPosition, 0.5f);
        foreach (Key key in FlatKeys)
        {
            if (menuKeyPositions.TryGetValue(key, out Vector3 position)) 
                key.transform.DOMove(position, 0.5f);
        }
    }
    
    public void ResetToGamePositions()
    {
        GameObject parent = GameObject.Find("Keyboard");
        if (parent != null && gameKeyboardParentPosition != Vector3.zero)
            parent.transform.DOMove(gameKeyboardParentPosition, 0.5f);
        foreach (Key key in FlatKeys)
        {
            if (gameKeyPositions.TryGetValue(key, out Vector3 position)) 
                key.transform.DOMove(position, 0.5f);
        }
    }
}
