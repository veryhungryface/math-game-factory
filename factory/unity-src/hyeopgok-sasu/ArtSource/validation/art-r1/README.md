# 협곡 사수 아트 라운드 1 — 로컬 검증 인계

이 문서는 **5차 시각 수정의 검증 스냅샷**이다. 내부 시각 평균 **8.075/10 (표시 8.08)으로 사용자 목표 8.5에 미달**한다. 최저 축은 8.0로 하한 7은 넘지만 상용 참고와 같은 품질로 판정하지 않았다. 점수·반복 수정·축별 남은 격차는 [self-score.json](self-score.json), 수치와 파일 SHA는 [summary.json](summary.json)이 정본이다. **push·게시·배포하지 않았다.**

**최종 빌드:** 5차는 왕/거인의 둥근 관절, 연속 정점 지면색, 아군 탑 접지, 피해 숫자 중복 억제, 44px 탭과 단원 카드 문자열 캐시를 추가했다. 캡처 Build/팩 SHA는 `round5/report.json`과 `round5-start-hashes.json`에 고정했다. 외부 팩 수정 뒤 봇과 실제 입력 133검사를 재통과했으며, 최종 단독 입력 회귀 161검사와 60팩 선택 213검사는 통과했다. 공식 QA는 치명0으로 통과했다. 아래는 최종 빌드와 현재 팩 스냅샷의 수치다.

## A–F 5차 반영과 남은 한계

| 바이블 | 5차 반영 |
|---|---|
| A · 카메라 | 원근 FOV 32°·피치 55°·방위 −8°, 왕 데드존 추적, 전환/건설 0.6초 줌아웃. 외곽 암석·숲·청록 안개와 확장 바닥으로 경계를 가림. |
| B · 음영 | Blender 44종 정점 AO(4,164,480 rays), 정점색 × AO 셰이더, 따뜻한 태양·청록 간접광·소프트 섀도. 건물/나무/왕/거인 접지 블롭, 정적 블롭 중심 α0.35와 부드러운 가장자리. |
| C · 팔레트 | 청록 잔디·회갈 길·따뜻한 사암 윗면/차가운 옆면·진영 빨강/파랑·금화. 알파 패드와 블롭의 sRGB/linear 차이도 보정. |
| D · 지형/소품 | 원통 띠를 비대칭 암석 면으로 교체, 절벽 받침·숲 군집·풀/꽃/자갈, 길의 흐린 가장자리/바퀴 결, 연속 곡선 강. 기와/널판·벽돌·휘장, 우물/자루/통/상자/통나무 작업장과 흙마당. 정적 메시 병합. |
| E · 캐릭터/VFX | 왕/병사 비율·진영 실루엣, 행군/공격·흰 사망 플래시, 4갈래 타격/볼트/짧은 연기/피해 숫자, 바닥 코인과 투자 연출. 둥근 손·팔·장화·어깨로 보완했으나 캐릭터 개성과 전투 효과 반복은 남음. |
| F · UI/팩 선택 | 둥근 굵은 서체·외곽선, 알약 HUD, 청색/금색 문제 현판 하나, 월드 라벨/화살표/유령 손/붓기 말풍선. 학교급·학년 탭과 단원 카드 스크롤, 학기→단원순 정렬, 누락 메타는 팩 JSON에서 읽음. |

긴 원문은 390px에서 본문 17.5px를 지키며 기본 3줄과 명시적 말줄임으로 표시하고, 같은 배너를 탭하면 전문을 펼친다. **모든 원문을 항상 3줄에 표시한 것은 아니다.** 팩 문장·수학 판정·이동/붓기 규칙은 아트 작업에서 변경하지 않았다.

8축 점수 순서는 카메라 **8.0** / 조명·AO **8.0** / 팔레트 **8.3** / 지형·소품 **8.1** / 캐릭터·떼 **8.0** / VFX **8.2** / UI **8.0** / 종합 **8.0**다. 다음 수정의 중심은 지면·건축·캐릭터의 일관된 재질감, 중앙 패드와 하단 교전의 시선 분산, 캐릭터 관절 조형이다.

## 기술 검증 범위

- 최종 공식 QA **44/45, fatal 0**. Apple M4/ANGLE Metal에서 390 **82fps**, 1280 **81fps**, 15초 유지 **85→82fps, 0.96**. 다른 브라우저 작업을 종료한 뒤 측정했다. 실제 iPhone의 발열·메모리·성능 검증은 아니다.
- WebGL의 모든 draw 호출을 Unity `preMainLoop/postMainLoop` 사이에서 집계했다. 그림자/UI 패스를 포함해 최대 **125회 ≤150**. 그림자 포함 삼각형은 약 45만으로, 단일 가시 메시의 삼각형 수와 같은 지표가 아니다.
- 게임 폴더 전송량 **10,620,818 bytes <12,000,000**. 이미 압축된 Unity/PNG는 그대로, 나머지 텍스트는 gzip level 9로 합산했다. 원본 합계 **13,384,684 bytes**라 QA의 원본 파일 합산 `static.assets` 1건은 비치명 실패로 남겼다. 외부 팩 추가에 따라 크기는 변할 수 있다.
- `?artprobe=1`은 WebGL Boehm `GC_get_total_bytes`로 `Game.Update`, `Battle.Update`, `Environment.LateUpdate`를 각각 측정한다. 257-byte 배열 할당 교정에서 **304 bytes**가 검출됐고, 워밍업 뒤 15초 표본 **390은 1236프레임, 1280은 1252프레임**의 세 범위는 각각 0 bytes였다. 로딩·입력/문제/메뉴 전환과 범위 밖 공용 브리지/엔진 콜백은 제외한다. 이벤트 할당은 [round5/report.json](round5/report.json)에 보존했다. **애플리케이션 전체의 모든 프레임이 무할당이라는 증거는 아니다.** 지원되지 않는 관리 API를 사용했던 round1의 0-byte 결과는 무효이며 이력만 보존했다.

