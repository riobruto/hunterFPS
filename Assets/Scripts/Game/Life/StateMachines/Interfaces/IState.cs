namespace Life.StateMachines.Interfaces
{
    public interface IState
    {
        public void Start();

        public void Update();

        public void End();

        public void DrawGizmos();
    }
}