using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Lumina.Essentials.Attributes;
using UnityEngine;
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

    void Start()
    {
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

        // get the first item in each row to determine lane positions
        for (int row = 0; row < keys.Count; row++)
        {
            if (keys[row].Count > 0)
            {
                float lane = keys[row][0].transform.position.y;
                Lanes[row] = lane;
                Debug.DrawLine(new Vector3(-10f, lane, 0f), new Vector3(10f, lane, 0f), Color.green, 300f);
            }
        }

        #region Modifiers
        ComboController comboController = ComboController.Instance;
        var qweCombo = new List<KeyCode> { KeyCode.Q, KeyCode.W, KeyCode.E };
        comboController.CreateCombo(qweCombo);

        // asdf combo
        var asdfCombo = new List<KeyCode> { KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F };
        comboController.CreateCombo(asdfCombo, true);

        // select three random keys to be oGCD actions
        List<Key> oGCD_Keys = FlatKeys
                             .Where
                              (x => x.KeyboardLetter != KeyCode.Q && x.KeyboardLetter != KeyCode.W && x.KeyboardLetter != KeyCode.E && x.KeyboardLetter != KeyCode.A && x.KeyboardLetter != KeyCode.S && x.KeyboardLetter != KeyCode.D && x.KeyboardLetter != KeyCode.F &&
                                    x.KeyboardLetter != KeyCode.G) // exclude combo keys and mash key
                             .OrderBy(x => Guid.NewGuid())
                             .Take(3)
                             .ToList();

        foreach (Key key in oGCD_Keys) key.OffGlobalCooldown = true;

        // set G key to be a mash key
        Key mashKey = GetKey(KeyCode.G);
        if (mashKey != null) mashKey.Mash = true;
        #endregion

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
            
            // Temporarily disable all key markers during intro animation
            foreach (Key key in FlatKeys)
            {
                key.offGCDMarker.SetActive(false);
                key.ComboMarker.SetActive(false);
                key.MashMarker.SetActive(false);
            }
            
            // Center the title "TYPER" in the middle of the middle row
            string title = GameManager.Instance.GameName;
            const int middleRow = 1;
            List<Key> middleRowKeys = keys[middleRow];
            int startIdx = (middleRowKeys.Count - title.Length) / 2;
            List<KeyCode> titleKeyCodes = title.Select(c => (KeyCode) Enum.Parse(typeof(KeyCode), c.ToString().ToUpper())).ToList();
            List<Key> titleKeys = titleKeyCodes.Select(tc => FlatKeys.FirstOrDefault(k => k.KeyboardLetter == tc)).Where(k => k != null).ToList();
            
            parent.transform.DOMove(new (3.5f, -2f), 1.5f).SetEase(Ease.OutCubic);

            yield return new WaitForSeconds(1.5f);

            List<Key> keysMinusTitle = FlatKeys.Except(titleKeys).ToList();
            List<Key> shuffledKeys = keysMinusTitle.OrderBy(x => Guid.NewGuid()).ToList();

            // save positions of all keys
            Dictionary<Key, Vector3> originalPositions = FlatKeys.ToDictionary(k => k, k => k.transform.position);

            #region Swap positions of title keys with random keys
            for (int i = 0; i < titleKeyCodes.Count && startIdx + i < middleRowKeys.Count; i++)
            {
                Key targetKey = FlatKeys.FirstOrDefault(k => k.KeyboardLetter == titleKeyCodes[i]);

                if (targetKey != null)
                {
                    int targetRow, targetCol;
                    bool found;
                    (found, targetRow, targetCol) = FindKey(targetKey.KeyboardLetter);

                    if (found)
                    {
                        Key temp = keys[middleRow][startIdx + i];
                        keys[middleRow][startIdx + i] = targetKey;
                        keys[targetRow][targetCol] = temp;
                        temp.transform.DOMove(targetKey.transform.position, 0.75f);
                        targetKey.transform.DOMove(temp.transform.position, 0.75f);

                        float random = Random.Range(0.02f, 0.05f);
                        yield return new WaitForSeconds(random);
                    }
                }
            }

            yield return new WaitForSeconds(0.75f);

            foreach (Key key in shuffledKeys)
            {
                key.Disable();
                yield return new WaitForSeconds(Random.Range(0.02f, 0.05f));
            }
            #endregion

            #region Wait for Player to Start
            // Animate combo markers on title keys
            foreach (Key titleKey in titleKeys)
            {
                titleKey.ComboMarker.SetActive(titleKey.Combo = true);
                yield return new WaitForSeconds(0.1f);
            }

            comboController.CreateCombo(titleKeys);
            yield return new WaitUntil(() => GameManager.Instance.TyperEntered);

            // Remove combo markers from title keys
            foreach (Key titleKey in titleKeys)
            {
                titleKey.ComboMarker.SetActive(titleKey.Combo = false);
                yield return new WaitForSeconds(0.1f);
            }

            // Remove the combo from the system after the markers have been removed for visual flair
            comboController.RemoveCombo(titleKeys);
            #endregion

            #region Return to Original Positions
            foreach (Key key in FlatKeys)
            {
                key.transform.DOMove(originalPositions[key], 0.5f);
                yield return new WaitForSeconds(Random.Range(0.01f, 0.03f));
            }
            
            foreach (Key key in FlatKeys)
            {
                key.Enable();
                yield return new WaitForSeconds(Random.Range(0.02f, 0.05f));
            }
            #endregion

            foreach (Key key in FlatKeys)
            {
                key.offGCDMarker.SetActive(key.OffGlobalCooldown);
                key.ComboMarker.SetActive(key.Combo);
                key.MashMarker.SetActive(key.Mash);
            }
            
            parent.transform.DOMove(new (0.85f, -5f), 1.5f).SetEase(Ease.InOutCubic).OnComplete(() =>
            {
                GameObject canvas = GameObject.Find("Canvas").gameObject;
                canvas.SetActive(true);
            });
        }
    }

    // TODO: Add a stop highlighting function, and reorder the keys to put PLAY in the center when highlighting.
    public void HighlightPlay()
    {
        List<KeyCode> playKeyCodes = new () { KeyCode.P, KeyCode.L, KeyCode.A, KeyCode.Y };
        List<Key> playKeys = playKeyCodes.Select(tc => FlatKeys.FirstOrDefault(k => k.KeyboardLetter == tc)).Where(k => k != null).ToList();
        var keysExceptPlay = FlatKeys.Except(playKeys).ToList();

        foreach (Key key in keysExceptPlay)
        {
            key.Disable();
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
}
