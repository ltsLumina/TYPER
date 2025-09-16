using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Pulse", menuName = "Combos/New Pulse", order = 7)]
public class KE_Pulse : KeyEffect
{
	protected override void Invoke(KeyCode keyCode, Key key, (bool byKey, Key key) trigger)
	{
		KeyManager.Instance.Pulse(key, 10, 0.3f);
	}
}
