using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;
using IBM.Watson.DeveloperCloud.Services.VisualRecognition.v3;
using IBM.Watson.DeveloperCloud.Logging;
using IBM.Watson.DeveloperCloud.Utilities;
using IBM.Watson.DeveloperCloud.Connection;


public class FaceDetector : MonoBehaviour
{
    public CameraRender cd;
    public Text dataOutput;
    public VisualRecognition _visualRecognition;
    string filePath;

    string classifierId = "AlbertoClassify_462833706";

    string positiveExamplesPath;
    string negativeExamplesPath;

    Dictionary<string, string> positiveExamples;
    List<string> classifierIds = new List<string>();

    private string _classifierToDelete;
    private bool _isClassifierReady = false;
    
    public string _serviceUrl;
    public string _iamApikey;
    byte[] imageData;
    string imageMimeType;
     
    void Start() {
        LogSystem.InstallDefaultReactors();

        filePath = Application.dataPath + "/screenshot.jpg";
        positiveExamplesPath = "C:\\Users\\Alberto\\Desktop\\Culmination\\FacialRecognition\\positive_examples.zip";
        negativeExamplesPath = "C:\\Users\\Alberto\\Desktop\\Culmination\\FacialRecognition\\negative_examples.zip";
        positiveExamples = new Dictionary<string, string> {
            ["AlbertoPositive"] = positiveExamplesPath
        };

        

        Debug.Log("Read file bytes");
        imageData = File.ReadAllBytes(filePath);

        Debug.Log("Define Mime Type");
        imageMimeType = "image/jpg";


        Runnable.Run(CreateService());
    }

    private IEnumerator CreateService() {
        Debug.Log("Starting API Connection");
        if (string.IsNullOrEmpty(_iamApikey)) {
            throw new WatsonException("Please provide IAM ApiKey for the service.");
        }

        Credentials credentials = null;

        TokenOptions tokenOptions = new TokenOptions() {
            IamApiKey = _iamApikey
        };

        credentials = new Credentials(tokenOptions, _serviceUrl);

        //wait for token
        while (!credentials.HasIamTokenData())
            yield return null;
        
        //create credentials
        _visualRecognition = new VisualRecognition(credentials);
        _visualRecognition.VersionDate = "2019-03-27";
        Debug.Log("Finished API Connection");

        classifierIds.Add(classifierId);
    }

    //Checks whether it can detect a face in the screenshot provided
    public void DetectFaces() {
        Debug.Log("Detect faces called");

        //classify using image path
        if(!_visualRecognition.DetectFaces(OnDetectFaces, OnFail, filePath, "en")) {
            Debug.Log("ExampleVisualRecognition.DetectFaces(), Detect faces failed!");
        } else {
            Debug.Log("Calling Watson");
            dataOutput.text = "Compiling data!";
            GetClassifier();
        }
    }

    //What happens when it detects a face
    public void OnDetectFaces(DetectedFaces path, Dictionary<string, object> customData)
    {
        Debug.Log(dataOutput.text = "Minimum age: " + path.images[0].faces[0].age.min + ", Maximum age: " + path.images[0].faces[0].age.max + ", Score: " + path.images[0].faces[0].age.score);
        // Debug.Log(dataOutput.text = "Gender: " + path.images[0].faces[0].gender);
    }

    //What happens when it fails to detect a face
    private void OnFail(RESTConnector.Error error, Dictionary<string, object> customData)
    {
        Debug.LogError("ExampleVisualRecognition.OnFail(): Error received: " + error.ToString());
    }

    //Check the IBM Cloud for a classifier
    public void GetClassifier() {
        Debug.Log("Checking for classifiers");

        //If no classifier, option to Train a new one
        if(!_visualRecognition.GetClassifier(OnGetClassifier, OnFail, classifierId)) 
            Log.Debug("ExampleVisualRecognition.GetClassifier()", "Getting classifier failed!");
            // Debug.Log("No classifier, training now!");
            // TrainClassifier();
    }

    //If classifier found, either update the classifier and/or check if the classifier is ready; also runs IsClassifierReady() to keep checking
    //the classifier until it is ready and not run it prematurely
    public void OnGetClassifier(ClassifierVerbose classifier, Dictionary<string, object> customData) {
        Debug.Log("Classifier found!");
        Log.Debug("ExampleVisualRecognition.OnGetClassifier()", "Get classifier result: {0}", customData["json"].ToString());
        // UpdateClassifier();
        _classifierToDelete = classifier.classifier_id;
        Runnable.Run(IsClassifierReady(_classifierToDelete));
    }

    // Classify the screenshot against the classifier online
    public void Classify() {
        if(!_visualRecognition.Classify(successCallback: OnClassify, failCallback: OnFail, imageData: imageData, imageMimeType: imageMimeType, classifierIDs: classifierIds.ToArray(), acceptLanguage: "en")) {
            Debug.Log("Couldn't classify the image!");
            Log.Debug("ExampleVisualRecognition.Classify()", "Classifiy image failed!");
        };
    }

    // CLASSIFY IMAGE
    //public void Classify()
    //{
    //    bool v = _visualRecognition.Classify(successCallback: OnClassify, failCallback: OnFail, imageData: imageData, imageMimeType: imageMimeType, classifierIDs: classifierIds.ToArray(), acceptLanguage: "en");
    //    Debug.Log(v);
    //    if (!v)
    //    {
    //        Debug.Log("Couldn't classify the image!");
    //        Log.Debug("ExampleVisualRecognition.Classify()", "Classifiy image failed!");
    //    }
    //}

