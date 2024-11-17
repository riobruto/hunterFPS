using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class HUDHeartDisplay : MonoBehaviour
    {
        public float Health { get; set; } = 10;
        public float MaxHealth { get; set; } = 100;
        private float _duration;
        [SerializeField] private Sprite[] _defaultSprites;
        [SerializeField] private Sprite[] _lowHealthSprites;
        private Sprite[] _currentSpriteArray;
        private Image image;
        private int index = 0;
        private float timer = 0;

        private void Start()
        {
            image = GetComponent<Image>();
        }

        private void LateUpdate()
        {
            bool lowLife = Health < MaxHealth / 4;
            _duration = Mathf.Clamp(Mathf.InverseLerp(0, MaxHealth, Health), 0.25f, 1);
            _currentSpriteArray = lowLife ? _lowHealthSprites : _defaultSprites;
            if ((timer += Time.deltaTime) >= (_duration / _currentSpriteArray.Length))
            {
                timer = 0;
                image.sprite = _currentSpriteArray[index];
                index = (index + 1) % _currentSpriteArray.Length;
            }
        }
    }
}