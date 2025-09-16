using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "Thorned", menuName = "Combos/New Thorned", order = 8)]
public class KE_Thorned : KeyEffect
{
	[SerializeField] float cooldown = 3f;
	bool isActive = true;

	protected override void Invoke(KeyCode keyCode, Key key, (bool byKey, Key key) trigger)
	{
		if (isActive && !trigger.byKey) // Doesn't deal damage if triggered by a key, but still activates the thorned effect.
		{
			Debug.Log($"Key {keyCode} activated while thorned! Player takes damage.");
			GameManager.Instance.TakeDamage(1);
			var prefab = Resources.Load<GameObject>("PREFABS/Enemy Death VFX");
			var vfx = ObjectPoolManager.FindObjectPool(prefab).GetPooledObject<ParticleSystem>(true, key.transform.position, Quaternion.identity);
			ParticleSystem.MainModule mainModule = vfx.main;
			mainModule.startColor = Color.red;
		}    
		
		if (DOTween.IsTweening("Thorned")) return; // Prevent overlapping thorned animations.
		
		var sequence = DOTween.Sequence();
		sequence.Append(key.ThornedMarker.transform.DOMoveY(-1f, 0.5f).SetRelative(true).SetEase(Ease.Linear));
		sequence.Join(key.ThornedMarker.transform.DOScaleY(0f, 0.25f).SetEase(Ease.Linear));
		sequence.AppendCallback(() => isActive = false);
		sequence.AppendInterval(cooldown);
		sequence.Append(key.ThornedMarker.transform.DOMoveY(1f, 0.5f).SetRelative(true).SetEase(Ease.Linear));
		sequence.Join(key.ThornedMarker.transform.DOScaleY(1f, 0.25f).SetEase(Ease.Linear));
		sequence.AppendCallback(() => isActive = true);
		sequence.SetId("Thorned");
		sequence.Play();
	}
}
