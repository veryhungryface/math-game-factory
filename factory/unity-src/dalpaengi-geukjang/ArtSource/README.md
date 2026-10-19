# 달팽이 극장 — 아트 출처와 런타임 구현

- 아트 단계 기록: `factory/work/art.json`, `art-hero.json`, `art-title.json`, `art-thumb.json`, `art-square.json`.
- 생성 원본: `public/g/dalpaengi-geukjang/assets/hero.png`, `assets/title.png`, `thumb.png`, `square.png`.
- 런타임은 생성 이미지를 배경 빌보드로 쓰지 않는다. `Scripts/DalpaengiView.cs`가 분홍 나선 껍데기 벽, 꽃잎 지붕, 잎 객석, 입장종, 달팽이 관객을 Unity 프리미티브와 절차적 메시로 만든다. 따라서 생성 이미지 로드 실패나 외부 네트워크 요청이 없다.
- `hero.png`는 달팽이 비율·재질 참고, `title.png`는 세로 구도 참고다. 실제 캐릭터는 통통한 몸통·납작한 껍데기·더듬이로 재구성했고 특정 상표나 기존 캐릭터를 복제하지 않았다.
- 효과음은 첫 사용자 입력 뒤 `MgfSfx`가 만드는 코드 합성음만 사용한다. 정답은 입장 이동·잎 의자 오버슈트·껍데기 파동·따뜻한 빛으로, 오답은 객석 유령 표·입장종 금·오답 전용 카메라 흔들림으로 구분한다.
- 팔레트: shell `#DDA6BA`, leaf `#71934D`, ivory `#F8F0DA`, ink `#4D3445`, amber `#DC9851`.
