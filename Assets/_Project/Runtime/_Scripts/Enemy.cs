using System;
using System.Collections;
using DG.Tweening;
using MelenitasDev.SoundsGood;
using UnityEngine;
using Random = UnityEngine.Random;

interface IDamageable
{
    void TakeDamage(int damage, bool isCritical = false);
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

    public override string ToString() => name = $"Enemy (on Lane {Lane + 1} | Health: {Health})";

    void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void Start()
    {
        // randomize health
        Health += WeightedRandomHealth();
        Health = Mathf.Max(1, Health);
        if (Random.value < 0.1f) // 10% chance to be a tank enemy
        {
            Health = 10;
            spriteRenderer.color = Color.red;
        }
        
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
        { < 0.6f => 1, // 60% chance of 1 extra health
          < 0.9f => 2, // 30% chance of 2 extra health
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
            var color = key.SpriteRenderer.color;
            color.a -= 0.3f;
            key.SpriteRenderer.color = color;
        }
        
        if (other.CompareTag("Finish"))
        {
            Debug.LogWarning("An enemy has reached the end!");
            TakeDamage(999, true);
            
            GameManager.Instance.TakeDamage(damage);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent(out Key key))
        {
            if (!key.IsActive) return;
            var color = key.SpriteRenderer.color;
            color.a = 1f;
            key.SpriteRenderer.color = color;
        }
    }

    int consecutiveHits;
    Coroutine hitStopCoroutine;
    
    public void TakeDamage(int damage, bool isCritical = false)
    {
        Health = Mathf.Max(0, Health - damage);
        name = ToString();
        
        switch (Health)
        {
            // death
            case <= 0: {
                name = $"Enemy (on Lane {Lane + 1} | Dead)";
                Debug.LogWarning($"{name} has Died!");
            
                // add score. Score is calculated as 10 points per health at spawn time
                GameManager.Instance.AddScore(scoreValue);
            
                // VFX
                DeathVFX();

                // SFX
                var deathSFX = new Sound(SFX.deathSFX);
                deathSFX.SetOutput(Output.SFX);
                deathSFX.SetRandomPitch(new (0.95f, 1.05f));
                deathSFX.Play();

                Destroy(gameObject);
                break;
            }

            // hit
            case > 0: {
                consecutiveHits++;
                
                // shrink a bit based on remaining health
                Vector3 size = transform.localScale;
                size.x = 0.5f + Health * 0.1f;
                size.y = 0.5f + Health * 0.1f;
                transform.localScale = size;
                
                // reduce speed temporarily
                StartCoroutine(Slow(0.5f, 2f));

                // if 3 or more consecutive hits, stun for 3 seconds
                if (consecutiveHits >= 3) StartCoroutine(Stun(3f));

                // add a bit of time slowdown for juiciness
                hitStopCoroutine ??= StartCoroutine(HitStop(0.05f, 0.05f));

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
            enabled = false;
        
            yield return new WaitForSeconds(duration);
        
            enabled = true;
        }              

        IEnumerator HitStop(float duration, float slowdownFactor = 0f)
        {
            Time.timeScale = slowdownFactor;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }

        
        void HitVFX()
        {
            var hitVFX = Instantiate(this.hitVFX, transform.position, Quaternion.identity);
            var main = hitVFX.main;
            main.startColor = spriteRenderer.color * 0.8f; // slightly darker than enemy color
            
            // increase the amount of burst count based on damage taken
            var emission = hitVFX.emission;
            var burst = emission.GetBurst(0);
            // each point of damage increases burst count by 2<
            burst.count = (short)(burst.count.constant + damage * 2);
            emission.SetBurst(0, burst);
        }

        void DeathVFX()
        {
            var deathVFX = Instantiate(this.deathVFX, transform.position, Quaternion.identity, transform ? null : transform);
            
            // change color of the deathVFX to the colour of the enemy or red if critical
            var main = deathVFX.main;
            main.startColor = isCritical ? Color.red : spriteRenderer.color;
        }
    }
}
