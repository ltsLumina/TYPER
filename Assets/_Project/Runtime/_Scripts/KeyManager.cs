#region
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Lumina.Essentials.Attributes;
using Lumina.Essentials.Modules;
using UnityEngine;
using Random = UnityEngine.Random;
#endregion

public partial class KeyManager : MonoBehaviour
{
	enum KeyboardLayout
	{
		QWERTY,
		AZERTY,
		DVORAK,
	}

	enum KeySet
	{
		Alphabetic,
		Numeric,
		Alphanumeric,
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
	[SerializeField] [ReadOnly] float currentCooldown;

	ComboManager comboManager;

	Key highwayKey;
	GameObject wordHighway;
	Key keyObj;

	public static KeyManager Instance { get; private set; }

	/// <summary>
	///     The parent object containing all key objects.
	/// </summary>
	public GameObject Keyboard { get; private set; }

	public KeyCode KeyPressed { get; private set; }
	
	public float GlobalCooldown => globalCooldown;
	public float CurrentCooldown => currentCooldown;
	public bool OnCooldown => currentCooldown > 0;

	public List<List<Key>> Keys { get; } = new ();
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

	public Tooltip Tooltip { get; set; }

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
						return new (); // Placeholder
				}

				break;

			case KeySet.Numeric:
				switch (keyboardLayout)
				{
					case KeyboardLayout.QWERTY:
						return KeyboardData.Layouts.QWERTY.Numeric;

					case KeyboardLayout.AZERTY:
						return KeyboardData.Layouts.AZERTY.Numeric;

					case KeyboardLayout.DVORAK: // Placeholder
						return new ();          // Placeholder
				}

				break;

			case KeySet.Alphanumeric:
				switch (keyboardLayout)
				{
					case KeyboardLayout.QWERTY:
						return KeyboardData.Layouts.QWERTY.Alphanumeric;

					case KeyboardLayout.AZERTY:
						return KeyboardData.Layouts.AZERTY.Alphanumeric;

					case KeyboardLayout.DVORAK: // Placeholder
						return new ();          // Placeholder
				}

