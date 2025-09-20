#region
using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using MelenitasDev.SoundsGood;
using UnityEngine;
using Random = UnityEngine.Random;
#endregion

interface IDamageable
{
	void TakeDamage(int damage);
}

[SelectionBase]
public class Enemy : MonoBehaviour, IDamageable
{
	[SerializeField] int health = 1;
	[SerializeField] float speed = 5f;
	[SerializeField] int damage;

	[Header("Effects")]
	[SerializeField] ParticleSystem deathVFX;
	[SerializeField] ParticleSystem hitVFX;

	SpriteRenderer spriteRenderer;
	int frenzyValue;

	public int Lane { get; set; }

	public override string ToString() => name = $"Enemy (#{GetInstanceID()}) (on Lane {Lane + 1} | Health: {health})";

	void Awake() => spriteRenderer = GetComponentInChildren<SpriteRenderer>();

	void OnEnable() => OnDeath += Reset;

	void OnDisable() => OnDeath -= Reset;

	void Reset()
	{
		health = 1;
		speed = 5f;
		damage = 1;
		consecutiveHits = 0;
		pendingDamage = 0;
		isFrozenSlowed = false;
		touchingKeys.Clear();

		transform.localScale = new (0.5f, 0.5f, 1f);

		Start();
	}

	void Start()
	{
		// randomize health
		health += WeightedRandomHealth();
		health = Mathf.Max(1, health);

		if (Random.value < 0.1f) // 10% chance to be a tank enemy
			health = 10;

		if (health >= 10) spriteRenderer.color = Color.red;

		frenzyValue = health * 10;

		Vector3 size = transform.localScale;
		size.x = 0.5f + health * 0.1f;
		size.y = 0.5f + health * 0.1f;
		transform.localScale = size;

		// random size and speed based on health
		// larger enemies are slower
		speed -= health * 0.2f;
		speed = Mathf.Max(1f, speed);
		originalSpeed = speed;

		name = ToString();
	}

	int WeightedRandomHealth()
	{
		float rand = Random.value;

		return rand switch
		{ < 0.6f => 1,   // 60% chance of 1 extra health
		  < 0.9f => 2,   // 30% chance of 2 extra health
		  _      => 3 }; // 10% chance of 3 extra health
	}

	void Update()
	{
		transform.Translate(Vector3.left * (speed * Time.deltaTime));

		// damage is proportional to size (health)
		// damage is half of health rounded up, minimum 1
		damage = Mathf.Max(1, Mathf.RoundToInt(health / 2f));
	}

	float originalSpeed;
	bool isFrozenSlowed;

	void OnTriggerEnter2D(Collider2D other)
	{
		if (other.TryGetComponent(out Key key))
		{
			// do something
			if (key.IsActive)
			{
				Color color = key.SpriteRenderer.color;
				color.a -= 0.3f;
				key.SetColour(color);
			}

			// randomly decide to apply a negative modifier to the key
			// float rand = Random.value;
			//
			// if (rand < 0.2f && !key.HasModifier(Key.Modifiers.Chained)) // 20% chance to freeze
			// {
			// 	DOVirtual.DelayedCall
			// 	(0.35f, () =>
			// 	{
			// 		speed = 0;
			//
			// 		transform.DOPunchPosition(new (0.2f, 0f, 0f), 0.5f, 10)
			// 		         .SetEase(Ease.OutElastic)
			// 		         .OnComplete
			// 		          (() =>
			// 		          {
			// 			          // make sure again that the key doesn't already have the modifier
			// 			          if (key.HasModifier(Key.Modifiers.Frozen) || health <= 0) return;
			//
			// 			          key.AddModifier(Key.Modifiers.Chained);
			// 			          Debug.Log($"{name} has chained Key {key.ToKeyCode()}!");
			// 			          OnDeath?.Invoke();
			// 			          ObjectPoolManager.ReturnToPool(gameObject);
			// 		          });
			// 	});
			// }
		}

		if (other.CompareTag("Finish"))
		{
			Debug.LogWarning("An enemy has reached the end!");
			TakeDamage(999);

			GameManager.Instance.TakeDamage(damage);
		}
	}

	readonly List<Key> touchingKeys = new ();

	void OnTriggerStay2D(Collider2D other)
	{
		// Continuously check if the key is frozen while inside the trigger
		if (other.TryGetComponent(out Key key))
		{
			if (!touchingKeys.Contains(key)) touchingKeys.Add(key);

			bool anyFrozen = touchingKeys.Exists(k => k.IsFrozen);

			switch (anyFrozen)
			{
				case true when !isFrozenSlowed:
					speed = Mathf.Max(0.1f, speed * 0.1f);
					isFrozenSlowed = true;
					Debug.Log($"{name} is frozen! Speed reduced to {speed}");
					break;

				case false when isFrozenSlowed: {
					if (gameObject.activeSelf) StartCoroutine(LerpSpeed());
					isFrozenSlowed = false;
					break;
				}
			}
		}
	}

