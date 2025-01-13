using Core.Engine;
using Game.Audio;
using Game.Objectives;
using Game.Service;
using UnityEngine;

namespace Game.UI
{
    public class HUDSounds : MonoBehaviour
    {
        [SerializeField] private AudioClip Message;

        [Header("Objectives")]
        [SerializeField] private AudioClip ObjectiveAdvance;
        [SerializeField] private AudioClip ObjectiveCompleted;
        [SerializeField] private AudioClip ObjectiveFailed;

        [Header("Inventory")]
        [SerializeField] private AudioClip OpenInventory;

        [SerializeField] private AudioClip CloseInventory;

        [Header("Weapon")]
        [SerializeField] private AudioClip PickWeapon;

        [SerializeField] private AudioClip PickAmmo;
        [SerializeField] private AudioClip PickGranade;

        private void Start()
        {
            //UIService.CreateMessageEvent += (x) => PlayClip(Message);
           
            Bootstrap.Resolve<ObjectiveService>().CompleteEvent += (a, b) => PlayClip(ObjectiveCompleted);
            Bootstrap.Resolve<ObjectiveService>().AdvanceEvent += (a, b) => PlayClip(ObjectiveAdvance);
             InventoryService.Instance.ToggleInventoryEvent += (open) => PlayClip(open ? OpenInventory : CloseInventory, .4f);
            InventoryService.Instance.GiveAmmoEvent += (open) => PlayClip(PickAmmo, .4f);


        }

        private void PlayClip(AudioClip clip, float volume = 1)
        {
            AudioToolService.PlayUISound(clip, volume);
        }
    }
}