## 캡처와 입력 증거

[compare.png](compare.png)는 참고 3장과 실제 WebGL 전투 3장의 비율 유지 리사이즈/레터박스 비교다. 게임 픽셀을 다시 그리지 않았으며 원본 SHA는 [compare-sources.json](compare-sources.json)에 있다. 타이틀·팩 선택·붓기·분수 조립·성장·승리는 [round5/](round5/)에 보존했다.

`capture.mjs`는 첫 붓기에 실제 CDP 터치를 쓰지만, 이후 장면 이동에는 기존 `start/answerCorrect` 테스트 훅을 쓴다. 따라서 그 승리 캡처만으로 실제 입력 완주를 주장하지 않는다. 별도 `regression.mjs`는 정답 훅·코인 주입 없이 trusted 터치로 이동/붓기/이탈을 재현했다. round3은 **714검사·실제 10문항 2회 완주·fixture 10종**, round4는 **157검사·입력 경계·긴 문항·표지**, 최종 round5 단독 실행은 **161검사·fixture10종·실제 표지2장**을 재검증했다. 각 실행의 Build/팩 SHA와 상태는 해당 `regression-*.json`을 따른다. 최종 봇은 `bots-final/source-before.json`과 `source-after.json`에서 현재 u6/u7 및 Rules 해시가 실행 전후 동일함을 확인했다. 1,540,192 입력 영역과 61+3,721 실제 균등 동선 오류0, 대조군/우연 기준/팩 계약 검사를 통과했다.

팩 선택은 메모리 HTTP 응답으로만 만든 60개 팩을 390/1280에서 검사했다. 5차 최종은 **213검사 전체 PASS·선택/은행 갱신 18회·trusted 입력 424회**, 콘솔/요청 실패 0이다. 최종 `selector-round5/report.json`은 전체 폴더 SHA 가드도 통과했다. 이전 4차 실행의 가드는 외부 작업의 미사용 팩 `m2s1-u6.json`, `m2s2-u5.json` 추가로 실패했다. [selector/report.json](selector/report.json)의 실패를 유지하고 [concurrency-note.json](selector/concurrency-note.json)에 원인을 분리했다. 초기 213 PASS는 `selector-round1/`, 중간 동시 수정 실패는 `selector-round3/`에 남아 있다. 실제 팩 파일을 테스트용 항목으로 교체하지 않았다.

## 재현

저장소 루트에서 실행한다. 성능 QA는 다른 검증 브라우저를 닫고 단독 실행한다. 기존 증거를 보존하려면 아래처럼 새 태그/출력 경로를 쓴다. 브라우저 실행 파일이 다르면 `PUPPETEER_EXECUTABLE_PATH`를 지정한다.

```bash
/Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup --python factory/unity-src/hyeopgok-sasu/ArtSource/blender/build_models.py
source ~/.unity/env
bash factory/unity/build.sh hyeopgok-sasu
node factory/unity-src/hyeopgok-sasu/ArtSource/validation/art-r1/capture.mjs verification
node factory/unity-src/hyeopgok-sasu/ArtSource/validation/art-r1/regression.mjs hook,layouts,play,edges regression-verification
node factory/unity-src/hyeopgok-sasu/ArtSource/validation/art-r1/selector-stress.mjs selector-verification
node factory/lib/qa.mjs hyeopgok-sasu --out factory/unity-src/hyeopgok-sasu/ArtSource/validation/art-r1/qa-verification
```

`selector-stress.mjs`는 선택한 출력 태그를 쓰며, 인자를 생략하면 `selector/`다. `make-compare.py`는 기본 `round5/` 캡처와 명시된 참고 원본으로 비교표를 재생성한다. 팩 생성기·팩 JSON·공용 킷·공장 자동화는 이 아트 재현 과정에서 수정하지 않는다.

5차 입력 회귀의 첫 실행은 60팩 스트레스 브라우저와 병행하는 동안 Unity 로딩 60%에서 navigation 60초 제한에 걸렸다. 콘솔/요청 오류는 없었지만 통과로 계산하지 않았다. `regression-round5.json`과 실패 PNG를 보존했고 같은 소스로 `regression-round5-isolated`를 단독 재실행하여 161검사 PASS, 브라우저/요청 오류0을 확인했다. 이는 재현 조건 기록이며 원인이 확정됐다는 뜻은 아니다.

현재 index는 외부 작업에서 실제13팩으로 확장됐다. 아트 작업은 해당 파일과 생성기를 수정하거나 커밋에 포함하지 않았다. Rules와 HyeopgokBank.jslib는 최초 기준 SHA 그대로이며, Build와63개 런타임 소스/모델/셰이더의 동기화는 `compiled-source-check.json`에 기록했다. `qa-round5/report.json`이 최종 공식 QA 정본이다.
