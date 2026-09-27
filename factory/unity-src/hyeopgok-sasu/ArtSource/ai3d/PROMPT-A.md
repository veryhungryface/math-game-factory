너는 3D 아티스트 겸 파이프라인 엔지니어다. 교육용 Unity WebGL 게임 「협곡 사수」(저장소 /Users/sitpo/math-game-factory, 소스 `factory/unity-src/hyeopgok-sasu/`)의 **핵심 에셋을 AI 3D 로 새로 만든다 — A 단계(에셋 제작만)**.
배경: 코드·Blender 절차적 로우폴리로는 독립 판정이 6.3점대에서 정체(`ArtSource/validation/judge-r3/verdict.json`). 사용자가 킹샷급 품질을 위해 유료 AI 3D 사용을 승인했다.

## 만들 에셋 (디자인은 오리지널 — 킹샷 캐릭터·건물 복제 금지, 톤·비율·재질감만 참고. 첨부 = 킹샷 참고)
1. 왕(주인공): 치비 3~3.5등신, 금관, 파란 망토, 금테, 굵은 조형.
2. 파란 병사(아군): 2~2.5등신, 각진 투구+작은 방패+짧은 검. 3. 빨간 병사(적): 둥근 투구, 창. (둘 다 떼로 수백 개 인스턴싱 → 최종 **800~1,500 삼각형**)
4. 석궁탑: 석재 기둥 + 큰 석궁 + 파란 천. 5. 병영: 파란 지붕 널판, 교차 검 간판, 석재 기단. 6. 적 거인: 병사 3배, 빛나는 붉은 칼.

## 절차
1. 콘셉트 이미지: 너의 내장 이미지 생성 도구로 에셋별 **단일 오브젝트·정면 3/4·흰/단색 배경·그림자 없음** 이미지(필요하면 2~4 뷰). `ArtSource/ai3d/concepts/`. 톤: 스타일라이즈드 핸드페인트 + 약한 PBR, 채도 높은 킹샷풍 팔레트(`ART-BIBLE-KINGSHOT.md` §C).
2. Meshy 이미지→3D: 키는 `~/.config/mosslight/meshy-api-key.txt`(절대 출력·커밋 금지). 참고 구현 `/Users/sitpo/dev/unity-connection-test/AgentScripts/generate-v3-meshy.py`(엔드포인트·파라미터). 잔액 조회 `GET https://api.meshy.ai/openapi/v1/balance`. **총 사용 상한 400 크레딧** — 제출 전 잔액 기록, 매 제출 후 잔액 기록, 상한 초과 금지. 텍스처 포함 GLB 로 받아 `ArtSource/ai3d/raw/`(대용량이면 .gitignore 대상 폴더 `scratchpad/ai3d-raw/` 에 두고 경로만 기록).
3. Blender 헤드리스 최적화(`/Applications/Blender.app/Contents/MacOS/Blender -b --python`), 스크립트는 `ArtSource/blender/ai3d_optimize.py`: 데시메이트(왕 ≤6k·건물 ≤5k·거인 ≤4k·병사 800~1.5k tri), 원점·스케일·전방축 정규화(기존 `Resources/HyeopgokSasu/` 모델과 같은 규격 — `model-notes.md` 참고), 텍스처는 에셋당 512² 이하 1장(병사 2종은 256²), **AO 를 정점색에도 굽기**, 병사는 인스턴스 색 틴트가 먹도록 흰/회색 바탕 + 진영색 마스크 채널 고려. 출력 FBX + PNG 를 `ArtSource/ai3d/out/`(아직 `Resources` 에 넣지 마라).
4. 미리보기: Blender 로 에셋별 3/4 렌더 + 전체 라인업 한 장 `ArtSource/ai3d/lineup.png`. 킹샷 참고와 나란히 놓고 조형·재질이 같은 등급인지 스스로 확인, 부족하면 콘셉트부터 다시(상한 안에서).

## 제약
- **Unity 빌드·`public/`·`Resources/`·게임 스크립트 수정 금지**(다른 파이프라인이 Unity 를 쓰는 중). run.sh·config·prompts·lib 금지. Meshy 키 노출 금지.
- 로컬 커밋 가능(키·대용량 원본 제외), push 금지.
## 최종 응답(한국어 8줄 이내)
에셋별 삼각형 수·텍스처 크기·파일 경로 / Meshy 사용 크레딧(시작·끝 잔액) / lineup 경로 / 품질 자평 대신 사실로 본 약점.
