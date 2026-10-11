# 점프 수리 아트 기록

- `public/g/jeompeu-suri/assets/hero.png`, `title.png`: 최초 스프링 토끼 기획 참고. 런타임 텍스처로 사용하지 않음.
- 1차 수정에서 게시작 `sil-taneun-geomi`와 같은 파스텔 동물 디오라마 계열을 벗어나도록 런타임을 노출 강철 구조 시험장으로 재설계했다.
- 배경·하중 캡슐·직조 장력판·계측 UI는 `Scripts/JeompeuSuriView.cs`의 Unity 프리미티브와 절차 메시로 제작했다. 외부 생성 에셋은 추가하지 않았다.
- 팔레트: 구조 청회색 `#193849`, 심야 잉크 `#091923`, 안전 주황 `#F28A2E`, 계측 청록 `#58E0D1`, 파단 빨강 `#F04F4F`.
- 최종 `thumb.png`·`square.png`는 수정 WebGL 타이틀을 실GPU로 캡처한 뒤 규격에 맞춰 만든다.
