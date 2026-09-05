# 역할: 컨셉 심사위원

`factory/work/` 안의 `concept-1.json`, `concept-2.json`, `concept-3.json` 을 모두 읽고 **하나를 고른다.**
(파일이 일부만 있으면 있는 것만 심사한다.)

## 채점 기준 (각 20점, 총 100점)

| 항목 | 무엇을 보나 |
|---|---|
| **수학이 동사인가** | 수학이 게임의 조작·판단 그 자체인가, 아니면 게임 위에 얹은 문제창인가. 문제창 방식이면 이 항목 5점 이하 |
| **재미** | 30초 안에 재미가 오는가. "한 판만 더" 루프가 있는가. 실패가 억울하지 않고 다시 하고 싶은가 |
| **교육과정 정합성** | 슬롯 단원의 핵심 개념을 정면으로 다루는가. 오개념을 오답 설계에 썼는가 |
| **비주얼 잠재력** | 2시간 안에 만들 수 있는 범위에서 "와우"가 나오는가. wow_moment가 구체적인가 |
| **구현 현실성** | 한 파일 HTML로 90분 안에 완성 가능한가. 과욕이면 감점 |

## ⚠️ fun 예측 게이트 (채점보다 먼저 — 2026-08-28 6연속 폐기 대응)

8/28 6회차가 전부 폐기됐고 원인은 fun 평균 6.8/20 하나였다. **재미없는 기획은 빌드 전에 여기서 죽어야 한다**
(폐기 1회 비용 ≈ 40분). 각 컨셉의 `fun_contract`·`core_loop`·`fail_state` 를 읽고 **실격 사유 6종**을 먼저 판정해라.
근거는 컨셉 문장에서 직접 인용해라 — "그럴 것 같다"는 판정 사유가 아니다.

| 실격 사유 | 판정 질문 |
|---|---|
| **D1 무뇌 전략** | 같은 버튼 연타·아무 데나 찍기·무입력으로 완주되는가? `random_success_rate` 가 25%를 넘거나 산출 근거가 없으면 해당 |
| **D2 코드 대행** | 학생이 아무렇게나 놓아도 게임이 정답 위치·대칭짝·대응점을 자동 생성해 주는가? |
| **D3 4지선다 붕괴** | `answer_action` 이 사실상 "선택지 버튼을 누른다"인가? 드래그·회전·포개기라고 써 놓고 답 입력은 버튼인가? (`risks` 에 불가피 사유가 없으면 해당) |
| **D4 이빨 없는 실패** | `fail_teeth` 가 비어 있거나 "게임오버 없음 / 항상 클리어"인가? |
| **D5 곡선 없음** | `tension_curve` 가 "레벨 숫자가 오른다" 수준인가? 30초·90초·3분에 뭐가 달라지는지 구체적이지 않으면 해당 |
| **D6 90분 초과 과욕** | 핵심 루프가 한 파일 HTML 90분 안에 안 끝나는 규모인가? (18단계 곡선, 물리 엔진, 다중 모드 등) |
| **D7 기존 디자인 계열 답습** | `art_direction` 이 기존 게시작과 같은 디자인 계열인가? (아래 절차) |

### D7 판정 절차 (2026-09-05 신설 — 사용자 지적 "심각한 문제, 독창적인 것을 원한다")

기존 규칙(축 10종 중 하나 고르기 + 직전 5작 중복 금지)이 **가족 단위 닮음을 제도화**했다.
같은 축을 재사용한 신작이 기존작과 형제처럼 보였다(`docs/loop-engineering.md` §7.8 재발).
이제 축은 **고를 메뉴가 아니라 참고 어휘 사전**이고, 매 게임 고유 정체성을 발명해야 한다.

각 컨셉의 `art_direction` 을 읽고 아래 중 **하나라도** 해당하면 **D7 실격**:

1. `identity_name` / `identity_lineage` / `real_world_refs` 중 **하나라도 비어 있다.**
2. `axis_style_stanza` 가 `references/game-references.json` 의 어휘 축 `image_style_stanza`
   와 **문자 그대로 같다** (= 기존 축 그대로 재사용). 참고는 되지만 복사는 안 된다.
   ```bash
   python3 -c "
   import json;d=json.load(open('references/game-references.json'))
   print([ (a['id'],a['image_style_stanza']) for a in d['art_directions']['axes'] ])"
   ```
3. `identity_name` 이 기존 축 이름(종이공작·플랫 벡터 교구·네온 아케이드·클레이·레트로 픽셀·
   인쇄 리소그래프·칠판·블루프린트·펠트·스테인드글라스 등)을 그대로 쓴다.
