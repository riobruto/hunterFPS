using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Minigames
{
    public class PuzzlePerceptron : MonoBehaviour
    {
        //implementar un perceptron
        //la idea es que el jugador deba decodificar en señal positiva los patrones aureos de los desaparecidos.
        //y discriminar aquellos patrones que sean ruido.
        //dado un array de patrones, se configurara una serie de Knobs para que el jugador pueda descriminar los pixeles donde la señal es baja, y aumentar la señal alta.
        //el generador va a tirar imagenes
        //twekear los knobs hasta q la maquina solo admita los patrones de los perdidos en voltaje positivo.

        /*
        0,1,1,1
        0,0,1,0
        0,0,1,0
        0,0,1,0

        1,1,1,0
        0,1,0,0
        0,1,0,0
        0,1,0,0

        */

        private float[] _pattern = new float[16];
        private float[][] _patterns = new float[4][];
        private float[] _output = new float[16];

        [SerializeField] private PerceptronKnob[] _knobs;
        [SerializeField] private PerceptronKnob _balancer;

        [SerializeField] private Image[] _screens;
        [SerializeField] private Image[] _targetScreens;

        [SerializeField] private RectTransform _needle;

        public float Result;
        private float _lastUpdateTime;

        private void Start()
        {
            for (int i = 0; i < _patterns.Length; i++)
            {
                _patterns[i] = new float[16];
                int negativeParts = 0;
                int positiveParts = 0;

                for (int j = 0; j < _patterns[i].Length; j++)
                {
                    float prob = Random.Range(0f, 1f);
                    positiveParts = prob > 0.5f ? positiveParts + 1 : positiveParts;
                    negativeParts = prob < 0.5f ? negativeParts + 1 : negativeParts;

                    if(positiveParts > 8) { _patterns[i][j] = -10f; }
                    else if(negativeParts > 8) { _patterns[i][j] = 10f; }
                    else _patterns[i][j] = prob > 0.5f ? 10f : -10f;
                }
            }

            for (int i = 0; i < _targetScreens.Length; i++)
            {
                _targetScreens[i].color = Color.Lerp(Color.black, Color.white, Mathf.InverseLerp(-10, 10, _patterns[0][i]));
            }


        }

        [SerializeField] private int _currentPattern;

        private void Update()
        {
            if (Time.time - _lastUpdateTime > .5f)
            {
                _currentPattern = (int)Mathf.Repeat(_currentPattern + 1, _patterns.Length);

                for (int i = 0; i < _screens.Length; i++)
                {
                    _screens[i].color = Color.Lerp(Color.black, Color.white, Mathf.InverseLerp(-10, 10, _patterns[_currentPattern][i]));
                }
                _lastUpdateTime = Time.time;
            }

            for (int i = 0; i < _knobs.Length; i++)
            {
                _output[i] = _knobs[i].Value * _patterns[_currentPattern][i];
                

            }
            for (int i = 0; i < _knobs.Length; i++)
            {
                Result += _output[i];
            }

            Result = (Result / 16f) + _balancer.Value; 
            _needle.eulerAngles = new Vector3(0, 0, Mathf.Lerp(100, -100, Mathf.InverseLerp(-160, 160, Result)));



        }
    }
}