using Game.Life;
using UnityEngine;

namespace Life.StateMachines
{
    public class StateContext
    {
        public AgentCoverBehavior Cover { get; }
        public AgentHealthBehavior Health { get; }
        public AgentMoveBehavior Move { get; }
        public AgentPatrolBehavior Patrol { get; }
        public AgentPlayerBehavior Player { get; }
        public AgentRagdollBehavior Ragdoll { get; }
        public AgentSquadBehavior Squad { get; }
        public AgentWeaponBehavior Weapon { get; }

        public StateContext(AgentCoverBehavior cover, AgentHealthBehavior health, AgentMoveBehavior move, AgentPatrolBehavior patrol, AgentPlayerBehavior player, AgentRagdollBehavior ragdoll, AgentSquadBehavior sector, AgentWeaponBehavior weapon)
        {
            Cover = cover;
            Health = health;
            Move = move;
            Patrol = patrol;
            Player = player;
            Ragdoll = ragdoll;
            Squad = sector;
            Weapon = weapon;
        }

        public static StateContext CreateFromGameObject(GameObject gameObject)
        {
            return new StateContext(
                gameObject.GetComponent<AgentCoverBehavior>(),
                gameObject.GetComponent<AgentHealthBehavior>(),
                gameObject.GetComponent<AgentMoveBehavior>(),
                gameObject.GetComponent<AgentPatrolBehavior>(),
                gameObject.GetComponent<AgentPlayerBehavior>(),
                gameObject.GetComponent<AgentRagdollBehavior>(),
                gameObject.GetComponent<AgentSquadBehavior>(),
                gameObject.GetComponent<AgentWeaponBehavior>()

                );
        }
    }
}