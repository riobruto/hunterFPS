using Life.StateMachines.Interfaces;
using System;

namespace Life.StateMachines
{
    internal class ActionPredicate : IPredicate
    {
        public ActionPredicate(Action action)
        {
            action += OnCall;
        }

        private bool _actionCalled = false;

        private void OnCall()
        {
            _actionCalled = true;
        }

        bool IPredicate.Evaluate()
        {
            if (_actionCalled)
            {
                _actionCalled = false;

                return true;
            }
            return false;
        }
    }
}