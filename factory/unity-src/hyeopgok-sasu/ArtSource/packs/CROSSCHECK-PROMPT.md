너는 한국 중학교 수학 교사 겸 문항 검토 위원이다. 게임 「협곡 사수」의 문제 팩 13종(`public/g/hyeopgok-sasu/packs/*.json`, 목록 `index.json`)을 **교차 검토**한다. 팩은 다른 회사 모델이 만들었고, 계산 정답은 독립 검산기가 전수 확인했다. 너는 **사람 교사의 눈**으로 본다.

읽을 것: `public/g/hyeopgok-sasu/packs/README.md`(스키마·answer_mode·accept 의미), `curriculum/2022-middle-math.json`(최상단 style_guide·expression_traps, 각 단원), 각 팩 JSON.

각 팩에서 **난이도별로 고르게 30문항**(총 390문항)을 뽑아 하나씩:
1. 정답을 직접 다시 풀어라(틀리면 error).
2. 발문이 두 가지로 해석되거나 **정답이 여럿 가능한가**(특히 fraction_parts 의 accept 가 exact_parts 인데 다른 옳은 셈법이 있는가, amount 인데 단위·답 형식이 불명확한가).
3. 교과서 표현·용어(중2 수준, √ 금지, 「가정/결론」 금지, 확률 발문 「임의로」「서로 다른」「주머니」, 답 형식 명시).
4. 학년 범위 이탈(중3 내용 등).
5. choice 오답이 실제 오개념인가, 정답과 값이 같은 보기가 없는가.

산출: `factory/unity-src/hyeopgok-sasu/ArtSource/packs/crosscheck-grok.json` —
`{"reviewed": 390, "by_pack": {"m2s1-u1": {"checked":30,"errors":[...],"ambiguous":[...],"phrasing":[...]}, ...}, "verdict": "pass|fail", "summary": "..."}`
각 지적은 `{"id","issue","severity":"high|medium|low","fix"}`. 계산 오류나 정답 복수 가능은 high. 파일만 쓰고 다른 파일은 수정하지 마라.
