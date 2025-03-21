﻿using Life.Controllers;
using Life.StateMachines.Interfaces;

namespace Life.StateMachines
{
    public abstract class BaseState : IState
    {
        public AgentController Context;

        public BaseState(AgentController context)
        {
            Context = context;
        }

        void IState.Start()
        {
            Start();
        }

        void IState.Update()
        {
            Think();
        }

        void IState.End()
        {
            End();
        }

        void IState.DrawGizmos()
        {
            DrawGizmos();
        }

        public abstract void Start();

        public abstract void Think();

        public abstract void End();

        public abstract void DrawGizmos();
    }
}