# 햄스터 대회전 아트 소스

- 런타임 비주얼은 Unity C# 절차 메시와 공용 `MgfLook` 재질만으로 만든다.
- 세계: 무표백 리넨 세 겹, 자작나무 테, 여덟 면 거울, 좁은 필름 프레임, 붉은 장력끈, 황동 클립.
- 오리지널 생물: 리넨 필름에 먹선으로 인쇄된 둥근 햄스터 8프레임. 기존 캐릭터·상표를 참조하지 않았다.
- 아트 에이전트가 만든 `public/g/hamster-daehoejeon/assets/title.png`는 런타임에서 쓰지 않는다. 2차 수정에서 `thumb.png`는 실제 pointer 첫 정답 장면, `square.png`는 움직이는 WebGL 타이틀을 직접 캡처해 표지와 게임 화면을 일치시켰다.
- 최초 생성 출처와 프롬프트는 `factory/work/art.json` 및 `factory/work/chosen.json`에 기록되어 있다. 최종 표지 캡처 방법과 오류 없는 상태는 `validation/capture-runtime-covers.mjs`와 `runtime-cover-capture-results.json`에 기록했다.
- `validation/`에는 실제 pointer 정상·회복·게임오버 실행, B3 4종×200판, 최종 QA, firstplay 결과를 남겼다. 2차 수정 최종 WebGL artifact hash는 `64203f5af1bbb19d8f31273fdbbfea0f38b2cd193206bf82c1b86e96c4b5003b`이다.
