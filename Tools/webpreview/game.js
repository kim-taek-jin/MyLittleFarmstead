// My Little Farmstead - browser preview.
// A JavaScript port of the Unity game's rules (ItemDB, CropDB, DataManager, villagers, spirit,
// seasons of the heart, album...) using the same pixel art. Coordinates are Unity world units.
'use strict';

const ASSETS = /*__ASSETS__*/{};
const IMG = {};
const CW = 1148, CH = 656;
const worldCanvas = document.getElementById('world');
const uiCanvas = document.getElementById('ui');
const wctx = worldCanvas.getContext('2d');
const ctx = uiCanvas.getContext('2d');
wctx.imageSmoothingEnabled = false;
ctx.imageSmoothingEnabled = false;
const FONT = '"Pixelify Sans", "Courier New", monospace';

// ------------------------------------------------------------------ data
const SEASONS = ['Winter', 'Spring', 'Summer', 'Autumn'];
const SEASON_COLOR = ['#c8d8ff', '#ffb6d0', '#9dff9d', '#ffc46a'];
const THRESH = [0, 30, 60, 85];
const ARRIVAL = [
  'The world is quiet and gray.',
  'The snow is melting. Little green shoots push through the ground. You can finally breathe again.',
  'Long, bright days. The fields hum with life, and so do you.',
  "Golden light over everything you've grown. This is what fruition feels like.",
];

