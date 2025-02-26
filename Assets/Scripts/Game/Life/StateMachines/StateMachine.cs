using Life.StateMachines.Interfaces;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Life.StateMachines
{
    public delegate void ChangeStateDelegate(IState current, IState next);

    public class StateMachine
    {
        private StateNode _current;
        private Dictionary<Type, StateNode> _nodes = new();
        private HashSet<ITransition> _anyTransition = new();
        public event ChangeStateDelegate ChangeStateEvent;
        public IState CurrentState { get => _current.State; }

        public void Update()
        {
            ITransition transition = GetTransition();
            if (transition != null) ChangeState(transition.To);
            _current.State?.Update();
        }

        public void SetState(IState state)
        {
            _current = _nodes[state.GetType()];
            _current.State?.Start();
        }

        public void AddTransition(IState from, IState to, IPredicate condition)
        {
            GetOrAddNode(from).AddTransition(GetOrAddNode(to).State, condition);
        }

        public void AddAnyTransition(IState to, IPredicate condition)
        {
            _anyTransition.Add(new Transition(GetOrAddNode(to).State, condition));
        }

        public void DrawGizmos()
        {
            _current.State?.DrawGizmos();
        }

        private StateNode GetOrAddNode(IState state)
        {
            StateNode node = _nodes.GetValueOrDefault(state.GetType());

            if (node == null)
            {
                node = new StateNode(state);
                _nodes.Add(state.GetType(), node);
            }

            return node;
        }

        public void ForceChangeToState(IState to)
        {
            GetOrAddNode(to);
            ChangeState(to);
        }

        private void ChangeState(IState state)
        {
            if (state == _current.State)
            {
                Debug.LogError($"StateController tried changing state to current state {state}");
                return;
            }
            _current.State?.End();
            ChangeStateEvent?.Invoke(_current.State, state);
            _current = _nodes[state.GetType()];
            _current.State?.Start();
            Debug.Log($"Current State is now {_current.State}");
        }

        private ITransition GetTransition()
        {
            foreach (var transition in _anyTransition)
            {
                if (transition.Condition.Evaluate()) return transition;
            }
            foreach (var transition in _current.Transitions)
            {
                if (transition.Condition.Evaluate()) return transition;
            }
            return null;
        }

        

        private class StateNode
        {
            public IState State { get; }
            public HashSet<ITransition> Transitions { get; }

            public StateNode(IState state)
            {
                State = state;
                Transitions = new HashSet<ITransition>();
            }

            public void AddTransition(IState state, IPredicate condition)
            {
                Transitions.Add(new Transition(state, condition));
            }
        }
    }
}