﻿using Game.Entities;

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


            CharacterLimbHitbox[] limbs = GetComponentsInChildren<CharacterLimbHitbox>();

            foreach (CharacterLimbHitbox limb in limbs)
            {
                limb.Ragdoll();
            }
        }
    }
}