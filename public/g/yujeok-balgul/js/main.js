import { Game } from './game.js';

const game = new Game();
game.init().catch((e) => {
  // 초기화 실패는 화면에 알리고(콘솔 오류 대신) 훅은 준비되지 않은 채로 둔다
  const el = document.getElementById('tip');
  if (el) el.textContent = '불러오기에 실패했어요. 새로고침해 주세요. (' + String(e && e.message || e).slice(0, 80) + ')';
  console.warn(e);
});
