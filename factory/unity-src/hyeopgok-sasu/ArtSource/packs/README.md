# 협곡 사수 — 문제 팩 v2 제작 안내

팩 JSON만 교체하면 Unity를 다시 빌드하지 않고 단원을 바꿀 수 있습니다. 기본 `m2s2-u7`은 444문항(양 21, 분수 407, 선택 16), `m2s2-u6`은 400문항(전부 양)입니다. 합계 844문항이며, 기존 v1 팩도 `answer_mode`가 없으면 `choice`로 읽습니다.

## 추가 순서

1. 같은 폴더의 팩을 복사해 `pack_id`, 제목, 학교급, 학년, 단원, 성취기준과 문항을 바꿉니다. `pack_id`는 영문 소문자·숫자·하이픈, `file`은 같은 폴더의 상대 파일 이름입니다.
2. `index.json`의 `packs` 배열에 `{"pack_id":"my-unit","title":"새 단원","file":"my-unit.json"}`을 추가합니다. 기본 팩은 `default_pack`으로 정합니다.
3. 아래 구조 검증과 해당 단원에 맞는 **독립 정수·유리수 전수 검산**을 실행합니다. 구조 통과만으로 정답을 보증하지 않습니다.
4. 주소에 `?pack=my-unit`을 붙이거나 타이틀에서 팩을 선택합니다. 새로고침 뒤 제목·문항·입력 모드를 확인합니다.

```bash
node factory/unity-src/hyeopgok-sasu/ArtSource/packs/validate-pack.mjs public/g/hyeopgok-sasu/packs/my-unit.json
```

## 공통 구조

```json
{
  "schema_version": 2,
  "pack_id": "my-unit",
  "title": "확률 연습",
  "school": "middle",
  "grade": 2,
  "semester": 2,
  "unit_id": "m2s2-u7",
  "standards": ["[9수04-06]"],
  "economy": {"carry_capacity":120,"coin_per_kill":1,"min_spawn_coins":140},
  "items": []
}
```

실제 팩에는 고유 발문 300개 이상, 난이도 1~4를 모두 넣습니다. 괄호 설명이나 번호만 다른 문항은 고유 문항으로 세지 않습니다. `items[0]`은 첫 조작 안내이므로 작은 `amount` 답(현재 두 팩은 동전의 모든 경우 2)을 둡니다. 난이도는 1~3번 1단계, 4~6번 2단계, 7~8번 3단계, 9~10번 4단계에서 중복 없이 선택합니다.

각 문항은 고유 `id`, `prompt`, `answer_mode`, `answer`, `answerNumeric`, `format`(`int`/`frac`/`text`), 오답 후 한 줄 `explain`, 교과서 용어 `unitConcept`, `difficulty`(1~4)를 갖습니다. `answerNumeric`는 QA 주석이며 실제 분수 판정에는 쓰지 않습니다. 화면 속 분수는 `{frac:분자/분모}`로 쓰면 세로로 표시됩니다.

## amount — 붓는 코인 수가 답

자연수와 0으로 답하는 문항입니다. 왕이 패드에 올라가 한 닢씩 붓고 걸어 나오면 그 양을 제출합니다. 탭은 한 닢, 오래 서면 가속하며 쏟은 코인은 덜 수 없습니다. 패드를 나온 뒤 0.6초 동안 다시 들어가면 확인을 취소하고 더 부을 수 있습니다.

```json
{
  "id":"addition-001","prompt":"7 + 5를 계산하시오.",
  "answer_mode":"amount","answer":12,"max":60,"coin_budget":60,
  "answerNumeric":12,"choices":null,"format":"int",
  "explain":"7에 5를 더하면 12입니다.","unitConcept":"덧셈","difficulty":1
}
```

