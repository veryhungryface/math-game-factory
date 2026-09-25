// MGF Unity kit — 마우스·터치 공용 포인터. (구 Input Manager — 워크스페이스는 Input System 패키지를 쓰지 않는다)
// WebGL 에서 터치는 마우스 0번 버튼으로도 시뮬레이션된다(Input.simulateMouseWithTouches 기본 true).
// QA input.real 은 **브라우저가 만든 진짜 pointer 이벤트**로 캔버스를 누른다 → 여기로 들어온다.
using UnityEngine;

namespace Mgf
{
    public static class MgfPointer
    {
        public static bool Down => Input.GetMouseButtonDown(0);
        public static bool Held => Input.GetMouseButton(0);
        public static bool Up => Input.GetMouseButtonUp(0);
        /// <summary>화면 픽셀 좌표(왼쪽 아래 원점).</summary>
        public static Vector2 Position => Input.mousePosition;

        /// <summary>이번 프레임에 눌린 지점에서 레이캐스트. 콜라이더가 있어야 맞는다.</summary>
        public static bool DownHit(Camera cam, out RaycastHit hit, float maxDistance = 500f, int layerMask = ~0)
        {
            hit = default;
            if (!Down || !cam) return false;
            return Physics.Raycast(cam.ScreenPointToRay(Position), out hit, maxDistance, layerMask);
        }

        /// <summary>현재 포인터 위치의 레이(드래그 중 바닥 평면 교차 등에 쓴다).</summary>
        public static Ray Ray(Camera cam) => cam.ScreenPointToRay(Position);

        /// <summary>포인터 레이가 수평면 y=height 와 만나는 점.</summary>
        public static bool OnPlane(Camera cam, float height, out Vector3 point)
        {
            var r = Ray(cam);
            var plane = new Plane(Vector3.up, new Vector3(0, height, 0));
            if (plane.Raycast(r, out float d)) { point = r.GetPoint(d); return true; }
            point = default;
            return false;
        }
    }
}
