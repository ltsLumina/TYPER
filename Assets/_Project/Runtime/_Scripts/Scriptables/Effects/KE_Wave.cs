using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Wave", menuName = "Combos/New Wave", order = 3)]
public class KE_Wave : KeyEffect
{
	protected override void Invoke(KeyCode keyCode, Key key, (bool byKey, Key key) trigger)
	{
		KeyManager.Instance.Wave(1, 1, 0.25f);
	}
}
