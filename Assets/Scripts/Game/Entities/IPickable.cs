﻿using Game.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;

namespace Game.Entities
{
    internal interface IPickable
    {
        InventoryItem Take();
    }
}