# 원 찍어 — 에셋·출처 기록

| 에셋 | 출처 |
|---|---|
| `Resources/WonJjigeo/pin.png` | 아트 단계 생성(codex 내장 이미지 생성, 프롬프트: `factory/work/chosen.json` → `art_direction.assets_needed[pin]`). `public/g/won-jjigeo/assets/pin.png` 사본. 타이틀 로고의 핀으로만 쓴다 |
| 모래 베드·청동 녹청·철 텍스처 | 코드 생성(`WonJjigeoGame.SandTex/BronzeTex/IronTex`, value-noise fBm, 부팅 1회) |
| 명판 메시 | 코드 생성(베벨 있는 삼각 판, `ShapePlate`) — 명판 14장 풀 재사용 |
| 핀·트레이·랙·슈트·립·리벳 | 킷 둥근 상자 + 프리미티브 조합 |
| 효과음(clang·thunk·scrape·seat·chime·tick·refuse·pull) | 코드 합성(`BuildSounds`) — 외부 음원 없음 |
| 글꼴 | 킷 MgfKR(NotoSansKR Bold 서브셋, OFL) |

`public/g/won-jjigeo/assets/title.png`·`hero.png`(아트 단계 산출)는 Unity 트랙에서 쓰지 않는다 — 타이틀은
움직이는 3D 장면(데모 명판에 핀이 내리꽂히고 녹청 원이 스윕)으로 대신했다. 게임 폴더에는 남아 있다.

## B3 무뇌 봇 자가 테스트

`Editor/WonBotSelfTest.cs` — 게임과 같은 `Gen`·`Layout.Place`·`Geo.Hit` 로 봇 × 200판 + 명판 6000장 대표본.

```bash
bash factory/unity/build.sh won-jjigeo      # 워크스페이스에 소스 동기화
/Applications/Unity/Hub/Editor/6000.3.24f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics \
  -projectPath ~/UnityProjects/MGF-Workspace -executeMethod Mgf.WonJjigeo.WonBotSelfTest.Run -wonOut /tmp/won-bots.json -quit
```

결과: `bot-selftest.json`. 우연 수준 = 허용원 넓이 ÷ 핀을 놓을 수 있는 베드 넓이(≈0.26%).

## 기획서와 다르게 한 것

- 첫 문항 ∠A=50° → **∠A=30°(∠B=∠C=75°), 70° 회전.** ∠A=50° 이등변은 무게중심이 외심에서 0.1R 밖에 안 떨어져
  허용원(0.08R) 안에 들어온다 — 「한가운데 탭」이 통과하는 문항이었다. 28° 회전은 상자 중심이 0.199R 로 기준(0.2R) 미달.
- 허용 반지름 4.5%R → **8%R**(390폭 세로 화면에서 약 9px). 4.5%는 손가락으로 사실상 불가능했다.
  함정 자리(무게중심·반대 중심·아래 변 중점·상자 중심)는 모두 2.5×tol 밖에 있도록 풀을 거른다(위반 0).
- 조작: 끌어 놓기 외에 **베드 탭 → 조준 핀이 떠 있음 → 같은 자리 한 번 더 누르면 박힘**을 더했다(탭만 하는 학생도 진행).
- 판정은 「세 거리 차 3%」 보조 조건 없이 |P−중심| ≤ tol 하나로 한다(정수 밀리단위 거리² 비교). 두 조건은 사실상 같다.
