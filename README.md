# ProjectAR
### 학교에 대한 정보를 AR로 확인하기
- 개발 규모 : 4인
- 사용 엔진 : Unity 2021.3.6f1
- 개발 도구 : C# (Visual Studio Code)
- 사용 라이브러리 : UnityGPSConverter

# 시연 영상
## 3D MAP
![alt text](Image/1.gif)
## 3D 네비게이션
![alt text](Image/2.gif)

# 개발범위
## CameraFar
AR 카메라가 1000.f 거리 이내에 오브젝트만 표시하게 하고 나침반 기능을 활성화 시킴
## GetLocation
### 주요 변수
- objectToPlace : Anchor의 에셋, 3D 지도에 표시하는데 사용
- arrow : 화살표의 에셋, 네비게이션에서 사용
- placedObject : objectToPlace를 통해 설치한 오브젝트
- rhks : 학교 건물의 위도 경도 정보가 들어있는 Vector2 배열
- httpsjson : 인덱스에 해당하는 건물의 정보가 json형식으로 담겨있는 url, 본 Sogang-GPT 앱에서 해당 url을 통해 정보를 전달 받음
- imageurl : 인덱스에 해당하는 건물의 이미지가 담겨있는 url
- KtoJRoute : K관에서 J관으로의 루트가 위도 경도로 들어있는 Vector2 배열
### 주요 함수
#### Start
rhks과 KtoJRoute에 해당하는 위도 경도 값을 넣어주고 ObjectRotation()과 StartLocationService()를 Coroutine으로 실행시켜줌
#### Update
카메라가 placedObject를 30도 각도 이내에서 1초 이상 바라보면 GetJsonData()을 통해 해당 건물에 데이터를 불러와 UI에 띄워 줌
#### GetJsonData
인자로 들어온 인덱스를 가지는 건물의 정보를 사이트를 통해 가져옴
#### StartLocationService
나침반 기능을 통해 현재 자신의 좌표와 건물의 좌표의 차이를 계산해서 placedObject를 가상 세상에 배치함
#### ObjectRotation
Anchor가 언제나 유저를 바라볼 수 있도록 5초에 한번씩 회전함
#### setNav
인자로 들어온 bool값에 따라 KtoJ를 실행시켜 K <-> J 루트를 네비게이션 해줌
#### KtoJ
현재 위치하는 KtoJRoute의 인덱스보다 한칸 앞을 arrow가 바라보도록 하고 현재 인덱스랑 25.f 보다 가까워지면 인덱스를 증가시켜서 arrow가 자연스럽게 루트를 표시할 수 있도록 함