초등 덧셈·곱셈, 나머지, 도형 개수와 중학교 경우의 수·넓이 등 정수 답에 씁니다. 이 예시는 문항 형식만 보이며 학교급·단원·성취기준은 해당 교육과정의 실제 항목을 따로 선택해야 합니다. 런타임은 0도 허용합니다. 0을 내려면 빈 패드에서 선택 표시가 켜질 때까지 잠깐 멈춘 뒤, 첫 코인이 붓기 전에 나옵니다. 패드를 스쳐 지나가기만 한 입력은 답으로 확정되지 않습니다.

## fraction_parts — 두 양으로 분수 조립

`den_label` 패드에 분모, `num_label` 패드에 분자를 붓습니다. 두 패드의 양을 확정하면 가운데에 세로 분수가 조립됩니다. `max`는 **각 패드**의 상한이며 분모 0은 항상 오답입니다.

```json
{
  "id":"probability-001",
  "prompt":"1부터 8까지의 자연수가 각각 하나씩 적힌 카드 8장 중 한 장을 임의로 뽑을 때, 3의 배수일 확률을 구하시오.",
  "answer_mode":"fraction_parts","answer":{"num":2,"den":8},
  "accept":"equivalent","num_label":"사건","den_label":"전체",
  "max":60,"coin_budget":120,"answerNumeric":0.25,"choices":null,"format":"frac",
  "explain":"모든 경우 8가지 중 3, 6의 두 가지이므로 {frac:2/8}입니다.",
  "unitConcept":"경우의 수의 비율로서의 확률","difficulty":1
}
```

| accept | 2/8을 답으로 둔 예 | 용도 |
|---|---|---|
| `exact_parts` | 분자 2·분모 8만 정답. 1/4는 오답 | 경우를 **센 양 자체**를 평가. 발문에 약분하지 않는다고 명시 |
| `equivalent` | 1/4, 2/8, 3/12 등 값이 같으면 정답 | 분수의 값·동치, 정수 교차곱 판정 |
| `reduced` | 답을 `{num:1,den:4}`로 저장. 1/4만 정답 | 발문에 **기약분수로** 명시. 교차곱과 최대공약수 1 확인 |

확률 팩의 407개 분수 문항은 `전체`·`사건`의 실제 경우의 수를 답에 보존하고 `equivalent`로 판정합니다. 따라서 2/8과 1/4를 모두 정답으로 받으며, 확정 피드백에서 2/8→1/4 약분을 보여 줍니다. 초등 분수 팩은 라벨을 「분모」「분자」로 바꾸고, 예를 들어 `1/6 + 1/3`을 기약분수로 구하는 답을 `{num:1,den:2}`, `accept:"reduced"`로 만듭니다. 음수 분수는 코인 입력으로 표현하지 못하므로 이 모드로 출제하지 않습니다.

## choice — 개념·성질 또는 v1 호환

```json
{
  "id":"concept-001","prompt":"어떤 확률도 벗어날 수 없는 범위를 고르시오.",
  "answer_mode":"choice","answer":"0 이상 1 이하",
  "choices":["0 이상 1 이하","0보다 크다","1보다 작다","제한이 없다"],
  "format":"text","explain":"확률은 0 이상 1 이하입니다.",
  "unitConcept":"확률의 기본 성질","difficulty":1,
  "distractor_tags":["zero-excluded","one-excluded","probability-range-ignored"]
}
```

지상 4패드 중 하나를 고르는 v1 조작입니다. 보기는 정확히 4개, 정답 문자열이 보기 중 하나와 일치해야 하며 동치 숫자 보기를 함께 넣지 않습니다. 실제 오개념 두 종류 이상을 `distractor_tags`에 기록합니다. 짧은 용어·성질에 쓰고 전체 30% 이하를 권장합니다. 현재 확률 팩의 기본성질 0·1 문항 16개만 사용하여 3.60%입니다.

`answer_mode`와 `schema_version`을 생략한 기존 v1의 문자열 `answer`·4개 `choices`는 그대로 지원합니다. 새 `amount`/`fraction_parts`만 `choices:null`이며, 기존 4지선다 예외는 `choice`에만 적용됩니다.

## 코인 수급과 무뇌 정책

