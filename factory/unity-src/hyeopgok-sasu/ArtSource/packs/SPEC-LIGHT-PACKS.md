# 협곡 사수 — 전 학년 단원 팩 (경량) 제작 규격 (2026-09-28, 사용자 지시)

## 사용자 원문
> 지금 클로드로 단원을 여러 단원 넣는 작업해 줘. 문제 난도는 어렵게 하지는 말고. 각 단원 30문제 수준으로 초3, 4, 5, 6, 중1, 2 작업해.
> (앞서) 검증이 너무 심하다 — 차라리 나한테 검사받는 게 나을 것 같아.

## 범위
- 초3~6: `curriculum/textbook-structure.json` 의 `volumes`(학년·학기별 6단원, 8학기 = 48단원). 단원 제목·순서·차시·발문 관습(`style_guide`, `expression_traps`, 단원 `textbook_caveats`)을 따른다. 3-2·4-2·5-x·6-x 는 `curriculum/2022-elementary-math.json` 에도 단원 상세(성취기준·오개념)가 있다 — 있으면 쓴다.
- 중1: 2022 개정 중1 9단원 — 1학기 `m1s1-u1` 소인수분해 / `u2` 정수와 유리수 / `u3` 문자와 식(일차식·일차방정식) / `u4` 좌표평면과 그래프(정비례·반비례), 2학기 `m1s2-u1` 기본 도형 / `u2` 작도와 합동 / `u3` 평면도형의 성질 / `u4` 입체도형의 성질 / `u5` 자료의 정리와 해석. 성취기준은 `curriculum/2022-middle-math.json` 의 `typical_grade: 1` 코드.
- 중2 는 이미 13팩(각 300+)이 있으므로 이번에 만들지 않는다.

## 팩 형식 (스키마 v3 — `public/g/hyeopgok-sasu/packs/README.md`, 기존 팩 `m2s2-u7.json` 참고)
- 파일: `public/g/hyeopgok-sasu/packs/<pack_id>.json`. `pack_id` = 초등 `g<학년>s<학기>-u<순서>`(예: `g3s1-u1`), 중1 `m1s<학기>-u<순서>`.
- 최상위: `schema_version:3, pack_id, title(교과서 단원명), school("elementary"|"middle"), grade, semester, unit_id(=pack_id), unit_order, standards[](실재 코드만 — 모르면 빈 배열), economy(기존 팩과 동일 블록 복사), items[]`.
- **단원당 30문항**. 모든 문항 `answer_mode:"choice"`, `choices` 정확히 4개, `answer` ∈ choices, 값이 같은 보기 금지, 정답 위치 ①②③④ 고르게(각 7~8개).
- **난도: 쉽게**. difficulty 1 이 약 18개, 2 가 약 10개, 3 이 2개 이하. 한 단계 계산·기본 개념 위주. `items[0]` 은 가장 쉬운 문항.
- 오답 3개는 그 단원의 **흔한 실수**에서(받아올림 누락, 자릿값 착각, 분모끼리 더함, 단위 환산 실수 등) — 무작위 숫자 금지. `distractor_tags` 에 한두 단어로.
- 필드: `id, prompt, choices, answer, answerNumeric(수치면), format("int"|"frac"|"text"), explain(한 줄), unitConcept(교과서 용어), difficulty, distractor_tags`.
- **표기**: 분수는 `{frac:분자/분모}`(세로 분수), 대분수는 `2{frac:1/3}`처럼 정수 바로 뒤에, 거듭제곱은 `<sup>2</sup>`. √ 금지(중1). 단위는 보기에 붙인다(「35 cm」「120°」). 그림이 필요한 문항 금지 — 글로만 풀 수 있게.
- **학년 어휘**: 그 학년이 교과서에서 쓰는 말만. 초등은 짧은 문장(한 문항 70자 이내 권장). 「구하시오」「알맞은 것을 고르시오」.
- CLAUDE.md 「교과서 표현 함정」: 어림은 「올림/버림/반올림하여 ○의 자리까지」만, 이상·이하·초과·미만 경계, 답 형식(기약분수·대분수·소수 몇째 자리) 발문에 명시.

## 검증 (가볍게 — 사용자 지시)
- 계산 문항은 작은 스크립트로 정답을 **다시 계산해** 대조(`ArtSource/packs/check-light-<학년>.mjs`, 정수·분수 정확 연산). 개념 문항은 스스로 한 번 더 읽고 확인.
- 구조 검증: 4지선다·정답 1개·값 중복 없음·정답 위치 분포.
- 봇·게이트·캡처 등 무거운 검증은 하지 않는다. 사용자가 게임에서 직접 검사한다.

## 금지
`index.json`·기존 팩·Unity 소스·README·다른 학년 팩 수정 금지(오케스트레이터가 index 를 합친다). git 커밋 금지.
