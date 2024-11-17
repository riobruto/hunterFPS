using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Game.Train
{
    [CreateAssetMenu(fileName = "new TrainEngineConfig", menuName = "TrainEngineConfig")]
    public class TrainEngineConfig : ScriptableObject
    {

        [SerializeField] private AnimationCurve _tractiveForceCurve;
        [SerializeField]private EngineConfiguration _engineConfig;
        [SerializeField] private BrakeConfiguration _brakeConfig;
        [SerializeField] private NotchLevel[] _powerLevels;

        public NotchLevel[] PowerLevels { get => _powerLevels; }
        public EngineConfiguration EngineConfig { get => _engineConfig; }
        public BrakeConfiguration BrakeConfig { get => _brakeConfig; }

        [Serializable]
        public class NotchLevel
        {
            [SerializeField] public float Force;            
            [SerializeField] public float MaxSpeedInKilometersPerHour;              
            [SerializeField] public float _energyConsumptionRate;            
        }       

        [Serializable]
        public class EngineConfiguration
        {
            public float IdleRPM;
            public float MaxRPM;
            public float MinRPM;
            public float FuelCapacity;
        }
        [Serializable]
        public class BrakeConfiguration
        {

            public AnimationCurve Curve;

            public float MaxPressure;
            public float MinPressure;

            public float MaxDrag;
            public float MinDrag;

            public float ApplicationSpeed;

        }



    }
}
