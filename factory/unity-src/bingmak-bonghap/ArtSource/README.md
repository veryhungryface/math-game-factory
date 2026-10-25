# 빙막 봉합 아트·검증 기록

- 2차 수정 정체성: 낮의 미니어처 빙하 아치 캠프. 눈 둔덕, 깎인 얼음 기둥, 산호색·노란 텐트, 직조 깃발, 바람 눈가루, 오리지널 피카 안내원이 하나의 열린 2.5D 디오라마를 이룬다.
- 타이틀과 인게임은 모두 같은 Unity 런타임 세계를 쓴다. 포토리얼 생성 타이틀은 렌더 경로에서 제거했고 `thumb.png`·`square.png`도 최종 런타임 타이틀을 다시 캡처한다.
- `Resources/BingmakBonghap/Title.png`·`Thumb.png`와 `public/g/bingmak-bonghap/assets/`의 기존 생성 이미지는 1차 제작 출처 보존용이며 코드에서 로드하지 않는다. 원출처는 `factory/work/art-title.json`·`factory/work/art.json`이다.
- 기존 물범·수중 유영·투명 삼각창 사건과 1차 수정의 금속 계측 장치·순차 피톤 잠금·레일 썰매는 사용하지 않는다. 정답 사건은 직조 안전띠 동시 장력→조각 얼음 아치 기립→피카 네 번 도약 횡단이다.
- 외부 상표·기존 캐릭터·원격 리소스 없음. 게임 빌드는 외부 네트워크 요청을 만들지 않는다.
- `validation/fix1-firstplay/manifest.json`은 1차 수정 WebGL 해시 `c6aba2905385404a240bc0d4f7904cbf6f8e051042240db8a4566c9f24bf6d1a`의 보존 증거다. 2차 수정 최종 firstplay는 `factory/work/qa/bingmak-bonghap/firstplay/`의 artifact `8a267100fa6588c94a48795aada2c685d2aeaca602274fa47aa39f5d71dd2fcd`이며 t=6 연습·t=13 정답 세계 사건·t=17 실전 전이, console/page error 0이다.
- `validation/pointer-playthrough-results.json`은 최종 Build에서 정답 훅 0회로 첫 오답 수선 clear 8/8과 결과 CTA 뒤 독립 둘째 clear 8/8을 기록한다. `pointer-playthrough/02b-run1-repaired-arch.png`는 같은 실제 수선 드래그 직후 아치 기립과 피카 횡단을 캡처한다.

## 구현된 피드백 재질

- 정답: 세 직조 안전띠 동시 장력 → 균열광 안쪽 수축 → 입체 얼음 아치 기립 → 피카 도약 횡단.
- 오답: 제출 위치의 눌린 두 선, DF·EF 초과/부족 문장, 갈라진 무광 클립.
- 주스: 판단 직후 75 ms hit-stop, F 고리 squash/stretch, 판 전개 overshoot, 고정 길이 drag trail, 깃발 펄럭임·눈가루 ambient motion, 아치 기립 whoosh, 피카 연속 도약, 정답 광택 확산, 점수 count-up. 정답 화면 흔들림은 없다.
