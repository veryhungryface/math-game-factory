# 햄스터 대회전 아트 소스

- 런타임 비주얼은 Unity C# 절차 메시와 공용 `MgfLook` 재질만으로 만든다.
- 세계: 무표백 리넨 세 겹, 자작나무 테, 여덟 면 거울, 좁은 필름 프레임, 붉은 장력끈, 황동 클립.
- 오리지널 생물: 리넨 필름에 먹선으로 인쇄된 둥근 햄스터 8프레임. 기존 캐릭터·상표를 참조하지 않았다.
- 아트 에이전트 제공 파일은 `public/g/hamster-daehoejeon/assets/title.png`, `thumb.png`, `square.png` 세 개이며 런타임은 외부 이미지 없이 같은 정체성을 코드로 재구성한다.
- 생성 출처와 프롬프트는 `factory/work/art.json` 및 `factory/work/chosen.json`에 기록되어 있다. WebGL 런타임은 이 파일들 외의 에셋을 참조하지 않는다.
- `validation/`에는 실제 pointer 정상·회복·게임오버 실행, B3 4종×200판, 최종 QA, 45초 firstplay 결과를 남겼다. 모든 최종 결과의 artifact hash는 `b90cc18f88ead239495e8ec1b3f5353374adf3e2d4b8938f20bc07b617940562`이다.
