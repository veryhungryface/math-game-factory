# 협곡 사수 — 문제 팩 제작 안내

게임을 다시 빌드하지 않고 `public/g/hyeopgok-sasu/packs/`의 JSON 파일만 추가해 단원을 바꿀 수 있습니다. 초기 제공 팩은 중2 2학기 확률(`m2s2-u7`, 기본)과 경우의 수(`m2s2-u6`)이며, 각각 고유 문항 400개입니다. 초등 단원도 같은 형식을 사용합니다.

## 선생님이 새 팩을 넣는 순서

1. `packs/m2s2-u7.json`을 복사하고 파일 이름·`pack_id`·`title`·학교급·학년·학기·단원·성취기준·문항을 바꿉니다. `pack_id`는 영문 소문자·숫자·하이픈만 사용합니다.
2. `packs/index.json`의 `packs` 배열에 `{"pack_id":"my-unit","title":"새 단원","file":"my-unit.json"}`을 추가합니다. 기본 팩도 바꾸려면 `default_pack`을 새 ID로 바꿉니다. `file`은 같은 폴더 안의 상대 파일 이름입니다.
3. 아래 구조 검증 명령을 실행하고, 해당 단원의 생성기와 **별도의 정수·유리수 검산기**로 정답과 모든 오답을 전수 확인합니다. 구조 검증 통과는 수학 검산 통과와 다릅니다.
4. 게임 주소 끝에 `?pack=my-unit`을 붙이거나 타이틀의 팩 선택을 이용합니다. 페이지를 새로 열어 제목과 실제 출제 문항이 바뀌었는지 확인합니다. 변경된 JSON을 서버에 반영하면 Unity 재빌드는 필요 없습니다.

```bash
node factory/unity-src/hyeopgok-sasu/ArtSource/packs/validate-pack.mjs public/g/hyeopgok-sasu/packs/my-unit.json
```

## 팩 형식

아래는 **문항 하나의 형식 예시**입니다. 실제 팩에는 고유 문항 **300개 이상**을 넣습니다(설계 최소 120보다 Unity QA 문제은행의 300개 요구가 더 큽니다). 같은 문제에 괄호 설명이나 번호만 붙인 것은 고유 문항으로 세지 않습니다.

```json
{
  "pack_id": "my-unit",
  "title": "확률 연습",
  "school": "middle",
  "grade": 2,
  "semester": 2,
  "unit_id": "m2s2-u7",
  "standards": ["[9수04-06]"],
  "items": [{
    "id": "my-unit-001",
    "prompt": "1부터 8까지의 자연수가 각각 하나씩 적힌 카드 8장 중 한 장을 임의로 뽑을 때, 3의 배수일 확률을 기약분수로 구하시오.",
    "choices": ["{frac:1/2}", "{frac:1/4}", "{frac:3/4}", "{frac:1/8}"],
    "answer": "{frac:1/4}",
    "answerNumeric": 0.25,
    "format": "frac",
    "explain": "3의 배수는 3, 6의 두 장이므로 {frac:2/8} = {frac:1/4}.",
    "unitConcept": "경우의 수의 비율로서의 확률",
    "distractor_tags": ["equal-likelihood-bias", "complement-confusion", "event-count-omitted"],
    "difficulty": 1
  }]
}
```

- `school`: `middle` 또는 `elementary`. `grade`는 학교급 안의 학년입니다. `standards`는 해당 교육과정 파일에 실재하는 코드여야 합니다.
- `id`는 팩 안에서 유일하게, `choices`는 **정확히 4개**로 만듭니다. `answer`는 보기 하나와 글자까지 같아야 합니다. 같은 수를 나타내는 보기(예: 약분 전후 분수)를 함께 넣지 않습니다.
- 분수는 모든 화면 문구에서 `{frac:분자/분모}` 토큰을 쓰면 세로 분수로 표시됩니다. 답·보기는 기약분수여야 합니다. 정수는 `"3"`처럼 씁니다. `format`은 `frac`, `int`, `text` 중 하나입니다.
- `answerNumeric`는 QA를 위한 수치 주석입니다. 정오 판정은 `answer`로 하므로 확률의 소수 근삿값을 판정에 사용하지 않습니다. 글자 답(`text`)에는 `answerNumeric`를 생략할 수 있습니다.
- `explain`은 틀린 뒤에 표시되는 짧은 풀이입니다. `unitConcept`에는 교과서 용어를 씁니다. 오답 세 개 중 적어도 두 개는 **서로 다른 실제 오개념을 역산한 값**이어야 하고 `distractor_tags`에 근거 이름을 남깁니다. 우연히 정답과 같아지는 파라미터는 제외합니다.
- `difficulty`는 1~4입니다. `items[0]`은 첫 출격 문제이므로 바로 풀 수 있는 1단계로 둡니다. 진행 난도는 1~3번이 1단계, 4~6번은 2단계, 7~8번은 3단계, 9~10번은 4단계에서 중복 없이 고릅니다. 모든 단계를 충분히 넣습니다.
- 글자 보기는 지상 패드 안에서 읽도록 짧은 교과서 용어로 씁니다(권장 10자 이하). 긴 설명은 `prompt`와 `explain`에 넣습니다.
- 패드 색·크기·왕과의 거리로 정답이 드러나지 않도록 하고 보기의 정답 위치는 균등하게 섞습니다.

