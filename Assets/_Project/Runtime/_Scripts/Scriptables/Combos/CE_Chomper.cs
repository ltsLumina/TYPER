using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ChomperLevelSettings : LevelSettings
{
	public int damage = 1;
}

/// <summary>
///     Deals additional damage to an enemy.
///     The higher the level, the more damage is dealt.
/// </summary>
/// <remarks> Currently the only combo effect that scales to level X. </remarks>
[CreateAssetMenu(fileName = "Chomper", menuName = "Combos/Chomper", order = 8)]
public class CE_Chomper : ComboEffect
{
	readonly Dictionary<int, bool> incrementedForLevel = new();

	protected override void Invoke(Key key, (bool byKey, Key key) trigger)
	{
		key.StartCoroutine(Chomp());

		return;
		IEnumerator Chomp()
		{
			int currentLevel = (int) Level;

			for (int levelIndex = 0; levelIndex <= currentLevel; levelIndex++)
			{
				if (!incrementedForLevel.ContainsKey(levelIndex) || !incrementedForLevel[levelIndex])
				{
					var settings = GetLevelSettings<ChomperLevelSettings>(levelIndex);
					key.Damage += settings.damage;
					//Debug.Log($"Chomper level {levelIndex + 1} activated! Damage increased by {settings.damage}. Total damage: {key.Damage}", key);
					incrementedForLevel[levelIndex] = true;
				}
			}
			
			yield return new WaitForSeconds(0.51f);
		
			// flash purple
			key.SetColour(Color.mediumPurple, 1f);
			var vfx = KeyManager.SpawnVFX(KeyManager.CommonVFX.Combo, key.transform.position);
			ParticleSystem.MainModule main = vfx.main;
			main.startColor = Color.mediumPurple;
		}
	}
}
