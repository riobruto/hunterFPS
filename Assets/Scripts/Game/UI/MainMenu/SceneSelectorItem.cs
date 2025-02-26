using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI.MainMenu
{
    public class SceneSelectorItem : MonoBehaviour
    {
        Button _button;
        [SerializeField] private string _sceneName;
        private void Start()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(() =>
            {
                FindObjectOfType<MainMenuElements>().SetSelectedScene(_sceneName);
            });
        }

    }
}