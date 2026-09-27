# 협곡 사수 AI 3D — A 단계

2026-09-27에 제작한 에셋 전용 산출물이다. Unity `Resources` 설치, 런타임 셰이더 연결,
WebGL 빌드와 `public/` 배포는 포함하지 않는다.

## 최종 산출물

| 에셋 | 삼각형 | 높이 | 텍스처 | FBX / PNG |
|---|---:|---:|---:|---|
| 왕 | 5,500 | 1.3275m | 512² RGBA | `out/king.fbx` / `out/king.png` |
| 파란 병사 | 1,300 | 0.78m | 256² RGBA | `out/ally_soldier.fbx` / `out/ally_soldier.png` |
| 빨간 병사 | 1,300 | 1.09m(창 포함) | 256² RGBA | `out/enemy_soldier.fbx` / `out/enemy_soldier.png` |
| 석궁탑 | 4,468 | 2.05m | 512² RGBA | `out/crossbow_tower.fbx` / `out/crossbow_tower.png` |
| 병영 | 4,407 | 1.845m | 512² RGBA | `out/barracks.fbx` / `out/barracks.png` |
| 적 거인 | 3,499 | 1.95m | 512² RGBA | `out/giant.fbx` / `out/giant.png` |

각 FBX는 하나의 정적 mesh/submesh/material이고, FBX 재임포트 후 높이·바닥 원점·중심·UV0·
COLOR.a AO를 다시 검증했다. 정본 수치와 SHA-256은 `out/manifest.json`, 개별 3/4 렌더는
`previews/`, 전체 실제 스케일 비교는 `lineup.png`에 있다.

## 제작 경로와 크레딧

- 선택 콘셉트는 `concepts/*-v2.png` 및 `concepts/king-v4.png`; 프롬프트 요약과 해시는
  `concepts/README.md`에 기록했다.
- Meshy 7.1 Image-to-3D 8회(무기 없는 A-pose 병사 2회 폐기 포함) 240크레딧,
  선택 작업 Remesh 6회 30크레딧을 사용했다.
- 세션 시작 잔액 515, 종료 잔액 245, 총 사용 270/400크레딧이다. 제출별 전후 잔액,
  task 상태, 원본/다운로드 해시는 `meshy-session.json`에 있고 키와 signed URL은 없다.
- 대용량 원본·리메시 GLB는 gitignored `scratchpad/ai3d-raw/hyeopgok-sasu-a/`에만 있다.

## 데이터 계약

- 좌표: metre, 바닥 Y=0, +Z 전방으로 Unity에서 해석할 FBX(-Z forward, Y up).
- UV0: 단일 base-colour atlas.
- COLOR.rgb: 흰 multiplier, COLOR.a: 48-ray 기하 AO.
- 병사 PNG alpha: 0은 진영 tint 후보, 1은 고정색. 자동 HSV 마스크라 통합 전에 수동 확인한다.

## 확인된 제한

- Meshy triangle 출력은 한 mesh로 묶였지만 face 경계 정점이 용접되지 않은 triangle-soup이다.
- 석궁 상부와 기둥은 하나의 FBX다. 회전 조준을 쓰려면 B 단계에서 피벗 기준으로 분리해야 한다.
- 거인 칼의 붉은 홈은 base colour에만 있으며 실제 emissive 채널은 아직 없다.
- 병사 UV1 rigid-part/motion mask는 아직 없고, 현재 Horde 셰이더는 이 PNG를 샘플링하지 않는다.
- 왕의 입력 비대칭 견갑은 Meshy가 양쪽 견갑으로 보정했다.

## 재현

```bash
python3 factory/unity-src/hyeopgok-sasu/ArtSource/ai3d/meshy_generate.py
python3 factory/unity-src/hyeopgok-sasu/ArtSource/ai3d/meshy_remesh.py
/Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup \
  --python factory/unity-src/hyeopgok-sasu/ArtSource/blender/ai3d_optimize.py -- --all
```

앞의 두 Python 명령은 기본 dry-run이다. 실제 유료 제출은 한 에셋 이름과 `--submit`을 함께
지정해야 하며, 세션 기준 400크레딧 상한을 넘으면 중단한다.
