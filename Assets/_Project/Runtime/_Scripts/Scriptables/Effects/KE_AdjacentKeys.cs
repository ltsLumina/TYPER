using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Adjacent Keys", menuName = "Combos/New Adjacent Keys", order = 1)]
public class KE_AdjacentKeys : KeyEffect
{
	[SerializeField] KeyManager.Direction direction = KeyManager.Direction.Right;
	
	public override string ToString() => $"Adjacent Keys ({direction})";

	protected override void Invoke(KeyCode keyCode, Key key, (bool byKey, Key key) trigger)
	{
		if (!key) keyCode.ToKey().StartCoroutine(InvokeWithDelay(keyCode, keyCode.ToKey()));
		else key.StartCoroutine(InvokeWithDelay(keyCode, key));
	}

	IEnumerator InvokeWithDelay(KeyCode keyCode, Key key)
	{
		GameManager.Instance.TriggerHitStop(0.1f, 0.1f);

		yield return new WaitForSecondsRealtime(0.1f);

		//Debug.Log($"Key Effect \"{ToString()}\" triggered on {keyCode}", key);

		Key adjacentKey = KeyManager.Instance.GetAdjacentKey(keyCode, direction, out List<Key> adjacentKeys);

		if (adjacentKey)
		{
			KeyManager.SpawnVFX(KeyManager.CommonVFX.Combo, key.transform.position);
			adjacentKey.Activate(true, 0.5f, key);
			yield return new WaitForSecondsRealtime(0.02f);
		}

		if (adjacentKeys is { Count: > 0 })
		{
			foreach (Key k in adjacentKeys)
			{
				KeyManager.SpawnVFX(KeyManager.CommonVFX.Combo, key.transform.position);
				k.Activate(true, 0.5f, key);
				yield return new WaitForSecondsRealtime(0.02f);
			}
		}
	}
}
