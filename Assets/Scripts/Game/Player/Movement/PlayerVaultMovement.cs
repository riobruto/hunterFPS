using Nomnom.RaycastVisualization;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Movement
{
    public delegate void VaultStateDelegate(bool state);

    public class PlayerVaultMovement : PlayerBaseMovement
    {
        public event VaultStateDelegate VaultEvent;

        private Vector3 _vaultSurfaceCollisionPoint;
        private int _edgeCastAmount = 6;
        [SerializeField] private float _detectDistance = .5f;
        private bool _isVaulting = false;
        private bool _wantVault;

        [SerializeField] private float _vaultTime = 10;

        internal bool AllowVault;

        public bool CanVault
        {
            get
            {
               
                
                if (!AllowVault) return false;
                if (_isVaulting) return false;
                if (!DetectFowardCollision()) return false;
                if (!DetectEdge()) return false;
                if (!DetectLandSurface()) return false;
                if (DetectObstruction()) return false;
                return true;
                
            }
        }

        protected override void OnUpdate()
        {
            if (_wantVault)
            {
                if (CanVault)
                {
                    BeginVault();
                }
                _wantVault = false;
            }
        }

        private void BeginVault()
        {
            _isVaulting = true;
            VaultEvent?.Invoke(true);
            StartCoroutine(MovePlayerToVaultPoint(_vaultSurfaceCollisionPoint));
        }

        private Vector3 _refVaultVelocity;

        private IEnumerator MovePlayerToVaultPoint(Vector3 point)
        {
            point = point + Vector3.up * Manager.Controller.skinWidth * 2;
            Vector3 Uppoint = new Vector3(transform.position.x, point.y, transform.position.z);

            while (Vector3.Distance(transform.position, Uppoint) > 0.5f)
            {
                transform.position = Vector3.SmoothDamp(transform.position, Uppoint, ref _refVaultVelocity, _vaultTime);
                yield return null;
            }
            while (Vector3.Distance(transform.position, point) > 0.1f)
            {
                transform.position = Vector3.SmoothDamp(transform.position, point, ref _refVaultVelocity, _vaultTime / 2);
                yield return null;
            }
            transform.position = point;
            _isVaulting = false;
            VaultEvent?.Invoke(false);

            yield return null;
        }

        private void OnJump(InputValue value)
        {
           // _wantVault = true;
        }

        private bool DetectFowardCollision()
        {
            bool raycast = VisualPhysics.BoxCast(transform.position + transform.up / 2, Vector3.one / 4, transform.forward, out RaycastHit hit, Quaternion.LookRotation(transform.forward), _detectDistance);

            return raycast;
        }

        [SerializeField] private float _maxHeightDetection = 2.5f;

        [SerializeField] private float _edgeDetectionHeightStart = 0.5f;

        private bool DetectEdge()
        {
            bool[] contacts = new bool[_edgeCastAmount];

            for (int i = 0; i < _edgeCastAmount; i++)
            {
                if (VisualPhysics.BoxCast(transform.position + (transform.up * i / _edgeCastAmount) * _maxHeightDetection + transform.up * _edgeDetectionHeightStart, Vector3.one * 1 / _edgeCastAmount, transform.forward, Quaternion.LookRotation(transform.forward), _detectDistance))
                {
                    contacts[i] = true;
                }
            }
            return contacts.Any(x => x);
        }

        private float _minimalValidSurfaceNormalDotProduct = .95f;

        private bool DetectLandSurface()
        {
            Vector3 origin = transform.position + transform.up * _maxHeightDetection + transform.forward;
            RaycastHit hit;
            if (VisualPhysics.Raycast(origin, Vector3.down, out hit, _maxHeightDetection))
            {
                float dot = Vector3.Dot(Vector3.up, hit.normal);
                bool dotCheck = dot > _minimalValidSurfaceNormalDotProduct;
                Debug.DrawRay(origin, hit.normal, dotCheck ? Color.green : Color.red);

                if (dotCheck)
                {
                    //Es plano
                    _vaultSurfaceCollisionPoint = hit.point;
                    return true;
                }

                return false;
            }
            return false;
        }

        private bool DetectObstruction()
        {
            return VisualPhysics.BoxCast(_vaultSurfaceCollisionPoint, Vector3.one / 2, Vector3.up, Quaternion.identity, 2);
        }
    }
}