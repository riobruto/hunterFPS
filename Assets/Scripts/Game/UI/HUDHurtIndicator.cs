using Core.Engine;
using Game.Player.Controllers;
using Game.Service;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class HUDHurtIndicator : MonoBehaviour
    {
        private PlayerHealth _health;

        [SerializeField] private Image _left;
        [SerializeField] private Image _right;
        [SerializeField] private Image _top;
        [SerializeField] private Image _bot;

        [SerializeField] private Color _color;

        private Vector3 _currentDirection;

        // Use this for initialization
        private void Start()
        {
            _health = Bootstrap.Resolve<PlayerService>().Player.GetComponent<PlayerHealth>();
            _health.HurtEvent += OnHurt;
        }

        private void OnHurt(HurtPayload payload)
        {
            _currentDirection += payload.Direction;
        }

        // Update is called once per frame
        private void LateUpdate()
        {
            _currentDirection = Vector3.ClampMagnitude(_currentDirection, Mathf.Clamp(_currentDirection.magnitude - Time.deltaTime, 0, 1));

            _left.color = CalculateColorFromDir(new Vector3(-1, 0, 0));
            _right.color = CalculateColorFromDir(new Vector3(1, 0, 0));
            _top.color = CalculateColorFromDir(new Vector3(0, 0, 1));
            _bot.color = CalculateColorFromDir(new Vector3(0, 0, -1));
        }

        private Color CalculateColorFromDir(Vector3 indicatorDir)
        {
            Color colorA = _color;
            colorA.a = 0;
            Color colorB = _color;
            return Color.Lerp(colorA, colorB, Vector3.Dot(_currentDirection, _health.transform.TransformDirection(indicatorDir)));
        }
    }
}