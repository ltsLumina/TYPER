using UnityEngine;

[CreateAssetMenu(fileName = "Shard Potion", menuName = "Shop/Shard Potion")]
public class ShardPotion : PotionItem
{
	[Tooltip("Amount of shards the potion gives when purchased.")]
	public int shardAmount;

	public override void UsePotion() => ShardManager.Instance.AddShards(shardAmount);
}