﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour {

    [Header("Player Movements")]
    [SerializeField] float moveSpeed = 10;
    [SerializeField] float movementYOffset = 1f;
    [SerializeField] float padding = 1f;
    [SerializeField] float paddingTop = 3f;

    [Header("Lazer Related")]
    [SerializeField] GameObject playerLazer;
    [SerializeField] float periodOfContinuousFiring = 0.1f;

    [Header("Player Health and Death")]
    [SerializeField] float maxHealth = 200;
    [SerializeField] TMPro.TextMeshProUGUI healthText;
    [SerializeField] AudioClip playerDeathAudio;
    [SerializeField] [Range(0, 1)] float soundVolume = 0.75f;
    [SerializeField] GameObject playerBlastPrefab;
    [SerializeField] float afterDeathTimeScale = 0.5f;
    [SerializeField] Slider playerHealthBar;
    [SerializeField] float screenShakeTimeOnHit = 0.5f;
    [SerializeField] float screenShakeTimeOnDeath = 0.75f;


    [SerializeField] GameObject shieldPrefab;

    float xMin, xMax, yMin, yMax;
    Coroutine fireCoroutine;
    float currentHealth;

    float clicked = 0;
    float clicktime = 0;

    // Use this for initialization
    void Start () {

        var healthInMemory = PlayerPrefs.GetFloat("Health");
        if (healthInMemory == default(System.Single))
        {
            PlayerPrefs.SetFloat("Health", maxHealth);
            currentHealth = maxHealth;
        }
        else
        {
            currentHealth = healthInMemory;
        }
        
        SetUpMoveBoundaries();
        UpdatePlayerHealth();
    }

    // Update is called once per frame
    void Update ()
    {
        Move();
        Fire();
    }

    private float CalculatePercentage(float current, float max)
    {
        if(current <= 0)
        {
            current = 0;
        }

        return (current / max) * 100;
    }

    private void Move()
    {
        var currentMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        var newXPos = Mathf.Clamp(currentMousePosition.x, xMin, xMax);
        var newYPos = Mathf.Clamp(currentMousePosition.y, yMin, yMax) + movementYOffset;
        transform.position = Vector2.MoveTowards(transform.position, new Vector2(newXPos, newYPos), moveSpeed * Time.deltaTime);

    }

    private void Fire()
    {
        if(Input.GetButtonDown("Fire1"))
        {
            fireCoroutine = StartCoroutine(FireContinuously());
        }
        if (Input.GetButtonUp("Fire1"))
        {
            StopCoroutine(fireCoroutine);
        }
    }

    IEnumerator FireContinuously()
    {
        while(true)
        {
            Instantiate(playerLazer, transform.position, Quaternion.identity);
            yield return new WaitForSeconds(periodOfContinuousFiring);
        }
    }

    private void MoveVertical()
    {
        var deltaY = Input.GetAxis("Vertical") * Time.deltaTime * moveSpeed;
        var newYPos = Mathf.Clamp(transform.position.y + deltaY, yMin, yMax);
        transform.position = new Vector2(transform.position.x, newYPos);
    }

    private void MoveHorizontal()
    {
        var deltaX = Input.GetAxis("Horizontal") * Time.deltaTime * moveSpeed;
        var newXPos = Mathf.Clamp(transform.position.x + deltaX, xMin, xMax);
        transform.position = new Vector2(newXPos, transform.position.y);
    }

    private void SetUpMoveBoundaries()
    {
        Camera gameCamera = Camera.main;

        xMin = gameCamera.ViewportToWorldPoint(new Vector3(0, 0, 0)).x + padding;
        xMax = gameCamera.ViewportToWorldPoint(new Vector3(1, 0, 0)).x - padding;

        yMin = gameCamera.ViewportToWorldPoint(new Vector3(0, 0, 0)).y + padding;
        yMax = gameCamera.ViewportToWorldPoint(new Vector3(0, 1, 0)).y - paddingTop;
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        var damageDealer = collider.GetComponent<DamageDealer>();

        if (GameObject.FindGameObjectsWithTag("PlayerShield").Length != 0)
        {
            if (damageDealer != null)
            {
                var enemy = collider.gameObject.GetComponent<Enemy>();

                if (enemy != null && !string.Equals(enemy.tag, "EnemyBoss"))
                {
                    enemy.Die();
                    damageDealer.Hit();
                }
            }

            return;
        }
        
        currentHealth -= damageDealer.GetDamage();
        PlayerPrefs.SetFloat("Health", currentHealth);
        CameraShake.ShakeCameraFor(screenShakeTimeOnHit);

        UpdatePlayerHealth();

        if (string.Equals(collider.gameObject.tag, "Enemy"))
        {
            if (collider.gameObject.GetComponent<Enemy>() == null)
            {
                currentHealth = 0;
                UpdatePlayerHealth();
                return;
            }
            else
            {
                collider.gameObject.GetComponent<Enemy>().Die();
                damageDealer.Hit();
            }

            return;
        }

        if (!string.Equals(collider.gameObject.tag, "EnemyBoss"))
        {
            damageDealer.Hit();
        }
    }

    private void UpdatePlayerHealth()
    {
        var currentHealthPercentage = CalculatePercentage(currentHealth, maxHealth);

        if(playerHealthBar != null)
        {
            playerHealthBar.value = currentHealthPercentage / 100;
        }

        if (healthText != null)
        {
            healthText.text = currentHealthPercentage.ToString();
        }

        if(currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        PlayerPrefs.DeleteKey("Health");
        CameraShake.ShakeCameraFor(screenShakeTimeOnDeath - screenShakeTimeOnHit);
        Time.timeScale = afterDeathTimeScale;
        AudioSource.PlayClipAtPoint(playerDeathAudio, Camera.main.transform.position, soundVolume);
        Instantiate(playerBlastPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
        LevelController.instance.LoadGameOverScene();
    }
}
