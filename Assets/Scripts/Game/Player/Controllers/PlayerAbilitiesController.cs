using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Game.Player.Controllers
{
    public enum AbilityType
    {
        BLINDINGLIGHT,
        PAUSE,
        REGENERATION,
        CONCEALMENT,
        REVELATION,
        ANGER,
        TEMPER,
        AURACLEARING
    }

    public enum AbilityState
    {
        BEGIN,
        ACTIVE,
        END,
    }

    public class PlayerAbilitiesController : MonoBehaviour
    {
        public event UnityAction<AbilityType, AbilityState> AbilityStateEvent;

        public event UnityAction<AbilityType> AbilityChangeEvent;

        public event UnityAction<bool> OpenRadialEvent;

        public float CurrentEnergy => _currentEnergy;
        public AbilityType CurrentPower => _currentAbilityType;

        public bool CanOpenRadial
        {
            get => _canOpenRadial;
            set
            {
                if (!value) CloseRadial();
                _canOpenRadial = value;
            }
        }

        private float _currentEnergy = 0;
        private float _maxEnergy = 100f;
        private AbilityType _currentAbilityType;
        private bool _usingPower;

        [SerializeField] private AbilitiesConfiguration _abilityConfiguration;

        [Header("Visual References")]
        [SerializeField] private Light _light;

        private bool _canOpenRadial;
        private bool _radialOpen;

        private void Start()
        {
            _canOpenRadial = true;
            _currentAbilityType = AbilityType.BLINDINGLIGHT;
            _currentEnergy = _maxEnergy;
            _light.intensity = 0;
        }

        private bool HasEnergyForCurrentAbility()
        {
            switch (_currentAbilityType)
            {
                case AbilityType.BLINDINGLIGHT:
                    return _abilityConfiguration.LightEnergyConsumption < _currentEnergy;

                case AbilityType.PAUSE:
                    return _abilityConfiguration.PauseEnergyConsumption < _currentEnergy;

                case AbilityType.REGENERATION:
                    return _abilityConfiguration.RegenerationEnergyConsumption < _currentEnergy;

                case AbilityType.CONCEALMENT:
                    return _abilityConfiguration.ConcealmentEnergyConsumption < _currentEnergy;

                case AbilityType.REVELATION:
                    return _abilityConfiguration.RevelationEnergyConsumption < _currentEnergy;

                case AbilityType.ANGER:
                    return _abilityConfiguration.AngerEnergyConsumption < _currentEnergy;

                case AbilityType.TEMPER:
                    return _abilityConfiguration.TemperEnergyConsumption < _currentEnergy;

                case AbilityType.AURACLEARING:
                    return _abilityConfiguration.AuraClearingEnergyConsumption < _currentEnergy;
            }

            return false;
        }

        private void OnPowerUse(InputValue value)
        {
            if (HasEnergyForCurrentAbility() && !_usingPower)
            {
                UsePower();
            }
        }

        private void OnPowerChange(InputValue value)
        {
            if ((int)_currentAbilityType + 1 > Enum.GetValues(typeof(AbilityType)).Length - 1)
            {
                _currentAbilityType = 0;
            }
            else _currentAbilityType++;
        }

        private void UsePower()
        {
            switch (_currentAbilityType)
            {
                case AbilityType.BLINDINGLIGHT:
                    StartCoroutine(BlindingLight()); return;
                case AbilityType.PAUSE:
                    StartCoroutine(Pause()); return;
                case AbilityType.REGENERATION:
                    StartCoroutine(Regeneration()); return;
                case AbilityType.CONCEALMENT:
                    StartCoroutine(Concealment()); return;
                case AbilityType.REVELATION:
                    StartCoroutine(Revelation()); return;
                case AbilityType.ANGER:
                    StartCoroutine(Anger()); return;
                case AbilityType.TEMPER:
                    StartCoroutine(Temper()); return;
                case AbilityType.AURACLEARING:
                    StartCoroutine(AuraClearing()); return;
            }
        }

        private IEnumerator BlindingLight()
        {
            _usingPower = true;
            //Spawns a light that blinds nearby enemies at player positions
            float time = 0;
            _currentEnergy -= _abilityConfiguration.LightEnergyConsumption;
            while (time < _abilityConfiguration.LightDuration)
            {
                time += Time.deltaTime;
                _light.intensity = ((time / _abilityConfiguration.LightDuration - 1f) * -1f) * 100f;

                yield return null;
            }
            _light.intensity = 0;
            _usingPower = false;
            yield return null;
        }

        private IEnumerator Pause()
        {
            _usingPower = true;
            float time = 0;
            _currentEnergy -= _abilityConfiguration.PauseEnergyConsumption;
            while (time < _abilityConfiguration.PauseDuration)
            {
                time += Time.deltaTime;
                Time.timeScale = _abilityConfiguration.PauseTimeScale;

                yield return null;
            }
            Time.timeScale = 1f;
            _usingPower = false;
            yield return null;
        }

        private IEnumerator Regeneration()
        {
            //regenerates player health to max regen
            yield return null;
        }

        private IEnumerator Concealment()
        {
            //makes player invisible
            yield return null;
        }

        private IEnumerator Revelation()
        {
            //makes all enemies invisible
            yield return null;
        }

        private IEnumerator Anger()
        {
            //augmented damage
            yield return null;
        }

        private IEnumerator Temper()
        {
            //augmented damage resistance por
            yield return null;
        }

        private IEnumerator AuraClearing()
        {
            //Spawns a explosion that hurts nearby enemies at player feet
            yield return null;
        }

        private void OnGUI()
        {
            GUILayout.Label($"{_currentAbilityType}", GUILayout.Height(UnityEngine.Screen.height));
        }

        private void OnAbilityRadial(InputValue value)
        {
            if (!_canOpenRadial)
            {
                if (_radialOpen)
                {
                    CloseRadial();
                }
                //close radial if open
                return;
            }

            if (value.isPressed)
            {
                OpenRadial();
            }
            else
            {
                CloseRadial();
            }
        }

        private void OpenRadial()
        {
            _radialOpen = true;
            OpenRadialEvent?.Invoke(true);
        }

        private void CloseRadial()
        {
            _radialOpen = false;
            OpenRadialEvent?.Invoke(false);
        }
    }

    [Serializable]
    public class AbilitiesConfiguration
    {
        [Header("Bliding Light")]
        public float LightDuration;

        public float LightEnergyConsumption;
        public float LightRadius;

        [Header("Pause")]
        public float PauseDuration = 5;

        public float PauseEnergyConsumption = 30;
        public float PauseTimeScale = 0.4f;

        [Header("Regeneration")]
        public float RegenerationDuration;

        public float RegenerationEnergyConsumption;
        public float RegenerationSpeed;

        [Header("Concealment")]
        public float ConcealmentDuration;

        public float ConcealmentEnergyConsumption;

        [Header("Revelation")]
        public float RevelationDuration;

        public float RevelationEnergyConsumption;
        public float RevelationRadius;

        [Header("Anger")]
        public float AngerDuration;

        public float AngerEnergyConsumption;
        public float AngerDamageMultiplier;

        [Header("Temper")]
        public float TemperDuration;

        public float TemperEnergyConsumption;
        public float TemperDamageResistanceMultiplier;

        [Header("Aura Clearing")]
        public float AuraClearingDuration;

        public float AuraClearingEnergyConsumption;
        public float AuraClearingRadius;
    }
}