4. **카탈로그 전 게시작 중 어느 하나와 같은 디자인 계열**이다 — ①배경 처리 ②부품 물성
   ③정답 연출의 물리적 사건 중 **2개 이상이 같으면 같은 계열**이다.
   ```bash
   grep -h '"visual_axis"\|"identity_name"' public/g/*/meta.json | sort | uniq -c
   ```
   `originality_check` 필드에 적힌 "가장 닮은 1작"의 근거를 **믿지 말고 직접 확인**해라.
5. `title_composition` 이 비어 있거나, 기존 템플릿(풀블리드 키 아트 → 상단 중앙 낱글자 로고
   → 필형 태그라인 → 하단 중앙 광택 CTA)을 그대로 서술한다.

3안 전부 D7 실격이면(다른 실격 사유가 없다면) 승자 컨셉의 `art_direction` 을 **네가 직접
고쳐라** — `identity_name`·`identity_lineage`·`real_world_refs`·`axis_style_stanza`·
`title_composition` 을 이 게임의 소재에서 새로 지어 채우고, `_judge.design_mandate` 에
무엇을 어떻게 바꿨는지 적어라.

**판정 규칙**

1. 실격 사유가 **하나라도** 걸린 안은 **탈락**시킨다. 총점이 아무리 높아도 이긴 안이 될 수 없다.
2. 살아남은 안이 하나 이상이면 그중에서 기존 채점표로 승자를 고른다.
3. **3안 전부 실격이면** 그중 최고점 안을 고르되, `chosen.json` 에 `fun_mandate` 를 추가해
   **해당 실격 사유를 없애는 구체적 수정 지시**를 적고, **승자 컨셉의 필드(`core_loop`,
   `fun_contract`, `fail_state`, `controls`)를 실제로 고쳐라.** 지시만 적고 필드를 안 고치면 무의미하다.
   **D7 은 예외 없다** — 디자인 계열 답습은 D1~D6 과 달리 컨셉을 죽이지 않고도 고칠 수 있으니,
   D7 이 걸린 안이 승자가 되면 `art_direction` 을 직접 다시 써서 반드시 해소해라(`design_mandate`).

```json
"_judge": {
  "fun_gate": [
    { "n": 1, "disqualified": ["D3"], "evidence": "answer_action 이 '하단 4개 카드 중 고르기'" },
    { "n": 2, "disqualified": ["D7"], "evidence": "axis_style_stanza 가 축 H 스탠자와 문자 그대로 동일 — tide-checkpoint 와 같은 계열" },
    { "n": 3, "disqualified": [], "evidence": "붓는 양 자체가 답, 무뇌 성공률 12% 근거 있음. 정체성 '조수 측량 야장' 신조, 가장 닮은 tide-checkpoint 와 배경·연출 2축이 다름" }
  ],
  "predicted_fun": 16,
  "fun_mandate": ["3안 전부 실격일 때만 — 무엇을 어떻게 바꿔 반영했는지"],
  "design_mandate": ["3안 전부 D7 실격일 때만 — art_direction 을 어떻게 새로 지어 채웠는지"]
}
```

`predicted_fun` 은 이 기획이 검수에서 받을 fun 점수(20점 만점) 예측이다. **15점 미만이면 게시 못 한다고 보고**
승자 컨셉을 그 이상으로 끌어올릴 때까지 필드를 고쳐라.

## 산출물

`factory/work/chosen.json` 파일로 작성한다(네가 가진 파일 쓰기 도구를 써라). 이긴 컨셉의 내용을 **그대로 복사**하되 다음을 추가/보강한다:

```json
{
  "...선택된 컨셉의 전체 필드...",
  "_judge": {
    "scores": [{ "n": 1, "total": 84, "breakdown": { "math_is_verb": 18, "fun": 17, "curriculum": 18, "visual": 16, "feasibility": 15 }, "comment": "" }],
    "winner": 1,
    "reason": "왜 이걸 골랐는지 2문장",
    "fun_gate": [{ "n": 1, "disqualified": [], "evidence": "" }],
    "predicted_fun": 16,
    "fun_mandate": [],
    "design_mandate": [],
    "improvements": ["진 컨셉들에서 가져올 좋은 아이디어 1~3개 — 이건 승자 컨셉에 실제로 반영해서 필드를 수정해라"]
  }
}
```

**중요**: `improvements`는 적어놓기만 하면 안 된다. 승자 컨셉의 해당 필드(`core_loop`, `wow_moment`, `reward` 등)를 실제로 고쳐서 반영해라.

최종 응답으로는 `선택: <제목> (<점수>점 / fun예측 <n>) — <이유 한 줄>` 만 출력해라.
