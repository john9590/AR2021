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
    private string[] httpsjson = { "https://mocki.io/v1/2a3dc021-69b6-4802-8b3b-a76f340fe71f",
                                    "https://mocki.io/v1/2a3dc021-69b6-4802-8b3b-a76f340fe71f" };
    private void Start()
    {
        Instance = this;
        rhks[0] = new Vector2(37.54969f, 126.9411f);
        rhks[1] = new Vector2(37.6228f, 127.1495f);
        DontDestroyOnLoad(gameObject);
        Input.compass.enabled = true;
        //StartCoroutine(StartLocationService());
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
        debugText2.text = "";
        GetJsonData(0);
        if(Input.compass.enabled) {
            StartCoroutine(StartLocationService());
            //Quaternion rotation = Quaternion.Euler(0, -Input.compass.trueHeading, 0);
            //transform.rotation = rotation;
            //debugText2.text = rotation.w.ToString() + rotation.y.ToString();
            Input.compass.enabled = false;
        }
        if (!isSet) return;
        // 카메라의 위치와 방향을 받아옴
        Vector3 cameraPosition = Camera.main.transform.position;
        Vector3 cameraForward = Camera.main.transform.forward;

        // 카메라에서 타겟 오브젝트까지의 방향 벡터
        bool all_watch = true;
        for (int it = 0; it < 2; it++) {
            if (!placedObject[it]) continue;
            Vector3 directionToTarget = deltavector(placedObject[it].transform.position, cameraPosition);

            // 카메라의 방향 벡터와 타겟 방향 벡터의 각도 계산
            float angle = Vector3.Angle(cameraForward, directionToTarget);
            //Debug.Log(objectToPlace.transform.position);
            //Debug.Log(cameraPosition);
            //Debug.Log(angle);
            //Debug.Log(Vector3.Dot(cameraForward, directionToTarget.normalized));
            // 시선 최대 각도 이내에 있으면서 타겟 오브젝트를 바라보고 있다면
            debugText2.text += cameraForward.ToString();
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
            //uiToShow.SetActive(false);
        }
    }
    private void GetJsonData(int it) {
        StartCoroutine(GetJson(httpsjson[it], (request) => 
        {
            if (request.result == UnityWebRequest.Result.Success && request.responseCode == 200)
            {
                string jsonString = request.downloadHandler.text;
                jsonData = JsonUtility.FromJson<FacilityInfo>(jsonString);
                uiText.text = "";
                for (int i=0;i<3;i++) {
                    uiText.text += jsonData.emptyClassrooms[i];
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
        
        Debug.Log("GPS why not ");

        
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
        // Set locational infomations
        debugText.text = "";
        for (int it=0;it<2;it++) {
            latitude = Input.location.lastData.latitude;
            longitude = Input.location.lastData.longitude;
            altitude = Input.location.lastData.altitude;
            Vector2 gps = new Vector2(latitude,longitude);
            Vector3 ucspos = ConvertGPStoUCS(deltavector(gps,rhks[it]));
            Debug.Log(ucspos);
            debugText.text += ucspos.ToString() + " ";
            placedObject[it] = Instantiate(objectToPlace, ucspos, Quaternion.identity);
            //placedObject[it].transform.localScale = new Vector3(ucspos.magnitude,ucspos.magnitude,ucspos.magnitude);
        }
        //debugText.text += North.w.ToString() + "\n" + North.y.ToString() + "\n";
        debugText.text += Input.compass.trueHeading;
        gps_count++;
        isSet = true;
    }

    private IEnumerator GetFacilityInfo(string url)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            // 요청을 보냅니다.
            yield return webRequest.SendWebRequest();

            // 에러가 있는지 확인합니다.
            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError(webRequest.error);
            }
            else
            {
                // 받아온 JSON 데이터를 FacilityInfo 오브젝트로 변환합니다.
                FacilityInfo facilityInfo = JsonUtility.FromJson<FacilityInfo>(webRequest.downloadHandler.text);

                // 데이터 사용 예시
                Debug.Log("Total Seats: " + facilityInfo.readingRoom);
                // 추가적인 데이터 처리...
            }
        }
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
        float th = Input.compass.trueHeading;
		float zPosition  = metersPerLat * (gps.x * t - _LatOrigin * t) / t; //Calc current lat
		float xPosition  = metersPerLon * (gps.y * t - _LonOrigin * t) / t; //Calc current lat
		return new Vector3(
            (float)(xPosition * Math.Cos(th) + zPosition * Math.Sin(th)),
            0.0f, 
            (float)(xPosition * Math.Sin(th) + zPosition * Math.Cos(th)));
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
