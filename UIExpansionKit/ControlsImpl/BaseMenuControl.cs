using System;
using UIExpansionKit.API.Controls;
using UnityEngine;

#nullable enable

namespace UIExpansionKit.ControlsImpl
{
    public class BaseMenuControl : IMenuControl
    {
        private GameObject? myGameObject;
        private bool myVisible = true;
        
        public virtual void ConsumeGameObject(GameObject obj)
        {
            myGameObject = obj;
            obj.SetActive(myVisible);
            
            OnInstanceCreated?.Invoke(obj);
        }

        public bool Visible
        {
            get => myGameObject != null ? myGameObject.activeSelf : myVisible;
            set
            {
                myVisible = value;
                if (myGameObject != null) 
                    myGameObject.SetActive(value);
            }
        }

        public event Action<GameObject>? OnInstanceCreated;
        public GameObject? CurrentInstance => myGameObject;
    }
}