# 햇살 바구니 — 재사용 에셋을 위한 레퍼런스

2026-09-08. 사용자 요청: 쌍둥이 요새와 햇살 바구니의 3D 스타일을 더 세밀하게 발전시키고, 다른 게임에서도 쓸 에셋으로 다듬기. 이번 단계는 햇살 바구니의 모티브 확인과 시각 자료 조사다. 아래 적용 방향은 제안이며 게임 구현은 변경하지 않았다.

## 원래 모티브: Hay Day

기존 [제작 기록](hayday-sunbasket-20260907.md)에 명시된 직접 모티브는 Supercell의 **Hay Day**다. 재배→수확→납품→농장 성장의 연결을 짧은 넓이 학습 게임에 적용했다. 현재 모델은 자체 제작한 3D 에셋이다.

[Supercell 공식 소개](https://supercell.com/en/games/hayday/) · [공식 플레이 이미지](https://supercell.com/images/32e8c9bae8508f7e03d245e466a206ff/Hay_Day_iPhone_image_V3.png)

![Hay Day 공식 페이지의 플레이 화면](https://supercell.com/images/32e8c9bae8508f7e03d245e466a206ff/Hay_Day_iPhone_image_V3.png)

## 직접 확인한 비교 자료

아래 형태·재질·구성 평가는 공식 이미지에서 도출한 시각적 해석이다. 실제 개발사의 모델 구조나 제작 기법을 확인했다는 뜻은 아니다. 스토어 홍보 합성 이미지는 실시간 렌더나 게임 성능의 근거로 삼지 않는다.

| 레퍼런스 | 확인한 화면에서 볼 점 | 우리 에셋에 적용할 방향 |
|---|---|---|
| [Hay Day](https://supercell.com/en/games/hayday/) | 작물마다 다른 높이와 색 덩어리, 겹친 잎, 흙길과 풀의 경계, 생산 시설마다 다른 외곽 형태 | 작물 종류·성장 상태가 작게 봐도 구분되도록 설계. 집·풍차·판매대의 대표 형태와 입구 깊이 강화 |
| [FarmVille 3](https://play.google.com/store/apps/details?id=com.zynga.farmville3) | 농가의 여러 지붕 형태, 돌출 현관, 굵은 기둥과 창틀, 색이 다른 벽과 지붕. 스토어의 설명·캐릭터 합성 이미지 | 농가·판매대의 지붕/벽/문/소품을 나누고, 비율과 조합으로 건물 변형 제작 |
| [Farm Together 2](https://store.steampowered.com/app/2418520/Farm_Together_2/) | 공식 Steam 스크린샷에서 길·울타리·밭·연못이 반복되며 공간을 구성. 목재·돌·식생의 표면 표현 | 길 직선/곡선/교차, 울타리 직선/모서리/문 등 조합 가능한 부품 설계. 표면 세부 묘사는 모바일 축소 상태에 맞춰 단순화 |
| [Township](https://play.google.com/store/apps/details?id=com.playrix.township) | 공식 스토어 홍보 이미지에서 당근·밀·옥수수의 외곽 형태가 다르고, 새싹→성장→성숙 밭이 구분됨 | 작물의 잎 수·높이·열매로 성장 단계를 표시. 납품 작물과 UI 아이콘의 형태 일치 |

### FarmVille 3 — 농가의 비율과 부품 변형

[공식 스토어 이미지 원본](https://play-lh.googleusercontent.com/nNofjBEcEzs0o-7P_uNqZB2j13l0gWCvMaaFGvPbpQS53-VcfJ_JbABTPbZmIVUaLahyfzE8JYq0orRLi6G8hw=w1600)

![FarmVille 3 농가 변형 홍보 이미지](https://play-lh.googleusercontent.com/nNofjBEcEzs0o-7P_uNqZB2j13l0gWCvMaaFGvPbpQS53-VcfJ_JbABTPbZmIVUaLahyfzE8JYq0orRLi6G8hw=w1600)

### Farm Together 2 — 반복 부품으로 구성한 농장

[공식 Steam 스크린샷 원본](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/2418520/a3740ebec258b80df2e1c0c471dc23ffb68f01ca/ss_a3740ebec258b80df2e1c0c471dc23ffb68f01ca.1920x1080.jpg)

![Farm Together 2 공식 스크린샷](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/2418520/a3740ebec258b80df2e1c0c471dc23ffb68f01ca/ss_a3740ebec258b80df2e1c0c471dc23ffb68f01ca.1920x1080.jpg)

## 현재 화면과 에셋에서 발견한 차이

현재 공개본 검증 이미지 `logs/manual-20260907-two-game-polish/sunbasket-farm/live/planning-390.png`를 함께 비교했다. 청록 지붕·크림색 벽·목재와 열린 농장 구도는 유지할 만한 정체성이다. 나무 수관은 비슷한 둥근 덩어리가 반복되고, 넓은 잔디 면과 강한 그림자가 눈에 먼저 들어온다. 꽃이나 못의 수를 더하는 것만으로 전체 인상이 크게 바뀌기는 어렵다.

현재 10종 GLB를 읽기 점검한 결과, +Y 위/+Z 앞·바닥 y=0이고 대부분 단일 메시다. 외부 텍스처 없이 정점색을 사용하며, 게임이 높이와 중심을 다시 정규화한다. 지붕·벽·잎의 독립 색 변경, 문·바퀴·열매의 독립 동작에는 부품 분리가 필요하다. 런타임 꽃·화분·길에는 현재 농장 좌표와 치수에 의존하는 부분이 있다.

## 첫 제작 묶음 제안

1. **농가:** 처마 두께·지붕 곡선·현관 깊이·창틀의 덩어리감부터 개선. 지붕/벽/문/창을 분리하여 같은 계열의 집·창고·작업소로 변형한다.
2. **판매대:** 어닝 처짐·목재 두께·진열 높이를 조율. 어닝/프레임/상품을 분리하여 농장 가게·시장·보급소에서 사용한다.
3. **사과나무:** 큰 수관과 작은 수관의 비대칭, 가지가 보이는 틈, 열매 배치를 설계. 어린 나무/성목/수확 후 상태와 나무 3종 변형을 만든다.
4. **당근:** 새싹/성장/성숙의 잎과 뿌리 형태를 구분한다. 이후 밀·딸기에 같은 성장 상태 규칙을 적용한다.

이 네 가지로 품질 기준을 정한 뒤 울타리·길·바위 묶음으로 넓힌다. 쌍둥이 요새와 공유할 후보는 목재·돌·풀·상자·수레이며, 농가의 지붕이나 성탑은 각 게임의 개성을 유지한다.

공용 원본은 크기 단위·바닥 원점·부품 이름·색 변경 규칙을 통일한다. 배포할 때는 각 게임 폴더로 필요한 파일을 복사해 자기완결 계약을 지킨다. 재사용성 검증은 같은 원본 에셋을 농장과 작은 시장의 서로 다른 배치에 넣어 확인한다.

완성도 판정에는 같은 카메라·조명의 전후 화면, 390px 실제 게임 크기, 축소 시 종류 구분, 실제 기기 성능을 사용한다. 공장 QA 점수를 상용 게임과 동급인 아트 품질의 근거로 쓰지 않는다.
