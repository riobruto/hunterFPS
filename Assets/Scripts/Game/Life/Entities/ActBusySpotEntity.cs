using Game.Life.WaypointPath;
using Game.Service;
using Life.Controllers;
using UnityEditor;
using UnityEngine;

namespace Game.Life.Entities
{
    public class ActBusySpotEntity : MonoBehaviour
    {
        [SerializeField] private AudioClip _soundClip;
        [SerializeField] private string _stateName;
        [SerializeField] private bool _useWaypoints;
        [SerializeField] private SubtitleParameters[] _stateSubtitle;
        [SerializeField] private WaypointGroup _waypointGroup;






        public bool Taken => CurrentAgent != null;
        public AgentController CurrentAgent { get; private set; }
    
        public AudioClip AudioClip { get => _soundClip; }
        public string StateName { get => _stateName; }
        public SubtitleParameters[] StateSubtitles { get => _stateSubtitle; }
        public bool UseWaypoints => _useWaypoints;
        public WaypointGroup WaypointGroup { get => _waypointGroup; }

        public bool TryTake(AgentController controller)
        {
            if (Taken) return false;
            CurrentAgent = controller;
            return true;
        }

        public void Release()
        {
            CurrentAgent = null;
        }

        private void Start()
        {
            AgentGlobalService.Instance.RegisterActBusySpot(this);

            //REGISTER
        }

        private void OnDestroy()
        {
            AgentGlobalService.Instance.UnregisterActBusySpot(this);
        }

#if UNITY_EDITOR

        private void OnDrawGizmos()
        {
            Handles.Label(transform.position, $"ACTBUSY: {Taken}");
            Gizmos.color = Color.yellow;
            Gizmos.color = Taken ? new Color(1, 1, 1, .33f) : new Color(1, 1, 0, .33f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(Vector3.up, Vector3.one + Vector3.up);
        }

#endif
    }
}