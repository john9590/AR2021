using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.XR.ARFoundation;
using UnityEngine.UI;
using UnityEngine.XR.ARSubsystems;
using System.Linq;
using UnityEngine.Networking;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using JetBrains.Annotations;
using UnityEngine.Android;
using Unity.VisualScripting;
using System;
using UnityEngine.Experimental.XR.Interaction;

public class FacilityInfo
{
    public string[] readingRoom;
    public string[] printShop;
    public string[] emptyClassrooms;
}

public class ReadingRoom
{
    public int totalSeats;
    public int availableSeats;
}

public class PrintShop
{
    public bool isOpen;
    public string hours;
}

public class GetLocation : MonoBehaviour
{
    public static GetLocation Instance { set; get; }
    public float latitude;
    public float longitude;
    public float altitude;
    public int gps_count = 0;
    public string message;
    public GameObject objectToPlace;
    public GameObject arrow;
    private GameObject[] placedObject = new GameObject[2];
    public ARRaycastManager raycastManager;
    public float maxWatchTime = 1f; // 최대 시선 유지 시간
    private float[] currentWatchTime = {0.0f, 0.0f}; // 현재 시선 유지 시간
    public GameObject uiToShow; // 활성화할 UI
    public TextMeshProUGUI uiText;
    public TextMeshProUGUI debugText;
    public TextMeshProUGUI debugText2;
    private Vector2[] rhks = new Vector2[2];
    private FacilityInfo jsonData;
    private bool isSet = false;
    private bool iscompass = true;
    private string[] httpsjson = { "http://3.39.11.150:8080/maps/building?building=K",
                                    "http://3.39.11.150:8080/maps/building?building=J" };
    private string[] imageurl = { "https://dbscthumb-phinf.pstatic.net/5701_000_34/20210806195737492_1ZXAYA0WY.jpg/aW1mb3RvfDM0NzI0.jpg?type=m935&autoRotate=true&wm=N",
                                    "https://lh3.googleusercontent.com/p/AF1QipOOhJ2n_DB9etfwv9ThRtqN0bFMLq263bxNVirq=s1360-w1360-h1020" };
    public RawImage rawImage;
    public AspectRatioFitter aspectRatioFitter;
    public AspectRatioFitter aspectRatioFitter1;
    private Vector2[] KtoJRoute = new Vector2[3];
    private Vector2 curLocation = new Vector2(0.0f,0.0f);
    private int cur_route_i = 0;
    private bool isNav = false;
    private float fromN = 0.0f;
    private void Start()
    {
        Instance = this;
        rhks[0] = new Vector2(37.55004f, 126.9401f);
        rhks[1] = new Vector2(37.55054f, 126.9437f);
        KtoJRoute[0] = new Vector2(37.55007f,126.94084f);
        KtoJRoute[1] = new Vector2(37.55025f,126.941784f);
        KtoJRoute[2] = new Vector2(37.550814f,126.943411f);
        DontDestroyOnLoad(gameObject);
        Input.compass.enabled = true;
        //StartCoroutine(GetTexture("https://lh3.googleusercontent.com/p/AF1QipOOhJ2n_DB9etfwv9ThRtqN0bFMLq263bxNVirq=s1360-w1360-h1020"));
        StartCoroutine(ObjectRotation());
        //GetJsonData(0);
        StartCoroutine(StartLocationService());
        /*string _path = Application.persistentDataPath + "/Scripts";
        if (File.Exists(_path + "/SogangLink.json")) {
            string data = File.ReadAllText(_path + "/SogangLink.json");
            jsonData = JsonUtility.FromJson<FacilityInfo>(data);
            //Debug.Log(jsonData.readingRoom.totalSeats);
            Debug.Log(jsonData.emptyClassrooms[0]);
        }
        /*string jsonFilePath = "/Scripts/SogangLink.json";
        if(File.Exists(Application.persistentDataPath + jsonFilePath))
        {
            string jsonString = File.ReadAllText(Application.persistentDataPath + jsonFilePath);
            FacilityInfo jsonData = JsonUtility.FromJson<FacilityInfo>(jsonString);
            Debug.Log(jsonData.readingRoom.totalSeats);
        }
        else {
            Debug.Log("aa");
        }*/
        //StartCoroutine(GetFacilityInfo(url));
        //GameObject placed1Object = Instantiate(objectToPlace, new Vector3(0.0f,0.0f,0.0f), Quaternion.identity);
        
    }
    
