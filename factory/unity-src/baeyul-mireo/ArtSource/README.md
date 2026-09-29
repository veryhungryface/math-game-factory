# 배율 밀어 — 에셋·출처 기록 (v1, 2026-09-30)

아트 단계 산출 이미지는 0장이었다(`factory/work/art.json` generated=[]). 그래서 전부 코드로 만들었고, 외부 음원·이미지는 쓰지 않았다.

| 에셋 | 출처 |
|---|---|
| 책상 면지 · 인화지(5 mm 모눈·섬유결·누름 자국) | 코드 생성(`BaeyulView.PaperTexture`, 512 px = 5 cm). 부팅할 때 1회 베이크 |
| 원본 고무판(나무 받침 + 붉은 고무 면) | 킷 둥근 상자(`MgfLook.Block`) + 먹 메시(`Ink`, 정점색) |
| 흑철 플래튼 · 주홍 립 · 놋쇠 나사 | 킷 둥근 상자 + 원기둥 프리미티브 |
| 놋쇠 레일(자) · 손잡이 · 멈춤쇠 · 고정판 | 킷 둥근 상자 + 원기둥, 눈금은 먹 메시 |
| 주홍 잉크 인상 · 번짐 · 겹인화 | 코드 메시(롤러가 지나간 반평면까지 볼록 다각형 자르기 → 채우기) |
| 스파이크 파일 | 원기둥 + 꽂힌 종이(둥근 상자) 7장 풀 |
| 로고 뒤 잉크 얼룩 · 예비 인화지 구김 | 코드 생성 스프라이트(`BlotSprite`, `CrumpleSprite`) |
| 효과음 | 코드 합성(`BaeyulSound`: tick·thunk·ink·good·spike·crumple·smear·refuse·slide·press·ripple·lock·combo·blip·end) |
| 글꼴 | 킷 MgfKR(NotoSansKR Bold 서브셋, OFL). ∽ △ □ ² ∠ ° ↓ ← 포함을 fontTools 로 확인했다(— ▼ ✗ 는 없어서 쓰지 않는다) |
| thumb.png · square.png | 게임 타이틀 화면(데모 루프에서 잉크가 번진 순간)을 실제로 렌더해 캡처했다(1200×630 · 1080×1080, 음소거 버튼은 숨김) |

## 설계 요지
- 모델·판정·오개념 매핑은 `Scripts/BaeyulRules.cs` 머리 주석에 있다. 손잡이는 「레일 변의 길이」를 5 mm 칸 단위로 정하고, 판정은 교차곱 `handle·m = os[gauge]·n` 이다.
- 12장 덱은 다음과 같다: 1~4 구성(현재 비 보임, 1:2 → 2:3 → 직사각형 → 축소 포함) · 5 다리(현재 비 걷힘) · 6·8 대응(△ABC∽△EFD, 레일 변 DE 의 대응변은 AB 가 아님, 손잡이는 AB 기준 오개념 값에서 시작) · 7·9·11 판별(레일 고정. SSS·SAS·AA·직사각형이고, 닮음/아님이 반반) · 10 넓이의 비 · 12 넓이 값(손잡이는 넓이 비를 닮음비로 쓴 값에서 시작).
- 판별 장은 닮음/아님이 반반이라 「항상 찍기」「항상 치우기」 연타가 우연 수준에 묶인다.

## 계획서 대비 축소
- 장 5의 「빗금 도장으로 대응변 찍기」 다리 미니게임은 빼고, 안내 카드와 1단계 빗금 표시, 정답 순간의 빗금 잠김으로 대신했다.
- 확대본 90° 회전 탭은 빼고, 대응 장은 그림을 돌리거나 뒤집어 보여 준다. 학생은 기호 순서로 대응변을 고른다.
- 겹인화가 다음 장의 작업 면을 좁히는 효과와 일일 시드 스탬프는 빼고, 예비 인화지 구김과 콤보 리셋만 남겼다.
- 입체 부피의 비와 직각삼각형 닮음 활용은 계획서 risks 에 따라 제외했다.

실행: `bash factory/unity/build.sh baeyul-mireo` 후 `Unity -batchmode -nographics -projectPath ~/UnityProjects/MGF-Workspace -executeMethod Mgf.BaeyulMireo.BaeyulBotSelfTest.Run -bmOut <md> -quit` → `bot-results.md`
