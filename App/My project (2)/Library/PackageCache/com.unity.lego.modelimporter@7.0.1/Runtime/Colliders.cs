// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LEGOModelImporter
{
    public class Colliders : MonoBehaviour, IEnumerable<Collider>
    {
        public int version = 0;
        [HideInInspector]
        public List<Collider> colliders = new List<Collider>();
        public Part part;

        public IEnumerator<Collider> GetEnumerator()
        {
            foreach(var collider in colliders)
            {
                yield return collider;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
