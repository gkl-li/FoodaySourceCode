using Crosstales.FB;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using ZXing;

public class QRReader : MonoBehaviour
{
    public static string filePath = "";
    public static string qrContent = "";
    public static IEnumerator ReadQRCode(Button openFileDialogButton)
    {
        openFileDialogButton.onClick.RemoveAllListeners();
        openFileDialogButton.onClick.AddListener(delegate {
            filePath = FileBrowser.Instance.OpenSingleFile("*");
        });

        filePath = "";
        yield return new WaitUntil(() => filePath != "");

        qrContent = "";
        if (System.IO.File.Exists(filePath))
        {
            try
            {
                Texture2D tex = null;
                byte[] fileData = System.IO.File.ReadAllBytes(filePath);
                tex = new Texture2D(100, 100);
                tex.LoadImage(fileData);
                BarcodeReader reader = new BarcodeReader();
                Result res = reader.Decode(tex.GetPixels32(), tex.width, tex.height);
                Debug.Log(res.Text);
                qrContent = res.Text;
            }
            catch
            {
                GameObject.FindObjectOfType<UIManager>().Alert("This QR code is not recognizable!");
            }
        }
    }

}
