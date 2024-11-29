namespace Game.Objectives
{
    public delegate void ObjectiveCompleted(Objective objective);

    public delegate void ObjectiveFailed(Objective objective);

    public abstract class Objective
    {
        public abstract string TaskName { get; }
        public abstract string TaskDescription { get; }
        public abstract bool IsCompleted { get; }

        public abstract void Run();

        public abstract void OnCompleted();

        public abstract void OnFailed();

        public abstract event ObjectiveCompleted CompletedEvent;

        public abstract event ObjectiveFailed FailedEvent;

        public abstract void Create<T>(string name, T target, string description = "");
    }
}