				break;
		}

		return null;
	}

	public (bool found, int row, int col) FindKey(KeyCode keycode)
	{
		for (int r = 0; r < Keys.Count; r++)
		{
			for (int c = 0; c < Keys[r].Count; c++)
				if (Keys[r][c].KeyCode == keycode)
					return (true, r, c);
		}

		return (false, -1, -1);
	}

	public Key GetKey(KeyCode keycode)
	{
		(bool found, int row, int col) = FindKey(keycode);
		return found ? Keys[row][col] : null;
	}
	#endregion

	void Awake()
	{
		if (Instance != null && Instance != this) Destroy(this);
		else Instance = this;
	}

	void Start()
	{
		Helpers.CameraMain.DOFieldOfView(60f, 1f).From(179f).SetEase(Ease.OutCirc);
		
		comboManager = ComboManager.Instance;

		InitializeKeyboard();

		// Anything below this point only runs in the main game scene
		if (SceneManagerExtended.ActiveSceneName != "Game") return;

		//InitializeWordHighway();

		// foreach (Key key in FlatKeys)
		// {
		// 	key.OnActivated += (hitEnemy, triggeredBy) =>
		// 	{
		// 		if (triggeredBy) return; // ignore if activated by another key (e.g., combo)
		//
		// 		if (comboManager.InProgress) return;
		// 		StartCoroutine(HandleNonComboKey(key));
		// 	};
		// }

		// comboManager.OnBeginCombo += key => HandleComboKey(key, 0);
		// comboManager.OnAdvanceCombo += (keys, indices) => HandleComboKey(keys.Item1, indices.Item1);
		// comboManager.OnCompleteCombo += HandleComboCompleted;
		// comboManager.OnComboReset += key => StartCoroutine(HandleComboReset(key));

		#region Modifiers
		if (SceneManagerExtended.ActiveSceneName != "Game") return;
		List<KeyCode> qweCombo = "QWE".ToKeyCodes();
		comboManager.CreateCombo(qweCombo);

		List<KeyCode> asdfCombo = "ASDF".ToKeyCodes();
		comboManager.CreateCombo(asdfCombo);

		List<KeyCode> rtyCombo = "RTY".ToKeyCodes();
		comboManager.CreateCombo(rtyCombo);

		List<KeyCode> cvbCombo = "CVB".ToKeyCodes();
		comboManager.CreateCombo(cvbCombo);

		List<Key> oGCD_Keys = "PLM".ToKeyCodes().ToKeys();
		oGCD_Keys.SetModifier(Key.Effects.OffGlobalCooldown);

		const float cooldown = 10f;
		KeyCode.V.ToKey().SetEffect(Key.Effects.OffGlobalCooldown, true, cooldown);

		// set G key to be a mash key
		Key mashKey = GetKey(KeyCode.G);
		mashKey.SetEffect(Key.Effects.Mash);

		// make H shake
		Key shakeKey = Instance.GetKey(KeyCode.H);
		shakeKey.SetEffect(Key.Effects.Loose);

		// chain J key
		Key chainKey = GetKey(KeyCode.J);
		chainKey.SetEffect(Key.Effects.Chained);
		
		// thorn K key
		Key thornKey = GetKey(KeyCode.K);
		thornKey.SetEffect(Key.Effects.Thorned);
		#endregion
	}

	void Update()
	{
		#region Input Handling
		if (!Input.anyKeyDown) return;

		KeyCode pressedKey = CurrentlyValidKeys.FirstOrDefault(Input.GetKeyDown);
		if (pressedKey == KeyCode.None) return;

		KeyPressed = pressedKey;

		keyObj = FlatKeys.FirstOrDefault(k => k.KeyCode == KeyPressed);
		if (keyObj != null) keyObj.Activate();
		#endregion
	}

	void InitializeKeyboard()
	{
		Keyboard = GameObject.Find("--- Keyboard ---");
		if (Keyboard != null) Destroy(Keyboard);
		Keyboard = new ("Keyboard");

		currentlyValidKeys.Clear();

		// create rows' parent objects
		GenerateRows();

		FlatKeys = GenerateKeys();

		if (SceneManagerExtended.ActiveSceneName == "Game") 
			Keyboard.transform.position = new (3.5f, -2f);

		// Set initial position for intro animation off-screen
		else Keyboard.transform.position = new (3.5f, 8f);

		for (int row = 0; row < Keys.Count; row++)
		{
			if (Keys[row].Count > 0)
			{
				float lane = Keys[row][0].transform.position.y;
				Lanes[row] = lane;
				Debug.DrawLine(new (-10f, lane, 0f), new (10f, lane, 0f), Color.green, 300f);
			}
		}

		#region Utility
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
				if (Keys.Count <= row) Keys.Add(new List<Key>());
				Keys[row].Add(key);
			}

			return Keys.SelectMany(row => row).ToList();
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
		#endregion
	}

	public void StartGlobalCooldown()
	{
		foreach (Key key in FlatKeys.Where(k => !k.IsOffGCD)) key.StartLocalCooldown(globalCooldown);
	}

	IEnumerator HandleNonComboKey(Key key)
	{
		yield return null; // wait one frame (fixes it for some reason)

		if (comboManager.RecentKey?.KeyCode == key.KeyCode)
		{
			if (highwayKey) Destroy(highwayKey.gameObject);
			yield break;
		}

		var prefab = Resources.Load<Key>("PREFABS/Highway Key");
		if (!highwayKey) highwayKey = Instantiate(prefab, wordHighway.transform.position, Quaternion.identity, wordHighway.transform);
		highwayKey.name = key.KeyCode.ToString();
		highwayKey.Letter.text = key.KeyCode.ToString();
		highwayKey.gameObject.SetActive(true);
	}

	readonly List<Key> comboHighwayKeys = new ();
	readonly Queue<Key> queuedKeys = new ();

	void HandleComboKey(Key recentKey, int index) // TODO: properly implement this
	{
		if (DOTween.IsTweening("highwayCompleted") || DOTween.IsTweening("highwayKeyPunch"))
		{
			queuedKeys.Enqueue(recentKey);

			//Debug.Log($"Queued {recentKey.KeyboardLetter} for combo highway! Queue length: {queuedKeys.Count}");
			StartCoroutine(HandleComboKeyQueue());
			return;
		}

		var prefab = Resources.Load<Key>("PREFABS/Highway Key");

		if (highwayKey) Destroy(highwayKey.gameObject);

		Vector3 comboPos = new (index * keyOffset - 3f, 0f, 0f);
		Key comboKey = Instantiate(prefab, wordHighway.transform.position, Quaternion.identity, wordHighway.transform);

		comboKey.transform.DOPunchPosition(new (0, 0.5f, 0), 0.3f).SetLink(comboKey.gameObject);
		comboKey.transform.localPosition = comboPos;
		comboKey.name = recentKey.KeyCode.ToString();
		comboKey.Letter.text = recentKey.KeyCode.ToString();
		comboKey.gameObject.SetActive(true);
		comboHighwayKeys.Add(comboKey);
	}

	IEnumerator HandleComboKeyQueue()
	{
		yield return new WaitWhile(() => DOTween.IsTweening("highwayCompleted") || DOTween.IsTweening("highwayKeyPunch"));
		yield return new WaitForSeconds(0.2f);

		if (queuedKeys.Count > 0) HandleComboKey(queuedKeys.Dequeue(), comboHighwayKeys.Count);
	}

	void HandleComboCompleted(List<Key> comboKeys)
	{
		var prefab = Resources.Load<ParticleSystem>("PREFABS/Combo VFX");
		ObjectPool pool = ObjectPoolManager.FindObjectPool(prefab.gameObject);

		foreach (Key key in comboHighwayKeys)
		{
			if (!key) continue;

			key.transform.DOPunchPosition(new (0, 1f, 0), 0.3f)
			   .SetDelay(0.3f) // waits for the instantiate punch to finish
			   .SetLink(key.gameObject)
			   .SetId("highwayKeyPunch")
			   .OnComplete
			    (() =>
			    {
				    var vfx = pool.GetPooledObject<ParticleSystem>(true, key.transform.position);
				    ParticleSystem.MainModule main = vfx.main;
				    main.maxParticles = 5;
				    main.startColor = Random.ColorHSV(0, 1, 1, 1, 1, 1);
				    vfx.Play();

				    key.transform.DOMoveY(10f, 0.5f).SetEase(Ease.InBack).SetId("highwayCompleted").SetLink(key.gameObject).OnComplete(() => Destroy(key.gameObject));
			    });
		}
	}

	IEnumerator HandleComboReset(Key recentKey)
	{
		yield return new WaitWhile(() => DOTween.IsTweening("highwayCompleted") || DOTween.IsTweening("highwayKeyPunch"));

		// // destroy combohighway keys
		// foreach (Key key in comboHighwayKeys)
		// {
		// 	if (!key) continue;
		// 	Destroy(key.gameObject);
		// }

		comboHighwayKeys.Clear();
	}

	void InitializeWordHighway() // Highlights the current keys pressed in the "Word Highway" area
	{
		wordHighway = GameObject.Find("Word Highway");
		if (wordHighway != null) Destroy(wordHighway);
		wordHighway = new ("Word Highway");

		wordHighway.transform.position = new (0, 3.5f);
		wordHighway.transform.localScale = Vector3.one * 0.75f;
	}
}