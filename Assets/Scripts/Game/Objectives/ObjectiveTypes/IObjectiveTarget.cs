using UnityEngine.Events;

namespace Game.Objectives
{
    public interface IObjectiveTarget
    {
        ObjectiveStatus Status { get; }

        event UnityAction<Objective, ObjectiveStatus> StatusChanged;
    }
}