    //Once the image is classified, output the results and determine who it is
    public void OnClassify(ClassifiedImages classify, Dictionary<string, object> customData)
    {
        Debug.Log("Classified the image!");
        Debug.Log("Here are your classification results: " + customData["json"].ToString());
        
        // If a value was not returned by the classifier (e.g. null) then it requests a new classifier and states that the image is not you
        if (classify.images[0].classifiers[0].classes.Length < 1)
        {
            Debug.Log("Index out of bounds, no classification available! " + classify.images[0].classifiers[0].ToString());
            dataOutput.text = "You're not Alberto! Please put Alberto on the screen!";
            //cd.CaptureImage();

        }
        // If a value is returned by the classifier it has correctly classified you but categorizes the score
        else
        {
            //If the classification score is above 50% it says hello to you and prints the score
            if (classify.images[0].classifiers[0].classes[0].score > 0.5f)
            {
                dataOutput.text = "Hello Alberto!\n" + classify.images[0].classifiers[0].classes[0].score;
                Log.Debug("ExampleVisualRecognition.OnClassify()", "Classify result: Score: {0}", classify.images[0].classifiers[0].classes[0].score);
            }

            //If the classification score is below 50% it lets you know it cannot recognize you properly and to take a better picture
            else
            {
                dataOutput.text = "Hard to tell who you are, please retake your photo!\n" + classify.images[0].classifiers[0].classes[0].score;
                Log.Debug("ExampleVisualRecognition.OnClassify()", "Classify result: Score: {0}", classify.images[0].classifiers[0].classes[0].score);
            }
        }
    }

    //Trains a new classifer and returns the data from the server
    void TrainClassifier() {
        Debug.Log("Training classifier");
        //If unable to train a classifier, it lets you know and outputs an error
        if(!_visualRecognition.TrainClassifier(OnTrainClassifier, OnFail, "AlbertoClassify", positiveExamples, negativeExamplesPath)) 
            Log.Debug("ExampleVisualRecognition.TrainClassifier()", "Train classifier failed!");
    }

    //If a new classifier is successfully trained, it will output its relevant data to the console and run IsClassifierReady()
    public void OnTrainClassifier(ClassifierVerbose classifier, Dictionary<string, object> customData)
    {
        Log.Debug("ExampleVisualRecognition.OnTrainClassifier()", "Train classifier result: {0}", customData["json"].ToString());
        _classifierToDelete = classifier.classifier_id;
        Debug.Log(customData["json"].ToString());
        Runnable.Run(IsClassifierReady(_classifierToDelete));
    }

    //Updates an existing classifier with the ID provided
    void UpdateClassifier() {
        Debug.Log("Updating classifier");
        //If unable to update the classifier, it lets you know and outputs an error
        if(!_visualRecognition.UpdateClassifier(OnUpdateClassifier, OnFail, classifierId, negativeExamplesPath, positiveExamples)) 
            Log.Debug("ExampleVisualRecognition.UpdateClassifier()", "Update classifier failed!");   
    }

    //If the classifier was updated, it will output its relevant data and run IsClassifierReady()
    public void OnUpdateClassifier(ClassifierVerbose classifier, Dictionary<string, object> customData) {
        Log.Debug("ExampleVisualRecognition.OnUpdateClassifier()", "Update classifier result: {0}", customData["json"].ToString());
        _classifierToDelete = classifier.classifier_id;
        Debug.Log(customData["json"].ToString());
        Runnable.Run(IsClassifierReady(_classifierToDelete));
    }

    
    //IEnumerator that keeps checking every 5 seconds whether the classifier is ready since the classifier can take several minutes, so it's important
    //to know if it's ready before using it; this is part of the reason why I don't train/update classifiers
    public IEnumerator IsClassifierReady (string classifierId) {
        Log.Debug("TestVisualRecognition.IsClassifierReady()", "Checking if classifier is ready in 5 seconds...");
        Debug.Log("Checking if classifier is ready in 5 seconds...");

        yield return new WaitForSeconds(5f);

        Dictionary<string, object> customData = new Dictionary<string, object>();
        customData.Add("classifierId", classifierId);
        //If it determines that the classifier is not ready, it assignes IsClassiferReady() the classifier ID and runs OnCheckIfClassifierReady()
        if (!_visualRecognition.GetClassifier(OnCheckIfClassiferReady, OnFailCheckingIfClassifierIsReady, classifierId)) 
            IsClassifierReady(classifierId);
    }

    //While the classifier is training/updating, it will keep checking if the classifier is returning a response
    public void OnCheckIfClassiferReady(ClassifierVerbose response, Dictionary<string, object> customData) {
        Log.Debug("TestVisualRecognition.IsClassifierReady()", "Classifier status is {0}", response.status);

        //If the response states that it's ready or that it failed, it will attempt to classify the image
        if (response.status == "ready" || response.status == "failed") {
            _isClassifierReady = true;
            Debug.Log("Classifier is ready!");
            Classify();
        }

        //If there is any other response, typically "Training", then it runs IsClassifierReady() again and both functions run each other
        //until a classifier is ready
        else {
            Runnable.Run(IsClassifierReady(response.classifier_id));
            Debug.Log("Classifier is not ready!");
        }
    }

    //If it fails to check if the classifier is ready, it throws a server error and lets you know it cannot communicate with the server
    private void OnFailCheckingIfClassifierIsReady(RESTConnector.Error error, Dictionary<string, object> customData) {
        IsClassifierReady(_classifierToDelete);
    }
}