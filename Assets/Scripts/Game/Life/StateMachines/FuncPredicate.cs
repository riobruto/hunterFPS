using Life.StateMachines.Interfaces;
using System;

namespace Life.StateMachines
{
    public class FuncPredicate : IPredicate
    {
        private readonly Func<bool> _func;
        public FuncPredicate(Func<bool> func)
        {
            _func = func;
        }

        bool IPredicate.Evaluate()
        {
            return _func.Invoke();
        }
    }
}