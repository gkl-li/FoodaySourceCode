using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QRCoder;
using UnityEngine.UI;
using QRCoder.Unity;

public class QRGenerator : MonoBehaviour
{
    public Image refImage;
    public void Load()
    {

        // Generate QR code texture
        var link = "FoodayJoinFamily:"+ActiveInfo.activeUser.familyID;
        var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(link, QRCodeGenerator.ECCLevel.M);
        var qrCode = new UnityQRCode(qrCodeData);
        var qrCodeImage = qrCode.GetGraphic(20);

        // Create sprite & display
        var sprite = Sprite.Create(qrCodeImage, new Rect(0, 0, qrCodeImage.width, qrCodeImage.height), new Vector2(.5f, .5f));
        refImage.sprite = sprite;
    }
}
