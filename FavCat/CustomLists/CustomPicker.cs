using System;
using UnhollowerBaseLib.Attributes;
using UnityEngine;
using UnityEngine.UI;

#nullable disable

namespace FavCat.CustomLists
{
    public class CustomPicker : MonoBehaviour
    {
        private RawImage myImage;
        private Text myLabelText;
        private Image myCornerImage;
        private Image myLowerCornerImage;
        private GameObject myPrivateImage;

        private IPickerElement? myListElement;
        private Action<string>? myOnClick;

        private Texture2D? myTexture;
        
        [HideFromIl2Cpp]
        public CustomPicker(IntPtr ptr) : base(ptr) {}

        [HideFromIl2Cpp]
        public void Initialize(IPickerElement pickerElement, Action<string> onClick)
        {
            myListElement = pickerElement;
            myTexture = null;
            myOnClick = onClick;

            if (myLabelText == null)
                return;
            
            myLabelText.text = pickerElement.Name;

            myCornerImage.gameObject.SetActive(true);
            if (!pickerElement.SupportsDesktop && !pickerElement.SupportsQuest)
                myCornerImage.gameObject.SetActive(false);
            else if (pickerElement.SupportsDesktop)
                myCornerImage.sprite = pickerElement.SupportsQuest ? AssetsHandler.IconUni : AssetsHandler.IconPC;
            else
                myCornerImage.sprite = pickerElement.SupportsQuest ? AssetsHandler.IconQuest : AssetsHandler.IconUni;

            var colorMaybe = pickerElement.CornerIconColor;
            if (colorMaybe.HasValue)
            {
                myLowerCornerImage.enabled = true;
                myLowerCornerImage.color = colorMaybe.Value;
            }
            else
            {
                myLowerCornerImage.enabled = false;
            }

            myPrivateImage.SetActive(pickerElement.IsPrivate);
            
            if (gameObject.activeSelf)
                DoDownloadImage();
        }

        [HideFromIl2Cpp]
        internal void Clean()
        {
            OnDisable();
            if (myListElement != null)
            {
                GlobalImageCache.CancelRequest(myListElement.ImageUrl, OnImageDownloaded);
            }
            myListElement = null;
            myOnClick = null;
            myTexture = null;
        }

        [HideFromIl2Cpp]
        private void DoDownloadImage()
        {
            if (myListElement != null)
            {
                if (myListElement.IsInaccessible)
                    myImage.texture = AssetsHandler.PreviewError.texture;
                else
                {
                    myImage.texture = AssetsHandler.PreviewLoading.texture;
                    GlobalImageCache.DownloadImage(myListElement.ImageUrl, OnImageDownloaded);
                }
            }
        }

        [HideFromIl2Cpp]
        private void OnImageDownloaded(Texture2D texture)
        {
            myTexture = texture;
            if (gameObject.activeSelf)
                myImage.texture = texture;
        }
        

        private void Awake()
        {
            myImage = GetComponentInChildren<RawImage>();
            myLabelText = transform.Find("Label").GetComponent<Text>();
            myCornerImage = transform.Find("CornerIcon").GetComponent<Image>();
            myLowerCornerImage = transform.Find("LowerCornerIcon").GetComponent<Image>();
            myPrivateImage = transform.Find("CornerLock").gameObject;
            
            GetComponent<Button>().onClick.AddListener(new Action(() => myOnClick?.Invoke(myListElement?.Id)));
            
            if (myListElement != null)
                Initialize(myListElement, myOnClick!);
        }

        private void OnEnable()
        {
            if (myTexture != null) 
                myImage.texture = myTexture;
            else
                DoDownloadImage();
        }

        private void OnDisable()
        {
            if (myImage != null)
                myImage.texture = null;
        }
    }
}