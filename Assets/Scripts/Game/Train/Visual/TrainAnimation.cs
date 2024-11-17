using System.Collections;
using UnityEngine;

namespace Game.Train.Visual
{
    public class TrainAnimation : MonoBehaviour
    {
        private TrainBase _train;

        private float offset;
        private float _amount;

        private void Start()
        {
            _train = GetComponentInParent<TrainBase>();
            offset = Random.Range(-10f, 10f);
        }

        private void LateUpdate()
        {
            _amount = Mathf.InverseLerp(0, 80, _train.Speed);
            transform.localRotation = Quaternion.Euler((.5f - Mathf.PerlinNoise(offset, Time.time + offset)) * 2f * _amount,
                0,
                (.5f - Mathf.PerlinNoise(Time.time - offset, offset)) * 5f * _amount);
            transform.localPosition = new Vector3(0, (.5f - Mathf.PerlinNoise(Time.time + offset, offset) - 0.5f) * .25f * _amount, 0);
        }
    }
}