using Life.StateMachines.Interfaces;

namespace Life.StateMachines
{
    internal class Transition : ITransition
    {
        private IState _state;
        private IPredicate _condition;

        public Transition(IState state, IPredicate condition)
        {
            _state = state;
            _condition = condition;
        }

        IState ITransition.To { get => _state; }

        IPredicate ITransition.Condition { get => _condition; }

        //Luli password
        // zm3ZUz4K)2
    }
}