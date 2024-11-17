using System;
using UnityEngine;

namespace Game.Train
{
    public class TractionBogie : Bogie
    {
        private float currentForce;

        internal void AddBogieForce(float value)
        {           
            rb.AddForce(transform.forward * value);
            Debug.DrawRay(transform.position + transform.up, transform.forward, Color.blue);
        } 
        
        
    }
}