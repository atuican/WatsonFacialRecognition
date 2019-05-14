using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using IBM.Watson.DeveloperCloud.Services.VisualRecognition.v3;
using IBM.Watson.DeveloperCloud.Logging;
using IBM.Watson.DeveloperCloud.Utilities;


public class CameraRender : MonoBehaviour
{
    public Image overlay;
    public FaceDetector fd;

    // Start is called before the first frame update
    public void Start()
    {
        Debug.Log("Camera Started");
        WebCamTexture backCam = new WebCamTexture();
        backCam.Play();
        overlay.material.mainTexture = backCam;
    }

    public void CaptureImage() {
        Debug.Log("Capture started");
        ScreenCapture.CaptureScreenshot("Assets/screenshot.jpg");
        fd.DetectFaces();
        // Application.persistentDataPath + "/screenshot.png"

    }
    // Update is called once per frame
    //void Update()
    //{
    //    if (Input.GetMouseButtonDown(0)) {
    //        CaptureImage();
    //    }
    //}
}