using Game.Entities;

using UnityEngine;
using UnityEngine.AI;

namespace Game.Life
{
    public class AgentRagdollBehavior : MonoBehaviour
    {
        [ContextMenu("Ragdoll")]
        public void Ragdoll()
        {
            GetComponent<Animator>().enabled = false;
            GetComponent<NavMeshAgent>().enabled = false;


            LimbHitbox[] limbs = GetComponentsInChildren<LimbHitbox>();

            foreach (LimbHitbox limb in limbs)
            {
                limb.Ragdoll();
            }
        }
    }
}