#region
using System.Collections;
using DG.Tweening;
using MelenitasDev.SoundsGood;
using UnityEngine;
using Random = UnityEngine.Random;
#endregion

interface IDamageable
{
	void TakeDamage(int damage);
}

public class Enemy : MonoBehaviour, IDamageable
{
	[SerializeField] int health = 1;
	[SerializeField] float speed = 5f;
	[SerializeField] int damage;

	[Header("Effects")]
	[SerializeField] ParticleSystem deathVFX;
	[SerializeField] ParticleSystem hitVFX;

	SpriteRenderer spriteRenderer;
	int scoreValue;

	public int Lane { get; set; }

	public int Health
	{
		get => health;
		private set => health = value;
	}

	public override string ToString() => name = $"Enemy ({GetInstanceID()}) (on Lane {Lane + 1} | Health: {Health})";

	void Awake() => spriteRenderer = GetComponentInChildren<SpriteRenderer>();

	void Start()
	{
		// randomize health
		Health += WeightedRandomHealth();
		Health = Mathf.Max(1, Health);

		if (Random.value < 0.1f) // 10% chance to be a tank enemy
			Health = 10;

		if (Health >= 10) spriteRenderer.color = Color.red;

		scoreValue = Health * 10;

		// random size and speed based on health
		// larger enemies are slower
		speed -= Health * 0.2f;
		speed = Mathf.Max(1f, speed);

		Vector3 size = transform.localScale;
		size.x = 0.5f + Health * 0.1f;
		size.y = 0.5f + Health * 0.1f;
		transform.localScale = size;

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
		damage = Mathf.Max(1, Mathf.RoundToInt(Health / 2f));
	}

	void OnTriggerEnter2D(Collider2D other)
	{
		if (other.TryGetComponent(out Key key))
		{
			if (!key.IsActive) return;
			Color color = key.SpriteRenderer.color;
			color.a -= 0.3f;
			key.SpriteRenderer.color = color;
		}

		if (other.CompareTag("Finish"))
		{
			Debug.LogWarning("An enemy has reached the end!");
			TakeDamage(999);

			GameManager.Instance.TakeDamage(damage);
		}
	}

	void OnTriggerExit2D(Collider2D other)
	{
		if (other.TryGetComponent(out Key key))
		{
			if (!key.IsActive) return;
			Color color = key.SpriteRenderer.color;
			color.a = 1f;
			key.SpriteRenderer.color = color;
		}
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
			Health = Mathf.Max(0, Health - pendingDamage);
			Debug.Log($"{name} took {pendingDamage} damage. Remaining health: {Health}");
			pendingDamage = 0;
		}
	}

	public void TakeDamage(int damage)
	{
		pendingDamage += damage;
		StartCoroutine(PendingDamage());
		name = ToString();

		ObjectPool hitVFXPool = ObjectPoolManager.FindObjectPool(hitVFX.gameObject);
		ObjectPool deathVFXPool = ObjectPoolManager.FindObjectPool(deathVFX.gameObject);

		switch (Health - pendingDamage)
		{
			// death
			case <= 0: {
				name = $"\"Enemy\" on Lane {Lane + 1} | Dead ({-Health})";

				//Debug.Log($"{name} has Died!");

				// add score. Score is calculated as 10 points per health at spawn time
				GameManager.Instance.AddScore(scoreValue);

				// VFX
				DeathVFX();

				// SFX
				var deathSFX = new Sound(SFX.deathSFX);
				deathSFX.SetOutput(Output.SFX);
				deathSFX.SetRandomPitch(new (0.9f, 1.05f));
				deathSFX.Play();

				Destroy(gameObject);
				break;
			}

			// hit
			case > 0: {
				consecutiveHits++;

				// shrink a bit based on remaining health using DOTween
				int newHealth = Health - pendingDamage;
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
			var vfx = hitVFXPool.GetPooledObject<ParticleSystem>(true);
			ParticleSystem.MainModule main = vfx.main;
			main.startColor = spriteRenderer.color * 0.8f; // slightly darker than enemy color

			// increase the amount of burst count based on damage taken
			ParticleSystem.EmissionModule emission = vfx.emission;
			ParticleSystem.Burst burst = emission.GetBurst(0);

			// each point of damage increases burst count by 2<
			burst.count = (short) (burst.count.constant + damage * 2);
			emission.SetBurst(0, burst);
		}

		void DeathVFX()
		{
			var vfx = deathVFXPool.GetPooledObject<ParticleSystem>(true, transform.position);

			// change color of the deathVFX to the colour of the enemy or red if critical
			ParticleSystem.MainModule main = vfx.main;
			main.startColor = damage > 100 ? Color.red : spriteRenderer.color;
		}
	}
}
