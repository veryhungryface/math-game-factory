// 한 끗 골프 — 화면 문구(중학교 지시형) 모음. Prewarm 에 그대로 넣는다.
namespace Mgf.HanKkeutGolf
{
    public static class UiStrings
    {
        public const string Title = "한 끗 골프";
        public const string Tagline = "레일 돌려 공을 굴려라";
        public const string TapBall = "공을 눌러 시작";
        public const string Badge = "중3 · 원과 직선";
        public const string GoalLine = "레일을 접선으로 돌리면 공이 통과한다";
        public const string PracticeSub = "손잡이를 끌어 유령 레일에 맞춘 뒤 손을 떼시오";
        public const string Busy = "결과를 정리하는 중입니다";
        public const string Moving = "다음 벙커로 가는 중입니다";
        public const string WaitBall = "공이 멈춘 뒤 돌리시오";
        public const string DragArc = "손잡이를 원호 방향으로 끌어 돌리시오";
        public const string GrabHandle = "레일 끝 고무 손잡이를 잡아 돌리시오";
        public const string PracticeStep1 = "① 빛나는 붉은 손잡이를 잡으시오";
        public const string PracticeStep2 = "② 흰 유령 레일 120°까지 끄시오";
        public const string PracticeStep3 = "③ 120°에서 손을 떼어 제출하시오";
        public const string RadiusFirst = "반지름 OA를 먼저 그으시오";
        public const string StartAtO = "중심 O를 눌러 A까지 끄시오";
        public const string EndAtA = "접점이 될 점 A에서 손을 떼시오";
        public const string EndAtO = "중심 O에서 손을 떼시오";
        public const string NotB = "B는 접점이 아니다 · 접점 A까지 그으시오";
        public const string DrawBridge = "반지름 OA를 직접 그으시오 · O에서 A까지 끄시오";
        public const string DrawPlain = "O에서 A까지 끌어 반지름 OA를 그으시오";
        public const string FixBridge = "보조선 OB는 접점 A를 지나지 않는다 · O에서 A까지 다시 그으시오";
        public const string FixPlain = "보조선 OB를 OA로 고치시오 · O에서 A까지 끄시오";
        public const string AimLine = "A에서 원 O에 접하도록 레일을 놓으시오";
        public const string Tolerance = "각도 오차 2° 이내 허용";
        public const string Repair = "수리: 같은 레일을 다시 돌려 접선을 만드시오";
        public const string Property = "접선은 접점을 지나는 반지름에 수직이다";
        public const string Paused = "시계 멈춤";
        public const string RadiusName = "반지름 OA";
        public const string Restart = "아무 곳이나 눌러 다시 치기";
        public const string ClearHead = "8코스 완주!";
        public const string OverHead = "라운드 종료";
        public const string WhyTime = "판단 시간 90초가 끝났다";
        public const string WhyBalls = "여분 공을 모두 썼다";
        public const string WhyMastery = "두 코스에서 첫 제출이 빗나가 통과 기준 7/8이 불가능하다";

        public const string All = Title + Tagline + TapBall + Badge + GoalLine + PracticeSub + Busy + Moving + WaitBall + DragArc + GrabHandle +
            PracticeStep1 + PracticeStep2 + PracticeStep3 + RadiusFirst + StartAtO + EndAtA + EndAtO + NotB + DrawBridge + DrawPlain + FixBridge + FixPlain + AimLine + Tolerance + Repair +
            Property + Paused + RadiusName + Restart + ClearHead + OverHead + WhyTime + WhyBalls + WhyMastery +
            "코스첫제출통과공남은초최고기록점수연속복습평행반대쪽같은각보조선먼저잇기방향으로이아니라수직기준0°90°180°⊥레일OB고침";
    }
}
