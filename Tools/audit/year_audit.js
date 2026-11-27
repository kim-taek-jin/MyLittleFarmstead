// F-0: one-year pacing audit on the browser preview (same rules as the game).
// Plays a steady, "doing everything" player day by day and prints heart, season and what happened.
//
//   python3 Tools/webpreview/build.py /tmp/preview.html
//   node Tools/audit/year_audit.js /tmp/preview.html 80      (needs: npm i playwright)
//
// Read the "season" column: winter should last until Daeboreum (~12 days), then spring, summer and
// autumn about 20 days each (SeasonPacing.TargetDays). Re-run after adding each season's content.
const { chromium } = require('playwright');
(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });
  const errors = [];
  page.on('pageerror', e => errors.push(e.message));
  await page.goto('file://' + process.argv[2]); await page.waitForTimeout(1500);
  await page.click('#start'); await page.waitForTimeout(300);
  const rows = await page.evaluate((DAYS) => {
    const out = [];
    const flush = () => { for (let i = 0; i < 60; i++) {
      if (panel && panel.kind === 'choice') { const pk = panel.pick; panel = null; pk(0); continue; }
      if (panel && panel.kind === 'view') { const cb = panel.onClose; panel = null; cb && cb(); continue; }
      if (jwibul) { const d = jwibul.done; jwibul = null; d(); continue; }
      if (!dialogue) break;
      if (dialogue.choice) { const ch = dialogue.choice; dialogue = null; ch.yes && ch.yes(); }
      else { dialogue.i++; if (dialogue.i >= dialogue.lines.length) { const d = dialogue.done; dialogue = null; d && d(); } } } };
    const V = id => VILLAGERS.find(v => v.id === id);
    screen = null; announce = null; G.gold = 300;
    for (let day = 1; day <= DAYS; day++) {

      const ev = []; broadcast = null; announce = null; panel = null;
      G.hour = 9;
      // neighbors by day
      for (const v of VILLAGERS) {
        const p = villagerProgress(v.id);
        if (!p.met) { talk(v); flush(); ev.push('meet ' + v.id); }
        const c = vNext(v); if (!c || !vReady(v)) continue;
        if (c.kind === 'Pumasi') { talk(v); flush(); const s = CHORES.find(x => x.id === c.spot); for (let i = 0; i < 3; i++) { G.energy = 100; useChore(s); } flush(); ev.push(v.id + ':pumasi ' + c.spot); }
        else if (c.kind === 'Fish') { talk(v); flush(); onFishCaught(); flush(); ev.push(v.id + ':fish'); }
        else if (c.kind === 'Letter') { letter(v); flush(); ev.push(v.id + ':letter'); }
        else if (c.kind === 'Saecham') {
          if (c.need && !has(c.need)) continue;
          if (!c.need) add('Rice Cake'); saecham(v, c.need || 'Rice Cake', true); flush(); ev.push(v.id + ':saecham'); }
      }
      // jang: buy on day 2, meju, hang, jars, lids, split, stew
      if (day === 2) { add('Soybeans', 8); add('Salt', 4); add('Wood', 6); useAgungi(); flush(); for (let i = 0; i < 2; i++) { remove('Soybeans', 4); remove('Wood', 1); add('Fresh Meju'); } useMejuRack(); flush(); ev.push('meju x2 hung'); }
      if (Jg().hung.some(d => G.day >= d)) { useMejuRack(); flush(); ev.push('meju dried'); }
      if (has('Dried Meju') && has('Salt', 2) && Jg().jars.some(x => x.start < 0)) { useJangdok(); flush(); ev.push('jars filled'); if (!Jg().lidsOpen) useJangdok(); }
      if (Jg().jars.some(x => x.start >= 0 && G.day - x.start >= JANG.ferment)) { useJangdok(); flush(); ev.push('jang split'); }
      if (has('Doenjang') && !has('Doenjang Stew') && vNext(V('rosa'))?.kind === 'Saecham') { remove('Doenjang', 1); add('Doenjang Stew'); ev.push('cook stew'); }
      // dongji
      if (dongjiWindow() && Rt().dongjiPrep < 3 && !Rt().dongjiDone) { for (let i = 0; i < 3; i++) { G.energy = 100; useRitePot(); } flush(); ev.push('dongji prep'); }
      if (G.day === RITE.dongji && !Rt().dongjiDone) { takePot(); flush(); for (const id of ['rosa', 'theo', 'mina', 'mayor']) { riteTalk(V(id)); flush(); } useGate(); flush(); ev.push('dongji done:' + Rt().dongjiDone); }
      // boreum prep + morning
      if (boreumSched() && G.day >= Rt().boreumDay - 2 && !daljipBuilt()) { add('Wood', 5); useDaljip(); flush(); ev.push('daljip built'); }
      if (boreumSched() && G.day >= Rt().boreumDay && !Rt().bureom) { riteTalk(V('mina')); flush(); ev.push('bureom'); }
      // evening
      G.hour = 19;
      if (boreumNight() && daljipBuilt()) { useDaljip(); flush(); ev.push('BOREUM RITE'); }
      else { const mv = VILLAGERS.find(v => vNext(v)?.kind === 'Masil' && vReady(v)) || VILLAGERS[day % 4]; if (S().masilDay !== G.day && villagerProgress(mv.id).met) { masil(mv); flush(); ev.push('masil ' + mv.id); } }
      // night
      G.log.meals = 1; G.log.drank = true; G.hour = 22;
      const before = { heart: Math.round(G.heart), day: G.day };
      endDay(false); flush(); announce = null; broadcast = null;
      out.push({ day: before.day, ev: ev.join(', ').slice(0, 60), heartAfter: Math.round(G.heart), season: S().season, boreum: Rt().boreumDay, restored: VILLAGERS.filter(vRestored).map(v => v.id).join('+'), demoEnd: !!(panel && panel.kind === 'demoEnd') || demoEndPending });
      panel = null; demoEndPending = false;

    }
    return out;
  }, +(process.argv[3] || 80));
  for (const r of rows) console.log(`${String(r.day).padStart(2)} | heart ${r.heartAfter} | season ${r.season} | boreum ${r.boreum} | restored ${r.restored || '-'} | ${r.ev}${r.demoEnd ? ' | DEMO END' : ''}`);
  console.log('errors', JSON.stringify(errors));
  await browser.close();
})();
