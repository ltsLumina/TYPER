using System;
using UnityEngine;

interface IDamageable
{
    void TakeDamage(int damage);
}

public class Enemy : MonoBehaviour, IDamageable
{
    [SerializeField] int health = 1;
    [SerializeField] float speed = 5f;

    [Header("Effects")]
    [SerializeField] ParticleSystem deathVFX;

    public int Health
    {
        get => health;
        private set => health = value;
    }

    void Update()
    {
        transform.Translate(Vector3.left * (speed * Time.deltaTime));
        
        if (transform.position.x < -10f)
        {
            Destroy(gameObject);
            Debug.LogWarning($"{name} has Escaped!");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out Key key))
        {
            var color = key.SpriteRenderer.color;
            color.a -= 0.3f;
            key.SpriteRenderer.color = color;
        }
        
        if (other.CompareTag("Finish"))
        {
            Debug.LogWarning("An enemy has reached the end!");
            Destroy(gameObject);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent(out Key key))
        {
            var color = key.SpriteRenderer.color;
            color.a = 1f;
            key.SpriteRenderer.color = color;
        }
    }

    public void TakeDamage(int damage)
    {
        Health -= damage;
        if (Health <= 0)
        {
            Destroy(gameObject);
            Debug.LogWarning($"{name} has Died!");
            var vfx = Instantiate(deathVFX, transform.position, Quaternion.identity);
        }
    }
}
