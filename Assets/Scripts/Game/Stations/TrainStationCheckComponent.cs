using Game.Train;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Stations
{
	public class TrainStationCheckComponent : MonoBehaviour
	{
		[SerializeField] private List<TrainBase> _currentTrainParked = new List<TrainBase>();
		[SerializeField] private bool _isStopped;
		private bool _canCheckTrain => _currentTrainParked.Count > 0;

		private void Update()
		{
			if (!_canCheckTrain) return;
			foreach (TrainBase train in _currentTrainParked)
			{
				_isStopped = train.Speed < 0.01f;
			}
		}

		private void OnTriggerEnter(Collider other)
		{
			if (other.TryGetComponent(out TrainBase car))
			{
				_currentTrainParked.Add(car);
			}
		}

		private void OnTriggerExit(Collider other)
		{
			if (other.TryGetComponent(out TrainBase car))
			{
				_currentTrainParked.Remove(car);
			}
		}
	}
}