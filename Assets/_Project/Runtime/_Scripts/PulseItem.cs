using UnityEngine;

[CreateAssetMenu(fileName = "Pulse Item", menuName = "Shop/Pulse Item")]
public class PulseItem : EffectItem
{
	public override void Grant(ComboEffect effect)
	{
		// find random key that doesn't already have pulse and add pulse to it
		KeyCode.U.ToKey().ComboEffect = effect;
		
		throw new System.NotImplementedException("need to code this lol");
	}

	public override void Upgrade(ComboEffect effect)
	{
		effect.GainLevel();
	}
}
