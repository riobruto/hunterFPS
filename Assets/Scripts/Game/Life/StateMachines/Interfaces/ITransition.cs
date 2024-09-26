namespace Life.StateMachines.Interfaces
{
    internal interface ITransition
    {
        IState To { get; }
        IPredicate Condition { get; }
    }
}