# 딱 맞는 방울 (ttak-matneun-bangul) — 소스·에셋 기록

중2 2학기 「삼각형의 성질」(m2s2-u1, [9수03-09]·[9수03-10]) — 내심·외심을 **씨앗을 놓는 위치**로 찾는 Unity WebGL 게임.

## 코드 (`Scripts/`)
| 파일 | 내용 |
|---|---|
| `BangulRules.cs` | 정수 판정(`Judge` — decimal 정수 곱, √·실수 비교 없음), 판 생성기(이등변·예각·직각(피타고라스 수·피타고라스 방향 회전)·둔각), 접기 띠 규칙, 오개념 판별, 문제 은행(484문항) |
| `BangulBots.cs` | B3 무뇌 봇 자가 테스트(같은 생성기·같은 판정). `?selftest=1` 로 열면 실행 |
| `TtakMatneunBangulGame.cs` | 상태 머신·입력(씨앗 끌기/접기 띠)·`IMgfGame` 훅 |
| `BangulView.cs` | 레몬 타일 욕조 세계(코드 메시), 거품가오리 루루(절차 메시), 방울·벽·못·연출 |
| `BangulUi.cs` | HUD·발문 카드·거리 막대·안내·결과, 390×844 ↔ 1280×800 레이아웃, 합성 효과음 |

판정: 안쪽 방울 = 세 변 직선까지 거리, 바깥 방울 = 세 꼭짓점까지 거리. `d_max ≤ (1+tol)·d_min` 을 제곱해 정수 비교
(tol 안쪽 28%, 바깥 16%). 판마다 정답 영역 넓이 비(우연 수준)가 3.5%를 넘으면 생성기가 버린다.
무게중심·판 중앙·삼각형 상자 중앙·반대 중심(I↔O)은 허용오차 2배로도 떨어지는 판만 쓴다(정삼각형 근처 제외).

## 에셋
| 런타임 파일 | 원본 | 처리 |
|---|---|---|
| `Resources/TtakMatneunBangul/logo.png` | 아트 단계 `public/g/…/assets/title_ink.png` (codex 이미지 생성, 김 서린 거울 글씨 「딱 맞는 방울」) | `prep-assets.mjs`: 가장자리 연결 흰 배경만 지우는 플러드필 크로마키 + 크롭 |
| `Resources/TtakMatneunBangul/soap.png` | 아트 단계 `assets/soap-life.png` (오리지널 비누 생물 6종) | 같은 키 + 3×2 아틀라스(셀 256) |
| 그 밖의 모든 것 | — | 코드: 타일·물빛·비누막·고리·김서림 텍스처, 루루·구·원기둥 메시, 효과음 합성 |

배경 일러스트 없음(기획 background_policy). `assets/title.png`·`lulu-reference.png` 는 구도·캐릭터 참고로만 썼다.
표지 `thumb.png`·`square.png` 는 **실제 런타임 타이틀**을 `capture-covers.mjs`(`?cover=1` 표지 구도)로 캡처했다.
아트 단계 AI 표지는 `ai-covers/` 에 보관.

## 검증 (`validation/`)
- `node …/validation/bot-selftest.mjs` → `bot-results.json` (200판×봇, 우연 수준 50덱 평균)
- `node …/validation/pointer-playthrough.mjs` → 실제 pointer 만으로 연습→R1~R6(접기 띠 포함)→결과, 오답→수리 경로
- `node factory/lib/qa.mjs ttak-matneun-bangul` — 45/45, 치명 0, 60fps(실 GPU)
