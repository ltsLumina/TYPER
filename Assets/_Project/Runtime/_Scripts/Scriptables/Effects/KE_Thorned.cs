using UnityEngine;

[CreateAssetMenu(fileName = "Thorned", menuName = "Combos/New Thorned", order = 6)]
public class KE_Thorned : KeyEffect
{
	protected override void Invoke(KeyCode keyCode, Key key, bool triggeredByKey)
	{
		GameManager.Instance.TakeDamage(1);
		
		var vfx = Instantiate(Resources.Load<ParticleSystem>("PREFABS/Enemy Death VFX"), key.transform.position, Quaternion.identity);
		ParticleSystem.MainModule mainModule = vfx.main;
		mainModule.startColor = Color.red;
		vfx.Play();
		
		Debug.Log($"Thorned effect triggered on {keyCode}. Player takes 1 damage." );
	}
}
