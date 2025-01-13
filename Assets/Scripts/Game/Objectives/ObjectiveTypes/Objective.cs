using UnityEngine;
using UnityEngine.Events;

namespace Game.Objectives
{
    public delegate void ObjectiveCompleted(Objective objective);

    public delegate void ObjectiveFailed(Objective objective);

    public abstract class Objective : MonoBehaviour, IObjectiveTarget
    {
        [SerializeField] private float _exitTimeDelayInSeconds;
        [SerializeField] private string _taskName;
        [SerializeField] private string _taskDescription;
        public string TaskName { get => _taskName; }
        public string TaskDescription { get => _taskDescription; }
        public float ExitDelayInSeconds { get => _exitTimeDelayInSeconds; }

        public event UnityAction<Objective, ObjectiveStatus> StatusChanged;

        public abstract Vector3 TargetPosition { get; }

        [Header("Events")]
        public UnityEvent BeginEvent;

        public UnityEvent UpdateEvent;
        public UnityEvent FailEvent;
        public UnityEvent CompleteEvent;

        /// <summary>
        /// Set the status and notifies an event with the value
        /// </summary>
        public ObjectiveStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                NotifyStatus(value);
            }
        }

        private ObjectiveStatus _status;
        /*
        private void Start()
        {
            NotifyStatus(Status);
        }*/

        public abstract void Run();

        public virtual void OnCompleted()
        { }

        public virtual void OnUpdated()
        { }

        public virtual void OnFailed()
        { }

        public virtual void OnBegin()
        { }

        private void NotifyStatus(ObjectiveStatus status)
        {
            switch (status)
            {
                case ObjectiveStatus.PENDING:
                    break;

                case ObjectiveStatus.ACTIVE:
                    OnBegin();
                    BeginEvent?.Invoke();
                    break;

                case ObjectiveStatus.UPDATED:
                    OnUpdated();
                    UpdateEvent?.Invoke();
                    break;

                case ObjectiveStatus.COMPLETED:
                    OnCompleted();
                    CompleteEvent?.Invoke();
                    break;

                case ObjectiveStatus.FAILED:
                    OnFailed();
                    FailEvent?.Invoke();
                    break;
            }

            StatusChanged?.Invoke(this, status);
        }

        public void ForceFailStatus()
        { Status = ObjectiveStatus.FAILED; }

        public void ForceCompletesStatus()
        { Status = ObjectiveStatus.COMPLETED; }

        /// <summary>
        /// Create the objective parameters
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="target"></param>
        /// <param name="description"></param>
        public abstract void Create<T>(string name, T target, string description = "");
    }
}