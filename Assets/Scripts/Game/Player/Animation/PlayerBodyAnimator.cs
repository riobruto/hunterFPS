﻿using System.Collections;
using UnityEngine;

namespace Game.Player.Animation
{
    public class PlayerBodyAnimator : MonoBehaviour
    {
        private Animator _animator;
       //[SerializeField] private Animator _wingAnimator;
        private PlayerRigidbodyMovement _controller;

        private void Start()
        {
            _animator = GetComponent<Animator>();

            _controller = transform.root.GetComponent<PlayerRigidbodyMovement>();
        }

        // Update is called once per frame
        private void LateUpdate()
        {
            _animator.SetFloat("mov_forward", _controller.RelativeVelocity.z);
            _animator.SetFloat("mov_right", _controller.RelativeVelocity.x);
            _animator.SetFloat("aim_vertical", ((_controller.VerticalLookAngle / 80f)) * -1);
            _animator.SetBool("CROUCH", _controller.CurrentState == PlayerMovementState.CROUCH);
            _animator.SetBool("FALLING", _controller.CurrentState == PlayerMovementState.FALLING);


         


            /*
            _wingAnimator.SetFloat("mov_forward", _controller.RelativeVelocity.z);
            _wingAnimator.SetFloat("mov_right", _controller.RelativeVelocity.x);
            _wingAnimator.SetFloat("aim_vertical", ((_controller.LookMovement.VerticalLookAngle / 80f)) * -1);
            _wingAnimator.SetBool("CROUCH", _controller.IsCrouching);
            _wingAnimator.SetBool("FLY", _controller.IsFlying);
            _wingAnimator.SetBool("FALLING", _controller.IsFalling);
            */
        }
    }
}