	void OnTriggerExit2D(Collider2D other)
	{
		if (other.TryGetComponent(out Key key))
		{
			if (key.IsActive)
			{
				Color color = key.SpriteRenderer.color;
				color.a = 1f;
				key.SetColour(color);
			}

			touchingKeys.Remove(key);

			bool anyFrozen = touchingKeys.Exists(k => k.IsFrozen);

			if (!anyFrozen && isFrozenSlowed)
			{
				if (gameObject.activeSelf) StartCoroutine(LerpSpeed());
				isFrozenSlowed = false;
			}
		}
	}

	IEnumerator LerpSpeed()
	{
		float elapsed = 0f;
		float duration = 0.25f;

		while (elapsed < duration)
		{
			elapsed += Time.deltaTime;
			speed = Mathf.Lerp(speed, originalSpeed, elapsed / duration);
			yield return null;
		}

		speed = originalSpeed;
	}

	int consecutiveHits;
	int pendingDamage;

	IEnumerator PendingDamage()
	{
		yield return new WaitForSecondsRealtime(0.1f);

		// Store the amount of damage taken this frame and apply it at the end of the frame.
		// This is due to each key applying damage once each, so the enemy takes damage multiple times per frame from different keys.
		if (pendingDamage > 0)
		{
			health = Mathf.Max(0, health - pendingDamage);
			Debug.Log($"{name} took {pendingDamage} damage." + $"\nRemaining health: {health}");
			pendingDamage = 0;
		}
	}

	public event Action OnDeath;

	public void TakeDamage(int damage)
	{
		pendingDamage += damage;
		StartCoroutine(PendingDamage());
		name = ToString();

		switch (health - pendingDamage)
		{
			// death
			case <= 0: {
				name = $"\"Enemy\" on Lane {Lane + 1} | Dead ({-health})";

				//Debug.Log($"{name} has Died!");

				// add score. Frenzy is calculated as 10 points per health at spawn time
				FrenzyManager.Instance.AddFrenzy(frenzyValue);

				// VFX
				DeathVFX();

				// SFX
				var deathSFX = new Sound(SFX.deathSFX);
				deathSFX.SetOutput(Output.SFX);
				deathSFX.SetVolume(0.5f);
				deathSFX.SetRandomPitch(new (0.9f, 1.05f));
				deathSFX.Play();

				ObjectPoolManager.ReturnToPool(gameObject);
				OnDeath?.Invoke();
				break;
			}

			// hit
			case > 0: {
				consecutiveHits++;

				// shrink a bit based on remaining health using DOTween
				int newHealth = health - pendingDamage;
				var targetSize = new Vector3(0.5f + newHealth * 0.1f, 0.5f + newHealth * 0.1f, transform.localScale.z);
				transform.DOScale(targetSize, 0.2f).SetEase(Ease.OutFlash, 1f, 0f).SetLink(gameObject);

				// reduce speed temporarily
				StartCoroutine(Slow(0.5f, 2f));

				// if 3 or more consecutive hits, stun for 3 seconds
				if (consecutiveHits % 3 == 0) StartCoroutine(Stun(1f));

				// ensure at least the current damage is applied if pendingDamage is zero
				pendingDamage = pendingDamage > 0 ? pendingDamage : damage;

				// scale with damage taken. Special case for high damage hits (8 or more)
				float slowdownTime = pendingDamage >= 8 ? 1f : 0.05f;
				float slowdownAmount = pendingDamage >= 8 ? 0.02f : 0.05f;
				bool specialHit = pendingDamage >= 8;
				if (specialHit) FrenzyManager.Instance.TriggerFrenzy(pendingDamage, true);

				// add a bit of time slowdown for juiciness
				GameManager.Instance.TriggerHitStop(slowdownTime, slowdownAmount);

				// VFX
				HitVFX();

				// SFX
				var hitSFX = new Sound(SFX.hitSFX);
				hitSFX.SetOutput(Output.SFX);
				hitSFX.SetRandomPitch();
				hitSFX.Play();
				break;
			}
		}

		return;

		IEnumerator Slow(float duration, float amount)
		{
			float originalSpeed = speed;
			speed = Mathf.Max(1f, speed - amount);

			yield return new WaitForSeconds(duration);

			speed = originalSpeed;
		}

		IEnumerator Stun(float duration)
		{
			float originalSpeed = speed;
			speed = 0f;

			yield return new WaitForSeconds(duration);

			speed = originalSpeed;

			consecutiveHits = 0;
		}

		void HitVFX()
		{
			var vfx = KeyManager.SpawnVFX(KeyManager.CommonVFX.Hit, transform.position, spriteRenderer.color * 0.8f);

			// increase the amount of burst count based on damage taken
			ParticleSystem.EmissionModule emission = vfx.emission;
			ParticleSystem.Burst burst = emission.GetBurst(0);

			// each point of damage increases burst count by 2<
			burst.count = (short) (burst.count.constant + damage * 2);
			emission.SetBurst(0, burst);
		}

		void DeathVFX() =>

				// change color of the deathVFX to the colour of the enemy or red if critical
				KeyManager.SpawnVFX(KeyManager.CommonVFX.Combo, transform.position, damage > 100 ? Color.red : spriteRenderer.color);
	}
}
