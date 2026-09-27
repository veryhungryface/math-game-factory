# 협곡 사수

사용자 직접 지시로 만든 중2 수학 협곡 디펜스. 2단계 정본 명세는 `ArtSource/DESIGN-PHASE2.md`이며, 현재는 **v2 코인 경제 = 수학 입력 로컬 수정본**이다. 1단계 이력은 `ArtSource/phase1-notes.md`, 이번 범위·검증 상태는 `ArtSource/phase2-notes.md`에 있다. `meta.qa`는 요청대로 미심사 상태를 유지한다. push·배포하지 않았다.

## 플레이

타이틀 뒤에서 전투가 시작된다. `출격` 후 왕을 끌어 이동하고, 처치 코인을 가까이에서 모아 등에 쌓는다. `amount` 문항은 패드에 부은 코인 수가 답이다. 서 있으면 한 닢씩 붓고, 오래 서면 가속하며, 짧게 탭하면 한 닢만 붓는다. 패드에서 걸어 나온 뒤 0.6초 확인 링이 차면 확정된다. 링이 끝나기 전에 패드로 돌아오면 더 부을 수 있으나 덜어 낼 수는 없다. 0은 패드에서 0.35초 멈춰 선택한 뒤 첫 자동 붓기(0.75초) 전에 나오며, 스쳐 지나간 입력은 방문이나 제출로 세지 않는다.

`fraction_parts`는 팩 라벨에 맞춰 두 패드에 각각 양을 붓는다. 확률 팩은 큰 「전체」「사건」 라벨에 실제 경우의 수를 붓고, 조립한 확률은 고정 HUD에서 세로 분수로 확인한다. 공개 확률 문항은 모두 `equivalent`로 정수 교차곱 판정하며 원시 경우의 수 쌍과 기약쌍을 모두 받는다. 확정 피드백은 원시 분수를 기약분수로 바꾸어 보여 준다. 런타임은 회귀 fixture를 위해 `exact_parts`·`reduced`도 정수 연산으로 지원한다. `choice` 문항과 `answer_mode`가 없는 v1 팩만 기존 지상 보기 패드의 0.8초 정지를 사용한다.

정답이면 부은 코인이 건물 투자·출격으로 이어지고, 오답이면 부은 코인이 사라지며 정답과 풀이를 보여 준다. 코인이 부족하면 전투에서 더 모은다. 성문 HP 0이면 종료하며, 10문항 중 첫 시도 정답 7개 이상이어야 승리한다. 첫 문항은 작은 `amount`다. 제한 시간은 문항의 모드와 난도에 따라 달라지며 화면에 남은 시간이 표시된다.

타이틀 팩 선택 또는 `?pack=m2s2-u6`으로 팩을 바꾼다. 팩은 Unity 빌드에 포함되지 않으며 부팅 시 상대경로로 읽는다. 세 가지 답 모드와 다른 학년·단원의 작성 예시는 `public/g/hyeopgok-sasu/packs/README.md`, 생성기·전수 검증기는 `ArtSource/packs/`에 있다. 현재 844문항으로, 경우의 수는 `amount` 400문항, 확률은 `amount` 21 / `fraction_parts` 407 / `choice` 16문항이다. 전수 검증과 입력 봇 결과는 `ArtSource/phase2-notes.md`와 해당 원시 JSON에서 확인한다.

## 재현

저장소 루트에서 실행한다.

```bash
/Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup --python factory/unity-src/hyeopgok-sasu/ArtSource/blender/build_models.py
node factory/unity-src/hyeopgok-sasu/ArtSource/packs/gen-m2s2-u7.mjs
node factory/unity-src/hyeopgok-sasu/ArtSource/packs/gen-m2s2-u6.mjs
node factory/unity-src/hyeopgok-sasu/ArtSource/packs/verify-packs.mjs
source ~/.unity/env
bash factory/unity/build.sh hyeopgok-sasu
```

QA의 출력 위치를 게임 소스 안으로 지정해 `factory/work/`를 건드리지 않는다.

```bash
export PUPPETEER_EXECUTABLE_PATH="$HOME/.cache/puppeteer/chrome/mac_arm-151.0.7922.47/chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing"
node factory/unity-src/hyeopgok-sasu/ArtSource/packs/verify-schema.mjs
node factory/lib/qa.mjs hyeopgok-sasu --out factory/unity-src/hyeopgok-sasu/ArtSource/validation/phase2/qa
node factory/unity-src/hyeopgok-sasu/ArtSource/validation/phase2/capture.mjs
node factory/unity-src/hyeopgok-sasu/ArtSource/validation/phase2/summarize.mjs
```

과거 1단계의 전후·레퍼런스 비교와 실제 렌더 표지는 다음 명령으로 재현한다. 위 팩 생성 명령은 팩을 다시 만들 때만 사용한다. 비주얼 수정에서 팩을 재생성하지 않는다.

