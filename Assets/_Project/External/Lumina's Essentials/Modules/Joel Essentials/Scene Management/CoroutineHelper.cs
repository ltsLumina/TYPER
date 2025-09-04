﻿#region
using System.Collections;
using UnityEngine;
#endregion

public class CoroutineHelper : MonoBehaviour
{
	static CoroutineHelper instance;

	[RuntimeInitializeOnLoadMethod]
	static void Initialize()
	{
		if (instance == null)
		{
			var coroutineHelperObject = new GameObject("[Coroutine Helper]");
			instance = coroutineHelperObject.AddComponent<CoroutineHelper>();
			DontDestroyOnLoad(coroutineHelperObject);
		}
	}

	void Awake()
	{
		if (instance != null && instance != this) Destroy(gameObject);
	}

	static CoroutineHelper Instance
	{
		get
		{
			if (instance == null) Initialize();
			return instance;
		}
	}

	public static MonoBehaviour GetHost() => Instance;

	new public static Coroutine StartCoroutine(IEnumerator coroutine)
	{
		if (Instance != null) return ((MonoBehaviour) Instance).StartCoroutine(coroutine);

		Debug.LogError("[Coroutine Helper] is null. Coroutine cannot be started.");
		return null;
	}

	new public static void StopCoroutine(Coroutine coroutine)
	{
		if (Instance != null) ((MonoBehaviour) Instance).StopCoroutine(coroutine);
		else Debug.LogError("[Coroutine Helper] is null. Coroutine cannot be stopped.");
	}
}
