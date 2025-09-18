using System.Collections;
using DG.Tweening;
using MelenitasDev.SoundsGood;
using UnityEngine;

[CreateAssetMenu(fileName = "Frozen", menuName = "Modifiers/Frozen", order = 5)]
public class KE_Frozen : KeyModifier
{
	protected override void Invoke(Key key, (bool byKey, Key key) trigger)
	{
		key.StartCoroutine(Freeze());
		Debug.Log($"Key Effect: {name} applied to {key.name}");
		
		return;
		IEnumerator Freeze()
		{
			// Apply a blue tint to the key and make it unpressable for a short duration
			var originalColor = key.SpriteRenderer.color;
			var frozenColor = Color.cyan;
			frozenColor.a = originalColor.a; // Preserve original alpha
			key.SpriteRenderer.DOColor(frozenColor, 0.2f).SetEase(Ease.OutQuad);
			key.Disable(false);

			yield return new WaitUntil(() => !key.IsFrozen);
			
			key.SpriteRenderer.DOColor(originalColor, 0.2f).SetEase(Ease.OutQuad);
			key.Enable(false);
		}
	}
}
