// 3차 수정 정적 검산: lockedThis 리셋, Area asked=target=AG, FindEc 이등변 기각, Three 발문.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const dir = path.dirname(fileURLToPath(import.meta.url));
const srcDir = path.resolve(dir, '../Scripts');
const game = fs.readFileSync(path.join(srcDir, 'GitNanwoGame.cs'), 'utf8');
const rules = fs.readFileSync(path.join(srcDir, 'GitRules.cs'), 'utf8');
const ui = fs.readFileSync(path.join(srcDir, 'GitUi.cs'), 'utf8');

const cases = [];
function check(name, ok, detail) { cases.push({ name, ok, detail }); }

const startFn = game.slice(game.indexOf('void StartRun'), game.indexOf('void EndPractice'));
check('StartRun clears lockedThis', /lockedThis\s*=\s*false/.test(startFn), startFn.includes('lockedThis = false') ? 'yes' : 'missing');
check('StartRun Practice keeps osc (tickVis = ad0, oscDir = 1)', /tickVis\s*=\s*cur\.ad0/.test(startFn) && /oscDir\s*=\s*1/.test(startFn), 'tickVis/oscDir');
check('StartRun Practice ShowGhost', /ShowGhost\(cur\.target\)/.test(startFn), 'ShowGhost on practice entry');
check('EndRun clears lockedThis', /void EndRun[\s\S]*?lockedThis\s*=\s*false/.test(game), 'EndRun unlock');

check('Area prompt asks AG not GBC area',
  rules.includes('중선 AD의 길이가 " + Cm(s.median) + "일 때, AG의 길이를 구하시오.')
  && !/Area:[\s\S]{0,200}△GBC의 넓이를 구하시오/.test(rules),
  'Area prompt');
check('Area OkSheet asked==target cm',
  /kind == Kind\.Area[\s\S]{0,280}asked != s\.target[\s\S]{0,80}askedUnit != "cm"/.test(rules),
  'OkSheet Area');
check('Area generator asked = target', /s\.kind = Kind\.Area[\s\S]{0,220}s\.asked = s\.target/.test(rules), 'Area()');
check('Goal does not spoil 2:1 cell', !rules.includes('2:1 칸에서 눌러라'), 'Goal');

check('FindEc OkSheet rejects isosceles',
  /Kind\.FindEc && s\.stage > 0 && \(s\.ab == s\.ac \|\| s\.ae == s\.target\)/.test(rules),
  'OkSheet FindEc');
check('FindEcAny skips ab==ac and ae==ad',
  /Sheet FindEcAny[\s\S]{0,400}if \(ac == ab\) continue[\s\S]{0,200}ae == ad/.test(rules),
  'FindEcAny');

check('Three prompt names the other line',
  rules.includes('그 직선에서 m과 n 사이의 길이를 구하시오'),
  'Three prompt');

check('Mobile prompt card compact',
  ui.includes('sizeDelta = new Vector2(320f, 56f)') && ui.includes('enableAutoSizing = true'),
  'portrait prompt 2-line autosize');

const failed = cases.filter(c => !c.ok);
console.log(cases.map(c => (c.ok ? 'OK  ' : 'FAIL') + ' ' + c.name + ' — ' + c.detail).join('\n'));
console.log(failed.length ? `\nFAILED ${failed.length}` : `\nALL ${cases.length} PASSED`);
process.exit(failed.length ? 1 : 0);
