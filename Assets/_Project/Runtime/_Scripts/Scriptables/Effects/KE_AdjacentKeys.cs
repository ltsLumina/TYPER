using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Adjacent Keys", menuName = "Combos/New Adjacent Keys", order = 1)]
public class KE_AdjacentKeys : KeyEffect
{
	[SerializeField] KeyManager.Direction direction = KeyManager.Direction.Right;

	public override string ToString() => $"Adjacent Keys ({direction})";

	protected override void Invoke(KeyCode keyCode, Key key, bool triggeredByKey)
	{
		if (!key) keyCode.ToKey().StartCoroutine(InvokeWithDelay(keyCode, keyCode.ToKey()));
		else key.StartCoroutine(InvokeWithDelay(keyCode, key));
	}

	IEnumerator InvokeWithDelay(KeyCode keyCode, Key key)
	{
		GameManager.Instance.TriggerHitStop(0.1f, 0.1f);

		yield return new WaitForSecondsRealtime(0.1f);

		Debug.Log($"Key Effect \"{ToString()}\" triggered on {keyCode}", key);

		var comboVFX = Resources.Load<ParticleSystem>("PREFABS/Combo VFX");
		ObjectPool comboPool = ObjectPoolManager.FindObjectPool(comboVFX.gameObject);

		Key adjacentKey = KeyManager.Instance.GetAdjacentKey(keyCode, direction, out List<Key> adjacentKeys);

		if (adjacentKey)
		{
			var vfx = comboPool.GetPooledObject<ParticleSystem>(true, adjacentKey.transform.position);
			ParticleSystem.MainModule main = vfx.main;
			main.startColor = Random.ColorHSV(0f, 1f, 1f, 1f, 1f, 1f);
			adjacentKey.Activate(true, 0.5f, key);
			yield return new WaitForSecondsRealtime(0.02f);
		}

		if (adjacentKeys is { Count: > 0 })
		{
			foreach (Key k in adjacentKeys)
			{
				var vfx = comboPool.GetPooledObject<ParticleSystem>(true, k.transform.position);
				ParticleSystem.MainModule main = vfx.main;
				main.startColor = Random.ColorHSV(0f, 1f, 1f, 1f, 1f, 1f);
				k.Activate(true, 0.5f, key);
				yield return new WaitForSecondsRealtime(0.02f);
			}
		}
	}
}