## 중2 표현과 범위

- 기본 발문은 「구하시오」입니다. 확률의 답 형식은 「기약분수로」 명시합니다.
- 두 주사위는 「서로 다른 두 개의 주사위」로 구별합니다.
- 공·카드 뽑기에는 「임의로」를 넣습니다. 공 문제는 「(단, 공의 모양과 크기는 모두 같다.)」로 끝냅니다.
- 연속 시행은 「확인한 후 다시 넣고」인 복원 추출만 제공합니다.
- 「또는」은 겹치지 않는 두 사건에만 사용합니다. 현재 팩은 서로 다른 음료 분류 또는 공 색입니다.
- 경우의 수는 두 단계 합·곱과 직접 순서쌍 세기에 한정합니다. 순열·조합 기호와 제곱근 기호는 사용하지 않습니다.

## 초기 팩 재생성과 독립 전수 검증

저장소 루트에서 실행합니다. 생성기는 위 두 초기 JSON과 목록을 재생성하므로, 새 팩을 추가한 뒤에는 `index.json` 목록을 보존해 두세요.

```bash
node factory/unity-src/hyeopgok-sasu/ArtSource/packs/gen-m2s2-u7.mjs
node factory/unity-src/hyeopgok-sasu/ArtSource/packs/gen-m2s2-u6.mjs
node factory/unity-src/hyeopgok-sasu/ArtSource/packs/verify-packs.mjs
```

- 생성 과정은 무작위 재시도 없이 유한 파라미터 풀에서 고르게 선택합니다. 유리수는 분자·분모 정수로 약분합니다.
- `*-proofs.json`은 각 문항의 원자료와 오답 도출 규칙을 기록합니다. Unity와 공개 팩에는 포함하지 않습니다.
- `verify-packs.mjs`는 생성 모듈을 가져오지 않고 공·카드·주사위·선택의 표본공간을 **독립적으로 직접 열거**합니다. 예상 정답 필드도 검증 대상이며 계산 입력으로 쓰지 않습니다.
- 검증 항목: 전체 800문항 정답, 3,200개 보기의 정답 유일성·동치 중복·기약분수, 2,400개 오답의 오개념 도출, 교과서 표현, 난이도별 정답 위치 균형. 결과는 `verification-report.json`과 `verification-report.md`에 기록됩니다.
- 새 단원 팩을 전수 검산하려면 그 단원의 독립 계산 규칙과 증명 원자료를 추가해야 합니다. 기존 `verify-packs.mjs`는 제공된 두 단원의 검산기입니다.

## 오개념 태그 근거

교육과정 정본 `curriculum/2022-middle-math.json`의 해당 단원 `misconceptions`를 기준으로, 세부 오류를 아래처럼 나누었습니다.

| 태그 계열 | 교과서 오개념과 도출 방식 |
|---|---|
| `equal-likelihood-bias`, `order-ignored` | 결과를 임의로 두 종류로 묶어 확률을 절반으로 답하거나, 주사위 순서를 없애 21개의 동등하지 않은 결과로 셈 |
| `ratio-reversed`, `denominator-omitted`, `wrong-sample-space` | 확률의 전체 경우 수를 빼거나 뒤집음, 한 주사위의 6을 분모로 씀, 성공 수를 전체에 다시 더함 |
| `event-count-omitted`, `event-complement-confusion` | 여러 성공 경우를 한 경우로 세거나 목표 사건과 여사건을 혼동 |
| `frequency-equals-theory` | 실험의 상대도수를 이론값 절반으로 무조건 바꿈 |
| `sum-product-confusion` | 동시에 고르는 두 단계를 더하거나, 겹치지 않는 두 사건을 곱함 |
| `first/second-stage-omitted`, `first/second-event-omitted` | 합·곱의 법칙에서 한 단계 또는 한 사건만 셈 |
| `leading-zero`, `same-card-reused` | 두 자리 자연수의 맨 앞 0을 허용하거나 한 장의 카드를 두 번 사용 |
| `roles-order-confusion`, `same-person-twice` | 대표와 회장·부회장의 구별을 혼동하거나 한 사람이 두 역할을 맡음 |
| `order-and-event-ignored`, `event-condition-ignored` | 순서 또는 주어진 사건 조건을 무시하고 모든 결과를 답함 |

일부 확률 오답은 의도적으로 1보다 큽니다. 「확률의 분모 생략·역수」라는 교육과정의 실제 오개념을 시험하기 위한 값이며 정답은 항상 0 이상 1 이하입니다.
