using Core.Weapon;
using Game.Player.Controllers;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Game.Player.Attachment
{
    public class LaserPointerAttachmentComponent : MonoBehaviour
    {
        private Vector3 point;
        private PlayerWeapons _weapons;
        private bool _laserActive;
        [SerializeField] private GameObject _laserPoint;

        private void Start()
        {
            _weapons = transform.root.GetComponent<PlayerWeapons>();
            _laserActive = true;
        }

        private void Update()
        {
            if (!_laserActive) return;
            if (_weapons.WeaponEngine == null) return;
            DrawLaser();
        }

        private void DrawLaser()
        {
            Physics.Raycast(transform.position,transform.forward, out RaycastHit hit, 100);
            if (hit.point == Vector3.zero)
            {
                _laserPoint.SetActive(false);
                return;
            }
            _laserPoint.SetActive(true);
            _laserPoint.transform.position = hit.point;
            _laserPoint.transform.forward = hit.normal;
        }
    }
}