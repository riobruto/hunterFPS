using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject _prefab;
    private float _timeBetween = 5;
    private float _lastTime = 0;

    // Update is called once per frame
    private void Update()
    {
        if (Time.time - _lastTime > _timeBetween)
        {
            _lastTime = Time.time;
            SpawnSquad();
        }
    }

    private void SpawnSquad()
    {
        Instantiate(_prefab, transform.position, transform.rotation);
    }
}