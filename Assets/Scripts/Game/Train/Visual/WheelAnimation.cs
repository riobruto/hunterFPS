using System.Collections;
using UnityEngine;

namespace GameTrain.Visual
{
    public class WheelAnimation : MonoBehaviour
    {
       
        Vector3 lastpos;
        [SerializeField] private float radio = .32f;

        private void LateUpdate()
        {

            float delta = Vector3.Distance(transform.position , lastpos);
            float dir =  Vector3.Dot(transform.position - lastpos  , transform.parent.forward) > 0 ? 1:-1;
            transform.Rotate((delta/2*Mathf.PI*radio)*360 * dir, 0, 0);

            lastpos = transform.position;
        }
    }
}