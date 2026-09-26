너는 이 저장소(/Users/sitpo/math-game-factory)의 Unity 게임 빌더다. 사용자가 직접 지시한 새 중학교 게임 「협곡 사수」(slug `hyeopgok-sasu`)를 처음부터 끝까지 만든다.

첨부 이미지 2장이 사용자가 보낸 레퍼런스(킹샷 광고)다. **최대한 비슷한 화면·손맛, 타격감 있게** — 단 이름·캐릭터·에셋은 오리지널.

## 반드시 먼저 끝까지 읽어라
1. `factory/unity-src/hyeopgok-sasu/ArtSource/DESIGN.md` — 이번 작업의 정본 명세(사용자 원문 요지 포함).
2. `CLAUDE.md`, `factory/prompts/_school-middle.md`, `factory/prompts/30-build.md`, `factory/prompts/30-build-unity.md`, `docs/unity-track.md`(킷·빌드·함정 정본).
3. `curriculum/2022-middle-math.json` 의 `m2s2-u6`·`m2s2-u7` 단원, 최상단 `style_guide`·`expression_traps`, `generation_constraints`.
4. 참고 구현: `factory/unity-src/won-jjigeo/`(같은 킷을 쓰는 게시작 — 구조·브리지·봇 테스트 방식).

## 도구
- Unity: `source ~/.unity/env`, 빌드는 `bash factory/unity/build.sh hyeopgok-sasu`(컴파일 에러는 `error CS…`). 에디터 GUI 금지.
- Blender: `/Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup --python <script.py>` 로 모델링 → FBX. 스크립트는 `ArtSource/blender/` 에 보관(재현 가능). Blender MCP 도구가 보이면 보조로 써도 된다.
- 이미지 생성(너의 내장 이미지 도구): 타이틀·표지용만.
- 로컬 QA: `node factory/lib/qa.mjs hyeopgok-sasu` (치명 0 필수). 스크린샷 `factory/work/qa/hyeopgok-sasu/*.png` 를 직접 열어 레퍼런스와 나란히 비교하고, 부족하면 고쳐라. 헤드리스 캡처 스크립트가 필요하면 `PUPPETEER_EXECUTABLE_PATH="$HOME/.cache/puppeteer/chrome/mac_arm-151.0.7922.47/chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing"`.

## 완료 조건
- 명세의 전투(수백 명 떼·충돌·석궁·파티클·히트스톱·화면 흔들림), 왕 이동과 답 패드, 팩 로딩(`packs/index.json`, `?pack=`), 팩 2종(확률 기본·경우의 수) 생성기+전수 검증, 첫 10초 훅, 승패.
- `node factory/lib/qa.mjs hyeopgok-sasu` 치명 0, 모바일·1280 fps 게이트 통과, 게임 폴더 12MB 미만.
- 무뇌 봇(30-build.md B3 4종 + 「가장 가까운 패드로 가기」 봇) 첫 시도 정답률이 우연 수준(보기 수 기준) 이하 — 결과를 `ArtSource/bot-results.md`.
- 팩을 하나 더 복사해 이름만 바꾼 임시 팩으로 **재빌드 없이** 교체되는지 실제로 확인(확인 후 임시 팩 삭제).
- `public/g/hyeopgok-sasu/meta.json`(명세의 값, qa 는 `{"score":0,"gate":80,"passed":false,"reviewed_at":"","notes":[]}`), `thumb.png` 1200×630, `square.png` 1080×1080.
- `factory/work/` 는 건드리지 마라(오케스트레이터가 채운다). git 커밋·배포 금지. run.sh·config·prompts·lib·킷은 수정 금지(킷 버그면 게임 쪽에서 우회하고 보고).

## 최종 응답 (한국어, 10줄 이내)
만든 것 / 화면 설명(레퍼런스와 닮은 점·다른 점) / QA total·fatal·fps / 봇 결과 / 팩 교체 검증 / 산출 크기 / 남은 위험.
