using System.Collections.Generic;
using UnityEngine;

namespace FavCat.CustomLists
{
    public class PickerPool
    {
        private readonly GameObject myPickerPrefab;
        private readonly Transform myPoolRoot;
        public static PickerPool Instance { get; private set; }

        private readonly Stack<GameObject> myObjects = new Stack<GameObject>();

        private int myObjectsBorrowed;

        public PickerPool(GameObject pickerPrefab, GameObject poolRoot)
        {
            myPickerPrefab = pickerPrefab;
            myPoolRoot = poolRoot.transform;
            
            poolRoot.SetActive(false);

            Instance = this;
        }

        public GameObject Request()
        {
            myObjectsBorrowed++;

            if (myObjects.Count == 0)
            {
                var result = Object.Instantiate(myPickerPrefab);
                result.AddComponent<CustomPicker>();
                return result;
            }

            return myObjects.Pop();
        }

        public void Release(GameObject go)
        {
            myObjectsBorrowed--;

            /*if (myObjects.Count > myObjectsBorrowed + 100)
            {
                Object.Destroy(go);
                return;
            }*/ // todo: better shrinking?
            
            go.transform.SetParent(myPoolRoot, false);
            myObjects.Push(go);
        }
    }
}