- `max`는 정답에 맞춰 줄이지 않습니다. 현재 두 팩은 **모든 붓기 문항에서 60**으로 고정해 패드 상한이 정답 힌트가 되지 않습니다.
- `coin_budget`도 정답 대신 입력 모드로 고정합니다. 양 60, 분수 120이며 생략 시에도 이 고정값을 사용합니다. 현재 런타임은 `carry_capacity:120`, `coin_per_kill:1`, `min_spawn_coins:140` 프로필만 지원합니다. 이 값은 팩에서 임의로 바꾸는 스폰 지시가 아닙니다. 전투의 지속 적 보충이 최소 140닢을 벌 수 있는 보수적 수급 계약을 충족하며, 상한·예산을 넘는 설정은 로더와 구조 검증기가 거절합니다. 실제 정답 비용 최대는 경우의 수 56닢, 확률 94닢입니다. 시간·스폰 여유는 실제 게임 검증으로 확인해야 합니다.
- 첫 문항도 무료 코인을 주지 않습니다. 처치 코인을 모아 2닢을 부어 첫 탑을 세웁니다. 코인 부족 시 전투에서 더 벌어 옵니다.
- 기존 큰 답의 문항은 작은 파라미터의 같은 개념 문항으로 교체했습니다. 경우의 수는 **2~59 범위**, 현재 최댓값 56이며 항상 1닢·상한까지 붓기의 자동 정답을 막는 콘텐츠 원칙입니다. 런타임은 여전히 0·1·60을 지원하며 별도 경계 fixture에서 확인합니다. 봇 실행 결과에 따라 값을 후조정한 것이 아닙니다.
- 우연 기준: 균등 0~60 입력이면 양 `1/61`, `exact_parts`/`reduced`는 `1/61²`입니다(분모 0 추출은 실패 포함). `equivalent`는 상한 안의 동치 정수쌍 개수/`61²`, `choice`는 `1/4`입니다. 안내 2닢 문항도 팩 전체 통계에 포함합니다.

## 중2 표현과 범위

두 주사위는 「서로 다른 두 개의 주사위」, 공·카드 뽑기는 「임의로」를 씁니다. 공 문장은 「(단, 공의 모양과 크기는 모두 같다.)」로 끝냅니다. 연속 시행은 「확인한 후 다시 넣고」인 복원 추출만 제공합니다. 「또는」은 겹치지 않는 사건을 기본으로 하며, 겹치는 사건은 원소를 직접 나열하는 풀이와 전수 열거로만 다룹니다. 순열·조합 공식과 중2 범위 밖 제곱근 기호를 쓰지 않습니다.

## 재생성과 검증

```bash
node factory/unity-src/hyeopgok-sasu/ArtSource/packs/gen-m2s2-u7.mjs
node factory/unity-src/hyeopgok-sasu/ArtSource/packs/gen-m2s2-u6.mjs
node factory/unity-src/hyeopgok-sasu/ArtSource/packs/verify-packs.mjs
node factory/unity-src/hyeopgok-sasu/ArtSource/packs/verify-schema.mjs
```

생성기는 유한 파라미터 풀을 결정적으로 고르게 뽑으며 기존 두 팩과 `index.json`을 재생성합니다. 새 팩을 추가했다면 목록을 보존하세요. `*-proofs.json`의 원자료·오답 도출 근거는 편집 소스에만 보관합니다. 비선택형에서도 기존 오개념 오답 근거를 보존해 전수 검산합니다.

검산기는 생성기 모듈을 가져오지 않고 표본공간을 별도로 열거합니다. 844문항·28,836개 표본결과·2,532개 오개념 도출·1,514,447개 분수 입력쌍을 확인했고 오류는 0입니다. `verification-report.json`/`.md`에 결과를 남깁니다. 새 단원은 별도의 독립 계산 규칙을 추가해야 합니다. `verify-schema.mjs`는 v1 하위 호환과 v2 잘못된 분모·상한·기약분수·공급 예산을 검증합니다. 실제 WebGL 입력 하위 호환은 `ArtSource/validation/phase2/` 증거를 따릅니다.
