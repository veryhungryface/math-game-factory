#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
2022 개정 교육과정 성취수준 hwpx -> 구조화 JSON 추출기.

  python3 curriculum/achievement-levels/tools/extract-achievement-levels.py

원본 hwpx 3개가 .orca/drops/ (또는 curriculum/sources/achievement-levels/) 에 있어야 한다.
출처: 교육부 · 한국교육과정평가원. 공공누리 제1유형(출처표시).
"""
import zipfile, sys, json, re, os, hashlib, difflib
import xml.etree.ElementTree as ET

# -*- coding: utf-8 -*-
"""hwpx(성취수준) -> 구조화 JSON"""

def local(t): return t.rsplit('}', 1)[-1]

# --- HWP 수식 script -> 읽을 수 있는 텍스트 --------------------------------
def eq_to_text(s):
    s = s.strip()
    s = re.sub(r'\{\s*([^{}]*?)\s*\}\s*over\s*\{\s*([^{}]*?)\s*\}', r'\1/\2', s)
    s = s.replace('TIMES', '×').replace('div', '÷').replace('cdot', '·')
    s = re.sub(r'\s+', ' ', s)
    return s.strip()

def para_text(p, drop_tables=True):
    out = []
    def walk(e):
        tn = local(e.tag)
        if tn == 'tbl' and drop_tables:
            return
        if tn == 'equation':
            for c in e:
                if local(c.tag) == 'script':
                    out.append(eq_to_text(c.text or ''))
            return
        if tn == 't':
            if e.text: out.append(e.text)
            for c in e:
                walk(c)
                if c.tail: out.append(c.tail)
            return
        if tn in ('lineBreak', 'tab'):
            out.append(' ')
        for c in e:
            walk(c)
    walk(p)
    return ''.join(out)

def find_tables(p):
    res = []
    def walk(e):
        for c in e:
            if local(c.tag) == 'tbl': res.append(c)
            else: walk(c)
    walk(p)
    return res

def blocks_of(el, depth=0):
    blocks = []
    for p in el:
        if local(p.tag) != 'p': continue
        for t in find_tables(p):
            blocks.append(table_block(t, depth))
        txt = clean(para_text(p))
        if txt: blocks.append({'type': 'p', 'text': txt})
    return blocks

def table_block(tbl, depth=0):
    rows = []
    for tr in tbl:
        if local(tr.tag) != 'tr': continue
        row = []
        for tc in tr:
            if local(tc.tag) != 'tc': continue
            addr = (0, 0); span = (1, 1); sub = None
            for ch in tc:
                ln = local(ch.tag)
                if ln == 'cellAddr': addr = (int(ch.get('colAddr')), int(ch.get('rowAddr')))
                elif ln == 'cellSpan': span = (int(ch.get('colSpan')), int(ch.get('rowSpan')))
                elif ln == 'subList': sub = ch
            cb = blocks_of(sub, depth + 1) if sub is not None else []
            txt = '\n'.join(b['text'] for b in cb if b['type'] == 'p')
            row.append({'col': addr[0], 'row': addr[1], 'cspan': span[0], 'rspan': span[1],
                        'text': txt, 'has_table': any(b['type'] == 'table' for b in cb)})
        if row: rows.append(row)
    return {'type': 'table', 'rows': rows}

CTRL = re.compile(r'[\x00-\x08\x0b\x0c\x0e-\x1f]')
def clean(s):
    s = CTRL.sub('', s)
    s = s.replace('　', ' ').replace('\xa0', ' ')
    s = re.sub(r'[ \t]+', ' ', s)
    return s.strip()

def grid(tbl):
    """rowspan/colspan 을 펼친 2차원 배열"""
    rows = tbl['rows']
    nrow = max((c['row'] + c['rspan']) for r in rows for c in r) if rows else 0
    ncol = max((c['col'] + c['cspan']) for r in rows for c in r) if rows else 0
    g = [[None] * ncol for _ in range(nrow)]
    for r in rows:
        for c in r:
            for dr in range(c['rspan']):
                for dc in range(c['cspan']):
                    rr, cc = c['row'] + dr, c['col'] + dc
                    if rr < nrow and cc < ncol:
                        g[rr][cc] = {'text': c['text'], 'origin': (dr == 0 and dc == 0), 'rspan': c['rspan']}
    return g

# --- 문서 파싱 -------------------------------------------------------------
SUBJECTS = {
    '국어': 'korean', '사회': 'social', '도덕': 'moral', '수학': 'math', '과학': 'science',
    '실과': 'practical-arts', '체육': 'pe', '음악': 'music', '미술': 'art', '영어': 'english',
    '바른 생활': 'right-life', '슬기로운 생활': 'wise-life', '즐거운 생활': 'joyful-life',
}
# 내용 요소 머리표: 아래아한글 사제 영역 문자(U+F02B1~) 또는 유니코드 원문자
BULLET = re.compile('^[\\U000F02B0-\\U000F02D0\\u2460-\\u2473\\u3251-\\u325F\\u32B1-\\u32BF\\uE0B1-\\uE0C0]\\s*')

DOMAIN_RE = re.compile(r'^\(\s*(\d+)\s*\)\s*(.+)$')
CODE_RE = re.compile(r'\[([0-9]+[가-힣]+[0-9]{2}-[0-9]{2})\]')

def parse(path, band):
    z = zipfile.ZipFile(path)
    secs = sorted(n for n in z.namelist() if re.match(r'Contents/section\d+\.xml$', n))
    blocks = []
    warnings = []
    for s in secs:
        try:
            root = ET.fromstring(z.read(s))
        except Exception as e:
            warnings.append(f'{s} 파싱 실패: {e}')
            continue
        blocks.extend(blocks_of(root, 0))

    subjects = {}
    cur = None; domain = None; elem = None
    for b in blocks:
        if b['type'] == 'p':
            t = b['text']
            m = DOMAIN_RE.match(t)
            if m and len(t) < 40:
                domain = m.group(2).strip(); elem = None; continue
            if BULLET.match(t) and len(t) < 40:
                elem = BULLET.sub('', t).strip(); continue
            continue
        rows = b['rows']
        # 과목 표지 표: 1행 2열 [번호, 과목명]
        if len(rows) == 1 and len(rows[0]) == 2:
            a, c = rows[0][0]['text'].strip(), rows[0][1]['text'].strip()
            if a.isdigit() and c in SUBJECTS:
                cur = c
                subjects.setdefault(cur, {'standards': [], 'domains': [], 'warnings': []})
                domain = None; elem = None
                continue
        if cur is None: continue
        g = grid(b)
        if not g or not g[0]: continue
        head = [x['text'].strip() if x else '' for x in g[0]]
        joined = ' '.join(head)
        if head[:1] == ['성취기준'] and '성취기준별 성취수준' in joined:
            parse_standard_table(g, subjects[cur], domain, elem)
        elif '영역별 성취수준' in joined:
            if head[:1] == ['영역'] and len(head) >= 4:
                parse_domain_table(g, subjects[cur], domain)
            elif len(head) == 3:
                parse_domain_table3(g, subjects[cur], domain)
            else:
                subjects[cur]['warnings'].append(f'영역별 성취수준 표 형식 미인식: {head}')
    return subjects, warnings

LEVELS = ('A', 'B', 'C', 'D', 'E')

def dotnorm(t):
    return re.sub(r'\s', '', t).replace('\u318d', '\u00b7').replace('\uffda', '\u00b7').replace('\u30fb', '\u00b7')


def pick_domain(cell, heading):
    """영역명은 셀 값을 우선하되, 절 제목이 같은 뜻이면 띄어쓰기가 살아있는 절 제목을 쓴다."""
    c = re.sub(r'\s+', ' ', cell.replace('\n', '')).strip()
    if heading and dotnorm(heading) == dotnorm(c):
        return heading
    return c or heading


def parse_standard_table(g, bucket, domain, elem):
    cur = None
    for r in g[1:]:
        if len(r) < 3 or not r[0]: continue
        std = r[0]['text'].strip()
        lvl = (r[1]['text'] if r[1] else '').strip()
        txt = (r[2]['text'] if r[2] else '').strip()
        if r[0]['origin']:
            m = CODE_RE.search(std)
            code = '[' + m.group(1) + ']' if m else None
            text = CODE_RE.sub('', std, count=1).strip() if m else std
            cur = {'code': code, 'text': text, 'domain': domain, 'content_element': elem, 'levels': {}}
            bucket['standards'].append(cur)
        if cur is None: continue
        if lvl in LEVELS and txt:
            cur['levels'][lvl] = txt
        elif txt:
            bucket['warnings'].append(f'수준 라벨 인식 실패: {cur.get("code")} / {lvl!r}')

def parse_domain_table(g, bucket, domain):
    name = None; cur = None
    for r in g[1:]:
        if len(r) < 4: continue
        dn = (r[0]['text'] if r[0] else '').strip().replace('\n', '')
        lvl = (r[1]['text'] if r[1] else '').strip()
        cat = (r[2]['text'] if r[2] else '').strip()
        txt = (r[3]['text'] if r[3] else '').strip()
        if r[0] and r[0]['origin']:
            cur = {'domain': pick_domain(dn, domain), 'levels': {}}
            bucket['domains'].append(cur)
        if cur is None: continue
        if lvl in LEVELS:
            cur['levels'].setdefault(lvl, {})
            key = re.sub(r'[\s]', '', cat).replace('･', '·').replace('ㆍ', '·')
            if txt: cur['levels'][lvl][key] = txt

def parse_domain_table3(g, bucket, domain):
    """영역명 열이 없는 3열 형태(예: 5~6 실과): 수준 | 범주 | 진술"""
    cur = {'domain': domain, 'levels': {}}
    bucket['domains'].append(cur)
    for r in g[1:]:
        if len(r) < 3: continue
        lvl = (r[0]['text'] if r[0] else '').strip()
        cat = (r[1]['text'] if r[1] else '').strip()
        txt = (r[2]['text'] if r[2] else '').strip()
        if lvl in LEVELS:
            cur['levels'].setdefault(lvl, {})
            key = re.sub(r'\s', '', cat).replace('･', '·').replace('ㆍ', '·')
            if txt: cur['levels'][lvl][key] = txt


# --- 산출물 빌드 ---------------------------------------------------------
ROOT = '/Users/sitpo/math-game-factory'
OUT = os.path.join(ROOT, 'curriculum/achievement-levels')
SRC_DIRS = [os.path.join(ROOT, 'curriculum/sources/achievement-levels'),
            os.path.join(ROOT, '.orca/drops')]


def find_source(fn):
    for d in SRC_DIRS:
        p = os.path.join(d, fn)
        if os.path.exists(p):
            return p
    raise SystemExit(f'원본 hwpx 를 찾을 수 없다: {fn}\n  탐색 경로: ' + ', '.join(SRC_DIRS))

FILES = [
    ('1-2', '★2022 개정 교육과정 교과별 성취수준(초등 1~2학년군).hwpx'),
    ('3-4', '★(초)2022 개정 교육과정에 따른 성취수준(3~4학년군).hwpx'),
    ('5-6', '★(초)2022 개정 교육과정에 따른 성취수준(5~6학년군).hwpx'),
]

def canonical_domain(raw, canon):
    """성취기준 절 제목과 영역별 성취수준 표의 영역명이 원본에서 미묘하게 다른 경우가 있다
    (「이해 영역」/「이해」, 「살아갈까?」/「살아갈까」, 「공동체과의」 오타 등).
    영역별 표의 이름으로 통일하고, 다르면 원문을 domain_as_written 으로 남긴다."""
    if not raw or not canon:
        return raw, None
    if raw in canon:
        return raw, None
    def key(t):
        t = re.sub(r'\s', '', t or '')
        t = t.replace('\u318d', '\u00b7').replace('\uffda', '\u00b7').replace('\u30fb', '\u00b7')
        t = re.sub(r'[?？]$', '', t)
        t = re.sub(r'영역$', '', t)
        return t
    k = key(raw)
    for c in canon:
        if key(c) == k:
            return c, raw
    best = difflib.get_close_matches(k, [key(c) for c in canon], n=1, cutoff=0.9)
    if best:
        for c in canon:
            if key(c) == best[0]:
                return c, raw
    return raw, None


def split_text(t):
    lines = [l.strip() for l in t.split('\n') if l.strip()]
    if not lines: return '', [], None
    head = lines[0]; inquiry = []; note = None
    rest = lines[1:]
    i = 0
    while i < len(rest):
        l = rest[i]
        if l.startswith('※'):
            note = (note + ' ' if note else '') + l.lstrip('※ ').strip()
        elif l == '탐구 활동':
            pass
        elif l.startswith('•') or l.startswith('·'):
            inquiry.append(l.lstrip('•· ').strip())
        else:
            head += ' ' + l
        i += 1
    return head, inquiry, note

def sha(path):
    h = hashlib.sha256()
    with open(path, 'rb') as f:
        for b in iter(lambda: f.read(1 << 20), b''):
            h.update(b)
    return h.hexdigest()

index = {
    'title': '2022 개정 교육과정 성취수준 (초등 전 과목) — 구조화 추출본',
    'levels': ['A', 'B', 'C'],
    'level_meaning': {
        'A': '기대하는 능력에 도달한 정도가 우수한 수준',
        'B': '기대하는 능력에 도달한 정도가 만족스러운 수준',
        'C': '기대하는 능력에 도달한 정도가 기초적인 수준',
    },
    'source': {
        'publisher': '교육부 · 한국교육과정평가원(KICE)',
        'documents': [],
        'license': '공공누리 제1유형(출처표시) — 출처 표시 후 자유 이용',
        'note': '원본 hwpx 는 용량 문제로 저장소에 커밋하지 않는다(curriculum/sources/achievement-levels/, .gitignore 처리).',
    },
    'bands': {},
}

allsubs = {}
for band, fn in FILES:
    path = find_source(fn)
    subs, warns = parse(path, band)
    allsubs[band] = (subs, warns)
    index['source']['documents'].append({
        'band': band, 'filename': fn,
        'bytes': os.path.getsize(path), 'sha256': sha(path),
    })

for band, (subs, warns) in allsubs.items():
    d = os.path.join(OUT, band)
    os.makedirs(d, exist_ok=True)
    entries = []
    for name, v in subs.items():
        slug = SUBJECTS[name]
        canon = [x['domain'] for x in v['domains']]
        stds = []
        for s in v['standards']:
            text, inquiry, note = split_text(s['text'])
            dom, as_written = canonical_domain(s['domain'], canon)
            e = {
                'code': s['code'],
                'domain': dom,
                'content_element': s['content_element'],
                'text': text,
                'levels': {k: s['levels'][k] for k in ('A', 'B', 'C') if k in s['levels']},
            }
            if as_written: e['domain_as_written'] = as_written
            if inquiry: e['inquiry_activities'] = inquiry
            if note: e['note'] = note
            stds.append(e)
        doc = {
            'subject': name,
            'subject_slug': slug,
            'band': band,
            'levels': ['A', 'B', 'C'],
            'source': {
                'publisher': '교육부 · 한국교육과정평가원(KICE)',
                'document': [f for b, f in FILES if b == band][0],
                'license': '공공누리 제1유형(출처표시)',
            },
            'counts': {
                'standards': len(stds),
                'level_statements': sum(len(x['levels']) for x in stds),
                'domains': len(v['domains']),
            },
            'domains': v['domains'],
            'standards': stds,
        }
        with open(os.path.join(d, slug + '.json'), 'w') as f:
            json.dump(doc, f, ensure_ascii=False, indent=1)
            f.write('\n')
        entries.append({'subject': name, 'slug': slug, 'file': f'{band}/{slug}.json',
                        'standards': len(stds), 'domains': len(v['domains'])})
    index['bands'][band] = {'subjects': entries, 'parse_warnings': warns + sum(
        ([f'{n}: {w}' for w in v['warnings']] for n, v in subs.items()), [])}

with open(os.path.join(OUT, 'index.json'), 'w') as f:
    json.dump(index, f, ensure_ascii=False, indent=1)
    f.write('\n')
print('written', OUT)
for b in index['bands']:
    print(b, sum(e['standards'] for e in index['bands'][b]['subjects']), 'standards',
          len(index['bands'][b]['parse_warnings']), 'warnings')
