# 빙막 봉합 아트·검증 기록

- 수정 후 정체성: 밤의 빙벽 측량 캠프. 불투명한 층진 얼음 절벽, 황동 피톤, 주황 보강선, 궤도형 측량 썰매가 하나의 2.5D 디오라마를 이룬다.
- 타이틀과 인게임은 모두 같은 Unity 런타임 세계를 쓴다. 포토리얼 생성 타이틀은 렌더 경로에서 제거했고 `thumb.png`·`square.png`도 최종 런타임 타이틀을 다시 캡처한다.
- `Resources/BingmakBonghap/Title.png`·`Thumb.png`와 `public/g/bingmak-bonghap/assets/`의 기존 생성 이미지는 1차 제작 출처 보존용이며 코드에서 로드하지 않는다. 원출처는 `factory/work/art-title.json`·`factory/work/art.json`이다.
- 기존 물범·수중 유영·투명 삼각창 사건은 사용하지 않는다. 정답 사건은 피톤 세 개 잠금→빙벽 균열 수축→황동 보강선 점등→측량 썰매 횡단이다.
- 외부 상표·기존 캐릭터·원격 리소스 없음. 게임 빌드는 외부 네트워크 요청을 만들지 않는다.
- `validation/fix1-firstplay/manifest.json`은 최종 WebGL 해시 `c6aba2905385404a240bc0d4f7904cbf6f8e051042240db8a4566c9f24bf6d1a`의 45초 순진 입력 16프레임이다. t=13에 실전 1/8로 전이했고 console/page error는 0이다.
- `validation/pointer-playthrough-results.json`은 정답 훅 0회로 첫 오답 수선 clear 8/8과 결과 CTA 뒤 독립 둘째 clear 8/8을 기록한다.

## 구현된 피드백 재질

- 정답: 세 황동 피톤 순차 잠금 → 균열 안쪽 수축 → 삼각 보강선 점등 → 측량 썰매 횡단.
- 오답: 제출 위치의 눌린 두 선, DF·EF 초과/부족 문장, 갈라진 무광 클립.
- 주스: 판단 직후 75 ms hit-stop, F 고리 squash/stretch, 판 전개 overshoot, 고정 길이 drag trail, 측량빔 ambient motion, 잠금음-피톤 동기화, 정답 광택 확산, 점수 count-up. 정답 화면 흔들림은 없다.