    private void Update()
    {
        //GetJsonData(0);
        float angle;
        if(iscompass && Input.compass.enabled) {
            StartCoroutine(StartLocationService());
            //Quaternion rotation = Quaternion.Euler(0, -Input.compass.trueHeading, 0);
            //transform.rotation = rotation;
            //debugText2.text = rotation.w.ToString() + rotation.y.ToString();
            iscompass = false;
        }
        if (!isSet) return;
        // 카메라의 위치와 방향을 받아옴
        Vector3 cameraPosition = Camera.main.transform.position;
        Vector3 cameraForward = Camera.main.transform.forward;
        
        debugText2.text = cameraForward.ToString();
        debugText2.text += Input.compass.trueHeading.ToString();
        // 카메라에서 타겟 오브젝트까지의 방향 벡터
        bool all_watch = true;
        for (int it = 0; it < 2; it++) {
            if (!placedObject[it]) continue;
            Vector3 directionToTarget = deltavector(placedObject[it].transform.position, cameraPosition);
            //placedObject[it].transform.forward = Quaternion.Euler(0,90,0) * directionToTarget.normalized;

            // 카메라의 방향 벡터와 타겟 방향 벡터의 각도 계산
            angle = Vector3.Angle(cameraForward, directionToTarget);
            //Debug.Log(objectToPlace.transform.position);
            //Debug.Log(cameraPosition);
            //Debug.Log(angle);
            //Debug.Log(Vector3.Dot(cameraForward, directionToTarget.normalized));
            // 시선 최대 각도 이내에 있으면서 타겟 오브젝트를 바라보고 있다면
            debugText2.text += directionToTarget.normalized.ToString();
            //debugText2.text += cameraPosition.ToString();
            if (angle <= 30.0f && Vector3.Dot(cameraForward, directionToTarget.normalized) > 0.5f)
            {
                currentWatchTime[it] += Time.deltaTime;
                all_watch = false;
                // 타겟 오브젝트를 바라보고 있는 상태
                if (maxWatchTime <= currentWatchTime[it] ) {
                    GetJsonData(it);
                    currentWatchTime[it] = 0.0f;
                }
            }
            else
            {
                currentWatchTime[it] = 0.0f;
                // 타겟 오브젝트를 바라보고 있지 않은 상태
                //Debug.Log("Camera is not looking at the target object.");
            }
        }
        if (all_watch) {
            uiToShow.SetActive(false);
        }
        //angle = (float)Math.Acos(Vector3.Dot(cameraForward,new Vector3(0.0f,0.0f,1.0f)));
        if (isNav) {
            //arrow.transform.position = cameraPosition + cameraForward * 10.0f;//new Vector3(10.0f * (float)Math.Sin(angle), 0.0f, 10.0f * (float)Math.Cos(angle));
            //debugText.text = KtoJRoute[cur_route_i].ToString();
            //curRotation();
            //debugText.text += _localOrigin.ToString();
        }
        //Debug.Log(angle);
    }
    private void GetJsonData(int it) {
        StartCoroutine(GetTexture(imageurl[it]));
        StartCoroutine(GetJson(httpsjson[it], (request) => 
        {
            if (request.result == UnityWebRequest.Result.Success && request.responseCode == 200)
            {
                string jsonString = request.downloadHandler.text;
                jsonData = JsonUtility.FromJson<FacilityInfo>(jsonString);
                uiText.text = "";
                foreach (string empty in jsonData.emptyClassrooms) {
                    uiText.text += empty + "\n";
                }
                foreach (string empty in jsonData.readingRoom) {
                    uiText.text += empty + "\n";
                }
                foreach (string empty in jsonData.printShop) {
                    uiText.text += empty + "\n";
                }
                uiToShow.SetActive(true);
            }
            else
            {
                Debug.Log("[Error]:" + request.responseCode + request.error);
            }
        }));
    }
    IEnumerator GetJson(string url, Action<UnityWebRequest> callback) {
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();
        callback(request);
    }
    private Vector3 deltavector(Vector3 a, Vector3 b) {
        float x,y,z;
        int t = 1000000;
        x = a.x * t - b.x * t;
        y = a.y * t - b.y * t;
        z = a.z * t - b.z * t;
        return new Vector3(x/t,y/t,z/t);
    }
    private IEnumerator StartLocationService()
    {
        // First, check if user has location service enabled
        

        
        if (!Input.location.isEnabledByUser)
        {
            Debug.Log("GPS not enabled");
            message = "GPS not enabled";
            yield break;
        }

        // Start service before querying location
        Input.location.Start();

        // Wait until service initializes
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        // Service didn't initialize in 20 seconds
        if (maxWait <= 0)
        {
            Debug.Log("Timed out");
            message = "Timed out";
            yield break;
        }

        // Connection has failed
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.Log("Unable to determine device location");
            message = "Unable to determine device location";
            yield break;
        }
        if (Input.compass.headingAccuracy < 0) {
            yield break;
        }
        // Set locational infomations
        debugText.text = "";
        for (int it=0;it<2;it++) {
            latitude = Input.location.lastData.latitude;
            longitude = Input.location.lastData.longitude;
            altitude = Input.location.lastData.altitude;
            Vector2 gps = new Vector2(latitude,longitude);
            _localOrigin = rhks[it];
            Vector3 ucspos = ConvertGPStoUCS(gps);
            Debug.Log(ucspos);
            debugText.text += ucspos.ToString() + " ";
            placedObject[it] = Instantiate(objectToPlace, ucspos, Quaternion.identity);
            //placedObject[it].transform.localScale = new Vector3(ucspos.magnitude,ucspos.magnitude,ucspos.magnitude);
        }
        //debugText.text += North.w.ToString() + "\n" + North.y.ToString() + "\n";
        //debugText.text += Input.compass.trueHeading.ToString();
        gps_count++;
        isSet = true;
        fromN = Input.compass.trueHeading;
        setNav(true);
    }

    private IEnumerator ObjectRotation()
    {
        Vector3 cameraPosition = Camera.main.transform.position;
        for (int it = 0; it < 2; it++) {
            if (!placedObject[it]) continue;
            Vector3 directionToTarget = deltavector(placedObject[it].transform.position, cameraPosition);
            placedObject[it].transform.eulerAngles = new Vector3(0.0f,(float)Math.Atan2(directionToTarget.z,directionToTarget.x),0.0f);
        }
        yield return new WaitForSeconds(5);
        StartCoroutine(ObjectRotation());
    }

    IEnumerator GetTexture(string url)
    {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
        yield return www.SendWebRequest();
        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
        }
        else
        {
            Texture myTexture = ((DownloadHandlerTexture)www.downloadHandler).texture;
            rawImage.texture = myTexture;
            AdjustImageSize(myTexture);
        }
    }
    
    void AdjustImageSize(Texture texture)
    {
        // 이미지 비율 고정
        float aspectRatio = (float)texture.width / texture.height;
        aspectRatioFitter.aspectRatio = aspectRatio;
        aspectRatioFitter1.aspectRatio = aspectRatio;
    }
    void setNav(bool k) {
        arrow.SetActive(true);
        isNav = true;
        if (k) cur_route_i = 0;
        else cur_route_i = 2;
        StartCoroutine(KtoJ(k));
    }
    IEnumerator KtoJ(bool k) {
        _localOrigin = new Vector2(Input.location.lastData.latitude,Input.location.lastData.longitude);
        while(ConvertGPS(curLocation).magnitude > 25.0f) {
            yield return new WaitForSeconds(0.1f);
            _localOrigin = new Vector2(Input.location.lastData.latitude,Input.location.lastData.longitude);
            Vector3 cameraPosition = Camera.main.transform.position;
            Vector3 cameraForward = Camera.main.transform.forward;
            arrow.transform.position = cameraPosition + cameraForward * 10.0f;
            //arrow.transform.forward = cameraForward;
            curRotation();
        }
        if (k) cur_route_i++;
        else cur_route_i--;
        if (cur_route_i < -1 || cur_route_i > 3) {
            isNav = false;
            arrow.SetActive(false);
            yield break;
        }
        curRotation();
        if(isNav) StartCoroutine(KtoJ(k));
    }

    void curRotation() {
        if (cur_route_i == -1) curLocation = rhks[0];
        else if (cur_route_i == 3) curLocation = rhks[1];
        else curLocation = KtoJRoute[cur_route_i];
        _localOrigin = new Vector2(Input.location.lastData.latitude,Input.location.lastData.longitude);
        Vector3 d = ConvertGPS(curLocation);
        Vector3 cf = Camera.main.transform.forward;
        cf = new Vector3(cf.x,0.0f,cf.z).normalized;
        double theta = -(Math.Atan2(d.z,d.x) - (Input.compass.trueHeading - 90.0) * 0.0174533);
        arrow.transform.forward = new Vector3(cf.x * (float)Math.Cos(theta) - cf.z * (float)Math.Sin(theta), 0.0f, 
        cf.x * (float)Math.Sin(theta) + cf.z * (float)Math.Cos(theta));
        debugText.text = KtoJRoute[cur_route_i].x + " " + KtoJRoute[cur_route_i].y + "\n";
        debugText.text += _localOrigin.x + " " + _localOrigin.y + '\n';
        //arrow.transform.eulerAngles = new Vector3(0.0f, Camera.main.transform.rotation.y,0.0f);
        //arrow.transform.eulerAngles = new Vector3(0.0f,(float)Math.Atan2(d.z,d.x) * 57.2958f- fromN + 90.0f,0.0f);
        //debugText.text = "\n";
        //debugText.text = (Math.Atan2(d.z,d.x) - Input.compass.trueHeading * 0.0174533).ToString() + '\n';
        //debugText.text += Input.compass.trueHeading.ToString() + "\n\n";
        //debugText.text += fromN.ToString() + "\n\n";
        //debugText.text += ((float)Math.Atan2(d.z,d.x) * 57.2958f).ToString() + '\n';
        //Debug.Log((float)Math.Atan2(d.z,d.x) * 57.2958f);
    }
    private Vector2 _localOrigin = Vector2.zero;
	private float _LatOrigin { get{ return _localOrigin.x; }}	
	private float _LonOrigin { get{ return _localOrigin.y; }}

	private float metersPerLat;
	private float metersPerLon;

    private void FindMetersPerLat(float lat) // Compute lengths of degrees
	{
	    float m1 = 111132.92f;    // latitude calculation term 1
	    float m2 = -559.82f;        // latitude calculation term 2
	    float m3 = 1.175f;      // latitude calculation term 3
	    float m4 = -0.0023f;        // latitude calculation term 4
	    float p1 = 111412.84f;    // longitude calculation term 1
	    float p2 = -93.5f;      // longitude calculation term 2
	    float p3 = 0.118f;      // longitude calculation term 3
	    
	    lat = lat * Mathf.Deg2Rad;
	
	    // Calculate the length of a degree of latitude and longitude in meters
	    metersPerLat = m1 + (m2 * Mathf.Cos(2 * (float)lat)) + (m3 * Mathf.Cos(4 * (float)lat)) + (m4 * Mathf.Cos(6 * (float)lat));
	    metersPerLon = (p1 * Mathf.Cos((float)lat)) + (p2 * Mathf.Cos(3 * (float)lat)) + (p3 * Mathf.Cos(5 * (float)lat));	   
	}

	private Vector3 ConvertGPStoUCS(Vector2 gps)  
	{
		FindMetersPerLat(_LatOrigin);
        int t = 1000000;
        float th = Input.compass.trueHeading * 0.0174533f;
		float zPosition  = metersPerLat * (gps.x * t - _LatOrigin * t) / t; //Calc current lat
		float xPosition  = metersPerLon * (gps.y * t - _LonOrigin * t) / t; //Calc current lat
        //return new Vector3(xPosition, 0.0f, zPosition);
		return new Vector3(
            (float)(-xPosition * Math.Cos(th) + zPosition * Math.Sin(th)),
            0.0f, 
            (float)(-xPosition * Math.Sin(th) - zPosition * Math.Cos(th)));
	}
	private Vector3 ConvertGPS(Vector2 gps)  
	{
		FindMetersPerLat(_LatOrigin);
        int t = 1000000;
		float zPosition  = metersPerLat * (gps.x * t - _LatOrigin * t) / t; //Calc current lat
		float xPosition  = metersPerLon * (gps.y * t - _LonOrigin * t) / t; //Calc current lat
        //return new Vector3(xPosition, 0.0f, zPosition);
        return new Vector3(xPosition,0.0f,zPosition);
	}
	private Vector2 ConvertUCStoGPS(Vector3 position)
	{
		FindMetersPerLat(_LatOrigin);
		Vector2 geoLocation = new Vector2(0,0);
		geoLocation.x = (_LatOrigin + (position.z)/metersPerLat); //Calc current lat
		geoLocation.y = (_LonOrigin + (position.x)/metersPerLon); //Calc current lon
		return geoLocation;
	}

}
