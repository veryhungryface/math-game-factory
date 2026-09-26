너는 이 저장소(/Users/sitpo/math-game-factory)의 Unity·Blender 아티스트 겸 빌더다. 게임 「협곡 사수」(`hyeopgok-sasu`)의 **비주얼·타격감 강화 라운드**를 한다.
첨부 2장이 사용자 레퍼런스(킹샷 광고)다. 사용자 지시: 「최대한 비슷하게, 타격감 있게」. 이름·캐릭터·에셋 복제는 금지(오리지널).

## 먼저 읽을 것
- `factory/unity-src/hyeopgok-sasu/ArtSource/DESIGN.md`(명세), `README.md`, `model-notes.md`, `combat-notes.md`, `blender/build_models.py`
- `factory/unity-src/hyeopgok-sasu/ArtSource/review-1.json`(1차 검수 78점 — 이미 커밋 f52e851 에서 고친 것과 남은 것을 구분해라. 고친 것을 되돌리지 마라: 시간 압박 완화, 타이틀 조작, 온보딩 포인팅, 승리 진도 HUD 등)
- `docs/unity-track.md`, `factory/prompts/30-build-unity.md`

## 현재 상태 vs 레퍼런스 (오케스트레이터 관찰)
구도(U자 협곡·관문 2곳·빨간 떼·파란 병영·석궁탑 2·점선 패드·왕)는 맞다. 부족한 것:
1. 지형이 **단색 플랫**이다. 레퍼런스는 절벽 단면이 층진 암석 면(어두운 남청 + 윗면 모서리 하이라이트), 흙길에 옅은 질감, 풀밭에 미세한 음영 변화가 있다.
2. 나무·바위가 적고 단순하다. 레퍼런스는 둥근 로우폴리 활엽수·침엽수가 고원 가장자리를 따라 무리 지어 있다.
3. 병사가 알갱이처럼 보인다. 레퍼런스 병사는 **투구·방패 실루엣**이 읽힌다(빨강=둥근 투구, 파랑=각진 투구+방패). 떼가 서로 겹쳐 **덩어리감**이 있다.
4. **충돌 지점 타격감**: 레퍼런스는 협곡 목에서 흰 섬광 별, 금빛 파편·코인, 흰 연기 뭉치가 끊임없이 터진다. 석궁 볼트가 **흰 궤적**을 남기며 날아가 맞은 자리에 섬광. 큰 처치 때 짧은 흔들림·히트스톱.
5. 조명이 평평하다 — 따뜻한 키 라이트 + 부드러운 그림자, 레퍼런스의 높은 채도.
6. 검수 low 지적: 표지(thumb/square)가 회화풍 생성 이미지라 실제 화면과 어긋난다 → **실제 게임 렌더 캡처**로 표지를 다시 만들어라(제목 오버레이는 게임 UI 톤).

## 도구
- **Blender MCP 가 연결돼 있다(사용자가 Blender 를 열어 둠, localhost:9876).** MCP 도구로 장면을 보고(get_scene_info, 뷰포트 스크린샷) 반복해 다듬어라. 최종 모델링 코드는 반드시 `ArtSource/blender/build_models.py`(또는 추가 스크립트)에 반영해 `Blender -b` 로 재현 가능하게 하라. FBX 는 `Resources/HyeopgokSasu/`.
- Unity: `bash factory/unity/build.sh hyeopgok-sasu`. 에디터 GUI 금지.
- 캡처: `node factory/lib/qa.mjs hyeopgok-sasu` 가 `factory/work/qa/hyeopgok-sasu/*.png` 를 만든다. 전투 한가운데 장면은 puppeteer 로 `__GAME_TEST__.start()` 후 몇 초 뒤 캡처(Chrome 경로: `$HOME/.cache/puppeteer/chrome/mac_arm-151.0.7922.47/chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing`). **레퍼런스와 나란히 놓고 직접 비교**하며 반복하라.

## 지켜야 할 것
- QA 치명 0, 모바일·1280 fps 게이트와 15초 저하 게이트 통과(인스턴싱 유지, 파티클 풀링, 매 프레임 할당 금지), 게임 폴더 gzip 12MB 미만.
- 게임 규칙·팩·브리지·봇 결과를 바꾸지 마라(비주얼·연출만). 바꿨다면 봇 테스트를 다시 돌려 `ArtSource/bot-results.md` 갱신.
- `factory/work/` 는 건드리지 마라. run.sh·config·prompts·lib·킷 수정 금지. git 커밋은 해도 되지만 push·배포 금지.

## 최종 응답 (한국어, 8줄 이내)
바뀐 것(항목별) / 레퍼런스 대비 남은 차이 / QA total·fatal·fps / 산출 크기 / 비교 캡처 경로.
