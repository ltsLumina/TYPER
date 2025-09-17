#region
using System.Collections.Generic;
using UnityEngine;
#endregion

public static class ObjectPoolManager
{
	static List<ObjectPool> objectPools = new ();

	static Transform objectPoolParent;
	static Transform ObjectPoolParent
	{
		get
		{
			if (objectPoolParent == null)
			{
				var objectPoolParentGameObject = new GameObject("--- Object Pools ---");
				objectPoolParent = objectPoolParentGameObject.transform;
			}

			return objectPoolParent;
		}
	}

	public static void Reset()
	{
		objectPools.Clear();
		ObjectPoolLookup.Clear();
	}

	// Dictionary to cache the object pools by prefab identifier for faster lookup.
	readonly static Dictionary<string, ObjectPool> ObjectPoolLookup = new ();

	static string GetPrefabKey(GameObject prefab)
	{
		// If prefab is loaded from Resources, use its path if possible, otherwise fallback to name
		// You may want to extend this for AssetBundles or addressables
		return prefab.name;
	}

	/// <summary>
	///     Adds an existing pool to the list of object pools.
	/// </summary>
	/// <param name="objectPool"></param>
	public static void AddExistingPool(ObjectPool objectPool)
	{
		if (objectPool == null)
		{
			Debug.LogError("Object pool cannot be null!");
			return;
		}

		objectPools.Add(objectPool);
		string key = GetPrefabKey(objectPool.ObjectPrefab);
		ObjectPoolLookup[key] = objectPool;
		objectPool.transform.parent = ObjectPoolParent;
	}

	/// <summary>
	///     Creates a new object pool as a new gameobject.
	/// </summary>
	/// <param name="objectPrefab"></param>
	/// <param name="startAmount"></param>
	/// <returns>The pool that was created.</returns>
	public static ObjectPool CreateNewPool(GameObject objectPrefab, int startAmount = 20)
	{
		if (objectPrefab == null)
		{
			Debug.LogError("Object prefab cannot be null!");
			return null;
		}

		var newObjectPool = new GameObject().AddComponent<ObjectPool>();
		newObjectPool.SetUpPool(objectPrefab, startAmount);

		return newObjectPool;
	}

	/// <summary>
	///     Returns the pool containing the specified object prefab.
	///     Creates and returns a new pool if none is found.
	/// </summary>
	/// <param name="objectPrefab"></param>
	/// <returns></returns>
	public static ObjectPool FindObjectPool(GameObject objectPrefab, int startAmount = 20)
	{
		if (objectPrefab == null)
		{
			Debug.LogError("Object prefab cannot be null!");
			return null;
		}

		string key = GetPrefabKey(objectPrefab);
		if (ObjectPoolLookup.TryGetValue(key, out ObjectPool objectPool)) return objectPool;

		Debug.LogWarning($"Object of type {key} is NOT yet pooled! Creating a new pool...");
		objectPool = CreateNewPool(objectPrefab, startAmount);
		ObjectPoolLookup[key] = objectPool;
		return objectPool;
	}

	/// <summary>
	/// Returns the object to its pool (deactivates it).
	/// Literally just sets it inactive, but this is a bit more semantic.
	/// </summary>
	/// <param name="gameObject"> The object to return to the pool. </param>
	public static void ReturnToPool(GameObject gameObject) => gameObject.SetActive(false);
}
