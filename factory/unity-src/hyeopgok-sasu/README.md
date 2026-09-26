# 협곡 사수

사용자 직접 지시로 만든 중2 수학 협곡 디펜스. 정본 명세는 `ArtSource/DESIGN.md`이며, 게시 전 소스와 WebGL 산출물이다. `meta.qa`는 요청대로 미심사 상태를 유지한다.

## 플레이

타이틀 뒤에서 전투가 시작된다. `출격`을 누르면 왕을 끌어 지상 답 패드로 이동할 수 있다. 패드 위에서 0.8초 정지해야 답이 확정되며, 그 전에 떠나면 취소된다. 정답은 석궁 건설·지원군·화살비, 오답은 균열·성문 피해·적의 돌진으로 이어진다. 성문 HP가 0이면 종료한다. 10문항을 버티고 첫 시도 정답이 7개 이상이어야 승리한다.

확률과 경우의 수는 각각 400문항이다. 타이틀 팩 선택 또는 `?pack=m2s2-u6`으로 바꾼다. 새 JSON 팩 추가 방법은 `public/g/hyeopgok-sasu/packs/README.md`에 있다. 팩은 Unity 빌드에 포함되지 않으며 부팅 시 상대경로로 읽는다.

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
node factory/lib/qa.mjs hyeopgok-sasu --out factory/unity-src/hyeopgok-sasu/ArtSource/validation/qa
node factory/unity-src/hyeopgok-sasu/ArtSource/validation/capture.mjs
node factory/unity-src/hyeopgok-sasu/ArtSource/validation/playthrough.mjs
node factory/unity-src/hyeopgok-sasu/ArtSource/validation/pack-swap.mjs
```

비주얼 라운드의 전후·레퍼런스 비교와 실제 렌더 표지는 다음 명령으로 재현한다.

```bash
node factory/unity-src/hyeopgok-sasu/ArtSource/validation/visual-pass-capture.mjs final
python3 factory/unity-src/hyeopgok-sasu/ArtSource/validation/visual-pass/make_artifacts.py
node factory/lib/qa.mjs hyeopgok-sasu --out factory/unity-src/hyeopgok-sasu/ArtSource/validation/visual-pass/qa
```

`visual-pass/artifacts/provenance.json`에는 표지에 사용한 원본 캡처와 해시를 남긴다.
합성 스크립트는 `artifacts/`에 표지를 만든다. 공개 폴더에도 반영하려면 `--install-public`을 추가한다.

봇은 같은 `HyeopgokRules.Move`·`Tick`으로 왕 이동과 정지를 실행한다. 고정 시드, 원시 결과, 통과하지 못한 조건은 `ArtSource/bot-results.md`에 그대로 남긴다. `Editor/HyeopgokBotSelfTest.cs`의 실행 주석을 참고한다.

## 구현과 증거

- Blender 원본·스크립트·FBX 목록: `ArtSource/blender/`, `ArtSource/model-notes.md`.
- 전투는 GPU 인스턴싱과 풀을 사용한다. 시작 시 적 400·아군 100, 적 최대 598·아군 최대 150이다. 히트 플래시, 넉백 사체, 파편, 화살, 0.05초 히트스톱과 카메라 흔들림을 결합한다.
- 답 판정은 정수 또는 기약분수의 정규 문자열 일치로 처리한다. `answerNumeric`은 QA 호환 필드일 뿐 판정에 쓰지 않는다. 800문항과 34,024개 표본 사건을 독립 검증했다.
- 최초 표지는 이미지 생성 도구를 사용했으나, 비주얼 강화 라운드에서 실제 WebGL 전투 캡처와 게임 UI 색상·폰트의 제목으로 교체했다. 현재 표지의 캡처·합성 재현 스크립트와 비교 증거는 `ArtSource/validation/visual-pass/`에 있다. 기존 생성 기록은 `ArtSource/image-prompts.md`에 이력으로 보존한다.
- `ArtSource/validation/playthrough.json`: 실제 터치 완주·오답 회복, 정지 취소, 승리 경계, 패배 유지, 팩 선택과 임시 팩 교체. 임시 JSON·목록 변경은 검증 후 복원하며 Build 해시가 같은지 확인한다.
- `ArtSource/validation/pack-swap.json`: 최종 표시 수정 뒤 빌드에서 이름만 바꾼 팩의 재빌드 없는 교체를 다시 확인했다. 초등 학교급 표시와 글자 보기의 실제 터치 정답도 일시적인 표시 검사용 팩으로 확인했다. 이 팩은 새로운 교육과정 팩이 아니며 검증 후 삭제한다.
- `ArtSource/validation/qa/report.json`: 공식 QA. `visual-review.md`와 PNG는 화면 검토 증거다.

코드로만 생성한 전투 렌더러의 인스턴싱 셰이더가 Unity의 미사용 변형 제거에 걸리는 문제를 발견했다. 게임 전용 `Editor/HyeopgokBuildSettings.cs`에서 빌드 동안 필요한 변형을 보존하고 이전 설정을 복원한다. 공유 킷·빌드 스크립트는 수정하지 않았다. FBX 좌표계 보정도 게임 전용 임포터에서 처리한다.

브라우저 QA는 Apple M4의 실 GPU를 사용하는 헤드리스 Chrome 측정이며 실제 iPhone의 발열·메모리·터치 성능을 보장하지 않는다. 레퍼런스의 협곡 구도·병사 떼·건설 패드·석궁을 따르되 표면과 조명은 더 단순한 로우폴리다. 봇의 원시 관측값과 정확 기대값은 별도로 읽어야 하며, 실패한 원시 조건을 기대값 증명으로 대신 통과시키지 않는다.
