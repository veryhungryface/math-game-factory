너는 이 저장소(/Users/sitpo/math-game-factory)의 Unity·Blender 아티스트 겸 빌더다. 게시작 「협곡 사수」(`hyeopgok-sasu`, 85점)를 **킹샷(Kingshot) 수준의 요소·그래픽 디테일로 끌어올리는 1단계 라운드**다.
사용자: 「킹샷은 훨씬 다양한 요소가 있고 그래픽이 더 디테일해」 → 조사 결과를 보고 「1번(비주얼·요소 업그레이드) → 2번(코인 경제 수학 루프)」 순서로 지시. **이번은 1번만.** 수학 입력 방식(왕이 보기 패드로 이동)과 문제 팩 구조는 유지한다.

## 먼저 읽을 것
- **`references/case-studies/kingshot-analysis.md` 전체** — 특히 §2 그래픽 해부(팔레트 hex·카메라·지면·VFX)와 §3 격차표 14항목.
- 첨부 이미지 6장(킹샷 스토어·플레이어블 광고 프레임). 더 필요하면 `scratchpad/kingshot-ref/img/`, `frames/*/_sheet.jpg` 를 직접 열어 봐라(저작물 — 참고만, 복제 금지).
- `factory/unity-src/hyeopgok-sasu/` 의 README·DESIGN·model-notes·combat-notes·blender/build_models.py, `docs/unity-track.md`, `factory/prompts/30-build-unity.md`.

## 할 일 — 격차표 1~14 를 가능한 한 전부(우선순위 순)
1 팔레트 보정 / 2 카메라(FOV 30~35°·거리+25%·맵 경계 숨김 링) / 3 코인 경제 **가시화**(처치 시 땅에 코인 → 왕이 흡수 → 등에 코인 탑 → 정답 패드에 부어지며 숫자 카운트다운 연출. 수학 판정은 지금처럼 패드 선택 — 코인은 연출·재화) / 4 건물 3단계 외형 + 흰 청사진 건설 연출(석궁탑·병영·성문) / 5 유닛 다양화(적 거인 HP바·빛나는 칼, 적 궁수, 탑 위 아군 궁수, 마지막 웨이브 보스) / 6 지면 디테일(풀 얼룩·풀 다발·꽃·자갈·흐린 길 가장자리·접지 그림자) / 7 적 관문 2채 + 분홍 적 영역 + 망루 / 8 목책·가시 말뚝 HP·손상 단계·화재 / 9 VFX(사망 흰 플래시·대포 폭발·숫자 팝업·건물 화재) / 10 소품 밀도 / 11 물·다리 병목(가능하면) / 12 웨이브 사이 짧은 밤 전환 / 14 공격 기울기·흰 스폰 애니메이션. (13 카드 UI 는 하지 마라 — 하단 선택지 금지 규칙)
- 모델은 **Blender MCP(사용자가 Blender 를 열어 둠, localhost:9876)** 로 보며 다듬고, 최종 코드는 `ArtSource/blender/*.py` 에 반영해 `Blender -b` 로 재현 가능하게. FBX → `Resources/HyeopgokSasu/`.
- 1차 검수 잔여 medium 도 같이: 「24초가 지나면 오답」 안내 문구를 화면에 명시, 두 주사위 문항 제한 시간 재검토(팩 difficulty 로 가변 제한 가능).

## 지켜야 할 것
- QA 치명 0, 모바일·1280 fps 게이트·15초 저하 게이트 통과. 드로콜 150 이하, 인스턴싱·고정 풀 유지, 매 프레임 할당 금지. 게임 폴더 gzip **12MB 미만**(지금 9.07MB — 텍스처 ≤400KB, 메시 ≤1MB 예산).
- 게임 규칙(문항 판정·승패 조건·팩 로딩)·`__GAME_TEST__` 계약 유지. 규칙이 바뀌면 봇 테스트 재실행 후 `ArtSource/bot-results.md` 갱신.
- `public/g/hyeopgok-sasu/meta.json`: `version: 2`, `qa` 를 `{"score":0,"gate":80,"passed":false,"reviewed_at":"","notes":[]}` 로 리셋, description·art_direction 갱신. 표지(thumb/square)는 새 실제 렌더로.
- `factory/work/` 는 건드리지 마라. run.sh·config·prompts·lib·킷 수정 금지. 로컬 git 커밋은 해도 되지만 push·배포 금지.
- **레퍼런스와 나란히 비교 캡처**(`ArtSource/validation/phase1/compare.png`, 킹샷 프레임 2장 + 우리 게임 전투 장면 2장)를 만들어 직접 보고 반복하라.

## 최종 응답 (한국어, 10줄 이내)
반영한 격차 항목 번호 / 남은 차이 / QA total·fatal·fps·15초 유지율 / 드로콜·산출 크기 / 비교 캡처 경로.