const ITEMS = {};
const ORDER = [];
function reg(name, cat, o = {}) { ITEMS[name] = Object.assign({ name, cat, sell: 0, hunger: 0, thirst: 0, heart: 0 }, o); ORDER.push(name); }
reg('Hoe', 'Tool', { tool: 'hoe', tier: 1, desc: 'Till dirt into farmland.' });
reg('Fertile Hoe', 'Tool', { tool: 'hoe', tier: 2, desc: 'Tills 3 tiles at once.' });
reg('Watering Can', 'Tool', { tool: 'can', tier: 1, cap: 10, desc: 'Waters crops. Refill at the well.' });
reg('Copper Watering Can', 'Tool', { tool: 'can', tier: 2, cap: 25, desc: 'Waters 3 tiles at once.' });
reg('Scythe', 'Tool', { tool: 'scythe', desc: 'Harvests ripe crops around the target.' });
reg('Fishing Rod', 'Tool', { tool: 'rod', desc: 'Press E at the farm pond to fish.' });
reg('Carrot Seed', 'Seed', { sell: 5, crop: 'carrot' });
reg('Potato Seed', 'Seed', { sell: 15, crop: 'potato' });
reg('Corn Seed', 'Seed', { sell: 12, crop: 'corn' });
reg('Strawberry Seed', 'Seed', { sell: 20, crop: 'strawberry' });
reg('Sunflower Seed', 'Seed', { sell: 15, crop: 'sunflower' });
reg('Pumpkin Seed', 'Seed', { sell: 20, crop: 'pumpkin' });
reg('Carrot', 'Crop', { sell: 25, hunger: 10 });
reg('Potato', 'Crop', { sell: 80, hunger: 8 });
reg('Corn', 'Crop', { sell: 50, hunger: 12 });
reg('Strawberry', 'Crop', { sell: 120, hunger: 6, thirst: 4, heart: 1 });
reg('Sunflower', 'Crop', { sell: 90 });
reg('Pumpkin', 'Crop', { sell: 160, hunger: 10 });
reg('Apple', 'Food', { sell: 8, hunger: 20, thirst: 5 });
reg('Bread', 'Food', { sell: 10, hunger: 40 });
reg('Roasted Carrot', 'Food', { sell: 60, hunger: 35, heart: 2 });
reg('Baked Potato', 'Food', { sell: 110, hunger: 50, heart: 2 });
reg('Corn Soup', 'Food', { sell: 130, hunger: 30, thirst: 35, heart: 3 });
reg('Strawberry Jam', 'Food', { sell: 420, hunger: 20, heart: 6 });
reg('Grilled Fish', 'Food', { sell: 90, hunger: 40, heart: 2 });
reg('Carrot Cake', 'Food', { sell: 250, hunger: 30, heart: 6 });
reg('Pumpkin Pie', 'Food', { sell: 320, hunger: 45, heart: 4 });
reg('Minnow', 'Fish', { sell: 15 });
reg('Carp', 'Fish', { sell: 45 });
reg('Golden Koi', 'Fish', { sell: 300 });
reg('Wood', 'Resource', { sell: 2 });
reg('Stone', 'Resource', { sell: 5 });
reg('Old Boot', 'Resource', { sell: 1 });
reg('Sunshine Shard', 'Special', { sell: 100, desc: 'Use on a crop: it ripens golden.' });
reg("Earth's Breath", 'Special', { sell: 100, desc: 'Use on a crop: it grows by one day.' });
reg('Star Fragment', 'Special', { sell: 50, desc: 'Your spirit loves it (all four colors).' });
reg('Campfire', 'Station', { sell: 20, desc: 'Needed for cooking recipes.' });
// the forest (map stage 3)
reg('Mattock', 'Tool', { tool: 'mattock', desc: 'Digs out stumps, boulders and fallen logs.' });
reg('Acorn', 'Seed', { desc: 'Plant it in a forest clearing and water it for a few days.' });
reg('Wild Garlic', 'Food', { sell: 20, hunger: 6 });
reg('Fern Sprout', 'Food', { sell: 25, hunger: 8 });
reg('Morel', 'Food', { sell: 60, hunger: 10 });
reg('Spring Greens', 'Food', { sell: 90, hunger: 25, heart: 2 });
reg('Morel Soup', 'Food', { sell: 220, hunger: 35, thirst: 20, heart: 4 });
// the lake (map stage 4-1)
reg('Bluegill', 'Fish', { sell: 35 });
reg('Moonfin', 'Fish', { sell: 110 });
reg('Glassfish', 'Fish', { sell: 240 });
reg('Wild Raspberry', 'Food', { sell: 30, hunger: 5, thirst: 3, heart: 1 });
reg('Watercress', 'Food', { sell: 25, hunger: 6, thirst: 4 });
// Sunset Hill (map stage 4-2)
reg('Pear', 'Food', { sell: 70, hunger: 12, thirst: 8 });
reg('Persimmon', 'Food', { sell: 60, hunger: 12, desc: 'Hang it on the drying rack on Sunset Hill to make dried persimmons.' });
reg('Dried Persimmon', 'Food', { sell: 260, hunger: 18, heart: 3, desc: 'Chewy and sweet, dried slowly in the autumn wind.' });
const iconKey = n => 'Items/' + n.toLowerCase().replace(/'/g, '').replace(/ /g, '_');
const edible = n => { const d = ITEMS[n]; return d && d.cat !== 'Tool' && d.cat !== 'Seed' && (d.hunger > 0 || d.thirst > 0 || d.heart > 0); };
const canSell = n => { const d = ITEMS[n]; return d && d.cat !== 'Tool' && d.sell > 0; };
const inHotbar = n => ['Tool', 'Seed', 'Food', 'Crop', 'Special'].includes((ITEMS[n] || {}).cat);
const sortKey = n => ({ Tool: 0, Seed: 1, Food: 2, Crop: 3, Special: 4 }[ITEMS[n].cat] ?? 5) * 1000 + ORDER.indexOf(n);

const CROPS = {
  carrot: { harvest: 'Carrot', days: 3, regrow: 0, season: 0 },
  potato: { harvest: 'Potato', days: 4, regrow: 0, season: 0 },
  strawberry: { harvest: 'Strawberry', days: 6, regrow: 3, season: 1 },
  corn: { harvest: 'Corn', days: 5, regrow: 3, season: 2 },
  sunflower: { harvest: 'Sunflower', days: 5, regrow: 0, season: 2 },
  pumpkin: { harvest: 'Pumpkin', days: 6, regrow: 0, season: 3 },
};

const SHOP = [['Wood', 5], ['Carrot Seed', 10], ['Potato Seed', 40], ['Strawberry Seed', 50], ['Corn Seed', 30], ['Campfire', 50], ['Bread', 20], ['Apple', 12]];

const RECIPES = [
  { out: 'Campfire', n: 1, req: [['Wood', 5], ['Stone', 2]] },
  { out: 'Fertile Hoe', n: 1, req: [['Hoe', 1], ["Earth's Breath", 1]] },
  { out: 'Copper Watering Can', n: 1, req: [['Watering Can', 1], ['Stone', 10], ['Wood', 5]], lock: 'recipe_copper_can' },
  { out: 'Mattock', n: 1, req: [['Wood', 5], ['Stone', 5]] },
  { out: 'Spring Greens', n: 1, req: [['Wild Garlic', 1], ['Fern Sprout', 1]], lock: 'forest_open' },
  { out: 'Morel Soup', n: 1, req: [['Morel', 2], ['Wild Garlic', 1]], station: 'Campfire', lock: 'forest_open' },
  { out: 'Roasted Carrot', n: 1, req: [['Carrot', 2], ['Wood', 1]], station: 'Campfire' },
  { out: 'Baked Potato', n: 1, req: [['Potato', 1], ['Wood', 1]], station: 'Campfire' },
  { out: 'Corn Soup', n: 1, req: [['Corn', 2]], station: 'Campfire' },
  { out: 'Strawberry Jam', n: 1, req: [['Strawberry', 3]], station: 'Campfire' },
  { out: 'Carrot Cake', n: 1, req: [['Carrot', 2], ['Bread', 1]], station: 'Campfire', lock: 'recipe_carrot_cake' },
  { out: 'Grilled Fish', n: 1, req: [['Minnow', 1], ['Wood', 1]], station: 'Campfire', lock: 'fishing' },
  { out: 'Grilled Fish', n: 2, req: [['Carp', 1], ['Wood', 1]], station: 'Campfire', lock: 'fishing' },
  { out: 'Pumpkin Pie', n: 1, req: [['Pumpkin', 1], ['Bread', 1]], station: 'Campfire', lock: 'season_autumn' },
];

const VILLAGERS = [
  { id: 'rosa', name: 'Grandma Rosa', scene: 'market', x: -6.0, y: 1.0,
    intro: 'Oh, a new face! Forgive me, dear... Since my husband passed, the colors just faded away. Even my bread tastes gray.',
    gray: ['The oven feels so cold these days.', 'I used to bake every morning. Now I just... sit.'],
    color: ['Come by anytime, dear! The bread\'s still warm.', 'You look tired. Did you eat today? Eat, eat!'],
    season: ["Winters are long when you're alone. Come in, warm your hands.", 'Smell that? Spring always makes me want to bake something sweet.', 'Summer! The bread rises so fast I can barely keep up.', 'Autumn was his favorite. Golden, just like a good crust.'],
    restored: 'I can see the colors again... Thank you, dear. Truly.',
    wishes: [
      { items: [['Carrot', 3]], req: 'Would you bring me 3 Carrots? I\'d like to bake something sweet again.', thanks: 'These are lovely! Here, some bread for you. And let me teach you my Carrot Cake.', heart: 3, reward: [['Bread', 3]], unlock: 'recipe_carrot_cake', unlockText: 'New recipe: Carrot Cake' },
      { items: [['Corn', 2], ['Apple', 2]], req: 'The children loved my corn muffins. I\'d need 2 Corn and 2 Apples.', thanks: 'Mmm, the whole market smells like muffins again! Take this for your trouble.', heart: 3, gold: 150 },
      { items: [['Strawberry Jam', 1]], req: 'Strawberry jam... he made it every summer. Could you make some? I\'d like to taste it once more.', thanks: '...It tastes just like his. Here, take these cakes.', heart: 8, gold: 300, reward: [['Carrot Cake', 2]] },
    ] },
  { id: 'theo', name: 'Theo the Smith', scene: 'market', x: -1.4, y: 1.0,
    intro: 'Hmph. The forge has been cold for a year. Nothing worth making anymore.',
    gray: ['Hmph.', "Hammer's heavier than it used to be."],
    color: ["Fire's roaring today. Feels good.", "Bring me good stone and I'll make you good tools."],
    season: ['Cold hands, cold forge. Same thing every winter.', "Snow's melting off the roof. Guess I'll fix the gutters.", 'Too hot to work the forge at noon. I work at dawn now.', "Autumn's for good tools. Fields need them before the frost."],
    restored: 'Listen to that. The forge is singing again.',
    wishes: [
      { items: [['Wood', 10]], req: "If you want to be useful, bring 10 Wood. Maybe I'll light the forge.", thanks: "...Fire's going. Here, I'll show you how to make a copper watering can.", heart: 2, unlock: 'recipe_copper_can', unlockText: 'New recipe: Copper Watering Can' },
      { items: [['Stone', 15]], req: "Chimney's cracked. Need 15 Stone to rebuild it.", thanks: 'Solid stone. Here, I made this with the first good fire in a year.', heart: 3, gold: 200, reward: [['Fertile Hoe', 1]] },
      { items: [['Sunshine Shard', 1], ["Earth's Breath", 1]], req: "My daughter used to say the forge sings when nature's spirit touches it. Bring me a Sunshine Shard and an Earth's Breath.", thanks: "...She was right. You hear that? Here. Don't argue, just take it.", heart: 8, gold: 500 },
    ] },
  { id: 'mina', name: 'Mina', scene: 'farm', x: 5.0, y: -2.4,
    intro: "...Hi. I'm not supposed to talk to strangers. Everything here is boring. Everything's gray.",
    gray: ['The pond is boring. The fish are boring.', 'Grandpa just sleeps all day now.'],
    color: ['Did you catch anything big? Show me!', 'Grandpa smiled today! A real smile!'],
    season: ["The pond froze! ...It's still boring though.", 'Look, the frogs are back! Spring is the best!', 'Summer means fishing every day! Grandpa says so!', 'The leaves are crunchy! Listen! *crunch crunch*'],
    restored: 'Everything looks so bright now! Is this what you see all the time?',
    wishes: [
      { items: [['Apple', 2]], req: "If you give me 2 Apples, I'll give you Grandpa's old fishing rod.", thanks: 'Deal! Here! You can fish in the pond. Stand close and press E!', heart: 2, reward: [['Fishing Rod', 1]], unlock: 'fishing', unlockText: 'Fishing unlocked! New recipe: Grilled Fish' },
      { items: [['Carp', 1]], req: 'Can you catch a Carp? A big orange one. I want to show Grandpa!', thanks: "Woah, it's huge! Here, I found these seeds in his shed.", heart: 3, reward: [['Strawberry Seed', 3]] },
      { items: [['Sunshine Shard', 1], ['Strawberry', 2]], req: "Bring me a Sunshine Shard and 2 Strawberries and I'll show you something!", thanks: 'Look! Sunflower seeds! And the market will sell them now too!', heart: 8, reward: [['Sunflower Seed', 5]], unlock: 'shop_sunflower', unlockText: 'Sunflower Seeds are now sold at the market' },
    ] },
];

const SPIRIT_FOOD = {
  'Sunshine Shard': [3, 0, 0, 0], Sunflower: [2, 0, 0, 0], Corn: [1, 0, 0, 0], "Earth's Breath": [0, 3, 0, 0],
  Carrot: [0, 1, 0, 0], Potato: [0, 1, 0, 0], Pumpkin: [0, 2, 0, 0], Minnow: [0, 0, 1, 0], Carp: [0, 0, 2, 0],
  'Golden Koi': [0, 0, 5, 0], 'Grilled Fish': [0, 0, 2, 0], Apple: [0, 0, 0, 1], Strawberry: [0, 0, 0, 1],
  'Strawberry Jam': [0, 0, 0, 3], 'Carrot Cake': [0, 1, 0, 2], 'Pumpkin Pie': [0, 1, 0, 2], 'Star Fragment': [1, 1, 1, 1],
  'Wild Garlic': [0, 0, 0, 1], 'Fern Sprout': [0, 1, 0, 0], Morel: [0, 2, 0, 0], 'Spring Greens': [0, 1, 0, 1], 'Morel Soup': [0, 2, 1, 0],
  Bluegill: [0, 0, 2, 0], Moonfin: [0, 0, 3, 0], Glassfish: [0, 0, 4, 0], 'Wild Raspberry': [0, 0, 0, 1], Watercress: [0, 0, 1, 0],
  Pear: [0, 0, 1, 1], Persimmon: [1, 0, 0, 1], 'Dried Persimmon': [2, 1, 0, 2],
};
const COLORS = ['sun', 'earth', 'water', 'bloom'];
const FORM_NAMES = { spirit_gray: 'Gray Wisp', spirit_prism: 'Prism Spirit', spirit_sun_young: 'Sunny Sprite', spirit_sun_grown: 'Sun Spirit', spirit_earth_young: 'Sprout Sprite', spirit_earth_grown: 'Grove Spirit', spirit_water_young: 'Droplet Sprite', spirit_water_grown: 'River Spirit', spirit_bloom_young: 'Petal Sprite', spirit_bloom_grown: 'Blossom Spirit' };

const PHOTOS = [
  [0, 'w1', 'First Light', 'Harvest your first Carrot.', 'harvest', 'Carrot', 'Items/carrot'],
  [0, 'w2', 'A Warm Bowl', 'Eat something you cooked.', 'eat_cooked', null, 'Items/roasted_carrot'],
  [0, 'w3', 'Early Night', 'Go to bed before midnight.', 'sleep_early', null, 'World/bed'],
  [0, 'w4', 'A Gray Friend', 'Meet a little spirit.', 'spirit_found', null, 'Spirit/spirit_gray'],
  [0, 'w5', 'Cold Water', 'Drink at the well.', 'drink', null, 'World/well_winter'],
  [0, 'w6', 'Hello, Neighbor', 'Talk to a villager.', 'villager_met', null, 'Villagers/rosa_0'],
  [1, 's1', 'Strawberry Season', 'Harvest a Strawberry.', 'harvest', 'Strawberry', 'Items/strawberry'],
  [1, 's2', 'A Little Color', "Grant a villager's wish.", 'wish', null, 'Villagers/mina_1'],
  [1, 's3', 'Rainy Day', 'Wake up to spring rain.', 'rain_morning', null, 'Tiles/tilled_wet'],
  [1, 's4', 'First Catch', 'Catch a fish.', 'fish', null, 'Items/minnow'],
  [1, 's5', 'Sharing', 'Share food with the village.', 'share', null, 'World/donation_box'],
  [1, 's6', 'Growing Up', 'Your spirit grows up a little.', 'spirit_young', null, 'Spirit/spirit_bloom_young'],
  [2, 'u1', 'Sunflower Smile', 'Harvest a Sunflower.', 'harvest', 'Sunflower', 'Items/sunflower'],
  [2, 'u2', 'Tall Corn', 'Harvest Corn.', 'harvest', 'Corn', 'Items/corn'],
  [2, 'u3', "It's a Deal!", 'Win a haggle with the merchant.', 'haggle', null, 'World/merchant'],
  [2, 'u4', 'The Big One', 'Catch a Carp.', 'fish', 'Carp', 'Items/carp'],
  [2, 'u5', 'Starry Night', 'Visit Star Hill.', 'starhill', null, 'World/star_ladder'],
  [2, 'u6', 'Something Saved', 'Have 1000G at once.', 'gold1000', null, 'World/storage_box'],
  [3, 'a1', 'Pumpkin Harvest', 'Harvest a Pumpkin.', 'harvest', 'Pumpkin', 'Items/pumpkin'],
  [3, 'a2', 'Golden Harvest', 'Harvest a crop blessed by sunlight.', 'golden', null, 'Items/sunshine_shard'],
  [3, 'a3', 'Shining Koi', 'Catch a Golden Koi.', 'fish', 'Golden Koi', 'Items/golden_koi'],
  [3, 'a4', 'Colors Restored', 'Bring all the color back to a villager.', 'restored', null, 'Villagers/rosa_3'],
  [3, 'a5', 'Shooting Star', 'Catch a Star Fragment.', 'star', null, 'Items/star_fragment'],
  [3, 'a6', 'Pie for Two', 'Bake a Pumpkin Pie.', 'craft', 'Pumpkin Pie', 'Items/pumpkin_pie'],
].map(p => ({ season: p[0], id: p[1], title: p[2], hint: p[3], evt: p[4], arg: p[5], sprite: p[6] }));

// ------------------------------------------------------------------ scenes
// the expanded farm (Assets/Resources/Maps/farm.json, inlined by build.py): one glyph per tile, 1 tile = 1 unit = 32 px
const MAP = /*__MAP__*/null;
const boundsOf = m => ({ x0: m.originX, y0: m.originY - m.height, x1: m.originX + m.width, y1: m.originY });
const MAPB = boundsOf(MAP);
// the forest (Assets/Resources/Maps/forest.json): opens in spring; a gray forest you bring back to colour
const FOREST = /*__FOREST__*/null;
const FORB = boundsOf(FOREST);
const forestAt = (cx, cy) => { const c = cx - FOREST.originX, r = FOREST.originY - 1 - cy; return r >= 0 && r < FOREST.height && c >= 0 && c < FOREST.width ? FOREST.rows[r][c] : '#'; };
const FCELLS = [];
FOREST.rows.forEach((row, r) => [...row].forEach((ch, c) => FCELLS.push([FOREST.originX + c, FOREST.originY - 1 - r, ch])));
const FSPOTS = FCELLS.filter(([, , ch]) => ch === 'c');                 // north, west, south (map order)
const FSEAT = FCELLS.filter(([, , ch]) => ch === 's').map(([x, y]) => [x + 0.5, y + 0.5])[0];
const FTREE = (() => { const o = FCELLS.filter(([, , ch]) => ch === 'O'); const xs = o.map(c => c[0]), ys = o.map(c => c[1]); return [(Math.min(...xs) + Math.max(...xs) + 1) / 2, Math.min(...ys)]; })();
const FORAGE_SPOTS = FCELLS.filter(([, , ch]) => ch === 'f').map(([x, y]) => [x, y]);
const CLEARING_NAMES = ['north', 'west', 'south'];
// the lake (Assets/Resources/Maps/lake.json): opens in summer; release fish to clear it, zone by zone
const LAKE = /*__LAKE__*/null;
const LAKB = boundsOf(LAKE);
const lakeAt = (cx, cy) => { const c = cx - LAKE.originX, r = LAKE.originY - 1 - cy; return r >= 0 && r < LAKE.height && c >= 0 && c < LAKE.width ? LAKE.rows[r][c] : '#'; };
const LCELLS = [];
LAKE.rows.forEach((row, r) => [...row].forEach((ch, c) => LCELLS.push([LAKE.originX + c, LAKE.originY - 1 - r, ch])));
const LMID = ch => LCELLS.filter(([, , c]) => c === ch).map(([x, y]) => [x + 0.5, y + 0.5]);
const LSTEPS = LMID('X')[0], LBENCH = LMID('h')[0], LFISH = LMID('F');
const LFORAGE_SPOTS = LCELLS.filter(([, , ch]) => ch === 'f').map(([x, y]) => [x, y]);
const LAKE_THRESH = [10, 25, 45], LAKE_ZONES = ['west cove', 'middle of the lake', 'east cove'];
const FISH_POINTS = { Minnow: 1, Carp: 2, Bluegill: 2, Moonfin: 3, Glassfish: 4, 'Golden Koi': 5 };
const RELEASABLE = ['Minnow', 'Carp', 'Bluegill', 'Moonfin', 'Glassfish'];   // the Golden Koi stays with you
const lakeBlocked = (cx, cy) => '#~RWB'.includes(lakeAt(cx, cy));
// Sunset Hill (Assets/Resources/Maps/hill.json): opens in autumn; old fruit trees, a drying rack, the overlook
const HILL = /*__HILL__*/null;
const HILB = boundsOf(HILL);
const hillAt = (cx, cy) => { const c = cx - HILL.originX, r = HILL.originY - 1 - cy; return r >= 0 && r < HILL.height && c >= 0 && c < HILL.width ? HILL.rows[r][c] : '#'; };
const HCELLS = [];
HILL.rows.forEach((row, r) => [...row].forEach((ch, c) => HCELLS.push([HILL.originX + c, HILL.originY - 1 - r, ch])));
const HTREES = HCELLS.filter(([, , ch]) => 'APK'.includes(ch)).slice(0, 6);            // [x, y, kind] in map order
const HRACK = HCELLS.filter(([, , ch]) => ch === 'D').map(([x, y]) => [x + 0.5, y + 0.5])[0];
const HBENCH = (() => { const v = HCELLS.filter(([, , ch]) => ch === 'V'); return [(Math.min(...v.map(c => c[0])) + Math.max(...v.map(c => c[0])) + 1) / 2, v[0][1] + 0.5]; })();
const hillBlocked = (cx, cy) => '#CAPKDRB'.includes(hillAt(cx, cy));
const FRUIT = { A: 'Apple', P: 'Pear', K: 'Persimmon' }, KIND = { A: 'apple', P: 'pear', K: 'persimmon' };
const forestBlocked = (cx, cy) => { const ch = forestAt(cx, cy); return '#~wOB'.includes(ch) || (ch === 'L' && !debrisCleared(cx, cy)); };
const mapAt = (cx, cy) => { const c = cx - MAP.originX, r = MAP.originY - 1 - cy; return r >= 0 && r < MAP.height && c >= 0 && c < MAP.width ? MAP.rows[r][c] : ' '; };
const MAPCELLS = [];  // [cellX, cellY, glyph]
MAP.rows.forEach((row, r) => [...row].forEach((ch, c) => MAPCELLS.push([MAP.originX + c, MAP.originY - 1 - r, ch])));
const isDebris = ch => ch === 'g' || ch === 'k' || ch === 'o';
const DEBRIS_TOTAL = MAPCELLS.filter(([, , ch]) => isDebris(ch)).length;
const exitRect = e => [Math.min(e.x0, e.x1), Math.min(e.y0, e.y1), Math.max(e.x0, e.x1), Math.max(e.y0, e.y1)];
const inRect = ([x0, y0, x1, y1], x, y, pad = 0) => x >= x0 - pad && x <= x1 + pad && y >= y0 - pad && y <= y1 + pad;
const arrival = from => { const a = MAP.arrivals.find(a => a.from === from); return a ? [a.x, a.y] : [0, 0]; };
const SCENES = {
  // the farm is bigger than the screen: the camera (cx, cy) follows the player inside the map
  farm: { ww: CW / 64, wh: CH / 64, cx: 0, cy: 0, bg: () => 'World/bg_farmmap_' + seasonKey(), outdoor: true, map: true, bounds: () => MAPB },
  forest: { ww: CW / 64, wh: CH / 64, cx: 0, cy: 0, bg: () => 'World/bg_forest_gray_' + seasonKey(), outdoor: true, map: true, bounds: () => FORB },
  lake: { ww: CW / 64, wh: CH / 64, cx: 0, cy: 0, bg: () => 'World/bg_lake_gray_' + seasonKey(), outdoor: true, map: true, bounds: () => LAKB },
  sunset: { ww: CW / 64, wh: CH / 64, cx: 0, cy: 0, bg: () => 'World/bg_hill_gray_' + seasonKey(), outdoor: true, map: true, bounds: () => HILB },
  home: { ww: 18.43, wh: 10.24, cx: 0, cy: -0.027, bg: () => unlocked('ending_seen') ? 'World/bg_home' : 'World/bg_home_gray' },   // ending A: gray until the end
  market: { ww: 18.28, wh: 10.24, cx: 0.09, cy: -0.08, bg: () => 'World/bg_market_' + seasonKey(), outdoor: true },
  haggle: { ww: 17.95, wh: 10.04, cx: -0.035, cy: -0.079, bg: () => 'World/bg_minigame' },
  hill: { ww: 17.92, wh: 10.24, cx: 0, cy: 0, bg: () => 'World/bg_starhill' },
};
const FIELD = [];
for (let y = 0; y <= 2; y++) for (let x = -8; x <= -2; x++) FIELD.push(x + ',' + y);
for (let y = -3; y <= -1; y++) for (let x = -6; x <= -1; x++) FIELD.push(x + ',' + y);
const FIELDSET = new Set(FIELD);
const TREES = [[4.78, 1.47], [2.53, 2.35], [3.41, 2.31], [5.08, 2.28], [4.21, 2.3]];
const STONES = [[4.12, 1.44], [4.94, -4.71], [-4.72, -2.53], [2.51, 1.5], [-3.49, -4.45], [3.38, 1.46]];
const APPLES = [[-7.29, 3.72], [3.5, -1.24], [-8.29, -1.63], [8.23, -2.72], [7.97, 4.18], [-0.44, -4.18]];
const POND = [7.05, -0.6];
const LADDER = [8.25, 2.9];

// ------------------------------------------------------------------ state
let G;  // game state
function newHill() { return { open: false, tree: [0, 0, 0, 0, 0, 0], fruitDay: [-1, -1, -1, -1, -1, -1], drying: [], restDay: -1 }; }
function newLake() { return { open: false, clarity: 0, released: 0, restDay: -1, forageDay: -1, forage: [] }; }
function newForest() { return { open: false, sapling: [-1, -1, -1], watered: [false, false, false], treeAwake: false, forageDay: -1, forage: [], restDay: -1 }; }
function newGame() {
  G = {
    scene: 'farm', px: 0.9, py: -2.0, dir: 'down', facingLeft: false, step: 0, stepT: 0,
    inv: { Hoe: 1, 'Watering Can': 1, Scythe: 1, 'Carrot Seed': 5, Apple: 3 },
    gold: 50, health: 100, hunger: 100, thirst: 100, energy: 0, heart: 20,
    day: 1, hour: 6, minute: 0, raining: false, snowing: false, water: 10,
    tiles: {}, crops: {}, cleared: [], clearedDebris: [], debrisHits: {},
    market: { demand: 'Carrot', haggle: 0, haggled: false, sold: {} },
    log: { harvested: 0, meals: 0, exhausted: false, starved: false, drank: false },
    story: { season: 0, highest: 0, unlocks: [], photos: [], villagers: {}, kindness: 0, shareDay: -1, sharedToday: 0, donationDay: -1,
      fish: {}, starsDay: -1, starsTonight: 0, starsTotal: 0, benchDay: -1,
      spirit: { found: false, xp: 0, pts: [0, 0, 0, 0], forms: [] },
      forest: newForest(), lake: newLake(), hill: newHill() },
    selected: 'Hoe', warnedH: false, warnedT: false, milestones: [],
  };
  G.energy = maxEnergy();
}
const S = () => G.story;
const seasonKey = () => SEASONS[G.story.season].toLowerCase();
const has = (n, c = 1) => (G.inv[n] || 0) >= c;
function add(n, c = 1) { if (c <= 0) return; G.inv[n] = (G.inv[n] || 0) + c; if ((n === 'Sunshine Shard' || n === "Earth's Breath") && !S().spirit.found) discoverSpirit(); }
function remove(n, c = 1) { if (!G.inv[n]) return; G.inv[n] -= c; if (G.inv[n] <= 0) delete G.inv[n]; G.water = Math.min(G.water, waterCap()); }
const unlocked = k => S().unlocks.includes(k);
function unlock(k) { if (unlocked(k)) return false; S().unlocks.push(k); return true; }
function villagerProgress(id) { return S().villagers[id] || (S().villagers[id] = { wish: 0, met: false }); }
function waterCap() { let c = 0; for (const n in G.inv) { const d = ITEMS[n]; if (d && d.tool === 'can') c = Math.max(c, d.cap); } return c; }

// spirit math (SpiritDB)
const spiritStage = s => s.xp >= 25 ? 2 : s.xp >= 8 ? 1 : 0;
function spiritDominant(s) { let b = -1, v = 0; for (let i = 0; i < 4; i++) if (s.pts[i] > v) { v = s.pts[i]; b = i; } return b < 0 ? null : COLORS[b]; }
function spiritPrism(s) { if (spiritStage(s) < 2) return false; const mn = Math.min(...s.pts), mx = Math.max(...s.pts); return mx > 0 && mn * 2 >= mx; }
function spiritForm(s) { const st = spiritStage(s), d = spiritDominant(s); if (st === 0 || !d) return 'spirit_gray'; if (spiritPrism(s)) return 'spirit_prism'; return `spirit_${d}_${st === 1 ? 'young' : 'grown'}`; }
function perk(color) { const s = S().spirit; if (!s.found) return 0; if (spiritPrism(s)) return 1; return spiritDominant(s) === color ? spiritStage(s) : 0; }
const maxEnergy = () => 100 * (0.6 + 0.4 * Math.min(1, G.heart / 100)) * (1 + 0.1 * perk('sun'));

// ------------------------------------------------------------------ assets
function loadAll() {
  return Promise.all(Object.entries(ASSETS).map(([k, src]) => new Promise(res => {
    const im = new Image(); im.onload = () => { IMG[k] = im; res(); }; im.onerror = res; im.src = src;
  })));
}

// ------------------------------------------------------------------ coordinates
function sc() { return SCENES[G.scene]; }
function ux() { return CW / sc().ww; }
function uy() { return CH / sc().wh; }
function toScreen(wx, wy) { const s = sc(); return [(wx - s.cx) * ux() + CW / 2, CH / 2 - (wy - s.cy) * uy()]; }
function toWorld(x, y) { const s = sc(); return [(x - CW / 2) / ux() + s.cx, (CH / 2 - y) / uy() + s.cy]; }
// draw a sprite at the world's own pixel size (32 px = 1 unit, same as the backgrounds); scale only for small pulses
function sprite(c, key, wx, wy, scale = 1, anchor = 'center', flip = false, alpha = 1) {
  const im = IMG[key]; if (!im) return;
  const k = Math.max(1, Math.round(uy() / 32)) * scale;
  const w = im.width * k, hh = im.height * k;
  let [x, y] = toScreen(wx, wy);
  x = Math.round(x - w / 2); y = Math.round(anchor === 'bottom' ? y - hh : y - hh / 2);
  c.save(); c.globalAlpha = alpha;
  if (flip) { c.translate(x + w, y); c.scale(-1, 1); c.drawImage(im, 0, 0, w, hh); } else c.drawImage(im, x, y, w, hh);
  c.restore();
}

// ------------------------------------------------------------------ HUD helpers
let toasts = [], announce = null, dialogue = null, panel = null, haggle = null, fishing = null;
const keys = {}, pressed = {};
function toast(text, dur = 3.2) {
  const ex = toasts.find(t => t.text === text); if (ex) { ex.t = dur; return; }
  toasts.push({ text, t: dur }); if (toasts.length > 4) toasts.shift();
}
function showAnnounce(text, dur = 6) { announce = { text, t: dur, born: performance.now() }; }
function say(name, lines, portrait, done) { dialogue = { name, lines, i: 0, portrait, done, choice: null, born: frame }; }
function ask(name, text, portrait, yes, no, yl = 'Yes', nl = 'No') { dialogue = { name, lines: [text], i: 0, portrait, choice: { yes, no, yl, nl }, born: frame }; }

// rich text: [#rrggbb]colored[/] segments, \n line breaks, word wrap
function richLines(text, size, maxW, base) {
  ctx.font = `${size}px ${FONT}`;
  const out = [];
  for (const para of text.split('\n')) {
    const toks = []; let color = base; const re = /\[(#[0-9a-fA-F]{6}|\/)\]/g; let last = 0, m;
    while ((m = re.exec(para))) { if (m.index > last) toks.push({ s: para.slice(last, m.index), c: color }); color = m[1] === '/' ? base : m[1]; last = re.lastIndex; }
    if (last < para.length) toks.push({ s: para.slice(last), c: color });
    let line = [], w = 0;
    for (const tk of toks) {
      for (const word of tk.s.split(/(\s+)/)) {
        if (!word) continue;
        const ww = ctx.measureText(word).width;
        if (w + ww > maxW && line.length && word.trim()) { out.push(line); line = []; w = 0; if (!word.trim()) continue; }
        line.push({ s: word, c: tk.c, w: ww }); w += ww;
      }
    }
    out.push(line);
  }
  return out;
}
function drawRich(text, x, y, size, maxW, align = 'left', base = '#f6efe2', lh = 1.3) {
  const lines = richLines(text, size, maxW, base);
  ctx.font = `${size}px ${FONT}`; ctx.textBaseline = 'top';
  lines.forEach((line, i) => {
    const total = line.reduce((a, t) => a + t.w, 0);
    let cx = align === 'center' ? x - total / 2 : align === 'right' ? x - total : x;
    for (const t of line) { ctx.fillStyle = '#1a120d'; ctx.fillText(t.s, cx + 2, y + i * size * lh + 2); ctx.fillStyle = t.c; ctx.fillText(t.s, cx, y + i * size * lh); cx += t.w; }
  });
  return lines.length * size * lh;
}
function box(x, y, w, h, fill = 'rgba(20,14,11,0.86)', border = '#c89552') {
  ctx.fillStyle = border; ctx.fillRect(x, y, w, h);
  ctx.fillStyle = fill; ctx.fillRect(x + 4, y + 4, w - 8, h - 8);
}

// ------------------------------------------------------------------ album / heart / kindness
function report(evt, arg = null) {
  for (const p of PHOTOS) {
    if (p.evt !== evt || p.season > S().season || S().photos.includes(p.id)) continue;
    if (p.arg && p.arg !== arg) continue;
    S().photos.push(p.id);
    const have = PHOTOS.filter(q => q.season === p.season && S().photos.includes(q.id)).length;
    toast(`[#ffd84a]New photo![/] "${p.title}" (${SEASONS[p.season]} ${have}/6)  [P] Album`);
    if (have >= 6 && unlock('album_' + p.season)) { G.gold += 300; changeHeart(5); showAnnounce(`[#ffd84a]${SEASONS[p.season]} page complete![/]\n\nEvery memory of this season is in color now.\n[#9dff9d]+300G  +5 Heart[/]`, 5); }
  }
}
function changeHeart(a, reason) {
  const before = G.heart; G.heart = Math.max(0, Math.min(100, G.heart + a));
  if (reason && Math.abs(a) >= 0.5) toast(`[#ff8fb1]${a > 0 ? '+' : ''}${Math.round(a)} Heart[/]  ${reason}`);
  for (const [m, t] of [[30, 'Color returns...\n\nThe gray haze of the city is starting to lift.'], [50, 'Breathing again\n\nYou notice the smell of the soil and the sound of the wind.'], [75, 'Warm days\n\nThe village feels like home now.'], [100, 'Happiness rebuilt\n\nThe world is full of color again.']]) {
    if (before < m && G.heart >= m && !G.milestones.includes(m)) { G.milestones.push(m); showAnnounce(`[#ff8fb1]${t.split('\n')[0]}[/]${t.slice(t.indexOf('\n'))}`, 5); }
  }
}
function addKindness(n, fromBox) {
  if (n <= 0) return;
  if (!fromBox) S().kindness += n;
  if (S().donationDay !== G.day) { S().donationDay = G.day; changeHeart(2, 'Sharing warms your heart.'); }
  if (S().kindness >= 60 && unlock('starhill')) showAnnounce('[#ffb6d0]Word of your kindness spreads[/]\n\nGrandma Rosa whispers: "On clear nights, look for the glowing vine at the edge of your farm."\n\nStar Hill unlocked: clear nights after 20:00, top-right of the farm.', 8);
}
function useEnergy(a) {
  if (G.energy >= a) { G.energy -= a; if (G.energy < 1) G.log.exhausted = true; return true; }
  G.log.exhausted = true; toast("You're too tired... Eat something or get some sleep."); return false;
}
function eat(n) {
  const d = ITEMS[n]; if (!d || !edible(n) || !has(n)) { if (n) toast(`You can't eat ${n}.`); return; }
  remove(n); G.hunger = Math.min(100, G.hunger + d.hunger); G.thirst = Math.min(100, G.thirst + d.thirst);
  G.energy = Math.min(maxEnergy(), G.energy + d.hunger * 0.25); G.log.meals++; if (d.thirst > 0) G.log.drank = true;
  if (d.cat === 'Food' && d.heart > 0) report('eat_cooked', n);
  if (d.heart > 0) changeHeart(d.heart);
  toast(`Ate ${n}.` + (d.heart > 0 ? `  [#ff8fb1]+${d.heart} Heart[/]` : ''));
}
function sellPrice(n) {
  const d = ITEMS[n]; if (!canSell(n)) return 0;
  let p = d.sell; if (n === G.market.demand) p *= 1.5; p *= 1 + G.market.haggle;
  p *= Math.max(0.5, 1 - 0.04 * (G.market.sold[n] || 0)); return Math.max(1, Math.round(p));
}
function addGold(a) { G.gold = Math.max(0, G.gold + a); if (G.gold >= 1000) report('gold1000'); }

// ------------------------------------------------------------------ hotbar
function hotbarItems() { return Object.keys(G.inv).filter(inHotbar).sort((a, b) => sortKey(a) - sortKey(b)); }
let hotOffset = 0;
function selectIndex(i) {
  const items = hotbarItems(); if (!items.length) return;
  i = ((i % items.length) + items.length) % items.length; G.selected = items[i];
  if (i < hotOffset) hotOffset = i; if (i >= hotOffset + 8) hotOffset = i - 7;
  const d = ITEMS[G.selected]; if (d && d.desc) toast(`${G.selected}: ${d.desc}`, 2.2);
}
function selectedItem() { if (!has(G.selected)) { const it = hotbarItems(); G.selected = it[0] || null; } return G.selected; }

// ------------------------------------------------------------------ farm actions (CropManager)
const tileAt = (x, y) => G.tiles[x + ',' + y];
const debrisCleared = (x, y) => G.clearedDebris.includes(x + ',' + y);
// soil you can till: the old field, open soil on the map, and plot cells you have cleared
function tillable(x, y) { if (FIELDSET.has(x + ',' + y)) return true; const ch = mapAt(x, y); return ch === 'x' || (isDebris(ch) && debrisCleared(x, y)); }
function debrisAt(x, y) {
  const ch = G.scene === 'forest' ? forestAt(x, y) : mapAt(x, y);
  return (isDebris(ch) || ch === 'L') && !debrisCleared(x, y) ? ch : null;
}
const debrisHitsFor = ch => ch === 'k' ? 2 : ch === 'o' || ch === 'L' ? 3 : 1;
const debrisKey = (ch, x, y) => ch === 'k' ? 'stump' : ch === 'o' ? 'boulder' : ch === 'L' ? 'forest_log' : 'weeds_' + (Math.abs(x + y) % 2);
let debrisPunch = {};
// clearing: weeds (scythe or hoe, 2 energy); stumps, boulders and forest logs (mattock, 4 energy a swing)
function hitDebris(x, y, ch, d) {
  const k = x + ',' + y;
  if (ch === 'g') {
    if (d.tool !== 'scythe' && d.tool !== 'hoe') { toast('Weeds. Cut them with a scythe or dig them out with a hoe.', 2.5); return; }
    if (!useEnergy(2)) return; clearDebris(x, y); return;
  }
  if (d.tool !== 'mattock') {
    const what = ch === 'k' ? 'An old stump.' : ch === 'o' ? 'A heavy boulder.' : 'A fallen log blocks the way.';
    toast(has('Mattock') ? `${what} Use the mattock.` : `${what} You need a mattock. (Market, or craft: 5 Wood + 5 Stone)`, 3); return;
  }
  const need = debrisHitsFor(ch); let left = Math.min(G.debrisHits[k] ?? need, need);
  if (!useEnergy(4)) return;
  left--; debrisPunch[k] = 1;
  if (left > 0) { G.debrisHits[k] = left; toast(ch === 'k' ? `The roots creak... (${left} more)` : ch === 'L' ? `The log rolls a little... (${left} more)` : `The boulder shifts... (${left} more)`, 1.2); return; }
  delete G.debrisHits[k];
  if (ch === 'k') { add('Wood', 2); toast('You pulled out the old stump. +2 Wood', 2); }
  else if (ch === 'L') { add('Wood', 3); toast('You cleared the fallen log. The path is open. +3 Wood', 2.5); G.clearedDebris.push(k); return; }
  else { add('Stone', 2); if (Math.random() * 100 < 10 * (0.5 + G.heart / 100)) { add("Earth's Breath"); toast("[#ffe28a]Earth's Breath was sleeping under the boulder![/]"); } else toast('You rolled the boulder away. +2 Stone', 2); }
  clearDebris(x, y);
}
function clearDebris(x, y) {
  G.clearedDebris.push(x + ',' + y);
  const done = MAPCELLS.filter(([cx, cy, ch]) => isDebris(ch) && debrisCleared(cx, cy)).length, before = Math.floor((done - 1) * 4 / DEBRIS_TOTAL), after = Math.floor(done * 4 / DEBRIS_TOTAL);
  if (after === before) return;
  if (done >= DEBRIS_TOTAL) { toast("[#ffe28a]The old field is completely cleared. It's all yours now![/]", 4); changeHeart(5, 'You brought the old field back to life.'); }
  else toast(`The old field is ${after * 25}% cleared. (${done}/${DEBRIS_TOTAL})`, 3);
}
const cropAt = (x, y) => G.crops[x + ',' + y];
const mature = c => c.days >= CROPS[c.id].days;
function cellsFor(cx, cy, tier) { return tier >= 2 ? [[cx, cy], [cx - 1, cy], [cx + 1, cy]] : [[cx, cy]]; }
function harvestCell(x, y) {
  const c = cropAt(x, y); const def = CROPS[c.id]; const blessed = c.blessed; const n = 1 + (blessed ? 1 : 0);
  if (def.regrow) { c.days = def.days - def.regrow; c.blessed = false; } else delete G.crops[x + ',' + y];
  add(def.harvest, n); G.log.harvested += n;
  toast(`Harvested ${def.harvest} x${n}` + (blessed ? '  [#ffd84a](golden!)[/]' : ''));
  report('harvest', def.harvest); if (blessed) { report('golden'); changeHeart(3, 'A golden harvest!'); }
  else if (G.log.harvested === n) changeHeart(1, 'Your first harvest of the day.');
  if (def.harvest === 'Sunflower') changeHeart(2 * n, 'The sunflower makes you smile.');
}
function worldClick(wx, wy) {
  if (G.scene !== 'farm' && G.scene !== 'forest' && G.scene !== 'sunset') return;
  const cx = Math.floor(wx), cy = Math.floor(wy);
  if (Math.hypot(G.px - (cx + 0.5), G.py - (cy + 0.5)) > 3.5) { toast("That's too far away. Walk closer.", 1.5); return; }
  const item = selectedItem(); const d = ITEMS[item] || {};
  const spot = G.scene === 'forest' ? FSPOTS.findIndex(([x, y]) => x === cx && y === cy) : -1;
  const tree = G.scene === 'sunset' ? HTREES.findIndex(([x, y]) => x === cx && y === cy) : -1;
  if (tree >= 0) { useTree(tree, item, d); return; }
  if (G.scene === 'sunset') return;
  if (spot >= 0) { useSpot(spot, item, d); return; }
  if (item === 'Acorn') { toast('Acorns want to grow in a forest clearing. Look for the ring of pebbles.'); return; }
  const deb = debrisAt(cx, cy); if (deb) { hitDebris(cx, cy, deb, d); return; }
  if (d.tool === 'scythe') {
    const ripe = []; for (let dx = -1; dx <= 1; dx++) for (let dy = -1; dy <= 1; dy++) { const c = cropAt(cx + dx, cy + dy); if (c && mature(c)) ripe.push([cx + dx, cy + dy]); }
    if (!ripe.length) { toast('Nothing is ripe here yet.'); return; }
    if (!useEnergy(2)) return; ripe.forEach(([x, y]) => harvestCell(x, y)); return;
  }
  const crop = cropAt(cx, cy);
  if (crop && mature(crop)) { harvestCell(cx, cy); return; }
  if (d.tool === 'hoe') {
    const t = cellsFor(cx, cy, d.tier).filter(([x, y]) => tillable(x, y) && !debrisAt(x, y) && !tileAt(x, y));
    if (!t.length) { toast(tileAt(cx, cy) ? 'This soil is already tilled. Plant a seed here!' : 'You can only till the dirt field.'); return; }
    if (!useEnergy(d.tier >= 2 ? 6 : 4)) return; t.forEach(([x, y]) => G.tiles[x + ',' + y] = { wet: false });
  } else if (d.tool === 'can') {
    if (G.water <= 0) { toast('Your watering can is empty. Refill it at the well (E).'); return; }
    const t = cellsFor(cx, cy, d.tier).filter(([x, y]) => { const tl = tileAt(x, y); const c = cropAt(x, y); return tl && (!tl.wet || (c && !c.watered)); });
    if (!t.length) { toast(tileAt(cx, cy) ? 'This spot is already watered.' : 'Water tilled soil or crops.'); return; }
    if (!useEnergy(2)) return; G.water--;
    t.forEach(([x, y]) => { tileAt(x, y).wet = true; const c = cropAt(x, y); if (c) c.watered = true; });
    if (G.water === 0) toast('The watering can is empty now.');
  } else if (d.cat === 'Seed') {
    if (!tileAt(cx, cy)) { toast('Till the soil with a hoe first.'); return; }
    if (crop) { toast('Something is already growing here.'); return; }
    const def = CROPS[d.crop];
    if (G.story.season < def.season) { toast(`It's still ${SEASONS[G.story.season]} in your heart. ${def.harvest} needs ${SEASONS[def.season]}.`); return; }
    if (!useEnergy(1)) return; remove(item); G.crops[cx + ',' + cy] = { id: d.crop, days: 0, watered: tileAt(cx, cy).wet, blessed: false };
  } else if (d.cat === 'Special' && item !== 'Star Fragment') {
    if (!crop) { toast('Use it on a growing crop.'); return; }
    if (item === "Earth's Breath") { crop.days++; toast("The crop breathes in the earth's energy and grows!"); }
    else { if (crop.blessed) { toast('Already glowing with sunlight.'); return; } crop.blessed = true; toast('Warm sunlight wraps around the crop.'); }
    remove(item);
  } else if (edible(item)) toast('Right-click or press F to eat.', 2);
}

// ------------------------------------------------------------------ forest (map stage 3)
const F = () => S().forest;
// regions built next to the farm (the exit name on the farm map -> its map and whether it's open)
const REGIONS = {
  Forest: { map: FOREST, open: () => F().open, scene: 'forest', line: 'The air turns cool and green. You step into the forest.' },
  Lake: { map: LAKE, open: () => Lk().open, scene: 'lake', line: 'The reeds rustle as you pass. The air smells of water.' },
  Hill: { map: HILL, open: () => Hl().open, scene: 'sunset', line: 'Leaves crunch underfoot. The path climbs toward the sunset.' },
};
const areaOpen = to => !!REGIONS[to] && REGIONS[to].open();
const forestRestored = () => F().sapling.filter(s => s >= 3).length;
function forestCheckOpen() { if (F().open || S().season < 1) return false; F().open = true; unlock('forest_open'); return true; }
function forageCount() { return S().season === 0 ? 2 : Math.min(7, 3 + Math.floor(G.heart / 25)); }
function rollForage() {
  const f = F(); f.forageDay = G.day; f.forage = [];
  const pool = FORAGE_SPOTS.slice(); const wantAcorn = f.sapling.some(s => s < 0) && !has('Acorn');
  for (let k = 0; k < Math.min(pool.length, forageCount()); k++) {
    const [x, y] = pool.splice(Math.floor(Math.random() * pool.length), 1)[0];
    const roll = Math.random() * 100; const item = wantAcorn && k === 0 ? 'Acorn' : roll < 45 ? 'Wild Garlic' : roll < 80 ? 'Fern Sprout' : 'Morel';
    f.forage.push([x, y, item]);
  }
}
// morning: saplings watered yesterday (or rained on) grow one step; three restored clearings wake the old tree
function forestMorning(lines) {
  let delta = 0, headline = null;
  if (forestCheckOpen()) headline = "[#ffb6d0]The gray thorns on the forest path have bloomed into an arch.[/]\nThe forest west of the farm is open.";
  const f = F(); if (!f.open) return [0, null];
  for (let i = 0; i < 3; i++) {
    if (f.sapling[i] >= 0 && f.sapling[i] < 3 && (f.watered[i] || G.raining)) { f.sapling[i]++; if (f.sapling[i] === 3) { delta += 3; lines.push(`Color came back to the ${CLEARING_NAMES[i]} clearing  [#9dff9d]+3[/]`); } }
    f.watered[i] = false;
  }
  if (!f.treeAwake && forestRestored() === 3) { f.treeAwake = true; delta += 5; lines.push('The old tree woke up  [#9dff9d]+5[/]'); headline = '[#ffb6d0]Overnight, the old tree in the forest woke up.[/]\nColor is flowing back into the whole forest.'; }
  rollForage();
  return [delta, headline];
}
function useSpot(i, item, d) {
  const f = F(), stage = f.sapling[i], name = CLEARING_NAMES[i];
  if (stage < 0) {
    if (item !== 'Acorn') { toast(`A ring of pebbles in the ${name} clearing. An acorn could grow here.`); return; }
    if (!useEnergy(1)) return; remove('Acorn'); f.sapling[i] = 0; f.watered[i] = false; toast('You planted the acorn. Water it every day and it will grow.'); return;
  }
  if (stage >= 3) { toast(`The young oak is doing well. The ${name} clearing is full of color again.`); return; }
  if (d.tool === 'can') {
    if (f.watered[i]) { toast("It's already had water today.", 2); return; }
    if (G.water <= 0) { toast('Your watering can is empty. The spring or the well can fill it.'); return; }
    if (!useEnergy(2)) return; G.water--; f.watered[i] = true; toast(stage === 0 ? 'The soil soaks up the water.' : 'The sapling drinks the water.', 2); return;
  }
  toast(`${['An acorn is sleeping in the soil.', 'A tiny sprout is peeking out.', 'The sapling is getting taller.'][stage]} (${stage}/3) Water it every day.`);
}

// ------------------------------------------------------------------ lake (map stage 4-1)
const Lk = () => S().lake;
const lakeZones = () => LAKE_THRESH.filter(t => Lk().clarity >= t).length;
const lakeClear = () => Lk().clarity >= LAKE_THRESH[2];
function lakeCheckOpen() { if (Lk().open || S().season < 2) return false; Lk().open = true; unlock('lake_open'); return true; }
function rollLakeForage() {
  const l = Lk(); l.forageDay = G.day; l.forage = [];
  const pool = LFORAGE_SPOTS.slice(); const n = S().season === 0 ? 2 : Math.min(6, 2 + Math.floor(G.heart / 25));
  for (let k = 0; k < Math.min(pool.length, n); k++) { const [x, y] = pool.splice(Math.floor(Math.random() * pool.length), 1)[0]; l.forage.push([x, y, Math.random() < 0.55 ? 'Wild Raspberry' : 'Watercress']); }
}
function lakeCatch(roll) {
  const koi = 0.02 + 0.05 * G.heart / 100; if (roll < koi) return 'Golden Koi'; roll -= koi;
  if (roll < 0.06) return 'Old Boot'; roll -= 0.06;
  if (lakeZones() >= 2) { if (roll < 0.10) return 'Glassfish'; roll -= 0.10; }
  if (G.hour >= 19 || G.hour < 5) { if (roll < 0.25) return 'Moonfin'; roll -= 0.25; }
  return roll < 0.40 ? 'Bluegill' : 'Carp';
}
function releaseFish() {
  const parts = [], l = Lk(); let count = 0, points = 0;
  for (const f of RELEASABLE) { const n = G.inv[f] || 0; if (!n) continue; parts.push(`${f} x${n}`); count += n; points += n * FISH_POINTS[f]; }
  if (!count) { const next = LAKE_THRESH.find(t => l.clarity < t); toast(lakeClear() ? 'The lake is clear and calm. You can still let fish go here anytime.' : `Stone steps down to the gray water. Catch some fish and let them go here. (${next - l.clarity} to the next part)`, 3.5); return; }
  ask('The lake', `Let these fish go?\n${parts.join(', ')}\n[#c8c0b0](Golden Koi stays with you)[/]`, null, () => {
    const before = lakeZones(), wasClear = lakeClear();
    for (const f of RELEASABLE) delete G.inv[f];
    l.released += count; l.clarity += points;
    toast(`The fish slip back into the water. The lake feels a little lighter. (+${points})`);
    for (let z = before; z < lakeZones(); z++) changeHeart(3, `The ${LAKE_ZONES[z]} runs clear and blue again.`);
    if (!wasClear && lakeClear()) { changeHeart(5, 'The whole lake is clear.'); showAnnounce('[#8fd0ff]The lake is clear[/]\n\nYou can see the pebbles on the bottom.\nWhen night falls, fireflies come out over the water.', 6); }
  }, null, 'Let them go', 'Keep them');
}

// ------------------------------------------------------------------ Sunset Hill (map stage 4-2)
const Hl = () => S().hill;
const hillRevived = () => Hl().tree.filter(t => t >= 2).length;
function hillCheckOpen() { if (Hl().open || S().season < 3) return false; Hl().open = true; unlock('hill_open'); return true; }
const hasFruit = i => Hl().tree[i] >= 2 && S().season !== 0 && Hl().fruitDay[i] >= 0 && G.day >= Hl().fruitDay[i];
function useTree(i, item, d) {
  const h = Hl(), [, , k] = HTREES[i], kind = KIND[k];
  if (h.tree[i] === 0) {
    if (d.tool !== 'scythe') { toast(`An old ${kind} tree, gray and tangled. Prune it with a scythe first.`); return; }
    if (!useEnergy(4)) return; h.tree[i] = 1; toast(`You cut back the dead branches. The ${kind} tree looks lighter. (It needs Earth's Breath now.)`, 3.5); return;
  }
  if (h.tree[i] === 1) {
    if (item !== "Earth's Breath") { toast(`The pruned ${kind} tree is waiting. Give it Earth's Breath to wake it up.`); return; }
    remove("Earth's Breath"); h.tree[i] = 2; h.fruitDay[i] = G.day + 3; changeHeart(2, `The old ${kind} tree woke up. Color spreads around it.`);
    if (hillRevived() === 6) { changeHeart(5, 'The whole orchard is alive again.'); showAnnounce('[#ffb070]The orchard is alive again[/]\n\nApples, pears and persimmons, all in color.\nThe hill glows in the evening light.', 6); }
    return;
  }
  if (hasFruit(i)) { h.fruitDay[i] = G.day + 3; add(FRUIT[k], 2); toast(`You picked 2 ${FRUIT[k]}.` + (k === 'K' ? ' (Hang them on the drying rack!)' : '')); }
  else toast(S().season === 0 ? "It's resting through the cold. No fruit in winter." : `No fruit yet. Come back in ${Math.max(1, h.fruitDay[i] - G.day)} day(s).`);
}
function useRack() {
  const h = Hl(); const ready = h.drying.filter(d => G.day >= d).length; h.drying = h.drying.filter(d => G.day < d);
  if (ready) add('Dried Persimmon', ready);
  const hang = Math.min(G.inv.Persimmon || 0, 8 - h.drying.length);
  for (let k = 0; k < hang; k++) h.drying.push(G.day + 3);
  if (hang) remove('Persimmon', hang);
  toast(ready && hang ? `You took down ${ready} Dried Persimmon and hung up ${hang} more.` : ready ? `You took down ${ready} Dried Persimmon. Chewy and sweet!` :
    hang ? `You hung up ${hang} persimmon(s). They'll be dried in 3 days.` : h.drying.length ? `The persimmons are still drying. (${h.drying.length} on the rack)` : 'A drying rack. Hang persimmons here and wait a few days.');
}
const fieldCleared = () => MAPCELLS.filter(([x, y, ch]) => isDebris(ch) && debrisCleared(x, y)).length / DEBRIS_TOTAL;
const allVillagers = () => VILLAGERS.every(v => villagerProgress(v.id).wish >= 3);
function panoramaLayers() {
  const s = S(), farm = fieldCleared() >= 0.75, forest = s.forest.treeAwake, lake = lakeClear(), village = allVillagers(), home = unlocked('ending_seen'), orchard = hillRevived() === 6;
  const L = ['pano_base'];
  if (farm && forest && lake && village && home && orchard) L.push('pano_land', 'pano_mountains');
  if (forest) L.push('pano_forest'); if (lake) L.push('pano_lake'); if (village) L.push('pano_village');
  if (farm) L.push('pano_farm'); if (home) L.push('pano_home'); if (orchard) L.push('pano_orchard');
  const mark = (ok, n) => ok ? `[#ffd84a]${n}[/]` : `[#8a8a92]${n}[/]`;
  return [L, [mark(farm, 'The old field'), mark(forest, 'The forest'), mark(lake, 'The lake'), mark(village, 'The village'), mark(orchard, 'The orchard'), mark(home, 'Home')].join('   ')];
}
// ending A: the last color is yours. When every villager has their colors back and Heart is 90+, the ending starts at home.
const endingReady = () => !unlocked('ending_seen') && allVillagers() && G.heart >= 90;
function startHomeEnding() {
  say('', ['Morning light slips through the window. On the table: one more letter from the city.',
    '"Your old desk is still empty. Come back. We\'ll pay double."',
    'You look around the little house. Everything outside has its colors back. Only these walls are still gray.'], null, () =>
    ask('A letter from the city', 'Write back?', null, () => {
      unlock('ending_seen'); unlock('chose_village'); changeHeart(5);
      showAnnounce('[#ffb6d0]The last color was yours[/]\n\nThe walls warm up. The curtains blush. The kettle shines.\nThe gray was never the city\'s. It was yours, and now it\'s gone.\n\n[#ff8fb1]Thank you for playing.[/]  Life in the village goes on.', 20); save();
    }, () => toast('You fold the letter away. Maybe tomorrow.'), '"I\'m staying. This is home now."', 'Not yet'));
}

// ------------------------------------------------------------------ spirit
function discoverSpirit() {
  const s = S().spirit; s.found = true; if (!s.forms.includes('spirit_gray')) s.forms.push('spirit_gray');
  G.spirit = { x: G.px - 0.8, y: G.py + 0.8, vx: 0, pulse: 0 };
  showAnnounce('[#ff9fc4]A tiny gray spirit[/]\n\nDrawn by the light, a little gray wisp drifts out and starts following you. It looks hungry for color.\n\n[#ffd84a][G][/] Feed it the item you\'re holding.\nSun: Sunshine Shard, Sunflower, Corn   Earth: Earth\'s Breath, Carrot, Potato\nWater: fish   Bloom: Apple, Strawberry, Jam', 9);
  report('spirit_found');
}
function feedSpirit() {
  const s = S().spirit; if (!s.found) return; const item = selectedItem();
  if (!item) { toast('Hold something to feed your spirit.'); return; }
  const f = SPIRIT_FOOD[item]; if (!f) { toast(`Your spirit sniffs the ${item} and turns away.`); return; }
  const stageBefore = spiritStage(s), formBefore = spiritForm(s);
  remove(item); for (let i = 0; i < 4; i++) s.pts[i] += f[i]; s.xp += f.reduce((a, b) => a + b, 0);
  G.spirit.pulse = 1;
  const labels = ['[#ffd84a]Sun[/]', '[#9dff9d]Earth[/]', '[#8fd0ff]Water[/]', '[#ff9fc4]Bloom[/]'];
  toast(`Your spirit munches the ${item}!` + f.map((v, i) => v ? ` +${v} ${labels[i]}` : '').join(''));
  const form = spiritForm(s);
  if (form !== formBefore) {
    const isNew = !s.forms.includes(form); if (isNew) s.forms.push(form);
    if (spiritStage(s) >= 1) report('spirit_young');
    showAnnounce(`[#ff9fc4]${spiritStage(s) > stageBefore ? 'Your spirit grew!' : 'Your spirit changed its colors!'}[/]\n\n${FORM_NAMES[form]}${isNew ? '  [#9dff9d](new form!)[/]' : ''}\n\nForms found: ${s.forms.length} / 10`, 5);
    if (isNew) changeHeart(2, 'Your spirit is glowing with color.');
  }
}

// ------------------------------------------------------------------ villagers
function villagerSprite(v) { return `Villagers/${v.id}_${Math.min(3, villagerProgress(v.id).wish)}`; }
function wishReady(v) { const p = villagerProgress(v.id); if (p.wish >= 3) return false; return v.wishes[p.wish].items.every(([n, c]) => has(n, c)); }
const itemsText = arr => arr.map(([n, c]) => `${c} ${n}`).join(', ');
function talk(v) {
  const p = villagerProgress(v.id); const portrait = villagerSprite(v);
  v.idle = (v.idle || 0) + 1; const seasonal = v.idle % 2 === 1;
  if (p.wish >= 3) { say(v.name, [seasonal ? v.season[S().season] : v.color[v.idle % v.color.length]], portrait); return; }
  const w = v.wishes[p.wish];
  if (!p.met) { p.met = true; report('villager_met'); say(v.name, [v.intro, w.req], portrait); return; }
  if (wishReady(v)) { ask(v.name, `${w.req}\n[#ffd84a]Give ${itemsText(w.items)}?[/]`, portrait, () => fulfill(v), null, 'Give', 'Not yet'); return; }
  say(v.name, [seasonal ? v.season[S().season] : v.gray[v.idle % v.gray.length], `${w.req}\n[#c8c0b0](Needs: ${itemsText(w.items)})[/]`], portrait);
}
function fulfill(v) {
  const p = villagerProgress(v.id); const w = v.wishes[p.wish];
  w.items.forEach(([n, c]) => remove(n, c)); p.wish++; v.pulse = 1;
  addKindness(3); report('wish'); if (p.wish >= 3) report('restored');
  const rw = []; if (w.gold) { addGold(w.gold); rw.push(w.gold + 'G'); }
  (w.reward || []).forEach(([n, c]) => { add(n, c); rw.push(`${n} x${c}`); });
  if (w.unlock && unlock(w.unlock)) rw.push(w.unlockText);
  changeHeart(w.heart, `${v.name} smiled.`);
  const lines = [w.thanks]; if (rw.length) lines.push('[#9dff9d]Received:[/] ' + rw.join(', ')); if (p.wish >= 3) lines.push(v.restored);
  say(v.name, lines, villagerSprite(v), () => {
    if (p.wish >= 3) { showAnnounce(`[#ffd84a]${v.name}'s colors have returned![/]`, 4); changeHeart(5, 'The village feels a little brighter.'); }
  });
}

// ------------------------------------------------------------------ day end (DataManager.EndDay)
function trySleep() {
  if (G.hour < 18 && G.energy > maxEnergy() * 0.25) { toast("It's too early to sleep. (You can sleep after 18:00)"); return; }
  endDay(false);
}
function endDay(passedOut) {
  const lines = []; let delta = 0;
  if (passedOut) { delta -= 8; lines.push('Passed out from exhaustion  [#ff8080]-8[/]'); const lost = Math.min(Math.floor(G.gold / 10), 100); if (lost) { G.gold -= lost; lines.push(`Someone carried you home... you lost ${lost}G`); } }
  else if (G.hour < 24) { delta += 4; lines.push('Went to bed on time  [#9dff9d]+4[/]'); report('sleep_early'); }
  else { delta -= 2; lines.push('Stayed up too late  [#ff8080]-2[/]'); }
  if (G.log.meals) { delta += 3; lines.push('Ate proper meals  [#9dff9d]+3[/]'); }
  if (G.log.drank) { delta += 1; lines.push('Drank fresh water  [#9dff9d]+1[/]'); }
  if (!G.log.exhausted) { delta += 3; lines.push("Didn't overwork  [#9dff9d]+3[/]"); } else { delta -= 3; lines.push('Worked until exhausted  [#ff8080]-3[/]'); }
  if (G.log.starved) { delta -= 5; lines.push('Went hungry or thirsty  [#ff8080]-5[/]'); }
  if (G.log.harvested) { delta += 2; lines.push(`Harvested ${G.log.harvested} crops  [#9dff9d]+2[/]`); }
  changeHeart(delta);
  G.hunger = Math.max(0, G.hunger - 15); G.thirst = Math.max(0, G.thirst - 20); G.health = Math.min(100, G.health + 30);

  // seasons of the heart
  const before = S().season; let after = before; const target = THRESH.filter(t => G.heart >= t).length - 1;
  if (target > before) after = target; else if (target < before && G.heart < THRESH[before] - 5) after = before - 1;
  S().season = after; S().highest = Math.max(S().highest, after); if (after >= 3) unlock('season_autumn');
  // weather follows mood
  let chance = [0.3, 0.3, 0.15, 0.25][after]; if (delta >= 6) chance *= 0.5; else if (delta < 0) chance = Math.min(0.9, chance + 0.3);
  const wet = Math.random() < chance; G.snowing = wet && after === 0; G.raining = wet && after !== 0;
  // farm growth
  const earth = perk('earth'); let matured = 0;
  for (const k in G.crops) {
    const c = G.crops[k]; const def = CROPS[c.id];
    if (c.watered && !mature(c) && after >= def.season) { c.days++; if (!mature(c) && Math.random() < 0.1 * earth) c.days++; if (mature(c)) matured++; }
    c.watered = G.raining;
  }
  for (const k in G.tiles) G.tiles[k].wet = G.raining;
  const bloom = perk('bloom'); if (bloom) { delta += bloom; lines.push(`Your spirit hummed you to sleep  [#9dff9d]+${bloom}[/]`); changeHeart(bloom); }
  // market + resources + log
  const cands = Object.values(CROPS).filter(c => c.season <= after).map(c => c.harvest);
  G.market = { demand: cands[Math.floor(Math.random() * cands.length)], haggle: 0, haggled: false, sold: {} };
  G.cleared = []; G.log = { harvested: 0, meals: 0, exhausted: false, starved: false, drank: false };
  G.energy = passedOut ? maxEnergy() * 0.5 : maxEnergy();
  G.day++; G.hour = 6; G.minute = 0; G.warnedMid = false;

  let header = `[${SEASON_COLOR[after]}]${SEASONS[after]}, Day ${G.day}[/]\n` + (G.snowing ? 'Soft snow is falling.' : G.raining ? "It's raining. The fields are watered." : (delta >= 6 ? 'A clear morning. Yesterday was a good day.' : 'A clear morning.'));
  if (matured) header += `\n${matured} crop(s) are ready to harvest!`;
  if (after > before) header = `[${SEASON_COLOR[after]}]${SEASONS[after]} has come[/]\n${ARRIVAL[after]}\n[#9dff9d]Now growing: ${Object.values(CROPS).filter(c => c.season === after).map(c => c.harvest).join(', ')}[/]`;
  else if (after < before) header = `[#c8d8ff]A cold snap...[/]\nYou pushed yourself too hard. It feels like ${SEASONS[after]} again. Some crops stopped growing for now.`;
  const [fDelta, fHead0] = forestMorning(lines);
  let fHead = fHead0;
  if (lakeCheckOpen()) fHead = (fHead ? fHead + '\n' : '') + '[#8fd0ff]The gray reeds south-east of the farm turned green and parted.[/]\nA path leads down to the lake.';
  if (Lk().open) rollLakeForage();
  if (hillCheckOpen()) fHead = (fHead ? fHead + '\n' : '') + '[#ffb070]The thorns on the south lane turned into an arch of autumn leaves.[/]\nA path climbs up to Sunset Hill.';
  if (fDelta) { delta += fDelta; changeHeart(fDelta); }
  if (fHead) header += '\n' + fHead;
  if (G.raining) report('rain_morning');
  showAnnounce(`${header}\n\n${lines.join('\n')}\n\n[#ff8fb1]Heart ${delta >= 0 ? '+' : ''}${Math.round(delta)}  (${Math.round(G.heart)}/100)[/]${todayPreview(after)}`, 9);
  letters();
  save();
}
function todayPreview(season) {
  const tips = [];
  const ripe = Object.values(G.crops).filter(mature).length; if (ripe) tips.push(`${ripe} crop(s) ripe`);
  const tm = Object.values(G.crops).filter(c => !mature(c) && season >= CROPS[c.id].season && c.days + 1 >= CROPS[c.id].days).length;
  if (tm) tips.push(`${tm} crop(s) ripen tomorrow if watered`);
  if (season < 3) tips.push(`${SEASONS[season + 1]} at ${Math.ceil(THRESH[season + 1] - G.heart)} more Heart`);
  const ready = VILLAGERS.find(v => villagerProgress(v.id).met && wishReady(v)); if (ready) tips.push(`You have everything for ${ready.name}'s wish`);
  if (unlocked('starhill') && !G.raining && !G.snowing) tips.push('Clear skies tonight: the star vine will grow');
  const left = 6 - PHOTOS.filter(p => p.season === season && S().photos.includes(p.id)).length; if (left) tips.push(`${left} ${SEASONS[season]} photo(s) left [P]`);
  return tips.length ? '\n\n[#ffd84a]Today[/]  ' + tips.slice(0, 3).join('  ·  ') : '';
}
function letters() {
  if (G.day >= 4 && unlock('letter1')) {
    ask('A letter from the city', '"It\'s your old boss. We\'re short-staffed. Come back this weekend, just two days. I\'ll pay 400G."\n[#c8c0b0](Accept: +400G, but the gray creeps back.)[/]', null,
      () => { addGold(400); G.energy *= 0.5; changeHeart(-12, 'You worked all weekend. The gray creeps back in.'); },
      () => changeHeart(5, 'You wrote back: "No, thank you." It felt good.'), 'Accept', 'Decline');
  } else if (G.day >= 10 && unlocked('letter1') && unlock('letter2')) {
    ask('A letter from the city', '"Final offer. Full-time position, 1500G signing bonus."\n[#c8c0b0](Accept: +1500G. The city will take its toll.)[/]', null,
      () => { addGold(1500); changeHeart(-25, 'Your phone buzzes all night. Everything looks gray again.'); },
      () => { changeHeart(8, 'You tore up the letter. This is home now.'); unlock('chose_village'); }, 'Accept', 'Tear it up');
  }
}

// ------------------------------------------------------------------ scenes / travel
function go(scene, x, y) {
  G.scene = scene; G.px = x; G.py = y; panel = null; fishing = null;
  if (G.spirit) { G.spirit.x = x - 0.8; G.spirit.y = y + 0.8; }
  if (scene === 'forest' && F().forageDay !== G.day) rollForage();
  if (scene === 'lake' && Lk().forageDay !== G.day) rollLakeForage();
  if (scene === 'market') toast(`Today's demand: ${G.market.demand} (+50%)` + (G.market.haggle ? `  |  Haggle bonus +${Math.round(G.market.haggle * 100)}%` : ''), 4);
  if (scene === 'haggle') {
    if (G.market.haggled) { haggle = { done: true, text: 'The merchant is busy today.\nCome back tomorrow!', t: 2.5 }; }
    else haggle = { v: 0, dir: 1, done: false };
  }
  if (scene === 'hill') { if (S().starsDay !== G.day) { S().starsDay = G.day; S().starsTonight = 0; } G.stars = []; G.starTimer = 1.5; report('starhill'); toast('You climb the glowing vine... the whole sky opens up.'); }
}
function objectsFor(scene) {
  if (scene !== 'farm') return [];
  const o = [];
  TREES.forEach(([x, y], i) => o.push({ kind: 'tree', x, y, key: 't' + i }));
  STONES.forEach(([x, y], i) => o.push({ kind: 'rock', x, y, key: 's' + i }));
  APPLES.forEach(([x, y], i) => o.push({ kind: 'apple', x, y, key: 'a' + i }));
  for (const [x, y, ch] of MAPCELLS) {
    const kind = ch === 'T' ? 'tree' : ch === 'r' ? 'rock' : ch === 'a' ? 'apple' : null;
    if (kind) o.push({ kind, x: x + 0.5, y: y + 0.5, key: 'm' + x + ',' + y });
  }
  return o.filter(ob => !G.cleared.includes(ob.key));
}
function gather(ob) {
  if (ob.kind === 'apple') { G.cleared.push(ob.key); add('Apple'); toast('Picked up an Apple. (Right-click / F to eat)'); return; }
  if (!useEnergy(8)) { G.px += (G.px - ob.x) * 0.3; G.py += (G.py - ob.y) * 0.3; return; }
  G.cleared.push(ob.key);
  const item = ob.kind === 'tree' ? 'Wood' : 'Stone'; add(item);
  if (Math.random() * 100 < 10 * (0.5 + G.heart / 100)) {
    const sp = ob.kind === 'tree' ? 'Sunshine Shard' : "Earth's Breath"; add(sp);
    toast(`[#ffe28a]You found ${sp}![/]`);
  } else toast(`+1 ${item}`, 1.5);
}

// ------------------------------------------------------------------ interaction (E)
function near(x, y, r) { return Math.hypot(G.px - x, G.py - y) <= r; }
function interact() {
  if (G.scene === 'sunset') {
    if (near(HRACK[0], HRACK[1], 1.5)) { useRack(); return; }
    if (near(HBENCH[0], HBENCH[1], 1.6)) {
      const [layers, caption] = panoramaLayers(); panel = { kind: 'view', layers, caption };
      if (Hl().restDay !== G.day) { Hl().restDay = G.day; changeHeart(2, 'You sat on the hill and watched the sun go down over the valley.'); }
    }
    return;
  }
  if (G.scene === 'lake') {
    const spot = LFISH.find(([x, y]) => near(x, y, 1.3)); if (spot || (fishing && fishing.lake)) { fish(spot || fishing.at, true); return; }
    if (near(LSTEPS[0], LSTEPS[1], 1.3)) { releaseFish(); return; }
    if (near(LBENCH[0], LBENCH[1], 1.3)) { if (Lk().restDay !== G.day) { Lk().restDay = G.day; changeHeart(2, 'You sat under the willow and watched the water.'); } else toast("The willow sways. You've already rested here today."); return; }
    return;
  }
  if (G.scene === 'forest') {
    if (near(FSEAT[0], FSEAT[1], 1.3)) {
      G.thirst = 100; G.log.drank = true; const cap = waterCap(); if (cap) G.water = cap;
      if (F().restDay !== G.day) { F().restDay = G.day; changeHeart(2, 'You sat by the spring and listened to the forest.'); }
      else toast("You drink from the spring. It's cool and clear.");
      return;
    }
    if (near(FTREE[0], FTREE[1] + 0.6, 2.2)) { toast(F().treeAwake ? 'The old tree hums softly. Petals drift down around you.' : `The old tree is asleep, gray to its roots. Maybe if the clearings around it came back to life... (${forestRestored()}/3)`, 4); return; }
    return;
  }
  if (G.scene === 'farm') {
    const mina = VILLAGERS[2]; if (near(mina.x, mina.y, 1.0)) { talk(mina); return; }
    if (near(3.68, -0.4, 1.2)) { G.thirst = 100; G.log.drank = true; report('drink'); const cap = waterCap(); if (cap) { G.water = cap; toast(`You drink cool water and fill your watering can. (${G.water}/${cap})`); } else toast('You drink cool, fresh water.'); return; }
    if (starVineOpen() && near(LADDER[0], LADDER[1] - 1.0, 1.2)) { go('hill', 0, -3.1); return; }
    if (near(POND[0], POND[1], 1.9)) { fish(); return; }
  } else if (G.scene === 'home') {
    if (Math.abs(G.px + 0.07) < 1.2 && Math.abs(G.py - 1.26) < 1.5) { trySleep(); return; }
  } else if (G.scene === 'market') {
    for (const v of VILLAGERS.slice(0, 2)) if (near(v.x, v.y, 1.0)) { talk(v); return; }
    if (near(-4.2, -2.0, 1.1)) { share(); return; }
    if (Math.abs(G.px - 3.92) < 3 && Math.abs(G.py - 3.01) < 4.5) { panel = { kind: 'shop', tab: 'buy' }; return; }
  } else if (G.scene === 'hill') {
    if (near(0, -4.4, 1.4)) { go('farm', LADDER[0] - 0.6, LADDER[1] - 1.0); return; }
    if (near(5.1, -2.0, 1.5)) { if (S().benchDay === G.day) toast('You already watched the stars tonight.'); else { S().benchDay = G.day; changeHeart(3, 'You sat on the bench and watched the stars.'); } }
  }
}
function share() {
  const item = selectedItem(); const d = ITEMS[item];
  if (!d || !['Crop', 'Food', 'Fish'].includes(d.cat)) { toast('Hold crops, food or fish to share them with the village.'); return; }
  const room = 5 - (S().shareDay === G.day ? S().sharedToday : 0);
  if (room <= 0) { toast('The sharing box is full for today. Come back tomorrow!'); return; }
  const per = Math.max(1, Math.floor(d.sell / 10)); const count = Math.min(G.inv[item], keys.ShiftLeft || keys.ShiftRight ? Math.ceil(room / per) : 1);
  remove(item, count); if (S().shareDay !== G.day) { S().shareDay = G.day; S().sharedToday = 0; }
  const got = Math.min(per * count, room); S().sharedToday += got; S().kindness += got;
  toast(`You shared ${item} x${count}.  [#ff9fc4]Kindness +${got}[/]  (${S().kindness}/60)`);
  addKindness(got, true); report('share');
}
function fish(at = POND, lake = false) {
  if (!fishing) {
    if (!has('Fishing Rod')) { toast('You need a fishing rod. Maybe someone nearby has an old one?'); return; }
    if (!useEnergy(3)) return; fishing = { state: 'wait', t: 1.5 + Math.random() * 3, at, lake }; toast('You cast your line... wait for it.', 2); return;
  }
  if (fishing.state === 'wait') { fishing = null; toast('Too early! The fish swam away.'); return; }
  const onLake = fishing.lake; fishing = null;
  const koi = 0.03 + 0.07 * G.heart / 100 + 0.02 * perk('water'); const r = Math.random();
  const f = onLake ? lakeCatch(r) : r < koi ? 'Golden Koi' : r < koi + 0.07 ? 'Old Boot' : r < koi + 0.42 ? 'Carp' : 'Minnow';
  const first = !S().fish[f]; add(f);
  if (f !== 'Old Boot') { S().fish[f] = (S().fish[f] || 0) + 1; report('fish', f); }
  if (f === 'Golden Koi') { showAnnounce('[#ffd84a]Golden Koi![/]\n\nIt shimmers like sunlight on water.', 4); if (first) changeHeart(5, 'A once-in-a-lifetime catch!'); }
  else toast(f === 'Old Boot' ? "You caught... an Old Boot. Well, it's something." : `You caught a ${f}!` + (first ? '  [#9dff9d](new!)[/]' : ''));
}
const starVineOpen = () => unlocked('starhill') && G.hour >= 20 && !G.raining && !G.snowing;

// ------------------------------------------------------------------ panels (shop / craft / journal / album)
let hits = [];  // clickable regions for the current frame
function hit(x, y, w, h, fn) { hits.push({ x, y, w, h, fn }); }
function shopStock() {
  const list = SHOP.slice();
  if (unlocked('shop_sunflower')) list.push(['Sunflower Seed', 60]);
  if (S().season >= 3) list.push(['Pumpkin Seed', 70]);
  if (!has('Mattock')) list.push(['Mattock', 150]);
  return list.filter(([n]) => { const d = ITEMS[n]; return !(d.cat === 'Seed' && d.crop && CROPS[d.crop].season > S().season); });
}
function canCraft(r) {
  if (r.station && !has(r.station)) return `Needs a ${r.station}.`;
  for (const [n, c] of r.req) if (!has(n, c)) return `Not enough ${n}.`;
  return null;
}
function drawPanel() {
  if (!panel) return;
  hit(0, 0, CW, CH, () => {}); // block clicks to the world while a panel is open (checked last)
  const x = 124, y = 70, w = 900, h = 500; box(x, y, w, h);
  ctx.textBaseline = 'top';
  const close = () => { panel = null; };
  if (panel.kind === 'shop') {
    drawRich(`[#ffd84a]Market[/]   Gold ${G.gold}G   Today's demand: ${G.market.demand} (+50%)`, x + 24, y + 20, 24, 820);
    ['buy', 'sell'].forEach((t, i) => {
      const bx = x + 24 + i * 120, by = y + 60; box(bx, by, 110, 40, panel.tab === t ? '#5a3f2a' : '#2a201a', panel.tab === t ? '#ffd84a' : '#8a6a4a');
      drawRich(t === 'buy' ? 'Buy' : 'Sell', bx + 55, by + 10, 22, 100, 'center'); hit(bx, by, 110, 40, () => { panel.tab = t; });
    });
    const rows = panel.tab === 'buy' ? shopStock().map(([n, p]) => ({ n, label: `${p}G`, fn: () => { if (G.gold >= p) { G.gold -= p; add(n); toast(`Bought ${n}.`, 1.6); } else toast('Not enough gold!'); } }))
      : Object.keys(G.inv).filter(canSell).sort((a, b) => sortKey(a) - sortKey(b)).map(n => ({ n, label: `x${G.inv[n]}  ${sellPrice(n)}G${n === G.market.demand ? ' *' : ''}`, fn: () => { const p = sellPrice(n); remove(n); G.market.sold[n] = (G.market.sold[n] || 0) + 1; addGold(p); toast(`Sold ${n} for ${p}G.`, 1.6); } }));
    rows.slice(0, 15).forEach((r, i) => {
      const cx = x + 24 + (i % 3) * 290, cy = y + 120 + Math.floor(i / 3) * 70;
      box(cx, cy, 276, 60, '#2a201a', '#8a6a4a');
      const im = IMG[iconKey(r.n)]; if (im) ctx.drawImage(im, cx + 8, cy + 6, 48, 48);
      drawRich(r.n, cx + 64, cy + 8, 18, 200); drawRich(`[#ffd84a]${r.label}[/]`, cx + 64, cy + 32, 18, 200);
      hit(cx, cy, 276, 60, r.fn);
    });
    if (!rows.length) drawRich('Nothing to sell yet. Tools cannot be sold.', x + 24, y + 130, 20, 800, 'left', '#c8c0b0');
    drawRich('Click to buy / sell one.   [E] or [Esc] Close', x + w - 24, y + h - 40, 18, 700, 'right', '#c8c0b0');
  } else if (panel.kind === 'craft') {
    drawRich('[#ffd84a]Crafting[/]   Cooking needs a Campfire in your bag.', x + 24, y + 20, 24, 840);
    const list = RECIPES.filter(r => !r.lock || unlocked(r.lock));
    list.forEach((r, i) => {
      const cx = x + 24 + (i % 2) * 432, cy = y + 66 + Math.floor(i / 2) * 76; const why = canCraft(r);
      box(cx, cy, 420, 68, why ? '#241b16' : '#2f3a22', why ? '#6a5a4a' : '#9dff9d');
      const im = IMG[iconKey(r.out)]; if (im) ctx.drawImage(im, cx + 8, cy + 8, 52, 52);
      drawRich(`${r.out}${r.n > 1 ? ' x' + r.n : ''}`, cx + 70, cy + 8, 19, 340, 'left', why ? '#b8ab98' : '#f6efe2');
      drawRich(r.req.map(([n, c]) => `${n} x${c}`).join(', ') + (r.station ? `  [${r.station}]` : ''), cx + 70, cy + 34, 15, 340, 'left', '#c8c0b0');
      hit(cx, cy, 420, 68, () => {
        const why2 = canCraft(r); if (why2) { toast(why2); return; }
        r.req.forEach(([n, c]) => remove(n, c)); add(r.out, r.n);
        if (ITEMS[r.out].tool === 'can') G.water = waterCap();
        toast(`Crafted ${r.out}!`); report('craft', r.out);
      });
    });
    drawRich('[C] or [Esc] Close', x + w - 24, y + h - 40, 18, 400, 'right', '#c8c0b0');
  } else if (panel.kind === 'journal') {
    const s = S(); const sp = s.spirit; const L = [];
    L.push(`[#ffd84a]Journal[/]   Day ${G.day}   [${SEASON_COLOR[s.season]}]${SEASONS[s.season]}[/]` + (s.season < 3 ? `  (${SEASONS[s.season + 1]} at ${Math.ceil(THRESH[s.season + 1] - G.heart)} more Heart)` : ''));
    L.push(`[#ff8fb1]Heart[/] ${Math.round(G.heart)}/100   Gold ${G.gold}   Album ${s.photos.length}/24`);
    L.push(''); L.push('[#ffd84a]Villagers[/]');
    VILLAGERS.forEach(v => { const p = villagerProgress(v.id); L.push(`${v.name}  [${'#'.repeat(p.wish)}${'-'.repeat(3 - p.wish)}]  ` + (!p.met ? '[#a0a0a0]not met yet[/]' : p.wish >= 3 ? '[#9dff9d]colors restored[/]' : 'wants ' + itemsText(v.wishes[p.wish].items))); });
    if (sp.found) L.push(`\n[#ff9fc4]Spirit[/]  ${FORM_NAMES[spiritForm(sp)]}   [#ffd84a]Sun ${sp.pts[0]}[/]  [#9dff9d]Earth ${sp.pts[1]}[/]  [#8fd0ff]Water ${sp.pts[2]}[/]  [#ff9fc4]Bloom ${sp.pts[3]}[/]   forms ${sp.forms.length}/10`);
    L.push(`\n[#ffb6d0]Kindness[/] ${s.kindness}/60  ` + (unlocked('starhill') ? `Star Hill open on clear nights. Stars caught: ${s.starsTotal}` : `(sharing box today ${S().shareDay === G.day ? S().sharedToday : 0}/5)`));
    if (s.forest.open) L.push(`[#9dff9d]Forest[/]  clearings restored ${forestRestored()}/3   ${s.forest.treeAwake ? 'The old tree is awake.' : 'The old tree is asleep.'}`);
    if (s.hill.open) L.push(`[#ffb070]Sunset Hill[/]  orchard trees revived ${hillRevived()}/6   on the drying rack: ${s.hill.drying.length}`);
    if (s.lake.open) L.push(`[#8fd0ff]Lake[/]  clear parts ${lakeZones()}/3   fish released ${s.lake.released}` + (lakeClear() ? '   Fireflies come out at night.' : `   ${LAKE_THRESH.find(t => s.lake.clarity < t) - s.lake.clarity} more to clear the next part`));
    L.push(`\n[#8fb8ff]Fish caught[/]  ` + ['Minnow', 'Carp', 'Golden Koi', 'Bluegill', 'Moonfin', 'Glassfish'].map(f => s.fish[f] ? `${f} x${s.fish[f]}` : '???').join('   '));
    drawRich(L.join('\n'), x + 30, y + 24, 20, 840);
    drawRich('[J] or [Esc] Close', x + w - 24, y + h - 40, 18, 400, 'right', '#c8c0b0');
  } else if (panel.kind === 'view') {
    ctx.fillStyle = 'rgba(10,8,12,0.94)'; ctx.fillRect(0, 0, CW, CH);
    const vw = 1024, vh = 576, vx = (CW - vw) / 2, vy = 14;
    ctx.fillStyle = '#c89552'; ctx.fillRect(vx - 4, vy - 4, vw + 8, vh + 8);
    ctx.imageSmoothingEnabled = false;
    for (const key of panel.layers) { const im = IMG['World/' + key]; if (im) ctx.drawImage(im, vx, vy, vw, vh); }
    drawRich(panel.caption, CW / 2, vy + vh + 18, 18, 1100, 'center');
    drawRich('[E] or [Esc] Close', CW / 2, vy + vh + 44, 15, 600, 'center', '#c8c0b0');
  } else if (panel.kind === 'album') {
    const page = panel.page; const reached = page <= S().highest;
    const count = PHOTOS.filter(p => p.season === page && S().photos.includes(p.id)).length;
    drawRich(`[${SEASON_COLOR[page]}]${SEASONS[page]}[/]   ${count}/6`, x + w / 2, y + 18, 30, 800, 'center');
    PHOTOS.filter(p => p.season === page).forEach((p, i) => {
      const cx = x + 40 + (i % 3) * 280, cy = y + 74 + Math.floor(i / 3) * 200; const taken = S().photos.includes(p.id);
      ctx.fillStyle = taken ? '#f6efe2' : '#7a7068'; ctx.fillRect(cx, cy, 250, 140);
      const im = IMG[p.sprite];
      if (im) { const k = Math.min(110 / im.height, 210 / im.width); const iw = im.width * k, ih = im.height * k;
        ctx.save(); if (!taken) ctx.filter = 'brightness(0.12) saturate(0)'; ctx.drawImage(im, cx + 125 - iw / 2, cy + 70 - ih / 2, iw, ih); ctx.restore(); }
      drawRich(taken ? p.title : '???', cx + 125, cy + 148, 18, 250, 'center', taken ? '#ffd84a' : '#d8ccbc');
      if (!taken && reached) drawRich(p.hint, cx + 125, cy + 170, 14, 250, 'center', '#a89888');
    });
    drawRich(reached ? `[<-] [->] Season    [P] Close    Total ${S().photos.length}/24` : "This season hasn't come yet. Keep living well.", x + w / 2, y + h - 40, 18, 800, 'center', '#c8c0b0');
  }
  void close;
}

// ------------------------------------------------------------------ update
let frame = 0, speed = 1, last = 0;
function update(dt) {
  frame++;
  for (const t of toasts) t.t -= dt; toasts = toasts.filter(t => t.t > 0);
  if (announce) { announce.t -= dt; if (announce.t <= 0) announce = null; }

  // dialogue input
  if (dialogue && dialogue.born !== frame) {
    if (dialogue.choice) {
      if (pressed.KeyY) { const c = dialogue.choice; dialogue = null; c.yes && c.yes(); }
      else if (pressed.KeyN || pressed.Escape) { const c = dialogue.choice; dialogue = null; c.no && c.no(); }
    } else if (pressed.KeyE || pressed.Space || pressed.Enter) {
      dialogue.i++; if (dialogue.i >= dialogue.lines.length) { const d = dialogue.done; dialogue = null; d && d(); }
    }
    clearPressed(); return;
  }
  if (panel) {
    if (pressed.Escape || (panel.kind === 'view' && (pressed.KeyE || pressed.Space)) || (panel.kind === 'shop' && pressed.KeyE) || (panel.kind === 'craft' && pressed.KeyC) || (panel.kind === 'journal' && pressed.KeyJ) || (panel.kind === 'album' && pressed.KeyP)) panel = null;
    else if (panel.kind === 'album') { if (pressed.ArrowLeft) panel.page = (panel.page + 3) % 4; if (pressed.ArrowRight) panel.page = (panel.page + 1) % 4; }
    clearPressed(); return;
  }
  if (announce && (pressed.Space || pressed.Enter) && performance.now() - announce.born > 500) announce = null;
  if (G.dead) { clearPressed(); return; }

  // haggle minigame
  if (G.scene === 'haggle') {
    if (!haggle.done) {
      haggle.v += haggle.dir * 2 * dt; if (haggle.v >= 1) { haggle.v = 1; haggle.dir = -1; } if (haggle.v <= 0) { haggle.v = 0; haggle.dir = 1; }
      if (pressed.Space) {
        const d = Math.abs(haggle.v - 0.5); let bonus = 0, text;
        if (d <= 0.1 * 0.35) { bonus = 0.5; text = 'PERFECT! Today\'s sell prices +50%'; } else if (d <= 0.1) { bonus = 0.25; text = 'Deal! Today\'s sell prices +25%'; } else text = 'Haggle failed... Try again tomorrow.';
        G.market.haggled = true; G.market.haggle = Math.max(G.market.haggle, bonus); if (bonus) { changeHeart(1, 'A friendly chat with the merchant.'); report('haggle'); }
        haggle = { done: true, text, t: 2.2 };
      }
    } else { haggle.t -= dt; if (haggle.t <= 0) go('farm', ...arrival('MiniGame')); }
    clearPressed(); return;
  }

  // time + needs
  const hours = dt * 2 * speed / 60;
  G.minute += dt * 2 * speed; while (G.minute >= 60) { G.minute -= 60; G.hour++; }
  if (G.hour >= 24 && !G.warnedMid) { G.warnedMid = true; toast("It's midnight. You should go to bed soon."); }
  if (G.hour >= 26) { endDay(true); go('home', 0, -3.5); clearPressed(); return; }
  G.hunger = Math.max(0, G.hunger - 5 * hours); G.thirst = Math.max(0, G.thirst - 7 * hours);
  if (G.hunger <= 0 || G.thirst <= 0) { G.log.starved = true; G.health -= 15 * hours; if (G.health <= 0) { G.health = 0; G.dead = true; showAnnounce('[#ff8080]You collapsed...[/]\n\nTake better care of yourself.\n\nPress "새로 시작" below to try again.', 9999); } }
  else if (G.hunger > 50 && G.thirst > 50) G.health = Math.min(100, G.health + 2 * hours);
  if (G.hunger < 20 && !G.warnedH) { G.warnedH = true; toast("You're getting hungry. (Right-click / F to eat)"); } if (G.hunger > 30) G.warnedH = false;
  if (G.thirst < 20 && !G.warnedT) { G.warnedT = true; toast("You're thirsty. Drink at the well (E)."); } if (G.thirst > 30) G.warnedT = false;

  // movement
  let mx = (keys.KeyD || keys.ArrowRight ? 1 : 0) - (keys.KeyA || keys.ArrowLeft ? 1 : 0);
  let my = (keys.KeyW || keys.ArrowUp ? 1 : 0) - (keys.KeyS || keys.ArrowDown ? 1 : 0);
  const moving = mx || my;
  if (moving) {
    const l = Math.hypot(mx, my); let sp = 5 * (G.hunger <= 0 || G.thirst <= 0 ? 0.6 : 1);
    const ox = G.px, oy = G.py;
    const tiles = G.scene === 'forest' || G.scene === 'lake' || G.scene === 'sunset';
    G.px += mx / l * sp * dt; if (tiles && feetBlocked()) G.px = ox;   // trees, water, the old tree, logs, reeds
    G.py += my / l * sp * dt; if (tiles && feetBlocked()) G.py = oy;
    if (Math.abs(mx) > Math.abs(my)) { G.dir = 'side'; G.facingLeft = mx < 0; } else G.dir = my > 0 ? 'up' : 'down';
    G.stepT += dt; if (G.stepT > 0.125) { G.stepT = 0; G.step = (G.step + 1) % 4; }
  } else G.step = 0;
  clampPlayer();

  // portals
  if (G.scene === 'farm') {
    const open = MAP.exits.find(e => !e.locked && inRect(exitRect(e), G.px, G.py));
    if (near(-8.38, -4.54, 0.7)) go('home', 0, -3.5);
    else if (open) go(open.to.toLowerCase(), open.spawnX, open.spawnY);          // the end of the road is the way to the market
    else if (G.py > 4.35 && Math.abs(G.px) < 0.7) go('haggle', 0, 0);
    const gate = MAP.exits.find(e => areaOpen(e.to) && inRect(exitRect(e), G.px, G.py));
    if (gate && G.scene === 'farm') {
      const R = REGIONS[gate.to], a = R.map.arrivals.find(a => a.from === 'Farm'); go(R.scene, a.x, a.y); toast(R.line);
    }
    const thorn = MAP.exits.find(e => e.locked && !areaOpen(e.to) && inRect(exitRect(e), G.px, G.py - 0.4, 0.5));
    if (thorn && performance.now() > (G.signT || 0)) {
      G.signT = performance.now() + 4000;
      toast(S().season >= SEASONS.indexOf(thorn.locked) ? 'The gray thorns have started to loosen... Not yet, but soon.' : thorn.sign, 3.5);
    }
  }
  if (G.scene === 'sunset') { const back = HILL.exits.find(e => inRect(exitRect(e), G.px, G.py)); if (back) go('farm', back.spawnX, back.spawnY); }
  if (G.scene === 'home' && endingReady() && !announce && !dialogue && !panel) startHomeEnding();
  if (G.scene === 'lake') {
    const back = LAKE.exits.find(e => inRect(exitRect(e), G.px, G.py)); if (back) go('farm', back.spawnX, back.spawnY);
    const l = Lk(); const i = l.forage.findIndex(([x, y]) => near(x + 0.5, y + 0.4, 0.45));
    if (i >= 0) { const [, , item] = l.forage.splice(i, 1)[0]; add(item); toast(`You found ${item}.`, 2.5); }
    if (fishing) {
      fishing.t -= dt;
      if (fishing.state === 'wait' && fishing.t <= 0) { fishing.state = 'bite'; fishing.t = 0.9 + 0.2 * perk('water'); toast('[#ffd84a]!![/]  Something\'s biting! Press E!', 1); }
      else if (fishing.state === 'bite' && fishing.t <= 0) { fishing = null; toast('It got away...'); }
      if (fishing && !near(fishing.at[0], fishing.at[1], 2.2)) { fishing = null; toast('You reeled in your line.'); }
    }
  }
  if (G.scene === 'forest') {
    const back = FOREST.exits.find(e => inRect(exitRect(e), G.px, G.py)); if (back) go('farm', back.spawnX, back.spawnY);
    const f = F(); const i = f.forage.findIndex(([x, y]) => near(x + 0.5, y + 0.4, 0.45));
    if (i >= 0) { const [, , item] = f.forage.splice(i, 1)[0]; add(item); toast(item === 'Acorn' ? 'You found an Acorn! Plant it in a clearing.' : `You found ${item}.`, 2.5); }
  }
  if (G.scene === 'farm') {
    for (const ob of objectsFor('farm')) if (near(ob.x, ob.y, ob.kind === 'apple' ? 0.45 : 0.55)) gather(ob);
    if (fishing) {
      fishing.t -= dt;
      if (fishing.state === 'wait' && fishing.t <= 0) { fishing.state = 'bite'; fishing.t = 0.9 + 0.2 * perk('water'); toast('[#ffd84a]!![/]  Something\'s biting! Press E!', 1); }
      else if (fishing.state === 'bite' && fishing.t <= 0) { fishing = null; toast('It got away...'); }
      if (fishing && !near(fishing.at[0], fishing.at[1], 2.2)) { fishing = null; toast('You reeled in your line.'); }
    }
  } else if (G.scene === 'home') { if (G.px > 8.4 && Math.abs(G.py) < 1.2) go('farm', ...arrival('Home')); }
  else if (G.scene === 'market') { if (G.px < -8.5 && Math.abs(G.py) < 1.3) go('farm', ...arrival('Market')); }
  else if (G.scene === 'hill') updateStars(dt);

  // spirit follows
  if (G.spirit) {
    const tx = G.px - 0.46, ty = G.py + 0.41 + Math.sin(performance.now() / 400) * 0.06;
    G.spirit.vx = (tx - G.spirit.x); G.spirit.x += (tx - G.spirit.x) * Math.min(1, dt * 5); G.spirit.y += (ty - G.spirit.y) * Math.min(1, dt * 5);
    G.spirit.pulse = Math.max(0, G.spirit.pulse - dt * 2.5);
  }
  VILLAGERS.forEach(v => v.pulse = Math.max(0, (v.pulse || 0) - dt * 2));
  for (const k in debrisPunch) { debrisPunch[k] -= dt * 6; if (debrisPunch[k] <= 0) delete debrisPunch[k]; }

  // keys
  if (pressed.KeyE) interact();
  if (pressed.KeyF) eat(selectedItem());
  if (pressed.KeyG) feedSpirit();
  if (pressed.KeyC) panel = { kind: 'craft' };
  if (pressed.KeyJ) panel = { kind: 'journal' };
  if (pressed.KeyP) panel = { kind: 'album', page: S().season };
  for (let i = 1; i <= 8; i++) if (pressed['Digit' + i]) { const it = hotbarItems(); if (hotOffset + i - 1 < it.length) selectIndex(hotOffset + i - 1); }
  clearPressed();
}
function feetBlocked() {
  const blocked = G.scene === 'lake' ? lakeBlocked : G.scene === 'sunset' ? hillBlocked : forestBlocked;
  for (const [dx, dy] of [[-0.2, -0.5], [0.2, -0.5], [-0.2, -0.28], [0.2, -0.28]]) if (blocked(Math.floor(G.px + dx), Math.floor(G.py + dy))) return true;
  return false;
}
function clampPlayer() {
  const s = sc(); let minX = s.cx - s.ww / 2 + 0.4, maxX = s.cx + s.ww / 2 - 0.4, minY = s.cy - s.wh / 2 + 0.6, maxY = s.cy + s.wh / 2 - 0.9;
  if (G.scene === 'farm') { minX = MAPB.x0 + 0.3; maxX = MAPB.x1 - 0.3; minY = MAPB.y0 + 0.5; maxY = Math.abs(G.px) > 0.7 ? MAPB.y1 - 0.8 : MAPB.y1 - 0.35; }
  if (G.scene === 'forest') { minX = FORB.x0 + 0.3; maxX = FORB.x1 - 0.3; minY = FORB.y0 + 0.5; maxY = FORB.y1 - 0.8; }
  if (G.scene === 'lake') { minX = LAKB.x0 + 0.3; maxX = LAKB.x1 - 0.3; minY = LAKB.y0 + 0.5; maxY = LAKB.y1 - 0.8; }
  if (G.scene === 'sunset') { minX = HILB.x0 + 0.3; maxX = HILB.x1 - 0.3; minY = HILB.y0 + 0.5; maxY = HILB.y1 - 0.8; }
  if (G.scene === 'market') maxY = 2.4;
  if (G.scene === 'home') maxY = 3.0;
  if (G.scene === 'hill') { minX = -8.4; maxX = 8.4; minY = -4.6; maxY = -1.2; }
  G.px = Math.max(minX, Math.min(maxX, G.px)); G.py = Math.max(minY, Math.min(maxY, G.py));
  const solids = [];
  if (G.scene === 'farm') {
    solids.push([3.68, -0.45, 0.55]); solids.push([VILLAGERS[2].x, VILLAGERS[2].y - 0.4, 0.25]);
    // stumps and boulders near the player block the way (weeds don't)
    const pcx = Math.floor(G.px), pcy = Math.floor(G.py - 0.4);
    for (let dx = -1; dx <= 1; dx++) for (let dy = -1; dy <= 1; dy++) { const ch = debrisAt(pcx + dx, pcy + dy); if (ch === 'k' || ch === 'o') solids.push([pcx + dx + 0.5, pcy + dy + 0.3, 0.45]); }
    // gray thorns / reeds close the lanes that are not open yet (west: forest, east: lake, south: sunset hill)
    for (const e of MAP.exits) if (e.locked && !areaOpen(e.to)) {
      const [x0, y0, x1, y1] = exitRect(e), feet = G.py - 0.4;
      if (x1 >= MAPB.x1 - 0.1 && feet > y0 - 0.3 && feet < y1 + 0.3) G.px = Math.min(G.px, x0 - 0.35);
      if (x0 <= MAPB.x0 + 0.1 && feet > y0 - 0.3 && feet < y1 + 0.3) G.px = Math.max(G.px, x1 + 0.35);
      if (y0 <= MAPB.y0 + 0.1 && G.px > x0 - 0.2 && G.px < x1 + 0.2) G.py = Math.max(G.py, y1 + 0.75);
    }
  }
  if (G.scene === 'market') { solids.push([-4.2, -2.35, 0.35]); VILLAGERS.slice(0, 2).forEach(v => solids.push([v.x, v.y - 0.4, 0.25])); }
  const fx = G.px, fy = G.py - 0.4;
  for (const [x, y, r] of solids) { const d = Math.hypot(fx - x, fy - y); if (d < r && d > 0) { G.px += (fx - x) / d * (r - d); G.py += (fy - y) / d * (r - d); } }
}
function updateStars(dt) {
  for (let i = G.stars.length - 1; i >= 0; i--) {
    const s = G.stars[i]; if (s.fall > 0) { const d = Math.min(s.fall, dt * 6); s.fall -= d; s.y -= d; continue; }
    if (near(s.x, s.y, 0.7)) { G.stars.splice(i, 1); add('Star Fragment'); S().starsTonight++; S().starsTotal++; report('star'); toast(`You caught a Star Fragment! (${S().starsTonight}/10 tonight)`); }
  }
  if (S().starsTonight + G.stars.length >= 10 || G.stars.length >= 3) return;
  G.starTimer -= dt; if (G.starTimer > 0) return; G.starTimer = 2 + Math.random() * 2;
  const x = -7.9 + Math.random() * 15.8, y = -4.3 + Math.random() * 2.8; G.stars.push({ x, y: y + 4, fall: 4 });
}
function clearPressed() { for (const k in pressed) delete pressed[k]; }

// ------------------------------------------------------------------ render
function daylight(h) { if (h >= 24 || h < 4) return 0; if (h < 7) return (h - 4) / 3; if (h < 17) return 1; if (h < 21) return 1 - (h - 17) / 4; return 0; }
// the camera follows the player inside the map and never shows past its edges (snapped to the art's pixel grid)
function updateCamera() {
  const s = sc(), b = s.bounds(); const hw = s.ww / 2, hh = s.wh / 2;
  const clamp = (v, a, b2) => Math.max(a, Math.min(b2, v));
  s.cx = Math.round(clamp(G.px, b.x0 + hw, b.x1 - hw) * 32) / 32;
  s.cy = Math.round(clamp(G.py, b.y0 + hh, b.y1 - hh) * 32) / 32;
}
// fireflies over a clear lake on summer nights (drawn above the night shade, they glow)
function drawFireflies(c) {
  const h = G.hour + G.minute / 60;
  if (!(lakeClear() && (h >= 20 || h < 4) && S().season !== 0)) return;
  const t = performance.now() / 1000, mid = [(LAKB.x0 + LAKB.x1) / 2, (LAKB.y0 + LAKB.y1) / 2];
  for (let i = 0; i < 16; i++) {
    const fx = mid[0] - 9 + (i * 37 % 18) + Math.sin(t * 0.35 + i * 1.7) * 1.2, fy = mid[1] - 5 + (i * 23 % 10) + Math.sin(t * 0.45 + i) * 0.8;
    sprite(c, 'World/firefly', fx, fy, 1, 'center', false, 0.6 + 0.4 * Math.sin(t * 3 + i));
  }
}
// the gray forest, with colour shown only through the round masks of restored clearings (all of it once the tree wakes)
const maskCanvas = document.createElement('canvas'); maskCanvas.width = CW; maskCanvas.height = CH; const mctx = maskCanvas.getContext('2d');
function drawMaskedGround(c, b, grayKey, colorKey, masks, full) {
  const mid = [(b.x0 + b.x1) / 2, (b.y0 + b.y1) / 2];
  sprite(c, grayKey, mid[0], mid[1]);
  if (full) { sprite(c, colorKey, mid[0], mid[1]); return; }
  if (!masks.length) return;
  mctx.globalCompositeOperation = 'source-over'; mctx.clearRect(0, 0, CW, CH);
  for (const [key, x, y] of masks) sprite(mctx, key, x, y);
  mctx.globalCompositeOperation = 'source-in'; sprite(mctx, colorKey, mid[0], mid[1]); mctx.globalCompositeOperation = 'source-over';
  c.drawImage(maskCanvas, 0, 0);
}
function drawHillGround(c) {
  const h = Hl();
  drawMaskedGround(c, HILB, sc().bg(), 'World/bg_hill_' + seasonKey(), HTREES.filter((t, i) => h.tree[i] >= 2).map(([x, y]) => ['World/forest_mask', x + 0.5, y + 1.2]), hillRevived() === 6);
}
function drawLakeGround(c) {
  const z = lakeZones();
  drawMaskedGround(c, LAKB, sc().bg(), 'World/bg_lake_' + seasonKey(), LAKE.zones.slice(0, z).map(m => ['World/lake_mask_' + m.size, m.x, m.y]), lakeClear());
}
function drawForestGround(c) {
  const mid = [(FORB.x0 + FORB.x1) / 2, (FORB.y0 + FORB.y1) / 2], color = 'World/bg_forest_' + seasonKey();
  sprite(c, sc().bg(), mid[0], mid[1]);
  const f = F();
  if (f.treeAwake) { sprite(c, color, mid[0], mid[1]); return; }
  if (!f.sapling.some(s => s >= 3)) return;
  mctx.globalCompositeOperation = 'source-over'; mctx.clearRect(0, 0, CW, CH);
  FSPOTS.forEach(([x, y], i) => { if (f.sapling[i] >= 3) sprite(mctx, 'World/forest_mask', x + 0.5, y + 0.5); });
  mctx.globalCompositeOperation = 'source-in'; sprite(mctx, color, mid[0], mid[1]); mctx.globalCompositeOperation = 'source-over';
  c.drawImage(maskCanvas, 0, 0);
}
const seasonal = base => IMG[`World/${base}_${seasonKey()}`] ? `World/${base}_${seasonKey()}` : `World/${base}`;
function renderWorld() {
  const c = wctx; c.clearRect(0, 0, CW, CH);
  if (sc().map) {
    updateCamera();
    if (G.scene === 'forest') drawForestGround(c);
    else if (G.scene === 'lake') drawLakeGround(c);
    else if (G.scene === 'sunset') drawHillGround(c);
    else sprite(c, sc().bg(), (MAPB.x0 + MAPB.x1) / 2, (MAPB.y0 + MAPB.y1) / 2);
  } else { const bg = IMG[sc().bg()] || IMG['World/bg_farm']; if (bg) c.drawImage(bg, 0, 0, CW, CH); }
  const draws = [];  // [sortY, fn]
  if (G.scene === 'farm') {
    const soil = FIELD.slice();
    for (const [x, y, ch] of MAPCELLS) if (ch === 'x' || (isDebris(ch) && debrisCleared(x, y))) soil.push(x + ',' + y);
    for (const k of soil) { const [x, y] = k.split(',').map(Number); const t = G.tiles[k]; const key = t ? (t.wet ? 'Tiles/tilled_wet' : 'Tiles/tilled') : 'Tiles/dirt';
      const [sx, sy] = toScreen(x, y + 1); if (sx < -64 || sy < -64 || sx > CW || sy > CH) continue; c.drawImage(IMG[key], Math.round(sx), Math.round(sy), 64, 64); }
    for (const [x, y, ch] of MAPCELLS) {
      if (!isDebris(ch) || debrisCleared(x, y)) continue;
      const k = x + ',' + y, p = debrisPunch[k] || 0;
      draws.push([y + 0.1, () => sprite(c, seasonal(debrisKey(ch, x, y)), x + 0.5, y + 0.03, 1 + Math.sin(p * Math.PI) * 0.12, 'bottom')]);
    }
    for (const e of MAP.exits) if (e.locked) {
      const [x0, y0, x1, y1] = exitRect(e), w = 56 / 32, h = 52 / 32;
      const tx = Math.max(MAPB.x0 + w / 2 - 0.3, Math.min(MAPB.x1 - w / 2 + 0.3, (x0 + x1) / 2)), ty = Math.max(MAPB.y0 + h / 2, Math.min(MAPB.y1 - h / 2, (y0 + y1) / 2));
      const art = areaOpen(e.to) ? (e.openArt || 'flower_arch') : (e.art || 'thorns');   // the season turns thorns into an arch, gray reeds green
      draws.push([ty - h / 2, () => sprite(c, seasonal(art), tx, ty)]);
    }
    for (const k in G.crops) {
      const cr = G.crops[k]; const [x, y] = k.split(',').map(Number); const def = CROPS[cr.id];
      const stage = mature(cr) ? 3 : Math.min(2, Math.floor(cr.days * 3 / def.days));
      draws.push([y + 0.5, () => { c.save(); if (!cr.watered && !mature(cr)) c.filter = 'brightness(0.8)'; if (cr.blessed) c.filter = 'sepia(0.6) saturate(2) brightness(1.15)';
        sprite(c, `Crops/${cr.id}_${stage}`, x + 0.5, y + 0.5); c.restore(); }]);
    }
    const seasonalTree = G.story.season === 0 ? 'World/tree_winter' : G.story.season === 3 ? 'World/tree_autumn' : 'World/tree';
    for (const ob of objectsFor('farm')) {
      if (ob.kind === 'tree') draws.push([ob.y - 0.35, () => sprite(c, seasonalTree, ob.x, ob.y - 0.35, 1, 'bottom')]);
      if (ob.kind === 'rock') draws.push([ob.y, () => sprite(c, G.story.season === 0 ? 'World/rock_winter' : 'World/rock', ob.x, ob.y)]);
      if (ob.kind === 'apple') draws.push([ob.y, () => sprite(c, 'World/apple_ground', ob.x, ob.y)]);
    }
    draws.push([-0.03 - 0.6, () => sprite(c, G.story.season === 0 ? 'World/well_winter' : 'World/well', 3.68, -0.03)]);
    if (starVineOpen()) draws.push([LADDER[1] - 1.1, () => sprite(c, 'World/star_ladder', LADDER[0], LADDER[1])]);
  }
  if (G.scene === 'lake') {
    for (const [x, y, item] of Lk().forage) draws.push([y + 0.3, () => sprite(c, 'World/' + item.toLowerCase().replace(/ /g, '_') + '_ground', x + 0.5, y + 0.4)]);
  }
  if (G.scene === 'forest') {
    const f = F();
    draws.push([FTREE[1] + 0.2, () => sprite(c, f.treeAwake ? 'World/old_tree_awake' : 'World/old_tree_sleep', FTREE[0], FTREE[1], 1, 'bottom')]);
    FSPOTS.forEach(([x, y], i) => {
      const st = f.sapling[i];
      draws.push([y + 0.2, () => { c.save(); if (st >= 0 && st < 3 && !f.watered[i]) c.filter = 'brightness(0.82)'; sprite(c, st < 0 ? 'World/sapling_spot' : 'World/sapling_' + st, x + 0.5, y + 0.05, 1, 'bottom'); c.restore(); }]);
    });
    for (const [x, y, ch] of FCELLS) if (ch === 'L' && !debrisCleared(x, y)) {
      const p = debrisPunch[x + ',' + y] || 0;
      draws.push([y + 0.1, () => sprite(c, seasonal('forest_log'), x + 0.5, y + 0.03, 1 + Math.sin(p * Math.PI) * 0.12, 'bottom')]);
    }
    for (const [x, y, item] of f.forage) draws.push([y + 0.3, () => sprite(c, 'World/' + item.toLowerCase().replace(/ /g, '_') + '_ground', x + 0.5, y + 0.4)]);
  }
  if (G.scene === 'sunset') {
    const h = Hl();
    HTREES.forEach(([x, y, k], i) => {
      const t = h.tree[i], key = t === 0 ? 'orchard_gray' : t === 1 ? 'orchard_pruned' : `orchard_${KIND[k]}_${hasFruit(i) ? 'fruit' : 'bare'}`;
      draws.push([y + 0.1, () => sprite(c, seasonal(key), x + 0.5, y + 0.05, 1, 'bottom')]);
    });
    const rk = h.drying.length === 0 ? 'drying_rack_empty' : h.drying.some(d => G.day >= d) ? 'drying_rack_dried' : 'drying_rack_hung';
    draws.push([HRACK[1] - 0.5, () => sprite(c, 'World/' + rk, HRACK[0], HRACK[1] - 0.45, 1, 'bottom')]);
  }
  if (G.scene === 'farm' && unlocked('ending_seen')) draws.push([-3.85, () => sprite(c, seasonal('cottage_color'), -7.75, -3.85, 1, 'bottom')]);
  if (G.scene === 'home') { draws.push([0.5, () => sprite(c, 'World/bed', -0.07, 1.26)]); draws.push([0, () => sprite(c, 'World/exit_sign', 8.28, 0.03)]); }
  if (G.scene === 'market') {
    draws.push([2.3, () => sprite(c, 'World/merchant', 3.92, 3.01)]);
    draws.push([-2.5, () => sprite(c, 'World/donation_box', -4.2, -2.0)]);
    draws.push([0, () => sprite(c, G.story.season === 0 ? 'World/exit_sign_winter' : 'World/exit_sign', -8.27, -0.05)]);
  }
  if (G.scene === 'hill') for (const s of G.stars) draws.push([s.y, () => sprite(c, 'Items/star_fragment', s.x, s.y, 0.5 * (1 + Math.sin(performance.now() / 160 + s.x) * 0.1))]);
  for (const v of VILLAGERS) if (v.scene === G.scene) {
    draws.push([v.y - 0.45, () => {
      const k = 1 + Math.sin((v.pulse || 0) * Math.PI) * 0.15; sprite(c, villagerSprite(v), v.x, v.y, k);
      if (!villagerProgress(v.id).met || wishReady(v)) sprite(c, 'World/bubble', v.x + 0.19, v.y + 0.62 + Math.sin(performance.now() / 250) * 0.03);
    }]);
  }
  if (G.scene !== 'haggle') {
    const frameIdx = G.step === 1 ? 1 : G.step === 3 ? 2 : 0;
    draws.push([G.py - 0.45, () => sprite(c, `Player/player_${G.dir}_${frameIdx}`, G.px, G.py, 1, 'center', G.dir === 'side' && G.facingLeft)]);
    if (G.spirit) draws.push([G.spirit.y - 0.3, () => sprite(c, 'Spirit/' + spiritForm(S().spirit), G.spirit.x, G.spirit.y, 1 + Math.sin(G.spirit.pulse * Math.PI) * 0.25, 'center', G.spirit.vx < -0.05)]);
  }
  draws.sort((a, b) => b[0] - a[0]).forEach(d => d[1]());
  // night
  if (G.scene !== 'haggle' && G.scene !== 'hill') {
    const dl = daylight(G.hour + G.minute / 60) * ((G.raining || G.snowing) ? 0.8 : 1);
    const a = (1 - Math.max(0.45, dl)) * 0.95; if (a > 0.01) { c.fillStyle = `rgba(16, 22, 60, ${a})`; c.fillRect(0, 0, CW, CH); }
  }
  if (G.scene === 'lake') drawFireflies(c);

  // seasons of the heart: the world's saturation follows Heart (UI layer is not affected)
  const sat = 1 + (-80 + 95 * G.heart / 100) / 100;
  worldCanvas.style.filter = `saturate(${sat.toFixed(2)})`;
}

let particles = [];
function renderUI() {
  hits = [];
  ctx.clearRect(0, 0, CW, CH); ctx.textBaseline = 'top';
  // weather
  if (sc().outdoor && (G.raining || G.snowing)) {
    if (particles.length !== 80 || particles.snow !== G.snowing) { particles = Array.from({ length: 80 }, () => ({ x: Math.random() * CW, y: Math.random() * CH, s: Math.random() })); particles.snow = G.snowing; }
    for (const p of particles) {
      if (G.snowing) { p.y += 0.8 + p.s; p.x += Math.sin(performance.now() / 600 + p.s * 9) * 0.6; ctx.fillStyle = 'rgba(255,255,255,0.85)'; ctx.fillRect(p.x, p.y, p.s > 0.7 ? 6 : 4, p.s > 0.7 ? 6 : 4); }
      else { p.y += 12 + p.s * 4; p.x -= 1.5; ctx.fillStyle = 'rgba(190,215,255,0.55)'; ctx.fillRect(p.x, p.y, 2, 16); }
      if (p.y > CH) { p.y = -20; p.x = Math.random() * CW; }
    }
  }
  if (G.scene === 'haggle') {
    box(274, 470, 600, 120);
    if (!haggle.done) {
      drawRich('Press [#ffd84a]Space[/] at the right time!', CW / 2, 488, 22, 560, 'center');
      ctx.fillStyle = '#3a2a1e'; ctx.fillRect(314, 530, 520, 24); ctx.fillStyle = '#5f9a42'; ctx.fillRect(314 + 520 * 0.4, 530, 520 * 0.2, 24);
      ctx.fillStyle = '#ffd84a'; ctx.fillRect(314 + 520 * 0.5 - 520 * 0.035, 530, 520 * 0.07, 24);
      ctx.fillStyle = '#ffffff'; ctx.fillRect(314 + 520 * haggle.v - 4, 522, 8, 40);
    } else drawRich(haggle.text, CW / 2, 500, 24, 560, 'center');
  }
  // status (top left)
  const s = S(); const time = `${String(G.hour % 24).padStart(2, '0')}:${String(Math.floor(G.minute)).padStart(2, '0')}`;
  box(12, 10, 300, 74, 'rgba(20,14,11,0.78)', '#6a4f36');
  drawRich(`[${SEASON_COLOR[s.season]}]${SEASONS[s.season]}[/] Day ${G.day}  ${time}`, 26, 20, 22, 290);
  drawRich(`${G.snowing ? '[#e4ecf4]Snow[/]' : G.raining ? '[#8fb8ff]Rain[/]' : '[#ffe28a]Sunny[/]'}   [#ffd84a]${G.gold}G[/]   [#ff8fb1]Heart ${Math.round(G.heart)}[/]`, 26, 50, 18, 290);
  // bars (top right)
  const bars = [['HP', G.health, '#e0483e'], ['Food', G.hunger, '#e0a13a'], ['Water', G.thirst, '#58a4e4'], ['Energy', G.energy, '#7fd36a']];
  box(CW - 262, 10, 250, 112, 'rgba(20,14,11,0.78)', '#6a4f36');
  bars.forEach(([n, v, col], i) => {
    const y = 22 + i * 24; drawRich(n, CW - 248, y, 15, 70, 'left', '#d8ccbc');
    ctx.fillStyle = '#2a201a'; ctx.fillRect(CW - 178, y + 2, 152, 14); ctx.fillStyle = col; ctx.fillRect(CW - 178, y + 2, 152 * Math.max(0, Math.min(1, v / 100)), 14);
    if (n === 'Energy') { ctx.fillStyle = 'rgba(0,0,0,0.5)'; const cap = maxEnergy() / 100; ctx.fillRect(CW - 178 + 152 * cap, y + 2, 152 * (1 - cap), 14); }
  });
  // toasts
  toasts.forEach((t, i) => {
    ctx.globalAlpha = Math.min(1, t.t / 0.5); const y = 96 + i * 42;
    ctx.fillStyle = 'rgba(12,9,7,0.72)'; ctx.fillRect(CW / 2 - 400, y, 800, 36); drawRich(t.text, CW / 2, y + 8, 18, 780, 'center'); ctx.globalAlpha = 1;
  });
  // hotbar
  if (G.scene !== 'haggle') {
    const items = hotbarItems(); const sel = selectedItem(); const si = items.indexOf(sel);
    if (si >= 0 && (si < hotOffset || si >= hotOffset + 8)) hotOffset = Math.max(0, si - 7);
    hotOffset = Math.max(0, Math.min(hotOffset, Math.max(0, items.length - 8)));
    const x0 = CW / 2 - (8 * 66) / 2;
    for (let i = 0; i < 8; i++) {
      const n = items[hotOffset + i]; const x = x0 + i * 66, y = CH - 76;
      box(x, y, 60, 60, 'rgba(32,24,18,0.9)', n && n === sel ? '#ffd84a' : '#6a4f36');
      if (n) { const im = IMG[iconKey(n)]; if (im) ctx.drawImage(im, x + 6, y + 6, 48, 48);
        const d = ITEMS[n]; const label = d.tool === 'can' ? String(G.water) : d.cat === 'Tool' ? '' : String(G.inv[n]);
        if (label) drawRich(label, x + 54, y + 38, 15, 50, 'right'); hit(x, y, 60, 60, () => selectIndex(hotOffset + i)); }
      drawRich(String(i + 1), x + 6, y + 4, 12, 20, 'left', '#a89888');
    }
    if (sel) drawRich(sel, CW / 2, CH - 104, 18, 400, 'center', '#f6efe2');
    if (items.length > 8) drawRich(`${hotOffset + 1}-${Math.min(items.length, hotOffset + 8)} / ${items.length}  (wheel)`, x0 + 8 * 66 + 8, CH - 56, 14, 200, 'left', '#a89888');
  }
  // contextual prompt
  const prompt = promptText(); if (prompt && !dialogue && !panel) { ctx.fillStyle = 'rgba(12,9,7,0.7)'; ctx.fillRect(CW / 2 - 250, CH - 140, 500, 30); drawRich(prompt, CW / 2, CH - 134, 16, 480, 'center', '#ffe9b0'); }
  drawPanel();
  // dialogue
  if (dialogue) {
    box(124, CH - 296, 900, 200, 'rgba(26,18,14,0.95)', '#a8784a');
    let tx = 160; const im = dialogue.portrait ? IMG[dialogue.portrait] : null;
    if (im) { const k = Math.floor(150 / im.height); ctx.drawImage(im, 150, CH - 280, im.width * k, im.height * k); tx = 150 + im.width * k + 24; }
    drawRich(`[#ffd84a]${dialogue.name}[/]`, tx, CH - 278, 22, 1000 - tx);
    drawRich(dialogue.lines[dialogue.i], tx, CH - 246, 19, 1004 - tx);
    drawRich(dialogue.choice ? `[Y] ${dialogue.choice.yl}     [N] ${dialogue.choice.nl}` : (dialogue.i < dialogue.lines.length - 1 ? '[E] Next' : '[E] Close'), 1004, CH - 132, 16, 400, 'right', '#d8ccbc');
  }
  // announcement
  if (announce) {
    ctx.globalAlpha = Math.min(1, announce.t / 0.6);
    const lines = richLines(announce.text, 20, 700, '#f6efe2'); const h = Math.min(520, lines.length * 26 + 50);
    box(CW / 2 - 380, CH / 2 - h / 2 - 30, 760, h, 'rgba(14,12,20,0.93)', '#8a6aa8');
    drawRich(announce.text, CW / 2, CH / 2 - h / 2 - 6, 20, 700, 'center');
    if (announce.t < 9000) drawRich('[Space] Close', CW / 2 + 360, CH / 2 + h / 2 - 54, 14, 200, 'right', '#a89888');
    ctx.globalAlpha = 1;
  }
}
function promptText() {
  if (G.scene === 'farm') {
    const mina = VILLAGERS[2]; if (near(mina.x, mina.y, 1.0)) return `[E] Talk to ${mina.name}`;
    if (near(3.68, -0.4, 1.2)) return '[E] Drink and refill your watering can';
    if (starVineOpen() && near(LADDER[0], LADDER[1] - 1.0, 1.2)) return '[E] Climb the glowing vine';
    if (near(POND[0], POND[1], 1.9)) return fishing ? (fishing.state === 'bite' ? '[#ffd84a][E] NOW![/]' : 'Waiting for a bite...') : (has('Fishing Rod') ? '[E] Fish' : 'The pond is full of fish');
    if (near(-8.38, -4.54, 1.6)) return 'Home';
    if (G.px < MAPB.x0 + 2.2 && Math.abs(G.py + 9.1) < 1.4) return F().open ? '← To the forest' : 'Gray thorns block the forest path';
    if (G.px > MAPB.x1 - 2.4 && Math.abs(G.py + 10.6) < 1.4) return Lk().open ? 'To the lake →' : 'Tall gray reeds block the path to the lake';
    if (G.px > MAPB.x1 - 2.2 && Math.abs(G.py + 4.5) < 1.3) return 'To the market →';
    if (debrisAt(Math.floor(G.px), Math.floor(G.py - 0.4)) === 'g' || ['k', 'o'].some(ch => [[1, 0], [-1, 0], [0, 1], [0, -1]].some(([dx, dy]) => debrisAt(Math.floor(G.px) + dx, Math.floor(G.py - 0.4) + dy) === ch))) {
      const it = ITEMS[selectedItem()] || {}; return it.tool === 'hoe' || it.tool === 'scythe' ? 'Click to clear the old field' : 'The old field: clear it with a hoe or scythe';
    }
  } else if (G.scene === 'home') { if (Math.abs(G.px + 0.07) < 1.2 && Math.abs(G.py - 1.26) < 1.5) return '[E] Sleep and end the day'; if (G.px > 6.8) return 'Back to the farm';
  } else if (G.scene === 'market') {
    for (const v of VILLAGERS.slice(0, 2)) if (near(v.x, v.y, 1.0)) return `[E] Talk to ${v.name}`;
    if (near(-4.2, -2.0, 1.1)) return '[E] Share the food you hold (Shift+E: more)';
    if (Math.abs(G.px - 3.92) < 3 && Math.abs(G.py - 3.01) < 4.5) return '[E] Trade with the merchant';
    if (G.px < -7) return 'Back to the farm';
  } else if (G.scene === 'sunset') {
    if (near(HRACK[0], HRACK[1], 1.5)) { const h = Hl(); return h.drying.some(d => G.day >= d) ? '[E] Take down the dried persimmons' : has('Persimmon') ? '[E] Hang your persimmons' : h.drying.length ? 'The persimmons are drying' : 'A drying rack for persimmons'; }
    if (near(HBENCH[0], HBENCH[1], 1.6)) return '[E] Sit and look out over the valley';
    const i = HTREES.findIndex(([x, y]) => near(x + 0.5, y + 0.5, 1.6));
    if (i >= 0) { const t = Hl().tree[i], k = HTREES[i][2]; return t === 0 ? `An old ${KIND[k]} tree: prune it (Scythe)` : t === 1 ? `Give it Earth's Breath` : hasFruit(i) ? `Click to pick ${FRUIT[k]}` : `${KIND[k] === 'apple' ? 'An' : 'A'} ${KIND[k]} tree, growing fruit`; }
    if (G.py < HILB.y0 + 2) return 'Back to the farm ↓';
  } else if (G.scene === 'lake') {
    if (LFISH.some(([x, y]) => near(x, y, 1.3))) return fishing ? (fishing.state === 'bite' ? '[#ffd84a][E] NOW![/]' : 'Waiting for a bite...') : (has('Fishing Rod') ? '[E] Fish' : 'You need a fishing rod to fish here');
    if (near(LSTEPS[0], LSTEPS[1], 1.3)) return '[E] Let your fish go into the lake';
    if (near(LBENCH[0], LBENCH[1], 1.3)) return Lk().restDay === G.day ? 'The willow sways' : '[E] Rest under the willow';
    if (G.px < LAKB.x0 + 2) return '← Back to the farm';
  } else if (G.scene === 'forest') {
    if (near(FSEAT[0], FSEAT[1], 1.3)) return F().restDay === G.day ? '[E] Drink from the spring' : '[E] Rest by the spring';
    if (near(FTREE[0], FTREE[1] + 0.6, 2.2)) return '[E] Look at the old tree';
    const i = FSPOTS.findIndex(([x, y]) => near(x + 0.5, y + 0.5, 1.6));
    if (i >= 0) { const st = F().sapling[i]; return st < 0 ? (has('Acorn') ? 'Click the pebble ring with the Acorn to plant it' : 'An empty spot. Acorns turn up on the forest floor.') : st >= 3 ? 'This clearing is full of color' : F().watered[i] ? `Sapling ${st}/3: watered today` : `Sapling ${st}/3: water it today`; }
    if (G.px > FORB.x1 - 2) return 'Back to the farm →';
  } else if (G.scene === 'hill') { if (near(0, -4.4, 1.4)) return '[E] Climb back down'; if (near(5.1, -2.0, 1.5)) return '[E] Sit and watch the stars'; }
  return '';
}

// ------------------------------------------------------------------ save
const SAVE_KEY = 'mlf-preview-v2';
function save() { try { localStorage.setItem(SAVE_KEY, JSON.stringify(G)); } catch (e) { /* storage unavailable: preview still works */ } }
function load() { try { const s = localStorage.getItem(SAVE_KEY); if (s) { G = JSON.parse(s); return true; } } catch (e) { /* ignore */ } return false; }

// ------------------------------------------------------------------ input
uiCanvas.addEventListener('keydown', e => {
  if (['Space', 'ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Tab'].includes(e.code)) e.preventDefault();
  if (!keys[e.code]) pressed[e.code] = true; keys[e.code] = true;
});
uiCanvas.addEventListener('keyup', e => { keys[e.code] = false; });
uiCanvas.addEventListener('blur', () => { for (const k in keys) keys[k] = false; });
function canvasPoint(e) { const r = uiCanvas.getBoundingClientRect(); return [(e.clientX - r.left) * CW / r.width, (e.clientY - r.top) * CH / r.height]; }
uiCanvas.addEventListener('mousedown', e => {
  uiCanvas.focus(); const [x, y] = canvasPoint(e);
  if (dialogue) return;
  for (let i = hits.length - 1; i >= 0; i--) { const h = hits[i]; if (x >= h.x && x <= h.x + h.w && y >= h.y && y <= h.y + h.h) { h.fn(); return; } }
  if (G.dead || G.scene === 'haggle') return;
  if (e.button === 2) { eat(selectedItem()); return; }
  const [wx, wy] = toWorld(x, y); worldClick(wx, wy);
});
uiCanvas.addEventListener('contextmenu', e => e.preventDefault());
uiCanvas.addEventListener('wheel', e => { e.preventDefault(); const it = hotbarItems(); const i = it.indexOf(selectedItem()); selectIndex(i + (e.deltaY > 0 ? 1 : -1)); }, { passive: false });

// preview tools
const $ = id => document.getElementById(id);
$('t-heart').onclick = () => { changeHeart(10, 'Preview tool'); uiCanvas.focus(); };
$('t-day').onclick = () => { if (!G.dead) { endDay(false); if (G.scene === 'haggle') go('farm', ...arrival('MiniGame')); } uiCanvas.focus(); };
$('t-items').onclick = () => {
  [['Carrot', 5], ['Strawberry', 4], ['Corn', 3], ['Apple', 4], ['Minnow', 2], ['Carp', 1], ['Sunshine Shard', 2], ["Earth's Breath", 2], ['Star Fragment', 3], ['Wood', 12], ['Stone', 16], ['Campfire', 1], ['Bread', 2], ['Potato Seed', 4], ['Strawberry Seed', 4], ['Corn Seed', 4]].forEach(([n, c]) => add(n, c));
  toast('Preview: items added to your bag.'); uiCanvas.focus();
};
$('t-hill').onclick = () => { S().kindness = Math.max(S().kindness, 60); unlock('starhill'); G.raining = G.snowing = false; if (G.hour < 20) { G.hour = 20; G.minute = 30; } toast('Preview: Star Hill is open. The vine grows above the pond at night.'); uiCanvas.focus(); };
$('t-forest').onclick = () => {
  if (G.heart < 30) changeHeart(30 - G.heart, 'Preview tool');
  if (S().season < 1) { S().season = 1; S().highest = Math.max(S().highest, 1); }
  forestCheckOpen(); add('Acorn', 3); if (!has('Mattock')) add('Mattock');
  toast('Preview: spring, the forest is open. +3 Acorn, Mattock. The flower arch is at the west end of the farm lane.', 5); uiCanvas.focus();
};
$('t-hill2').onclick = () => {
  if (G.heart < 85) changeHeart(85 - G.heart, 'Preview tool');
  if (S().season < 3) { S().season = 3; S().highest = 3; unlock('season_autumn'); }
  forestCheckOpen(); lakeCheckOpen(); hillCheckOpen(); add("Earth's Breath", 6); add('Persimmon', 4);
  toast('Preview: autumn, Sunset Hill is open (south lane of the farm). +6 Earth\'s Breath, 4 Persimmons.', 5); uiCanvas.focus();
};
$('t-ending').onclick = () => {
  VILLAGERS.forEach(v => { const p = villagerProgress(v.id); p.met = true; p.wish = 3; });
  if (G.heart < 95) changeHeart(95 - G.heart, 'Preview tool');
  toast('Preview: every villager has their colors back. Go home: the ending starts there.', 5); uiCanvas.focus();
};
$('t-lake').onclick = () => {
  if (G.heart < 60) changeHeart(60 - G.heart, 'Preview tool');
  if (S().season < 2) { S().season = 2; S().highest = Math.max(S().highest, 2); }
  forestCheckOpen(); lakeCheckOpen(); if (!has('Fishing Rod')) add('Fishing Rod'); add('Minnow', 4); add('Carp', 3);
  toast('Preview: summer, the lake is open. +Fishing Rod, 4 Minnow, 3 Carp. The lake path is at the south-east of the farm.', 5); uiCanvas.focus();
};
$('t-night').onclick = () => { G.hour = Math.max(G.hour, 20); G.minute = 30; toast('Preview: it is night now.'); uiCanvas.focus(); };
$('t-speed').onclick = e => { speed = speed === 1 ? 4 : 1; e.currentTarget.classList.toggle('on', speed === 4); e.currentTarget.textContent = speed === 4 ? '시간 1배속' : '시간 4배속'; uiCanvas.focus(); };
$('t-new').onclick = () => { newGame(); toasts = []; dialogue = null; panel = null; announce = null; G.spirit = null; save(); introAnnounce(); uiCanvas.focus(); };

function introAnnounce() {
  showAnnounce('[#ff8fb1]A new life begins[/]\n\n"Day after day in the gray city felt endless. So I left it all behind and set out for a quiet village. I wonder... can I build my happiness again?"\n\nThe world is gray. It is winter in your heart.\nEat, drink, rest and go to bed on time: spring will come.\n\n[#c8c0b0]Till the dirt (Hoe) → plant (Carrot Seed) → water → sleep. Walk into trees and rocks to gather.\nEast of the road, an old overgrown field is waiting to be cleared.[/]', 14);
}

// ------------------------------------------------------------------ boot
function loop(t) {
  const dt = Math.min(0.05, (t - last) / 1000 || 0); last = t;
  update(dt); renderWorld(); renderUI();
  requestAnimationFrame(loop);
}
newGame();
const resumed = load();
if (G.scene === 'haggle' || G.scene === 'hill') { G.scene = 'farm'; G.px = 0.9; G.py = -2.0; }
G.clearedDebris = G.clearedDebris || []; G.debrisHits = G.debrisHits || {};   // saves from before the bigger farm
G.story.forest = Object.assign(newForest(), G.story.forest || {});             // saves from before the forest
G.story.lake = Object.assign(newLake(), G.story.lake || {});                   // saves from before the lake
G.story.hill = Object.assign(newHill(), G.story.hill || {});                   // saves from before Sunset Hill
if (G.scene === 'sunset' && !G.story.hill.open) { G.scene = 'farm'; G.px = 0.9; G.py = -2.0; }
if (G.scene === 'lake' && !G.story.lake.open) { G.scene = 'farm'; G.px = 0.9; G.py = -2.0; }
if (G.scene === 'forest' && !G.story.forest.open) { G.scene = 'farm'; G.px = 0.9; G.py = -2.0; }
if (G.story.spirit.found && !G.spirit) G.spirit = { x: G.px, y: G.py, vx: 0, pulse: 0 };
Promise.all([loadAll(), document.fonts ? document.fonts.ready : Promise.resolve()]).then(() => {
  renderWorld(); renderUI();
  $('start').onclick = () => {
    $('start').hidden = true; uiCanvas.focus();
    if (resumed) toast(`Welcome back. ${SEASONS[G.story.season]}, Day ${G.day}.`); else introAnnounce();
    requestAnimationFrame(t => { last = t; loop(t); });
  };
});