```bash
node factory/unity-src/hyeopgok-sasu/ArtSource/validation/phase1/capture.mjs final
python3 factory/unity-src/hyeopgok-sasu/ArtSource/validation/phase1/make_artifacts.py --tag final --install-public
node factory/lib/qa.mjs hyeopgok-sasu --out factory/unity-src/hyeopgok-sasu/ArtSource/validation/phase1/qa
```

`phase1/provenance.json`에는 표지에 사용한 원본 캡처와 해시를 남긴다.
합성 스크립트는 `phase1/`에 표지와 `compare.png`를 만든다. `--install-public`은 공개 게임 폴더의 표지만 교체하며 배포하지 않는다.

2단계 봇은 같은 `HyeopgokRules.Move`·`Tick`으로 왕 이동·붓기·이탈 확정을 실행한다. 고정 시드, 원시 결과, 통과하지 못한 조건은 `ArtSource/bot-results.md`에 그대로 남긴다. `Editor/HyeopgokBotSelfTest.cs`의 실행 주석을 참고한다.

## 구현과 증거

- Blender 원본·스크립트·FBX 목록: `ArtSource/blender/`, `ArtSource/model-notes.md`.
- 전투는 GPU 인스턴싱과 풀을 사용한다. 시작 시 적 400·아군 100, 적 최대 598·아군 최대 150이다. 히트 플래시, 넉백 사체, 파편, 화살, 0.05초 히트스톱과 카메라 흔들림을 결합한다.
- 답 판정은 정수 양, 정확한 두 정수, 정수 교차곱과 최대공약수로 처리한다. `answerNumeric`은 QA 호환 필드일 뿐 판정에 쓰지 않는다. 2단계 844문항 전수 검산은 오류 0이며, 정수 입력 전체 영역과 별도로 실제 붓기 정답 동선도 844/844 통과했다.
- 2단계 표지는 코인을 붓는 **실제 WebGL 원본 화면**이며 이미지 편집을 하지 않았다. 1200×630·1080×1080 표지의 SHA와 입력 상태는 `ArtSource/validation/phase2/integration.json`에 있다. 최초 이미지 생성 기록과 1단계 합성 표지는 각각 `ArtSource/image-prompts.md`, `ArtSource/validation/phase1/`에 이력으로 보존한다.
- `ArtSource/validation/phase2/integration.json`: 실제 터치 완주·오답 회복, 재진입 확인 취소, 세 분수 판정, 0 입력, v1 호환과 최대 비용 문항의 빈 지갑 수급을 검증한다. fixture는 페이지의 HTTP 응답만 교체하며 공개 팩 파일을 수정하지 않는다. 입력 신뢰 여부·Build/팩/캡처 SHA를 기록한다.
- `ArtSource/validation/playthrough.json`과 `pack-swap.json`은 코인 입력 이전 빌드의 이력이다. 해당 결과를 2단계 통과 증거로 재사용하지 않는다.
- `ArtSource/validation/phase1/qa/report.json`과 `phase1/summary.json`은 과거 1단계 증거다. 2단계 공식 QA·캡처·크기는 `ArtSource/validation/phase2/`에 새로 기록하며, 이전 결과를 이번 빌드에 적용하지 않는다.
- 최종 2단계 QA **45/45·fatal 0**, M4 실 GPU 모바일 **80fps**·1280 **62fps**·15초 유지율 **80%**. 실제 입력 통합 **895검사**, 첫 2닢 투자 **3.964초**, 빈 지갑에서 최대 94닢 정답 투자 **31.768초/제한 104초**. 게임 raw **9.84MB**, gzip 전송 합 **9.27MB**이며 SHA와 합산 방식은 `phase2/summary.json`에 있다.
- 외곽 사암은 `build_models.py`의 `rounded_sandstone`으로 만든 36개 덩어리다. `-- --only-terrain`으로 다른 34종 FBX를 건드리지 않고 지형만 재생성한다. 고정 시드·정점색·기존 정적 메시 결합을 사용한다.

코드로만 생성한 전투 렌더러의 인스턴싱 셰이더가 Unity의 미사용 변형 제거에 걸리는 문제를 발견했다. 게임 전용 `Editor/HyeopgokBuildSettings.cs`에서 빌드 동안 필요한 변형을 보존하고 이전 설정을 복원한다. 공유 킷·빌드 스크립트는 수정하지 않았다. FBX 좌표계 보정도 게임 전용 임포터에서 처리한다.

브라우저 QA는 Apple M4의 실 GPU를 사용하는 헤드리스 Chrome 측정이며 실제 iPhone의 발열·메모리·터치 성능을 보장하지 않는다. 레퍼런스의 협곡 구도·병사 떼·건설 패드·석궁을 따르되 표면과 조명은 더 단순한 로우폴리다. 봇의 원시 관측값과 정확 기대값은 별도로 읽어야 하며, 실패한 원시 조건을 기대값 증명으로 대신 통과시키지 않는다.
