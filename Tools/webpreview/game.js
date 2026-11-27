// My Little Farmstead - browser preview.
// A JavaScript port of the Unity game's rules (ItemDB, CropDB, DataManager, villagers, spirit,
// seasons of the heart, album...) using the same pixel art. Coordinates are Unity world units.
'use strict';

const ASSETS = /*__ASSETS__*/{};
const AUDIO = /*__AUDIO__*/{};   // S-1/S-2: sounds and gugak music (small mp3 copies of the game's files)
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
const THRESH = [0, 35, 60, 85];   // F-0: each season's band of the heart (SeasonPacing.cs)
const PACE = { days: [13, 20, 20, 20], gain: 0.6, minRoom: 0.15 };
const paceTop = s => s >= 3 ? 100 : THRESH[s + 1];
const paceScale = (s, h) => PACE.gain * Math.max(PACE.minRoom, Math.min(1, (paceTop(s) - h) / (paceTop(s) - THRESH[s])));
const color01 = h => Math.max(0, Math.min(1, (h - 10) / 90));
const seasonDays = () => G.day - (G.story.seasonStart || 1);
const seasonClosed = (s, day) => s === 0 ? !!(G.story.rites && G.story.rites.boreumDone) : day - (G.story.seasonStart || 1) >= PACE.days[s];
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
reg('Fishing Rod', 'Tool', { tool: 'rod', desc: 'Press E by the stream or the lake to fish.' });
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
reg('Rice Cake', 'Food', { sell: 10, hunger: 40, desc: 'Chewy rice cake from the mill. Fills you up.' });
reg('Roasted Carrot', 'Food', { sell: 60, hunger: 35, heart: 2 });
reg('Baked Potato', 'Food', { sell: 110, hunger: 50, heart: 2 });
reg('Steamed Corn', 'Food', { sell: 130, hunger: 30, thirst: 35, heart: 3, desc: 'Corn steamed in its husk, sweet and salty. A taste of summer.' });
reg('Strawberry Jam', 'Food', { sell: 420, hunger: 20, heart: 6 });
reg('Grilled Fish', 'Food', { sell: 90, hunger: 40, heart: 2 });
reg('Carrot Pancake', 'Food', { sell: 250, hunger: 30, heart: 6, desc: "Grandma Boksun's recipe. Crisp at the edges, warm in the middle." });
reg('Pumpkin Porridge', 'Food', { sell: 320, hunger: 45, heart: 4, desc: 'Golden pumpkin porridge with little rice cake balls. Tastes like a good year.' });
reg('Minnow', 'Fish', { sell: 15 });
reg('Carp', 'Fish', { sell: 45 });
reg('Golden Koi', 'Fish', { sell: 300 });
reg('Wood', 'Resource', { sell: 2 });
reg('Stone', 'Resource', { sell: 5 });
reg('Old Boot', 'Resource', { sell: 1 });
reg('Sunshine Shard', 'Special', { sell: 100, desc: 'Use on a crop: it ripens golden.' });
reg("Earth's Breath", 'Special', { sell: 100, desc: 'Use on a crop: it grows by one day.' });
reg('Star Fragment', 'Special', { sell: 50, desc: 'The dokkaebi fire loves it (all four colors).' });
reg('Compost', 'Special', { sell: 0, desc: 'Rich compost from the heap. Spread it on a gray, hardened plot to bring it back to life.' });
// K-2: making sauces
reg('Soybeans', 'Resource', { sell: 5, desc: 'Dried soybeans for meju. Boil four at the agungi to make one.' });
reg('Salt', 'Resource', { sell: 2, desc: 'Coarse sea salt. Two go in each sauce jar.' });
reg('Barley Tea', 'Food', { sell: 10, thirst: 50, desc: 'Cold bottled barley tea from the corner store. Take it anywhere.' });   // W-1
reg('Homemade Barley Tea', 'Food', { sell: 10, thirst: 60, heart: 1, desc: 'Barley tea boiled at your own agungi, kept in an old glass bottle. Tastes like home.' });
reg('Roasted Barley', 'Resource', { sell: 4, desc: 'Roasted barley grains. Boil them at the agungi for barley tea.' });
reg('Fresh Meju', 'Special', { sell: 0, desc: 'A block of boiled, pounded soybeans. Hang it on the meju rack to dry.' });
reg('Dried Meju', 'Special', { sell: 40, desc: 'Meju dried in the winter wind. Put it in a sauce jar with 2 Salt.' });
reg('Doenjang', 'Resource', { sell: 60, desc: 'Soybean paste you made yourself.' });
reg('Ganjang', 'Resource', { sell: 50, desc: 'Soy sauce you made yourself.' });
reg('Doenjang Stew', 'Food', { sell: 180, hunger: 45, thirst: 10, heart: 5, desc: 'Doenjang, potato and carrot, bubbling in an earthen pot.' });
reg('Red Bean Porridge', 'Food', { sell: 0, hunger: 40, heart: 3, desc: 'Winter solstice porridge with little rice balls. One bowl for every house.' });
reg('Braised Potatoes', 'Food', { sell: 160, hunger: 35, heart: 3, desc: 'Potatoes braised sweet and salty in your own ganjang.' });
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

const SHOP = [['Soybeans', 12], ['Salt', 8], ['Barley Tea', 30], ['Roasted Barley', 15], ['Wood', 5], ['Carrot Seed', 10], ['Potato Seed', 40], ['Strawberry Seed', 50], ['Corn Seed', 30], ['Campfire', 50], ['Rice Cake', 20], ['Apple', 12]];
// five-day market (장날): every 5th day the lane fills with stalls, rare goods and an old farmer to haggle with
const MARKET_CYCLE = 5;
const isMarketDay = d => d > 0 && d % MARKET_CYCLE === 3;
const daysUntilMarket = d => { for (let i = 0; i < MARKET_CYCLE; i++) if (isMarketDay(d + i)) return i; return MARKET_CYCLE; };
const FAIR_STOCK = [['Compost', 30], ["Earth's Breath", 180], ['Sunshine Shard', 200]];
const HAGGLER = [2.6, -2.2];
const FAIR_STALLS = [[-1.6, -2.7, 1.0], [2.6, -3.0, 1.3], [-5.6, -2.6, 1.1]];
const ALIASES = { Bread: 'Rice Cake', 'Corn Soup': 'Steamed Corn', 'Carrot Cake': 'Carrot Pancake', 'Pumpkin Pie': 'Pumpkin Porridge' };

const RECIPES = [
  { out: 'Campfire', n: 1, req: [['Wood', 5], ['Stone', 2]] },
  { out: 'Fertile Hoe', n: 1, req: [['Hoe', 1], ["Earth's Breath", 1]] },
  { out: 'Copper Watering Can', n: 1, req: [['Watering Can', 1], ['Stone', 10], ['Wood', 5]], lock: 'recipe_copper_can' },
  { out: 'Mattock', n: 1, req: [['Wood', 5], ['Stone', 5]] },
  { out: 'Spring Greens', n: 1, req: [['Wild Garlic', 1], ['Fern Sprout', 1]], lock: 'forest_open' },
  { out: 'Morel Soup', n: 1, req: [['Morel', 2], ['Wild Garlic', 1]], station: 'Campfire', lock: 'forest_open' },
  { out: 'Homemade Barley Tea', n: 3, req: [['Roasted Barley', 1], ['Wood', 1]], station: 'Agungi' },   // W-1
  { out: 'Fresh Meju', n: 1, req: [['Soybeans', 4], ['Wood', 1]], station: 'Agungi' },
  { out: 'Doenjang Stew', n: 1, req: [['Doenjang', 1], ['Potato', 1], ['Carrot', 1]], station: 'Campfire' },
  { out: 'Braised Potatoes', n: 2, req: [['Potato', 2], ['Ganjang', 1]], station: 'Campfire' },
  { out: 'Roasted Carrot', n: 1, req: [['Carrot', 2], ['Wood', 1]], station: 'Campfire' },
  { out: 'Baked Potato', n: 1, req: [['Potato', 1], ['Wood', 1]], station: 'Campfire' },
  { out: 'Steamed Corn', n: 1, req: [['Corn', 2]], station: 'Campfire' },
  { out: 'Strawberry Jam', n: 1, req: [['Strawberry', 3]], station: 'Campfire' },
  { out: 'Carrot Pancake', n: 1, req: [['Carrot', 2], ['Potato', 1]], station: 'Campfire', lock: 'recipe_carrot_cake' },
  { out: 'Grilled Fish', n: 1, req: [['Minnow', 1], ['Wood', 1]], station: 'Campfire', lock: 'fishing' },
  { out: 'Grilled Fish', n: 2, req: [['Carp', 1], ['Wood', 1]], station: 'Campfire', lock: 'fishing' },
  { out: 'Pumpkin Porridge', n: 1, req: [['Pumpkin', 1], ['Rice Cake', 1]], station: 'Campfire', lock: 'season_autumn' },
];

const VILLAGERS = [
 {
  "id": "rosa",
  "name": "Grandma Boksun",
  "scene": "market",
  "x": -6.0,
  "y": 1.0,
  "ex": -6.7,
  "ey": 0.75,
  "place": "the bench in front of the mill",
  "intro": "Oh my, a new face! Forgive me, dear... Since my husband passed, the mill has gone quiet and everything faded. Even my rice cakes taste gray.",
  "gray": [
   "The mill used to rumble from dawn. Now it's just me and the clock.",
   "I steam rice cakes out of habit, then don't eat them."
  ],
  "color": [
   "Come by anytime! The rice cakes are still warm.",
   "You look thin. Did you eat? Eat, eat!",
   "He would have liked you, you know."
  ],
  "season": [
   "Winter nights are long when you're alone. Come in, warm your hands.",
   "Spring! Time for mugwort rice cakes. Smell that?",
   "Too hot to light the steamer at noon. I work at dawn now.",
   "Autumn was his favorite. New rice, new rice cakes."
  ],
  "restored": "I can see the colors again... Thank you, dear. Truly.",
  "masil": [
   "Sit, sit. The night's long anyway. ...He used to sit right where you are.",
   "Look at the stars. In the city you can't see them, can you?",
   "Did you eat dinner? Don't lie to me."
  ],
  "chapters": [
   {
    "kind": "Pumasi",
    "invite": "Oh, perfect timing. I'm about to set rice cakes in the steamer, but these old arms aren't what they were. Would you lend me a hand?",
    "scene": [
     "Look at you, quick hands. Strange... someone doing what he used to do.",
     "Here, take some rice cakes. And I'll show you how I fry carrot pancakes.",
     "Pumasi gets paid back in work, you know. When your sauce jars need looking after, I'll come by."
    ],
    "hint": "By day, help steam rice cakes at the mill (the steamer by the door)",
    "spot": "steamer",
    "steps": [
     "You stoke the fire under the pot.",
     "You spread the rice flour in the steamer, layer by layer.",
     "Steam rises. You lift the lid together."
    ],
    "reward": [
     [
      "Rice Cake",
      3
     ]
    ],
    "unlock": "recipe_carrot_cake",
    "unlockText": "New recipe: Carrot Pancake",
    "favor": "favor_jang",
    "favorText": "Grandma Boksun will help look after your sauce jars (deeper taste)",
    "heart": 3.0
   },
   {
    "kind": "Saecham",
    "invite": "These days I can't bring myself to eat alone. If someone brought me a pot of doenjang stew, maybe...",
    "scene": [
     "You made this? With sauce you put down yourself? ...(She takes a spoonful and says nothing for a long while.)",
     "First time since he passed. Sitting across from someone, eating.",
     "It's good. Truly."
    ],
    "hint": "Bring her a doenjang stew for a work-break meal (saecham)",
    "need": "Doenjang Stew",
    "heart": 4.0
   },
   {
    "kind": "Masil",
    "invite": "Come by in the evening, if you like. I sit out on the bench in front of the mill.",
    "scene": [
     "He never said much. When he opened the mill before dawn, all he'd say was \"It's cold, sleep a bit more.\"",
     "I wanted to hear that so badly. Every morning.",
     "But since you started coming by... the evenings aren't so long."
    ],
    "hint": "In the evening, visit her on the bench in front of the mill (masil)",
    "reward": [
     [
      "Carrot Pancake",
      2
     ]
    ],
    "heart": 6.0
   }
  ]
 },
 {
  "id": "theo",
  "name": "Mr. Dongsu",
  "scene": "market",
  "x": -1.4,
  "y": 1.0,
  "ex": 6.3,
  "ey": 1.35,
  "place": "the plastic chairs outside the corner store",
  "intro": "Hmph. The forge behind the store's been cold for a year. Nothing worth making anymore.",
  "gray": [
   "Hmph.",
   "The hammer's heavier than it used to be."
  ],
  "color": [
   "Fire's roaring today. Feels good.",
   "Bring me good stone and I'll make you good tools.",
   "...Thanks. For before. Don't make it weird."
  ],
  "season": [
   "Cold hands, cold forge. Same every winter.",
   "Snow's off the roof. Guess I'll fix the gutters.",
   "Too hot at noon. I hammer at dawn now.",
   "Autumn's for sharp sickles. Fields need them before the frost."
  ],
  "restored": "Listen to that. The forge is singing again.",
  "masil": [
   "Sit. ...Want a sip? It's just sikhye. Don't look at me like that.",
   "Quiet night. I like it.",
   "The forge is banked for the night. It'll still be warm in the morning."
  ],
  "chapters": [
   {
    "kind": "Pumasi",
    "invite": "I'm thinking of lighting the forge again... but there's no one to work the bellows. You know how? Doesn't matter. You just pump.",
    "scene": [
     "...Good fire. First in a year.",
     "Pumasi's got to be paid back. Give me your hoe. ...There, sharpened. The ground won't feel so heavy now.",
     "And I'll show you how to make a copper watering can, while the fire's hot."
    ],
    "hint": "By day, work the bellows at the forge by the hardware store",
    "spot": "forge",
    "steps": [
     "You pump the bellows. The coals glow.",
     "Again. Sparks fly up.",
     "The fire roars. Mr. Dongsu picks up his hammer."
    ],
    "unlock": "recipe_copper_can",
    "unlockText": "New recipe: Copper Watering Can",
    "favor": "favor_hoe",
    "favorText": "Your hoe is sharpened: tilling takes less energy",
    "heart": 3.0
   },
   {
    "kind": "Masil",
    "invite": "...If you're not busy tonight, I sit outside the corner store. Not that I'm asking.",
    "scene": [
     "I've got a daughter. In Seoul. Does design, or something like that.",
     "We had a big fight. She asked what's the point of being a blacksmith these days. I said things too. Three years, no word.",
     "...You came from the city too. Is it a place where you hold on alone?"
    ],
    "hint": "In the evening, sit with him outside the corner store (masil)",
    "heart": 3.0
   },
   {
    "kind": "Letter",
    "invite": "I tried to write her a letter. These hands are good with iron, not with words. ...Would you write it for me?",
    "scene": [
     "...So I just post this. That's it?",
     "Thanks. Here, the first decent hoe I've made in a year. It's yours. Don't argue."
    ],
    "hint": "By day, help him write a letter to his daughter",
    "reward": [
     [
      "Fertile Hoe",
      1
     ]
    ],
    "heart": 6.0
   }
  ]
 },
 {
  "id": "mina",
  "name": "Mina",
  "scene": "farm",
  "x": 5.0,
  "y": -2.4,
  "ex": -2.35,
  "ey": -1.65,
  "place": "the bench in your yard",
  "intro": "...Hi. Grandma says not to talk to strangers. Everything here is boring. Everything's gray.",
  "gray": [
   "The stream is boring. The fish are boring.",
   "Grandpa just sleeps all day now."
  ],
  "color": [
   "Did you catch anything big? Show me!",
   "Grandpa smiled today! A real smile!",
   "When I grow up I'm gonna have a farm like yours."
  ],
  "season": [
   "The stream froze! ...It's still boring though.",
   "Look, the frogs are back! Spring is the best!",
   "Summer means fishing every day! Grandpa says so!",
   "The leaves are crunchy! Listen! *crunch crunch*"
  ],
  "restored": "Everything looks so bright now! Is this what you see all the time?",
  "masil": [
   "Grandma's snoring already. Hehe.",
   "Do you think the fish sleep under the ice?",
   "When I grow up I'll live in the city. ...Or maybe not."
  ],
  "chapters": [
   {
    "kind": "Fish",
    "invite": "Grandpa's got an old fishing rod. Wanna fish together? Alone is boring. Stand close to the stream and press E. Catch one and show me!",
    "scene": [
     "Woah! You really caught one! Next time I'll catch a bigger one.",
     "Here, I found these seeds in Grandpa's shed. Plant them in spring!"
    ],
    "hint": "Catch a fish in the stream with Mina's rod",
    "reward": [
     [
      "Strawberry Seed",
      3
     ]
    ],
    "unlock": "fishing",
    "unlockText": "Fishing! New recipe: Grilled Fish",
    "heart": 2.0
   },
   {
    "kind": "Pumasi",
    "invite": "The stream froze! Grandpa made me a sled, but there's no one to push me... Push me? Please?",
    "scene": [
     "One more! ...No, you're tired, right? Hehe.",
     "To say thanks, I'll water your field in the mornings sometimes. Secretly."
    ],
    "hint": "Push Mina's sled on the frozen stream",
    "spot": "sled",
    "steps": [
     "You give the sled a push. Mina squeals.",
     "Faster! She digs in with her ice picks.",
     "One last long glide down the stream."
    ],
    "favor": "favor_water",
    "favorText": "Mina will sometimes water your crops in the morning",
    "heart": 3.0
   },
   {
    "kind": "Masil",
    "invite": "Grandma goes to sleep at nine. ...Can I come sit on your bench in the evening?",
    "scene": [
     "I snuck out. Grandma doesn't know.",
     "Mom and Dad work in the city. They said they'd come for New Year. They said that last year too.",
     "You came from the city too. Why is everyone there so busy?",
     "...Anyway. Now I've got somewhere to go in the evening."
    ],
    "hint": "In the evening, wait for Mina on the bench in your yard",
    "reward": [
     [
      "Sunflower Seed",
      5
     ]
    ],
    "unlock": "shop_sunflower",
    "unlockText": "Sunflower Seeds are now sold at the market",
    "heart": 6.0
   }
  ]
 },
 {
  "id": "mayor",
  "name": "Village Head Park",
  "scene": "market",
  "x": -3.9,
  "y": 0.9,
  "ex": 5.55,
  "ey": -3.05,
  "place": "the bench outside the village hall",
  "intro": "Well now, you must be the one who moved into the empty house! I'm Park, the village head. Not many young folks left around here... it's good to see a new face. I make the announcements every morning on that loudspeaker.",
  "gray": [
   "Eating well? You look thin. City folks always do.",
   "If you hear the loudspeaker crackle, that's me. Don't laugh at my voice."
  ],
  "color": [
   "This village used to be full of children. Now it's quiet... but you're here now.",
   "Need a hand with anything? One announcement and the whole village comes running."
  ],
  "season": [
   "Winter's when we make meju for the sauces. Grandma Boksun knows how.",
   "Spring! Time to open the sauce jars and let the sun in.",
   "Hot, isn't it? Come sit in the pavilion when it gets too much.",
   "Autumn's busy. Kimchi-making day is coming, everyone will need a hand."
  ],
  "restored": "Ha! Look at that, the village has color in it again.",
  "masil": [
   "The hall's warm tonight, thanks to you. The old folks are playing hwatu inside.",
   "Count the lit windows with me. ...One more than last year."
  ],
  "chapters": [
   {
    "kind": "Pumasi",
    "invite": "The old folks at the village hall need coal briquettes in for the winter, but my back, I tell you...",
    "scene": [
     "Ah, that's a relief. Young hands are something else.",
     "Pumasi gets paid back. When you need hands, one announcement and the whole village will come."
    ],
    "hint": "By day, carry coal briquettes into the village hall",
    "spot": "briquettes",
    "steps": [
     "You carry a stack of briquettes inside. Your hands go black.",
     "Another stack. An old man inside says thank you.",
     "The last one. The hall's stove is lit."
    ],
    "favor": "favor_hands",
    "favorText": "The village head will call the neighbors when you need a hand",
    "heart": 3.0
   },
   {
    "kind": "Saecham",
    "invite": "After the broadcast my throat's all scratchy. Came out without breakfast again...",
    "scene": [
     "Well I'll be. When did someone last bring me food?",
     "Being village head, everyone asks me to do something for them. Nobody asks if I've eaten."
    ],
    "hint": "Bring the village head something to eat (any food)",
    "heart": 3.0
   },
   {
    "kind": "Masil",
    "invite": "I sit out by the village hall of an evening. Come keep an old man company.",
    "scene": [
     "Fifteen years I've been village head. In that time, twelve houses went empty.",
     "The school closed too. Some nights I can't sleep, wondering if the village will just... disappear.",
     "But you came. A light came on in an empty house... you don't know how good that felt."
    ],
    "hint": "In the evening, visit him outside the village hall (masil)",
    "heart": 6.0
   }
  ]
 }
];  // generated from VillagerDB.cs by vgen.py

const SPIRIT_FOOD = {
  'Sunshine Shard': [3, 0, 0, 0], Sunflower: [2, 0, 0, 0], Corn: [1, 0, 0, 0], "Earth's Breath": [0, 3, 0, 0],
  Carrot: [0, 1, 0, 0], Potato: [0, 1, 0, 0], Pumpkin: [0, 2, 0, 0], Minnow: [0, 0, 1, 0], Carp: [0, 0, 2, 0],
  'Golden Koi': [0, 0, 5, 0], 'Grilled Fish': [0, 0, 2, 0], Apple: [0, 0, 0, 1], Strawberry: [0, 0, 0, 1],
  'Strawberry Jam': [0, 0, 0, 3], 'Carrot Pancake': [0, 1, 0, 2], 'Pumpkin Porridge': [0, 1, 0, 2], 'Star Fragment': [1, 1, 1, 1],
  'Wild Garlic': [0, 0, 0, 1], 'Fern Sprout': [0, 1, 0, 0], Morel: [0, 2, 0, 0], 'Spring Greens': [0, 1, 0, 1], 'Morel Soup': [0, 2, 1, 0],
  Bluegill: [0, 0, 2, 0], Moonfin: [0, 0, 3, 0], Glassfish: [0, 0, 4, 0], 'Wild Raspberry': [0, 0, 0, 1], Watercress: [0, 0, 1, 0],
  Pear: [0, 0, 1, 1], Persimmon: [1, 0, 0, 1], 'Dried Persimmon': [2, 1, 0, 2],
};
const COLORS = ['sun', 'earth', 'water', 'bloom'];
const FORM_NAMES = { spirit_gray: 'Gray Dokkaebi Fire', spirit_prism: 'Rainbow Dokkaebi Fire', spirit_sun_young: 'Little Amber Flame', spirit_sun_grown: 'Amber Dokkaebi Fire', spirit_earth_young: 'Little Green Flame', spirit_earth_grown: 'Green Dokkaebi Fire', spirit_water_young: 'Little Blue Flame', spirit_water_grown: 'Blue Dokkaebi Fire', spirit_bloom_young: 'Little Pink Flame', spirit_bloom_grown: 'Pink Dokkaebi Fire' };

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
  [2, 'u3', "It's a Deal!", 'Win a haggle on market day.', 'haggle', null, 'World/haggler'],
  [2, 'u4', 'The Big One', 'Catch a Carp.', 'fish', 'Carp', 'Items/carp'],
  [2, 'u5', 'Starry Night', 'Visit Star Hill.', 'starhill', null, 'World/star_ladder'],
  [2, 'u6', 'Something Saved', 'Have 1000G at once.', 'gold1000', null, 'World/storage_box'],
  [3, 'a1', 'Pumpkin Harvest', 'Harvest a Pumpkin.', 'harvest', 'Pumpkin', 'Items/pumpkin'],
  [3, 'a2', 'Golden Harvest', 'Harvest a crop blessed by sunlight.', 'golden', null, 'Items/sunshine_shard'],
  [3, 'a3', 'Shining Koi', 'Catch a Golden Koi.', 'fish', 'Golden Koi', 'Items/golden_koi'],
  [3, 'a4', 'Colors Restored', 'Bring all the color back to a villager.', 'restored', null, 'Villagers/rosa_3'],
  [3, 'a5', 'Shooting Star', 'Catch a Star Fragment.', 'star', null, 'Items/star_fragment'],
  [3, 'a6', 'Porridge for Two', 'Cook Pumpkin Porridge.', 'craft', 'Pumpkin Porridge', 'Items/pumpkin_porridge'],
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
// village map (M): same rules as VillageMap.cs, positions from Assets/Resources/Maps/villagemap.json
const VMAP = /*__VMAP__*/null;
// H-2: the mill, the corner store and the village hall (rooms.json, drawn by rooms.py). They sit inside the
// market scene, east of the market; the room the player stands in decides the camera (InteriorRooms.cs).
const ROOMS = (/*__ROOMS__*/null || { rooms: [] }).rooms;
const roomW = (r, px, py) => [r.originX + (px - r.w / 2) / 32, r.originY + (r.h / 2 - py) / 32];
const roomRect = r => ({ x0: r.originX - r.w / 64, x1: r.originX + r.w / 64, y0: r.originY - r.h / 64, y1: r.originY + r.h / 64 });
const roomAt = (x, y) => G && G.scene === 'market' ? ROOMS.find(r => { const b = roomRect(r); return x >= b.x0 && x <= b.x1 && y >= b.y0 && y <= b.y1; }) : null;
const roomAnchor = (id, name, fb) => { const r = ROOMS.find(r => r.id === id), a = r && r.anchors.find(a => a.name === name); return a ? roomW(r, a.x, a.y) : fb; };
const STORE_KEEPER = roomAnchor('store', 'keeper', [3.92, 3.01]);
const VMAP_IDS = ['farm', 'market', 'forest', 'hill', 'lake', 'starhill'];
const VMAP_NAME = { farm: 'Home', market: 'Market', forest: 'Back Hill', hill: 'Sunset Hill', lake: 'Reservoir', starhill: 'Star Hill' };
const HILB = boundsOf(HILL);
const hillAt = (cx, cy) => { const c = cx - HILL.originX, r = HILL.originY - 1 - cy; return r >= 0 && r < HILL.height && c >= 0 && c < HILL.width ? HILL.rows[r][c] : '#'; };
const HCELLS = [];
HILL.rows.forEach((row, r) => [...row].forEach((ch, c) => HCELLS.push([HILL.originX + c, HILL.originY - 1 - r, ch])));
const HTREES = HCELLS.filter(([, , ch]) => 'APK'.includes(ch)).slice(0, 6);            // [x, y, kind] in map order
const HRACK = HCELLS.filter(([, , ch]) => ch === 'D').map(([x, y]) => [x + 0.5, y + 0.5])[0];
const HBENCH = (() => { const v = HCELLS.filter(([, , ch]) => ch === 'V'); return [(Math.min(...v.map(c => c[0])) + Math.max(...v.map(c => c[0])) + 1) / 2, v[0][1] + 0.5]; })();
const hillBlocked = (cx, cy) => '#CAPKDRBG'.includes(hillAt(cx, cy));
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
  home: { ww: 9, wh: 5.0625, cx: 0, cy: 0, bg: () => unlocked('ending_seen') ? 'World/bg_home' : 'World/bg_home_gray' },   // ending A: gray until the end
  market: { ww: 18.28, wh: 10.24, cx: 0.09, cy: -0.08, bg: () => 'World/bg_market_' + seasonKey(), outdoor: true },
  haggle: { ww: 17.95, wh: 10.04, cx: -0.035, cy: -0.079, bg: () => 'World/bg_minigame' },
  hill: { ww: 17.92, wh: 10.24, cx: 0, cy: 0, bg: () => 'World/bg_starhill' },
};
// direction C: the Korean homestead (farm.json): plots, compost heap, things moved to map anchors
const ANCHOR = (n, fx, fy) => { const a = (MAP.anchors || []).find(a => a.name === n); return a ? [a.x, a.y] : [fx, fy]; };
const PLOTS = MAP.plots || [];
const plotAt = (x, y) => PLOTS.find(p => x >= p.x0 && x <= p.x1 && y >= p.y0 && y <= p.y1);
const POND = ANCHOR('pond', 7.05, -0.6);
const LADDER = ANCHOR('starvine', 8.25, 2.9);
const WELL = ANCHOR('well', 3.68, -0.03);
const DOOR = ANCHOR('door', -8.38, -4.54);
const COMPOST = ANCHOR('compost', 9.5, 1.5);
const CHEST = ANCHOR('storage', -3.89, -1.07);
const COTTAGE = ANCHOR('cottage', -7.75, -3.85);
const AGUNGI = ANCHOR('agungi', 1.0, 0.0), MEJURACK = ANCHOR('mejurack', -0.4, 0.0), JANGDOK = ANCHOR('jangdok', 7.0, 0.0);
const farmBlocked = (cx, cy) => 'PwWHJKBCRb|'.includes(mapAt(cx, cy));
{ const [mx, my] = ANCHOR('mina', 5.0, -2.4); VILLAGERS[2].x = mx; VILLAGERS[2].y = my; }
const ORGANIC = new Set(['Carrot', 'Potato', 'Corn', 'Strawberry', 'Sunflower', 'Pumpkin', 'Apple', 'Pear', 'Persimmon', 'Wild Garlic', 'Fern Sprout', 'Morel', 'Wild Raspberry', 'Watercress', 'Acorn', 'Rice Cake', 'Roasted Carrot', 'Baked Potato', 'Steamed Corn', 'Spring Greens', 'Soybeans']);

// ------------------------------------------------------------------ state
let G;  // game state
function newHill() { return { open: false, tree: [0, 0, 0, 0, 0, 0], fruitDay: [-1, -1, -1, -1, -1, -1], drying: [], restDay: -1 }; }
function newLake() { return { open: false, clarity: 0, released: 0, restDay: -1, forageDay: -1, forage: [] }; }
function newForest() { return { open: false, sapling: [-1, -1, -1], watered: [false, false, false], treeAwake: false, forageDay: -1, forage: [], restDay: -1 }; }
function newGame() {
  G = {
    scene: 'farm', px: ANCHOR('start', 0.9, -2.0)[0], py: ANCHOR('start', 0.9, -2.0)[1], dir: 'down', facingLeft: false, step: 0, stepT: 0,
    inv: { Hoe: 1, 'Watering Can': 1, Scythe: 1, 'Carrot Seed': 5, Apple: 3 },
    gold: 50, health: 100, hunger: 100, thirst: 100, energy: 0, heart: 20,
    day: 1, hour: 6, minute: 0, raining: false, snowing: false, water: 10,
    tiles: {}, crops: {}, cleared: [], clearedDebris: [], debrisHits: {}, openPlots: [], restedPlots: [], compostPoints: 0, compostReady: 0,
    market: { demand: 'Carrot', haggle: 0, haggled: false, sold: {} },
    log: { harvested: 0, meals: 0, exhausted: false, starved: false, drank: false },
    story: { season: 0, highest: 0, unlocks: ['guide_on'], photos: [], villagers: {}, kindness: 0, shareDay: -1, sharedToday: 0, donationDay: -1,
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
function villagerProgress(id) { const p = S().villagers[id] || (S().villagers[id] = { wish: 0, met: false }); if (p.lastDay === undefined) Object.assign(p, { lastDay: -99, invited: false, chore: 0, saechamDay: -1 }); return p; }
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
function sc() {
  const r = roomAt(G.px, G.py);
  if (r) return { ww: r.w / 32, wh: r.h / 32, cx: r.originX, cy: r.originY, bg: () => 'World/room_' + r.id };   // inside a shop or the hall
  return SCENES[G.scene];
}
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
// D-2: action poses, little bursts, the color wave (same as PlayerPixelAnimator / ActionFX / ColorWave in Unity)
let act = null, fx = [], wave = null, wavePending = null;
function doAction(kind, cx, cy) {
  const dx = cx + 0.5 - G.px, dy = cy + 0.5 - G.py;
  if (dx * dx + dy * dy > 0.04) { if (Math.abs(dx) > Math.abs(dy)) { G.dir = 'side'; G.facingLeft = dx < 0; } else G.dir = dy > 0 ? 'up' : 'down'; }
  act = { kind, t: 0.31 };
  sfx({ hoe: 'till', water: 'water', harvest: 'harvest', plant: 'plant', seed: 'plant', scythe: 'swish', hit: 'hit' }[kind] || null);
}
const FX_COLORS = { dust: ['#784f2e', '#9a6b42', '#5c3b21'], splash: ['#c0e6ff', '#58a4e4'], seed: ['#93d66a', '#fff09a'], swish: ['#e6cc74', '#93d66a'] };
function burst(type, cx, cy) {
  const cols = FX_COLORS[type], n = type === 'swish' ? 10 : type === 'seed' ? 5 : 8;
  for (let i = 0; i < n; i++) {
    const a = (i + Math.random() * 0.6) / n * Math.PI * 2, sp = type === 'seed' ? 0.6 : type === 'swish' ? 2 : 1.3;
    fx.push({ x: cx + 0.5, y: cy + (type === 'dust' ? 0.25 : 0.4), vx: Math.cos(a) * sp * (0.6 + Math.random() * 0.6), vy: (type === 'splash' ? 2.2 : 1.5) * (0.6 + Math.random() * 0.8), g: type === 'splash' ? 9 : 6, t: 0, life: 0.5, col: cols[i % cols.length] });
  }
}
function pop(cx, cy, item) { fx.push({ pop: iconKey(item), x: cx + 0.5, y: cy + 0.5, t: 0, life: 0.7 }); burst('seed', cx, cy); }
function requestWave(big) { wavePending = wavePending === 'big' || big ? 'big' : 'small'; }
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
  if (a > 0) a *= paceScale(S().season, G.heart);   // F-0: the heart fills one season's band at a time, slower near its top
  const before = G.heart; G.heart = Math.max(0, Math.min(Math.max(before, paceTop(S().season)), G.heart + a));
  if (a >= 2 && G.heart > before && reason) requestWave(false);
  if (reason && Math.abs(a) >= 0.5) toast((a > 0 ? '[#ff8fb1]♥[/]  ' : '[#9aa4c0]♡[/]  ') + reason);
  for (const [m, t] of [[30, 'Color returns...\n\nThe gray haze of the city is starting to lift.'], [50, 'Breathing again\n\nYou notice the smell of the soil and the sound of the wind.'], [75, 'Warm days\n\nThe village feels like home now.'], [100, 'Happiness rebuilt\n\nThe world is full of color again.']]) {
    if (before < m && G.heart >= m && !G.milestones.includes(m)) { G.milestones.push(m); showAnnounce(`[#ff8fb1]${t.split('\n')[0]}[/]${t.slice(t.indexOf('\n'))}`, 5); }
  }
}
function addKindness(n, fromBox) {
  if (n <= 0) return;
  if (!fromBox) S().kindness += n;
  if (S().donationDay !== G.day) { S().donationDay = G.day; changeHeart(2, 'Sharing warms your heart.'); }
  if (S().kindness >= 60 && unlock('starhill')) showAnnounce('[#ffb6d0]Word of your kindness spreads[/]\n\nGrandma Boksun whispers: "On clear nights, look for the glowing vine at the edge of your farm."\n\nStar Hill unlocked: clear nights after 20:00, top-right of the farm.', 8);
}
function useEnergy(a) {
  if (G.energy >= a) { G.energy -= a; if (G.energy < 1) G.log.exhausted = true; return true; }
  G.log.exhausted = true; toast("You're too tired... Eat something or get some sleep."); return false;
}
function eat(n) {
  const d = ITEMS[n]; if (!d || !edible(n) || !has(n)) { if (n) toast(`You can't eat ${n}.`); return; }
  remove(n); sfx(d.thirst > d.hunger ? 'gulp' : 'eat', 0.8); G.hunger = Math.min(100, G.hunger + d.hunger); G.thirst = Math.min(100, G.thirst + d.thirst);
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
function addGold(a) { if (a > 0) sfx('coin', 0.9, 0.02); G.gold = Math.max(0, G.gold + a); if (G.gold >= 1000) report('gold1000'); }

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
const plotOpen = p => p && (p.startOpen || G.openPlots.includes(p.id));
const plotRested = p => p && G.restedPlots.includes(p.id);
const plotCost = p => Math.max(2, Math.floor(((p.x1 - p.x0 + 1) * (p.y1 - p.y0 + 1) + 2) / 3));
function tillable(x, y) { return plotOpen(plotAt(x, y)); }
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
  const c = cropAt(x, y); const def = CROPS[c.id]; const blessed = c.blessed, rich = c.rich; const n = 1 + (blessed ? 1 : 0) + (rich ? 1 : 0);
  if (def.regrow) { c.days = def.days - def.regrow; c.blessed = false; c.rich = false; } else delete G.crops[x + ',' + y];
  add(def.harvest, n); G.log.harvested += n; pop(x, y, def.harvest);
  toast(`Harvested ${def.harvest} x${n}` + (blessed ? '  [#ffd84a](golden!)[/]' : '') + (rich ? '  [#c9a46a](rested soil +1)[/]' : ''));
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
  const plot = G.scene === 'farm' ? plotAt(cx, cy) : null;
  if (plot && !plotOpen(plot)) {
    const have = G.inv.Compost || 0, cost = plotCost(plot);
    if (item !== 'Compost') { toast(`This plot has gone gray and hard. Spread ${cost} bags of compost on it to bring it back. (You have ${have})`, 3.5); return; }
    if (have < cost) { toast(`Not enough compost. This plot needs ${cost} bags. (You have ${have})`, 3); return; }
    remove('Compost', cost); G.openPlots.push(plot.id); if (!G.restedPlots.includes(plot.id)) G.restedPlots.push(plot.id);
    changeHeart(2); toast('The gray crust crumbles away. Dark, soft soil breathes again. (+2 Heart, rested soil)', 4); return;
  }
  if (item === 'Compost') { toast('Spread compost on a gray, hardened plot to bring it back.', 2.5); return; }
  if (d.tool === 'scythe') {
    const ripe = []; for (let dx = -1; dx <= 1; dx++) for (let dy = -1; dy <= 1; dy++) { const c = cropAt(cx + dx, cy + dy); if (c && mature(c)) ripe.push([cx + dx, cy + dy]); }
    if (!ripe.length) { toast('Nothing is ripe here yet.'); return; }
    if (!useEnergy(2)) return; ripe.forEach(([x, y]) => harvestCell(x, y)); doAction('hoe', cx, cy); burst('swish', cx, cy); return;
  }
  const crop = cropAt(cx, cy);
  if (crop && mature(crop)) { harvestCell(cx, cy); doAction('harvest', cx, cy); return; }
  if (d.tool === 'hoe') {
    const t = cellsFor(cx, cy, d.tier).filter(([x, y]) => tillable(x, y) && !debrisAt(x, y) && !tileAt(x, y));
    if (!t.length) { toast(tileAt(cx, cy) ? 'This ridge is already tilled. Plant a seed here!' : 'You can only farm on the ridges of an open plot.'); return; }
    if (!useEnergy((d.tier >= 2 ? 6 : 4) * (unlocked('favor_hoe') ? 0.6 : 1))) return; t.forEach(([x, y]) => G.tiles[x + ',' + y] = { wet: false }); doAction('hoe', cx, cy); burst('dust', cx, cy);
  } else if (d.tool === 'can') {
    if (G.water <= 0) { toast('Your watering can is empty. Refill it at the well (E).'); return; }
    const t = cellsFor(cx, cy, d.tier).filter(([x, y]) => { const tl = tileAt(x, y); const c = cropAt(x, y); return tl && (!tl.wet || (c && !c.watered)); });
    if (!t.length) { toast(tileAt(cx, cy) ? 'This spot is already watered.' : 'Water tilled soil or crops.'); return; }
    if (!useEnergy(2)) return; G.water--;
    t.forEach(([x, y]) => { tileAt(x, y).wet = true; const c = cropAt(x, y); if (c) c.watered = true; }); doAction('water', cx, cy); burst('splash', cx, cy);
    if (G.water === 0) toast('The watering can is empty now.');
  } else if (d.cat === 'Seed') {
    if (!tileAt(cx, cy)) { toast('Till the soil with a hoe first.'); return; }
    if (crop) { toast('Something is already growing here.'); return; }
    const def = CROPS[d.crop];
    if (G.story.season < def.season) { toast(`It's still ${SEASONS[G.story.season]} in your heart. ${def.harvest} needs ${SEASONS[def.season]}.`); return; }
    if (!useEnergy(1)) return; remove(item); const rich = plotRested(plotAt(cx, cy)); G.crops[cx + ',' + cy] = { id: d.crop, days: 0, watered: tileAt(cx, cy).wet, blessed: false, rich }; doAction('harvest', cx, cy); burst('seed', cx, cy);
    if (rich && !G.richHint) { G.richHint = true; toast('This plot rested overnight. The soil is rich: crops planted today give +1 on their first harvest.', 4); }
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
function vmapOpen(id) {
  if (id === 'forest') return F().open; if (id === 'lake') return Lk().open; if (id === 'hill') return Hl().open;
  if (id === 'starhill') return unlocked('starhill'); return true;
}
function vmapStatus(id) {
  const open = vmapOpen(id);
  if (id === 'farm') { const plots = MAP.plots || []; return `Plots ${plots.filter(p => p.startOpen || G.openPlots.includes(p.id)).length}/${plots.length} open`; }
  if (id === 'market') { if (isMarketDay(G.day)) return 'Market day today!'; const n = daysUntilMarket(G.day); return n === 1 ? 'Market day tomorrow' : `Market day in ${n} days`; }
  if (id === 'forest') return open ? `Clearings ${forestRestored()}/3` + (F().treeAwake ? ' · the zelkova is awake' : '') : 'When spring comes to your heart';
  if (id === 'lake') return open ? (lakeClear() ? 'Clear all the way down' : `Clear parts ${lakeZonesClear()}/${LAKE_THRESH.length}`) : 'When summer comes to your heart';
  if (id === 'hill') return open ? `Fruit trees ${hillRevived()}/6` : 'When autumn comes to your heart';
  if (id === 'starhill') return open ? 'Clear nights after 20:00' : `Keep sharing (kindness ${S().kindness}/60)`;
  return '';
}
const vmapWho = id => id === 'market' ? VILLAGERS.filter(v => v.scene === 'market').map(v => v.name).join(' · ') : id === 'farm' ? VILLAGERS.filter(v => v.scene === 'farm').map(v => v.name).join(' · ') : '';
function vmapHere() {
  const sc = G.scene;
  if (sc === 'market' || sc === 'haggle') return 'market';
  if (sc === 'forest') return 'forest'; if (sc === 'lake') return 'lake'; if (sc === 'sunset') return 'hill'; if (sc === 'hill') return 'starhill';
  return 'farm';
}
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
  if (!f.treeAwake && forestRestored() === 3) { f.treeAwake = true; delta += 5; lines.push('The old zelkova woke up  [#9dff9d]+5[/]'); headline = '[#ffb6d0]Overnight, the old zelkova on the back hill woke up.[/]\nColor is flowing back into the whole forest.'; }
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
const lakeZonesClear = () => LAKE_THRESH.filter(t => Lk().clarity >= t).length;
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
  showAnnounce('[#8fd0ff]A little dokkaebi fire[/]\n\nDrawn by the light, a tiny gray flame bobs out of the dark and starts following you.\nGrandma Boksun would say it\'s a dokkaebi-bul. It looks hungry for color.\n\n[#ffd84a][G][/] Feed it what you\'re holding. Its flame takes the color of what it eats.', 9);
  report('spirit_found');
}
function feedSpirit() {
  const s = S().spirit; if (!s.found) return; const item = selectedItem();
  if (!item) { toast('Hold something to feed the dokkaebi fire.'); return; }
  const f = SPIRIT_FOOD[item]; if (!f) { toast(`The dokkaebi fire flickers at the ${item} and turns away.`); return; }
  const stageBefore = spiritStage(s), formBefore = spiritForm(s);
  remove(item); for (let i = 0; i < 4; i++) s.pts[i] += f[i]; s.xp += f.reduce((a, b) => a + b, 0);
  G.spirit.pulse = 1;
  toast(`The dokkaebi fire gobbles up the ${item}!`);
  const form = spiritForm(s);
  if (form !== formBefore) {
    const isNew = !s.forms.includes(form); if (isNew) s.forms.push(form);
    if (spiritStage(s) >= 1) report('spirit_young');
    toast(`[#8fd0ff]${spiritStage(s) > stageBefore ? 'The dokkaebi fire burns a little bigger!' : 'The dokkaebi fire changed color!'}[/]  ${FORM_NAMES[form]}`, 4);
    if (isNew) changeHeart(2, 'The dokkaebi fire glows with color.');
  }
}

// ------------------------------------------------------------------ villagers
// K-3: neighbours (pumasi, saecham, masil). Rules = Neighbors.cs, scenes = NeighborScenes.cs
const isEvening = h => h >= 18 && h < 22, isDay = h => h >= 6 && h < 18;
const vRestored = v => villagerProgress(v.id).wish >= v.chapters.length;
const vNext = v => vRestored(v) ? null : v.chapters[villagerProgress(v.id).wish];
const vReady = v => { const p = villagerProgress(v.id); return p.met && !vRestored(v) && G.day >= p.lastDay + 2; };
const sittingOut = v => !!v.ex && isEvening(G.hour);
const vPos = v => sittingOut(v) ? (v.id === 'mayor' ? roomAnchor('hall', 'mayorSeat', [v.ex, v.ey]) : [v.ex, v.ey])
                                  : (v.id === 'rosa' ? roomAnchor('mill', 'keeper', [v.x, v.y]) : [v.x, v.y]);   // H-2: Grandma works inside the mill; the village head's evenings by the hall stove
function villagerSprite(v) { const st = Math.min(3, villagerProgress(v.id).wish); return sittingOut(v) ? `Villagers/${v.id}_sit_${st}` : `Villagers/${v.id}_${st}`; }
function hasBubble(v) {
  const p = villagerProgress(v.id); if (!p.met) return true;
  if (sittingOut(v)) return S().masilDay !== G.day && vNext(v)?.kind === 'Masil' && vReady(v);
  const c = vNext(v); return !!c && c.kind !== 'Masil' && vReady(v) && !(c.kind === 'Pumasi' && p.invited);
}
function idleLine(v) { v.idle = (v.idle || 0) + 1; const pool = vRestored(v) ? v.color : v.gray; return v.idle % 2 === 1 ? v.season[S().season] : pool[v.idle % pool.length]; }
function invite(v, c) {
  const p = villagerProgress(v.id), lines = [c.invite];
  if (c.kind === 'Pumasi') p.invited = true;
  if (c.kind === 'Fish' && !has('Fishing Rod')) { add('Fishing Rod'); unlock('fishing'); lines.push('[#9dff9d]Received:[/] Fishing Rod'); }
  if (c.kind === 'Saecham' && c.need && !has(c.need)) lines.push(`[#c8c0b0](Hold a ${c.need} and talk to them.)[/]`);
  lines[lines.length - 1] += `\n[#c8c0b0]${c.hint}[/]`;
  return lines;
}
function talk(v) {
  G.log.talked = (G.log.talked || 0) + 1;
  const p = villagerProgress(v.id), portrait = villagerSprite(v), c = vNext(v);
  if (!p.met) { p.met = true; p.lastDay = -99; report('villager_met'); say(v.name, [v.intro, ...(c && c.kind !== 'Letter' ? invite(v, c) : [])], portrait); return; }
  if (v.id === 'rosa' && S().sikhyeDay !== G.day && G.thirst < 60) {   // W-1: Grandma's sikhye, once a day when you're thirsty
    S().sikhyeDay = G.day;
    say(v.name, ['You look parched. Here, have a bowl of sikhye before you go. I made it this morning.', '(Sweet, cold rice punch with rice grains floating on top. It goes down in one breath.)'], portrait,
      () => { sfx('gulp', 0.8); G.thirst = Math.min(100, G.thirst + 45); G.hunger = Math.min(100, G.hunger + 5); G.log.drank = true; changeHeart(1, 'Grandma Boksun gave you a bowl of sikhye.'); });
    return;
  }
  if (sittingOut(v)) {
    if (S().masilDay !== G.day) ask(v.name, `Sit with ${v.name} for a while? (masil: one house a night)`, portrait, () => masil(v), () => say(v.name, [idleLine(v)], portrait), 'Sit down', 'Just say hi');
    else say(v.name, ['Already been visiting tonight? Go get some sleep.'], portrait);
    return;
  }
  const held = selectedItem(), food = !!held && ITEMS[held]?.cat === 'Food' && has(held), day = isDay(G.hour);
  if (c && c.kind === 'Saecham' && vReady(v) && day && food && (!c.need || c.need === held)) {
    ask(v.name, `${c.invite}\n[#ffd84a]Share your ${held} as a work-break meal?[/]`, portrait, () => saecham(v, held, true), null, 'Eat together', 'Not now'); return;
  }
  if (c && vReady(v) && c.kind !== 'Masil') {
    if (c.kind === 'Letter') { say(v.name, [c.invite], portrait, () => letter(v)); return; }
    v.idle = (v.idle || 0) + 1;
    if (!(c.kind === 'Pumasi' && p.invited) || v.idle % 2 === 0) { say(v.name, invite(v, c), portrait); return; }
  }
  if (food && day && p.saechamDay !== G.day) { ask(v.name, `${idleLine(v)}\n[#ffd84a]Share your ${held}?[/]`, portrait, () => saecham(v, held, false), null, 'Eat together', 'Not now'); return; }
  const lines = [idleLine(v)]; if (c && c.kind === 'Masil' && vReady(v)) lines.push(c.invite);
  say(v.name, lines, portrait);
}
function finishChapter(v, done, before) {
  const p = villagerProgress(v.id), c = vNext(v);
  p.wish++; p.lastDay = G.day; p.invited = false; p.chore = 0; v.pulse = 1;
  addKindness(3); report('wish'); const restored = vRestored(v); if (restored) report('restored');
  const rw = []; if (c.gold) { addGold(c.gold); rw.push(c.gold + 'G'); }
  (c.reward || []).forEach(([n, k]) => { add(n, k); rw.push(`${n} x${k}`); });
  if (c.unlock && unlock(c.unlock) && c.unlockText) rw.push(c.unlockText);
  const lines = [...(before || []), ...c.scene]; if (rw.length) lines.push('[#9dff9d]Received:[/] ' + rw.join(', '));
  if (c.favor && unlock(c.favor) && c.favorText) lines.push('[#ffd84a]Pumasi returned:[/] ' + c.favorText);
  if (restored) lines.push(v.restored);
  if (c.heart) changeHeart(c.heart, `${v.name} smiled.`);
  say(v.name, lines, villagerSprite(v), () => {
    if (restored) { showAnnounce(`[#ffd84a]${v.name}'s colors have returned![/]\n\n${VILLAGERS.filter(vRestored).length} / ${VILLAGERS.length} neighbors`, 4); changeHeart(5, 'The village feels a little brighter.'); }
    if (done) done();
  });
}
function saecham(v, food, chapter) {
  if (!has(food)) return; remove(food, 1); villagerProgress(v.id).saechamDay = G.day; G.log.saecham = true; S().saechamCount = (S().saechamCount || 0) + 1;
  if (chapter) { finishChapter(v); return; }
  addKindness(1); changeHeart(2, 'You shared a work-break meal.');
  say(v.name, [`(You sit down together and share the ${food}.)`, 'Food tastes better when someone brings it. Thank you.'], villagerSprite(v));
}
function masil(v) {
  S().masilDay = G.day; G.log.masil = true; S().masilCount = (S().masilCount || 0) + 1;
  const stood = [G.px, G.py]; const seat = vPos(v); G.px = seat[0] + 0.72; G.py = seat[1]; G.sitting = true;
  const standUp = () => { G.sitting = false; [G.px, G.py] = stood; G.hour = Math.min(G.hour + 1, 23); };
  if (vNext(v)?.kind === 'Masil' && vReady(v)) { finishChapter(v, standUp); return; }
  changeHeart(1, 'A quiet evening with a neighbor.');
  say(v.name, ['(You sit side by side for a while.)', v.masil[G.day % v.masil.length]], villagerSprite(v), standUp);
}
function letter(v) {
  const face = villagerSprite(v);
  const write = (first, last) => { showAnnounce(`[#ffd84a]A letter[/]\n\n${first}\nI lit the forge again. A neighbor worked the bellows for me. I remembered what you used to say, that it sings.\n${last}`, 9); finishChapter(v); };
  const second = first => ask(v.name, '...And how should it end?', face, () => write(first, '"I miss you."'), () => write(first, '"Come anytime. I\'ll keep the fire going."'), 'I miss you', 'Come anytime');
  ask(v.name, 'How should it start? You write, I\'ll talk.', face, () => second('"How are you doing? It\'s Dad."'), () => second('"I\'m sorry. It\'s Dad."'), 'How are you?', "I'm sorry.");
}
function onFishCaught() { const m = VILLAGERS.find(v => v.id === 'mina'); if (m && villagerProgress('mina').met && vNext(m)?.kind === 'Fish') finishChapter(m); }
const CHORES = [{ id: 'steamer', who: 'rosa', scene: 'market', x: roomAnchor('mill', 'steamer', [-7.8, 0.95])[0], y: roomAnchor('mill', 'steamer', [-7.8, 0.95])[1] }, { id: 'forge', who: 'theo', scene: 'market', x: 1.15, y: 0.85 },
                { id: 'briquettes', who: 'mayor', scene: 'market', x: 4.35, y: -3.35 }, { id: 'sled', who: 'mina', scene: 'farm', x: -6.0, y: -3.55 }];
const choreNear = () => CHORES.find(s => s.scene === G.scene && near(s.x, s.y + 0.3, 1.4));
function useChore(s) {
  const v = VILLAGERS.find(x => x.id === s.who), p = villagerProgress(v.id), c = vNext(v);
  const mine = c && c.kind === 'Pumasi' && c.spot === s.id;
  if (!mine || !p.invited || !vReady(v) || !isDay(G.hour)) {
    toast(mine && !p.invited ? `Talk to ${v.name} first.` : mine && !isDay(G.hour) ? "It's too late for that today." : { steamer: `${v.name}'s rice-cake steamer.`, forge: `${v.name}'s forge and bellows.`, briquettes: 'Coal briquettes for the village hall.', sled: "Mina's wooden ice sled." }[s.id], 2.5); return;
  }
  if (!useEnergy(3)) return;
  p.chore++; doAction(s.id === 'sled' ? 'harvest' : 'hoe', s.x, s.y); burst('dust', s.x, s.y);
  toast(`${c.steps[p.chore - 1]}  (${p.chore}/${c.steps.length})`, 2.5);
  if (p.chore >= c.steps.length) { G.log.pumasi = true; finishChapter(v); }
}
const helpWanted = () => VILLAGERS.find(v => vNext(v)?.kind === 'Pumasi' && vReady(v))?.id || null;

// ------------------------------------------------------------------ K-4: the winter solstice and Jeongwol Daeboreum (RiteState.cs, RiteScenes.cs)
function Rt() { const r = G.story.rites || (G.story.rites = {}); return Object.assign(r, Object.assign({ dongjiPrep: 0, dongjiMemory: null, dongjiPot: false, dongjiDelivered: [], dongjiDone: false,
  boreumDay: -1, daljipWood: 0, bureom: false, jwibul: false, lit: false, burned: null, wish: null, boreumDone: false }, r)); }
const RITE = { dongji: 7, prepFrom: 5, steps: 3, bowls: 5, earliest: 12, notice: 2, scheduleFrom: 9 };
const POT_AT = roomAnchor('mill', 'pot', [-7.6, -0.55]), GATE_AT = [2.0, -2.55], DALJIP = ANCHOR('daljip', 18.5, -9.4);
const woodNeed = () => unlocked('favor_hands') ? 3 : 5;
const boreumSched = () => Rt().boreumDay >= 0 && !Rt().boreumDone;
const daljipBuilt = () => Rt().daljipWood >= woodNeed();
const boreumNight = () => boreumSched() && G.day >= Rt().boreumDay && G.hour >= 19;
const gathering = () => boreumNight() && daljipBuilt();
const dongjiWindow = () => G.day >= RITE.prepFrom && G.day <= RITE.dongji;
const canDeliver = who => G.day === RITE.dongji && Rt().dongjiPot && !Rt().dongjiDone && !Rt().dongjiDelivered.includes(who);
const MEMORY = { dinner: ['Convenience-store dinners, eaten alone', 'Rolling rice balls, I remembered all those dinners eaten alone. Tonight I ate with someone.'],
  train: ['The last train home', "Rolling rice balls, I remembered the last train home. I won't need to run for it anymore."],
  office: ['Office lights that never went off', 'Rolling rice balls, I remembered the office lights that never went off. Here, the lights go out at night.'] };
const BURN = { badge: ['My company ID badge', 'The badge that hung around my neck curls up in the fire. My neck feels lighter.'],
  overtime: ['My overtime log', 'Page after page of late nights goes up in sparks. Every one of those hours was mine.'],
  mail: ['A printout of unread emails', 'Three hundred unread emails, and none of them can reach me here.'] };
const WISH = { sleep: ['I want to sleep well', 'Just like I wrote on my wish paper, I slept well.'], laugh: ['I want to laugh with someone', 'Just like I wrote on my wish paper, I laughed with someone.'],
  eat: ['I want to eat slowly', 'Just like I wrote on my wish paper, I ate slowly.'], rest: ["It's okay to do nothing", 'Just like I wrote on my wish paper, I did nothing for a while. It was fine.'] };
const DONGJI_LINE = { rosa: "Me too? Ha, first time in years someone's served me a bowl. ...It's sweet.", theo: "Red beans keep the bad stuff away, they say. ...Thanks. I'll eat it while it's hot.",
  mina: "Grandma says I have to eat as many rice balls as I am old! ...That's nine. I can do it.", mayor: 'Ah, patjuk on the winter solstice! Now it really feels like the village is alive.' };
const vName = id => VILLAGERS.find(v => v.id === id).name, vFace = id => `Villagers/${id}_${Math.min(3, villagerProgress(id).wish)}`;
const wishKept = (w, f) => w === 'sleep' ? f.onTime && !f.passedOut : w === 'laugh' ? !!f.masil : w === 'eat' ? f.meals >= 1 : w === 'rest' ? !!f.rested : false;
function choose(title, options, pick) { panel = { kind: 'choice', title, options, pick }; }
function riteBroadcast(day) {
  const r = Rt();
  if (!r.dongjiDone && day === RITE.prepFrom) return 'The winter solstice is the day after tomorrow. Grandma Boksun is making red bean porridge at the mill. Lend a hand sorting beans, eh?';
  if (!r.dongjiDone && day === RITE.dongji) return "Today is the winter solstice. Share a bowl of red bean porridge, and don't forget to scatter beans at your gate.";
  if (!boreumSched()) return null;
  const left = r.boreumDay - day;
  if (left === 2) return 'The first full moon, Jeongwol Daeboreum, is in two days! Bring wood to the empty paddy south of the new house for the moon-house bonfire.';
  if (left === 1) return 'Tomorrow is Daeboreum. Anyone helping stack the moon house, head down to the paddy.';
  if (left === 0) return 'Today is Jeongwol Daeboreum! Crack your nuts this morning, and at seven tonight we light the moon house in the paddy!';
  return left < 0 ? 'The moon house is still standing. Come down to the paddy this evening, even if it\'s a day late.' : null;
}
function riteTalk(v) {
  const r = Rt();
  if (v.id === 'rosa' && G.day === RITE.dongji && r.dongjiPrep >= RITE.steps && !r.dongjiPot && !r.dongjiDone) { takePot(); return true; }
  if (canDeliver(v.id) && has('Red Bean Porridge')) {
    remove('Red Bean Porridge', 1); r.dongjiDelivered.push(v.id); r.dongjiDone = r.dongjiDelivered.length >= RITE.bowls;
    say(v.name, [DONGJI_LINE[v.id]], villagerSprite(v), r.dongjiDone ? finishDongji : null); return true;
  }
  if (v.id === 'mina' && boreumSched() && G.day >= r.boreumDay && G.hour >= 6 && G.hour < 18 && !r.bureom) {
    say(v.name, ['Grandma says to bite these. Walnuts and peanuts. They\'re called bureom.',
      "If you crack something hard with your teeth on the morning of Daeboreum, you won't get boils all year. ...I don't know what boils are either.",
      'On three, together! One, two, three... *crack!*'], villagerSprite(v), () => { r.bureom = true; sfx('bureom_crack'); changeHeart(1, 'Crack! No boils this year.'); G.log.riteNote = 'Mina and I cracked walnuts with our teeth this morning. Hers went flying.'; });
    return true;
  }
  return false;
}
function takePot() {
  Rt().dongjiPot = true; add('Red Bean Porridge', RITE.bowls);
  say(vName('rosa'), ['Five bowls. One for each house: the Dongsu place, little Mina, the village head... and me, of course.', 'And the last one, scatter it at your own gate. Red beans keep the bad things out.'], vFace('rosa'));
}
function finishDongji() {
  G.log.riteNote = "On the solstice I carried porridge from house to house. The longest night, and I wasn't cold.";
  changeHeart(4, 'Every house got a bowl of porridge.'); requestWave(false);
  showAnnounce('[#ffd84a]Winter solstice[/]\n\nThe longest night of the year.\nFrom tomorrow, the days grow longer again.', 6);
}
function useRitePot() {
  const r = Rt();
  if (dongjiWindow() && G.hour >= 6 && G.hour < 20 && r.dongjiPrep < RITE.steps && !r.dongjiDone) {
    if (!useEnergy(2)) return; r.dongjiPrep++; doAction('harvest', POT_AT[0], POT_AT[1]); burst('dust', POT_AT[0], POT_AT[1]);
    toast(['You sort the red beans, picking out the little stones.', 'You press the boiled beans through a sieve. The kitchen smells sweet.', 'You roll little rice-flour balls (saealsim) between your palms.'][r.dongjiPrep - 1] + `  (${r.dongjiPrep}/${RITE.steps})`, 2.5);
    if (r.dongjiPrep >= RITE.steps) choose('Rolling rice balls, something from the city comes back to you...', Object.values(MEMORY).map(m => m[0]), i => {
      const id = Object.keys(MEMORY)[i]; r.dongjiMemory = id; G.log.riteNote = MEMORY[id][1];
      say(vName('rosa'), ["You've gone quiet. ...You don't have to say. Rice balls are for rolling things up and letting them go.", 'Come back on the solstice. I\'ll ladle you out the porridge.'], vFace('rosa'));
    });
    return;
  }
  if (G.day === RITE.dongji && r.dongjiPrep >= RITE.steps && !r.dongjiPot && !r.dongjiDone) { takePot(); return; }
  toast(r.dongjiPrep >= RITE.steps && !r.dongjiPot ? 'The porridge will be ready on the solstice (day 7).' : 'A big pot of red bean porridge, bubbling.', 2.5);
}
function useGate() {
  const r = Rt(); if (!canDeliver('gate') || !has('Red Bean Porridge')) return false;
  remove('Red Bean Porridge', 1); r.dongjiDelivered.push('gate'); r.dongjiDone = r.dongjiDelivered.length >= RITE.bowls; doAction('water', GATE_AT[0], GATE_AT[1]);
  toast('You scatter red beans at the gate. Bad memories stop here.', 3); if (r.dongjiDone) finishDongji(); return true;
}
function useDaljip() {
  const r = Rt();
  if (boreumSched() && G.day >= r.boreumDay - RITE.notice && !daljipBuilt()) {
    const have = G.inv.Wood || 0; if (!have) { toast(`The moon house needs wood. (${r.daljipWood}/${woodNeed()})`, 3); return; }
    const n = Math.min(have, woodNeed() - r.daljipWood); r.daljipWood += n; remove('Wood', n); doAction('harvest', DALJIP[0], DALJIP[1]); burst('dust', DALJIP[0], DALJIP[1]);
    if (!daljipBuilt()) { toast(`You stack the wood. (${r.daljipWood}/${woodNeed()})`, 2.5); return; }
    G.log.pumasi = true;
    say(vName('mayor'), [...(unlocked('favor_hands') ? ['See? One announcement and the whole village came out. Pumasi pays back.'] : []),
      "Grandma Boksun brought straw, Dongsu brought pine branches, and Mina tied the rope. It's a fine moon house.", `Seven o'clock on Daeboreum night (day ${r.boreumDay}). Don't be late.`], vFace('mayor'));
    return;
  }
  if (daljipBuilt() && boreumNight()) { ceremony(); return; }
  if (daljipBuilt() && boreumSched()) { toast(G.day < r.boreumDay ? `The moon house is ready. Daeboreum is day ${r.boreumDay}, at seven in the evening.` : 'Come back at seven this evening.', 3); return; }
  if (r.boreumDone) toast('Where the moon house burned. The ashes are still warm.', 2.5);
}
let jwibul = null;
function ceremony() {
  const r = Rt();
  if (!r.jwibul) { say(vName('mina'), ['I put embers in the can! Swing it round and round, like this!', '(Hold the wire and swing. A ring of fire spins in the dark.)'], vFace('mina'), () => { jwibul = { t: 0, done: () => { r.jwibul = true; moonrise(); } }; }); return; }
  moonrise();
}
function moonrise() {
  say(vName('mayor'), ["There! The moon's up! Light it, light it!", 'Everyone, make a wish when the flames go up.'], vFace('mayor'), () => {
    Rt().lit = true; sfx('fire_whoosh');
    panel = { kind: 'view', layers: ['rite_moon'], caption: '[#ffd84a]The first full moon of the year[/]   The moon house burns, and the village stands around it.', onClose: burnChoice };
  });
}
function burnChoice() {
  choose('You brought something from the city. What goes into the fire?', Object.values(BURN).map(b => b[0]), i => {
    const id = Object.keys(BURN)[i]; Rt().burned = id; say('Me', [BURN[id][1]], null, wishChoice);
  });
}
function wishChoice() {
  choose('A wish paper. One wish, just for you.', Object.values(WISH).map(w => w[0]), i => {
    const r = Rt(); r.wish = Object.keys(WISH)[i]; r.lit = true; r.boreumDone = true;
    G.log.riteNote = `I put ${BURN[r.burned][0][0].toLowerCase() + BURN[r.burned][0].slice(1)} in the moon house fire, and wrote "${WISH[r.wish][0]}" on a wish paper.`;
    say(vName('rosa'), [`Tied your wish on? "${WISH[r.wish][0]}"... good. The fire will carry it up.`], vFace('rosa'),
      () => say(vName('mayor'), ["Well then. Spring will be here tomorrow morning. You'll see."], vFace('mayor'), () => { changeHeart(6, 'The village greeted the first full moon together.'); requestWave(true); }));
  });
}
function riteNear() {
  if (G.scene === 'market' && dongjiWindow() && !Rt().dongjiDone && near(POT_AT[0], POT_AT[1] + 0.3, 1.5)) return 'pot';
  if (G.scene === 'farm' && canDeliver('gate') && has('Red Bean Porridge') && near(GATE_AT[0], GATE_AT[1] + 0.3, 1.5)) return 'gate';
  if (G.scene === 'farm' && (boreumSched() && G.day >= Rt().boreumDay - RITE.notice || Rt().boreumDone && S().season === 0) && near(DALJIP[0], DALJIP[1] - 0.2, 1.6)) return 'daljip';
  return null;
}
function ritePrompt() {
  const n = riteNear(), r = Rt(); if (!n) return null;
  if (n === 'pot') return r.dongjiPrep < RITE.steps ? `[E] Help with the porridge (${r.dongjiPrep}/${RITE.steps})` : G.day === RITE.dongji && !r.dongjiPot ? '[E] Take the porridge' : 'Red bean porridge pot';
  if (n === 'gate') return '[E] Scatter red beans at the gate';
  if (!daljipBuilt() && boreumSched()) return `[E] Stack wood on the moon house (${r.daljipWood}/${woodNeed()})`;
  return boreumNight() ? '[#ffd84a][E] Join the Daeboreum rite[/]' : 'The moon house';
}

// ------------------------------------------------------------------ K-5: the demo ending (DemoEnd.cs, DemoRecap.cs)
let demoEndPending = false;
const STORE = { url: 'https://itch.io/', name: 'itch.io' };
function demoRecap() {
  const r = Rt(), j = Jg(), L = [`You lived in the village for ${G.day - 1} days.`];
  if (r.dongjiMemory) L.push(`On the solstice, rolling rice balls, you remembered "${MEMORY[r.dongjiMemory][0]}".`);
  if (r.burned) { const b = BURN[r.burned][0]; L.push(`You let the moon house fire take ${b.startsWith('My ') ? 'your ' + b.slice(3) : b[0].toLowerCase() + b.slice(1)}.`); }
  if (r.wish) L.push(`Your wish paper said: "${WISH[r.wish][0]}".`);
  const restored = VILLAGERS.filter(vRestored).map(v => v.name), close = VILLAGERS.filter(v => !vRestored(v) && villagerProgress(v.id).wish > 0).length;
  if (restored.length) L.push(`Color came back to ${restored.join(', ')}.`);
  if (close) L.push(`You grew closer to ${close} more neighbor(s). Their stories go on in spring.`);
  if ((j.madeD || 0) + (j.madeG || 0)) L.push(`You made your own sauces: ${j.madeD || 0} doenjang, ${j.madeG || 0} ganjang.`); else if (j.jars.some(x => x.start >= 0)) L.push('Sauce is still ripening in your jars.');
  if (S().saechamCount || S().masilCount) L.push(`You shared ${S().saechamCount || 0} work-break meal(s) and spent ${S().masilCount || 0} evening(s) visiting.`);
  return L;
}
function drawDemoEnd() {
  ctx.fillStyle = '#0d0a0a'; ctx.fillRect(0, 0, CW, CH); ctx.imageSmoothingEnabled = false;
  const pg = panel.page, pic = pg === 0 ? 'World/bg_farmmap_spring' : pg === 2 ? 'World/bg_lake_summer' : null;
  if (pic && IMG[pic]) ctx.drawImage(IMG[pic], CW / 2 - 400, 16, 800, 444);
  box(CW / 2 - 470, pic ? 470 : 140, 940, pic ? 190 : 420);
  const ty = pic ? 486 : 170;
  const T = ['Spring has come', 'Me, this winter', 'The rest of the year, in the full game', 'Thank you for playing the demo'][pg];
  const B = [ "The snow on the ridges melts. The stream runs again.\nThe village you came to with a gray heart is green this morning.", demoRecap().join('\n'),
    '· Spring: flower pancakes on Samjinnal, the back hill wakes up\n· Summer: washing your hair in iris water on Dano, the reservoir\n· Autumn: the Chuseok full moon, Sunset Hill, kimchi-making with the whole village\n· Every neighbor\'s spring, summer and autumn stories',
    `If you'd like to see the rest of the year, follow the game on ${STORE.name}.\nIt helps a small developer more than you know.\n\n[#ffd84a][1][/] Follow on ${STORE.name}     [#ffd84a][2][/] Back to title` ][pg];
  drawRich(`[#ffd84a]${T}[/]`, CW / 2, ty, 26, 900, 'center');
  drawRich(B, CW / 2, ty + 44, 17, 880, 'center');
  drawRich(pg < 3 ? '[E] / Space / click: next' : '[1] or [2]', CW / 2, CH - 30, 14, 600, 'center', '#c8c0b0');
  hit(0, 0, CW, CH, () => { if (panel.page < 3) panel.page++; });
}

const onBedding = () => G.scene === 'home' && Math.abs(G.px + 3.72) < 0.9 && Math.abs(G.py - 0.06) < 1.0;   // H-1: the yo on the araetmok

// ------------------------------------------------------------------ day end (DataManager.EndDay)
function trySleep() {
  if (G.hour < 18 && G.energy > maxEnergy() * 0.25) { toast("It's too early to sleep. (You can sleep after 18:00)"); return; }
  endDay(false);
}
function endDay(passedOut) {
  const lines = []; let delta = 0;
  const facts = { passedOut, lateNight: !passedOut && G.hour >= 24, onTime: !passedOut && G.hour < 24, meals: G.log.meals, harvested: G.log.harvested,
                  talked: G.log.talked || 0, drank: G.log.drank, exhausted: G.log.exhausted, starved: G.log.starved, rested: !!G.log.rested,
                  pumasi: !!G.log.pumasi, saecham: !!G.log.saecham, masil: !!G.log.masil, riteNote: G.log.riteNote };
  const wasOpen = [F().open, Lk().open, Hl().open], seasonBefore = S().season;
  if (passedOut) { delta -= 8; lines.push('Passed out from exhaustion  [#ff8080]-8[/]'); const lost = Math.min(Math.floor(G.gold / 10), 100); if (lost) { G.gold -= lost; lines.push(`Someone carried you home... you lost ${lost}G`); } }
  else if (G.hour < 24) { delta += 4; lines.push('Went to bed on time  [#9dff9d]+4[/]'); report('sleep_early'); }
  else { delta -= 2; lines.push('Stayed up too late  [#ff8080]-2[/]'); }
  if (G.log.meals) { delta += 3; lines.push('Ate proper meals  [#9dff9d]+3[/]'); }
  if (G.log.drank) { delta += 1; lines.push('Drank fresh water  [#9dff9d]+1[/]'); }
  if (!G.log.exhausted) { delta += 3; lines.push("Didn't overwork  [#9dff9d]+3[/]"); } else { delta -= 3; lines.push('Worked until exhausted  [#ff8080]-3[/]'); }
  if (G.log.starved) { delta -= 5; lines.push('Went hungry or thirsty  [#ff8080]-5[/]'); }
  if (G.log.harvested) { delta += 2; lines.push(`Harvested ${G.log.harvested} crops  [#9dff9d]+2[/]`); }
  { const j = Jg();   // K-2: a warm floor, and the jars count the day's sun or rain
    if (j.fireToday && !passedOut) { facts.warm = true; delta += 2; lines.push('Slept on a warm floor  [#9dff9d]+2[/]'); }
    if (j.lidsOpen) for (const jar of j.jars) if (jar.start >= 0) { if (G.raining || G.snowing) jar.rain++; else jar.care++; }
    j.fireToday = false; }
  if (wishKept(Rt().wish, facts)) { delta += 1; facts.wishLine = WISH[Rt().wish][1]; }
  changeHeart(delta);
  G.hunger = Math.max(0, G.hunger - 15); G.thirst = Math.max(0, G.thirst - 20); G.health = Math.min(100, G.health + 30);

  // seasons of the heart
  const before = S().season; let after = before;
  const holdSpring = before === 0 && !Rt().boreumDone;   // K-4: spring only after the Daeboreum night
  if (holdSpring && G.heart >= 17.5 && Rt().boreumDay < 0 && G.day + 1 >= RITE.scheduleFrom) { Rt().boreumDay = Math.max(G.day + 1 + RITE.notice, RITE.earliest); lines.push(`Spring is at the door. The village will greet it on Jeongwol Daeboreum (day ${Rt().boreumDay}).`); }
  if (before < 3 && seasonClosed(before, G.day + 1)) { after = before + 1; G.story.seasonStart = G.day + 1; G.heart = Math.max(G.heart, THRESH[after]); }   // F-0: after the season's last rite, one step forward
  S().season = after; S().highest = Math.max(S().highest, after); if (after >= 3) unlock('season_autumn');
  if (after > before) requestWave(true); else if (delta >= 4) requestWave(false);
  if (before === 0 && after >= 1 && Rt().boreumDone && unlock('demo_end')) demoEndPending = true;   // K-5: the demo ending
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
  if (!G.raining && unlocked('favor_water') && ((G.day + 1) * 7 + 3) % 2 === 0) {   // Mina's pumasi: some mornings she waters a few crops
    let w = 0; for (const k in G.crops) { const c = G.crops[k]; if (w >= 6 || c.watered || mature(c)) continue; c.watered = true; if (G.tiles[k]) G.tiles[k].wet = true; w++; }
    if (w) lines.push(`Small footprints by the field: Mina watered ${w} crop(s) this morning`);
  }
  // plots: an open plot left empty overnight rests (rich soil); the compost heap turns every two things into a bag
  let rested = 0;
  for (const p of PLOTS) { if (!plotOpen(p)) continue; const has = Object.keys(G.crops).some(k => { const [x, y] = k.split(',').map(Number); return plotAt(x, y) === p; });
    if (has) G.restedPlots = G.restedPlots.filter(id => id !== p.id); else if (!G.restedPlots.includes(p.id)) { G.restedPlots.push(p.id); rested++; } }
  if (rested) lines.push(`${rested} plot(s) rested overnight (rich soil for the next crop)`);
  const made = Math.floor(G.compostPoints / 2); if (made) { G.compostPoints -= made * 2; G.compostReady = Math.min(20, G.compostReady + made); lines.push(`The compost heap made ${made} bag(s) of compost`); }
  const bloom = perk('bloom'); if (bloom) { delta += bloom; lines.push(`The dokkaebi fire kept the room warm all night  [#9dff9d]+${bloom}[/]`); changeHeart(bloom); }
  // market + resources + log
  const cands = Object.values(CROPS).filter(c => c.season <= after).map(c => c.harvest);
  G.market = { demand: cands[Math.floor(Math.random() * cands.length)], haggle: 0, haggled: false, sold: {} };
  G.cleared = []; G.log = { harvested: 0, meals: 0, exhausted: false, starved: false, drank: false, talked: 0, rested: false, pumasi: false, saecham: false, masil: false };
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
  facts.delta = delta;
  const notes = lines.filter(l => !/^(Slept on a warm|Passed out|Went to bed|Stayed up|Ate proper|Drank fresh|Didn't overwork|Worked until|Went hungry|Harvested)/.test(l)).map(l => l.replace(/\s*\[#[0-9a-f]{6}\][+-]\d+\[\/\]/g, ''));
  sfx('page', 0.8);
  showAnnounce(`${header}\n\n[#e8d8b0]— Yesterday's diary —[/]\n${diaryLines(facts, G.day - 1).join('\n')}\n[#ff8fb1]${diaryMood(delta)}[/]` +
               (notes.length ? `\n\n[#c8c0b0]${notes.join('\n')}[/]` : '') + todayPreview(after), 10);
  queueBroadcast(broadcastLines({ day: G.day, before: seasonBefore, after, raining: G.raining, snowing: G.snowing,
    forestOpened: !wasOpen[0] && F().open, lakeOpened: !wasOpen[1] && Lk().open, hillOpened: !wasOpen[2] && Hl().open,
    mejuSeason: after === 0 && !Jg().jars.some(x => x.start >= 0) && !Jg().hung.length, lidsOpen: Jg().lidsOpen, helpWanted: helpWanted(), rite: riteBroadcast(G.day) }));
  letters();
  save();
}
function todayPreview(season) {
  const tips = [];
  const ripe = Object.values(G.crops).filter(mature).length; if (ripe) tips.push(`${ripe} crop(s) ripe`);
  const tm = Object.values(G.crops).filter(c => !mature(c) && season >= CROPS[c.id].season && c.days + 1 >= CROPS[c.id].days).length;
  if (tm) tips.push(`${tm} crop(s) ripen tomorrow if watered`);
  if (season > 0 && season < 3 && PACE.days[season] - seasonDays() <= 2) tips.push(`${SEASONS[season + 1]} feels close`);
  const ms = VILLAGERS.find(v => vNext(v)?.kind === 'Masil' && vReady(v)); if (ms) tips.push(`${ms.name} would like a visit this evening (masil)`);
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
  if (scene === 'home' || G.scene === 'home') sfx('door_slide', 0.8);
  G.scene = scene; G.px = x; G.py = y; panel = null; fishing = null;
  if (G.spirit) { G.spirit.x = x - 0.8; G.spirit.y = y + 0.8; }
  if (scene === 'forest' && F().forageDay !== G.day) rollForage();
  if (scene === 'lake' && Lk().forageDay !== G.day) rollLakeForage();
  if (scene === 'market') {
    if (isMarketDay(G.day)) toast("It's market day! Stalls line the lane, and the old farmer by the green tarp is up for some haggling.", 4);
    else toast(`Today's demand: ${G.market.demand} (+50%)` + (G.market.haggle ? `  |  Haggle bonus +${Math.round(G.market.haggle * 100)}%` : '') + `   Next market day: ${daysUntilMarket(G.day) === 1 ? 'tomorrow' : 'in ' + daysUntilMarket(G.day) + ' days'}`, 4);
  }
  if (scene === 'haggle') {
    if (G.market.haggled) { haggle = { done: true, text: "You've already haggled today.\nCome back next market day!", t: 2.5 }; }
    else haggle = { v: 0, dir: 1, done: false };
  }
  if (scene === 'hill') { if (S().starsDay !== G.day) { S().starsDay = G.day; S().starsTonight = 0; } G.stars = []; G.starTimer = 1.5; report('starhill'); toast('You climb the glowing vine... the whole sky opens up.'); }
}
function objectsFor(scene) {
  if (scene !== 'farm') return [];
  const o = [];
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
      if (Hl().restDay !== G.day) { Hl().restDay = G.day; G.log.rested = true; changeHeart(2, 'You sat in the pavilion and watched the sun go down over the valley.'); }
    }
    return;
  }
  if (G.scene === 'lake') {
    const spot = LFISH.find(([x, y]) => near(x, y, 1.3)); if (spot || (fishing && fishing.lake)) { fish(spot || fishing.at, true); return; }
    if (near(LSTEPS[0], LSTEPS[1], 1.3)) { releaseFish(); return; }
    if (near(LBENCH[0], LBENCH[1], 1.3)) { if (Lk().restDay !== G.day) { Lk().restDay = G.day; G.log.rested = true; changeHeart(2, 'You sat under the willow and watched the water.'); } else toast("The willow sways. You've already rested here today."); return; }
    return;
  }
  if (G.scene === 'forest') {
    if (near(FSEAT[0], FSEAT[1], 1.3)) {
      G.thirst = 100; G.log.drank = true; const cap = waterCap(); if (cap) G.water = cap;
      if (F().restDay !== G.day) { F().restDay = G.day; G.log.rested = true; changeHeart(2, 'You sat by the spring and listened to the forest.'); }
      else toast("You drink from the spring. It's cool and clear.");
      return;
    }
    if (near(FTREE[0], FTREE[1] + 0.6, 2.2)) { toast(F().treeAwake ? 'The old zelkova rustles softly. Its shade is cool and green.' : `The old zelkova on its stone platform is asleep, gray to its roots. Maybe if the clearings around it came back to life... (${forestRestored()}/3)`, 4); return; }
    return;
  }
  if (G.scene === 'farm') {
    { const v = VILLAGERS.find(v => v.scene === G.scene && near(...vPos(v), 1.0) && !(v.id === 'mina' && gathering())); if (v) { if (!riteTalk(v)) talk(v); return; } const ch = choreNear(); if (ch) { useChore(ch); return; }
      const rn = riteNear(); if (rn === 'pot') { useRitePot(); return; } if (rn === 'gate') { useGate(); return; } if (rn === 'daljip') { useDaljip(); return; } }
    if (near(COMPOST[0], COMPOST[1], 1.4)) { useHeap(); return; }
    { const s = jangSpot(); if (s === 'agungi') { useAgungi(); return; } if (s === 'rack') { useMejuRack(); return; } if (s === 'jars') { useJangdok(); return; } }
    if (near(WELL[0], WELL[1] - 0.4, 1.3)) { sfx('well_bucket', 0.8); sfx('gulp', 0.7); G.thirst = 100; G.log.drank = true; report('drink'); const cap = waterCap(); if (cap) { G.water = cap; toast(`You drink cool water and fill your watering can. (${G.water}/${cap})`); } else toast('You drink cool, fresh water.'); return; }
    if (starVineOpen() && near(LADDER[0], LADDER[1] - 1.0, 1.2)) { go('hill', 0, -3.1); return; }
    if (near(POND[0], POND[1], 1.9)) { fish(); return; }
  } else if (G.scene === 'home') {
    if (onBedding()) { trySleep(); return; }
  } else if (G.scene === 'market') {
    { const v = VILLAGERS.find(v => v.scene === G.scene && near(...vPos(v), 1.0) && !(v.id === 'mina' && gathering())); if (v) { if (!riteTalk(v)) talk(v); return; } const ch = choreNear(); if (ch) { useChore(ch); return; }
      const rn = riteNear(); if (rn === 'pot') { useRitePot(); return; } if (rn === 'gate') { useGate(); return; } if (rn === 'daljip') { useDaljip(); return; } }
    if (near(-4.2, -2.0, 1.1)) { share(); return; }
    if (!roomAt(G.px, G.py) && near(TAP[0], TAP[1] + 0.3, 1.15)) { useTap(); return; }   // W-1
    if (isMarketDay(G.day) && near(HAGGLER[0], HAGGLER[1], 1.3)) { if (G.market.haggled) toast('"We\'ve struck our deal for today. Come back next market day!"', 2.5); else go('haggle', 0, 0); return; }
    if (near(STORE_KEEPER[0], STORE_KEEPER[1] - 0.9, 1.4)) { panel = { kind: 'shop', tab: 'buy' }; return; }   // H-2: buy inside the corner store
    { const r = !roomAt(G.px, G.py) && ROOMS.find(r => near(r.entranceX, r.entranceY, 1.0)); if (r) { sfx(r.id === 'store' ? 'door_bell' : 'door_slide', 0.8); [G.px, G.py] = roomAnchor(r.id, 'arrive', [r.originX, r.originY]); return; } }
    if (Math.abs(G.px - 3.92) < 2 && G.py > 0.8 && G.py < 2.6) { toast("The corner store's stall. Step inside the store to buy and sell.", 2.5); return; }
  } else if (G.scene === 'hill') {
    if (near(0, -4.4, 1.4)) { go('farm', LADDER[0] - 0.6, LADDER[1] - 1.0); return; }
    if (near(5.1, -2.0, 1.5)) { if (S().benchDay === G.day) toast('You already watched the stars tonight.'); else { S().benchDay = G.day; G.log.rested = true; changeHeart(3, 'You sat on the bench and watched the stars.'); } }
  }
}
// ------------------------------------------------------------------ W-1: the village tap beside the hall
const TAP = [4.6, -1.0];
function useTap() {
  sfx('tap_water', 0.8); sfx('gulp', 0.7);
  G.thirst = 100; G.log.drank = true; report('drink'); const cap = waterCap(), w = S().season === 0;
  if (cap) { G.water = cap; toast(w ? `You unwrap the cloth, drink the icy tap water and fill your can. (${G.water}/${cap})` : `You drink from the village tap and fill your can. (${G.water}/${cap})`); }
  else toast(w ? 'You unwrap the cloth and drink the icy tap water.' : 'You drink cool water from the village tap.');
}
// ------------------------------------------------------------------ K-2: making sauces (agungi, meju rack, jangdokdae)
const JANG = { jars: 3, rack: 4, dry: 2, ferment: 4, salt: 2 };
function Jg() { if (!G.jang) G.jang = { hung: [], jars: [], lidsOpen: false, fireToday: false };
  while (G.jang.jars.length < JANG.jars) G.jang.jars.push({ start: -1, care: 0, rain: 0 }); return G.jang; }
const jarYield = (j, helper = 0) => { const b = (j.care >= 3) + (j.care >= 6) - (j.rain >= 2) + helper; return [Math.max(1, 2 + b), Math.max(1, 1 + b)]; };
const jarTaste = j => j.rain >= 2 ? "A little watery, but it's still yours." : j.care >= 6 ? 'Deep and rich. The sun is in it.' : j.care >= 3 ? 'A good, honest taste.' : "Mild. Next time I'll let it see more sun.";
function howTo(key, text) { if (unlock('howto_' + key)) showAnnounce(text, 8); }
function useAgungi() {
  const j = Jg();
  if (!j.fireToday) {
    if (!has('Wood')) { toast('You need a piece of firewood (Wood) to light the agungi.', 3); return; }
    remove('Wood', 1); j.fireToday = true; sfx('fire_whoosh', 0.45);
    toast('You light the agungi. The floor will be warm tonight. ([C] to cook here)', 4);
    howTo('agungi', '[#ffd84a]The agungi[/]\n\nThe kitchen fire also heats the floor of the room (ondol).\nLight it once a day with a piece of firewood, and you\'ll sleep warm.\n\nStanding here, press [#ffd84a][C][/] to cook without a campfire,\nand to boil soybeans into [#ffd84a]meju[/] (4 Soybeans + 1 Wood).');
    return;
  }
  panel = { kind: 'craft' };
}
function useMejuRack() {
  const j = Jg();
  const ready = j.hung.filter(d => G.day >= d).length; j.hung = j.hung.filter(d => G.day < d); if (ready) add('Dried Meju', ready);
  const have = G.inv['Fresh Meju'] || 0, hung = Math.min(have, JANG.rack - j.hung.length);
  for (let i = 0; i < hung; i++) j.hung.push(G.day + JANG.dry); if (hung) remove('Fresh Meju', hung);
  if (ready && hung) toast(`You took down ${ready} dried meju and hung ${hung} more.`, 3);
  else if (ready) toast(`You took down ${ready} dried meju. It smells nutty.`, 3);
  else if (hung) toast(`You hung ${hung} meju to dry. They'll be ready in ${JANG.dry} days.`, 3);
  else if (j.hung.length) toast(`The meju are still drying. (${j.hung.length} hanging)`, 2.5);
  else toast('A rack for drying meju. Boil soybeans into meju at the agungi first.', 3);
  if (hung) howTo('meju', '[#ffd84a]Drying meju[/]\n\nMeju dries slowly in the cold winter air.\nIn a few days, take the dried meju down and put it in a sauce jar\nat the jangdokdae with [#ffd84a]2 Salt[/].');
}
function useJangdok() {
  const j = Jg();
  const ready = j.jars.findIndex(x => x.start >= 0 && G.day - x.start >= JANG.ferment);
  if (ready >= 0) {
    const jar = j.jars[ready], [d, g] = jarYield(jar, unlocked('favor_jang') ? 1 : 0), taste = jarTaste(jar); j.jars[ready] = { start: -1, care: 0, rain: 0 };
    add('Doenjang', d); add('Ganjang', g); j.madeD = (j.madeD || 0) + d; j.madeG = (j.madeG || 0) + g; changeHeart(3, `You split your sauces: ${d} doenjang, ${g} ganjang. ${taste}`); return;
  }
  const empty = j.jars.findIndex(x => x.start < 0);
  if (empty >= 0 && has('Dried Meju') && has('Salt', JANG.salt)) {
    let filled = 0, e = empty;
    while (e >= 0 && has('Dried Meju') && has('Salt', JANG.salt)) { remove('Dried Meju', 1); remove('Salt', JANG.salt); j.jars[e] = { start: G.day, care: 0, rain: 0 }; filled++; e = j.jars.findIndex(x => x.start < 0); }
    toast(filled === 1 ? `You put the meju in a jar with salt water. In ${JANG.ferment} days it can be split into sauces.` : `You filled ${filled} jars with meju and salt water. In ${JANG.ferment} days they can be split into sauces.`, 4);
    howTo('jangdok', "[#ffd84a]Looking after the jars[/]\n\nOn sunny days, open the lids and let the sun in: the sauce gets deeper.\nWhen it rains or snows, close them, or rainwater gets in.\n(Press [#ffd84a][E][/] at the jangdokdae to open or close all the lids.)\nForgetting isn't a disaster. It just tastes a little milder.");
    return;
  }
  j.lidsOpen = !j.lidsOpen; sfx('jar_lid');
  const n = j.jars.filter(x => x.start >= 0).length, st = n ? `  (${n} jar(s) fermenting)` : '';
  if (j.lidsOpen) toast((G.raining || G.snowing ? 'You opened the lids... in this weather? Rain will get in.' : 'You opened the jar lids to let the sun in.') + st, 3);
  else toast('You closed the jar lids.' + st, 2.5);
  if (!n) howTo('jang', '[#ffd84a]The jangdokdae[/]\n\nThis is where the sauces of the house are made.\n1. Boil [#ffd84a]4 Soybeans[/] into meju at the agungi\n2. Hang it on the meju rack to dry for a few days\n3. Put [#ffd84a]1 Dried Meju + 2 Salt[/] in an empty jar here\n4. After a while, split it into doenjang and ganjang.\nSoybeans and salt are sold at the market.');
}
// the agungi and the meju rack stand side by side: [E] goes to the nearest one only
function jangSpot() {
  const c = [['agungi', AGUNGI[0], AGUNGI[1] + 0.4, 1.5], ['rack', MEJURACK[0], MEJURACK[1] + 0.4, 1.1], ['jars', JANGDOK[0], JANGDOK[1] - 0.3, 1.5]]
    .map(([n, x, y, r]) => [n, Math.hypot(G.px - x, G.py - y), r]).filter(([, d, r]) => d <= r).sort((a, b) => a[1] - b[1]);
  return c.length ? c[0][0] : null;
}
function jangPrompt() {
  const j = Jg(), s = jangSpot();
  if (s === 'agungi') return j.fireToday ? '[E] Cook at the agungi' : has('Wood') ? '[E] Light the agungi (1 Wood)' : 'Agungi (needs 1 Wood to light)';
  if (s === 'rack') { const r = j.hung.filter(d => G.day >= d).length;
    return r ? `[E] Take down ${r} dried meju` : has('Fresh Meju') ? '[E] Hang meju to dry' : j.hung.length ? `Meju drying (${j.hung.length})` : 'Meju rack'; }
  if (s === 'jars') {
    if (j.jars.some(x => x.start >= 0 && G.day - x.start >= JANG.ferment)) return '[E] Split the sauce';
    if (j.jars.some(x => x.start < 0) && has('Dried Meju') && has('Salt', JANG.salt)) return '[E] Put meju in the empty jars (1 Dried Meju + 2 Salt each)';
    return j.lidsOpen ? '[E] Close the jar lids' : '[E] Open the jar lids';
  }
  return null;
}
function useHeap() {
  if (G.compostReady > 0) { const n = G.compostReady; G.compostReady = 0; add('Compost', n); toast(`You dig out ${n} bag(s) of dark, crumbly compost.`, 3); return; }
  const item = selectedItem();
  if (ORGANIC.has(item)) { const n = keys.ShiftLeft || keys.ShiftRight ? G.inv[item] : 1; remove(item, n); G.compostPoints += n; toast(`Into the heap: ${item} x${n}. Every two become a bag of compost by morning. (In the heap: ${G.compostPoints})`, 3.5); return; }
  toast(G.compostPoints ? `The heap is warm and quietly working. (${G.compostPoints} inside, ready by morning)` : 'A compost heap. Hold a crop, fruit, greens or food and press E to add it. (Shift+E: all of it)', 3.5);
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
  const first = !S().fish[f]; add(f); onFishCaught();
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
  if (isMarketDay(G.day)) list.push(...FAIR_STOCK);
  if (!has('Mattock')) list.push(['Mattock', 150]);
  return list.filter(([n]) => { const d = ITEMS[n]; return !(d.cat === 'Seed' && d.crop && CROPS[d.crop].season > S().season); });
}
function canCraft(r) {
  const atAgungi = G.scene === 'farm' && near(AGUNGI[0], AGUNGI[1] + 0.4, 2.0);
  if (r.station && !has(r.station) && !(atAgungi && (r.station === 'Campfire' || r.station === 'Agungi'))) return r.station === 'Agungi' ? 'Stand at the agungi to make this.' : `Needs a ${r.station}.`;
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
    rows.slice(0, 18).forEach((r, i) => {   // six rows of three (W-1: more to buy)
      const cx = x + 24 + (i % 3) * 290, cy = y + 114 + Math.floor(i / 3) * 62;
      box(cx, cy, 276, 56, '#2a201a', '#8a6a4a');
      const im = IMG[iconKey(r.n)]; if (im) ctx.drawImage(im, cx + 8, cy + 6, 44, 44);
      drawRich(r.n, cx + 60, cy + 7, 18, 210); drawRich(`[#ffd84a]${r.label}[/]`, cx + 60, cy + 30, 18, 210);
      hit(cx, cy, 276, 56, r.fn);
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
    L.push(`[#ffd84a]Journal[/]   Day ${G.day}   [${SEASON_COLOR[s.season]}]${SEASONS[s.season]}[/]` + (s.season === 0 ? `  (spring comes after Jeongwol Daeboreum${Rt().boreumDay > 0 ? ' (day ' + Rt().boreumDay + ')' : ''})` : s.season < 3 ? `  (${SEASONS[s.season + 1]} in about ${Math.max(0, PACE.days[s.season] - seasonDays())} day(s))` : '  (the last season of the year)'));
    L.push(`[#ff8fb1]Heart[/] ${Math.round(G.heart)}/100   Gold ${G.gold}   Album ${s.photos.length}/24`);
    L.push(''); L.push('[#ffd84a]Villagers[/]');
    VILLAGERS.forEach(v => { const p = villagerProgress(v.id), n = v.chapters.length, d = Math.min(p.wish, n); L.push(`${v.name}  [${'#'.repeat(d)}${'-'.repeat(n - d)}]  ` + (!p.met ? '[#a0a0a0]not met yet[/]' : vRestored(v) ? '[#9dff9d]colors restored[/]' : 'Next: ' + vNext(v).hint + (vReady(v) ? '' : ' (give it a day or two)'))); });
    if (sp.found) L.push(`\n[#ff9fc4]Spirit[/]  ${FORM_NAMES[spiritForm(sp)]}   [#ffd84a]Sun ${sp.pts[0]}[/]  [#9dff9d]Earth ${sp.pts[1]}[/]  [#8fd0ff]Water ${sp.pts[2]}[/]  [#ff9fc4]Bloom ${sp.pts[3]}[/]   forms ${sp.forms.length}/10`);
    L.push(`\n[#ffb6d0]Kindness[/] ${s.kindness}/60  ` + (unlocked('starhill') ? `Star Hill open on clear nights. Stars caught: ${s.starsTotal}` : `(sharing box today ${S().shareDay === G.day ? S().sharedToday : 0}/5)`));
    if (s.forest.open) L.push(`[#9dff9d]Forest[/]  clearings restored ${forestRestored()}/3   ${s.forest.treeAwake ? 'The old zelkova is awake.' : 'The old zelkova is asleep.'}`);
    if (s.hill.open) L.push(`[#ffb070]Sunset Hill[/]  orchard trees revived ${hillRevived()}/6   on the drying rack: ${s.hill.drying.length}`);
    if (s.lake.open) L.push(`[#8fd0ff]Lake[/]  clear parts ${lakeZones()}/3   fish released ${s.lake.released}` + (lakeClear() ? '   Fireflies come out at night.' : `   ${LAKE_THRESH.find(t => s.lake.clarity < t) - s.lake.clarity} more to clear the next part`));
    L.push(`\n[#8fb8ff]Fish caught[/]  ` + ['Minnow', 'Carp', 'Golden Koi', 'Bluegill', 'Moonfin', 'Glassfish'].map(f => s.fish[f] ? `${f} x${s.fish[f]}` : '???').join('   '));
    drawRich(L.join('\n'), x + 30, y + 24, 20, 840);
    drawRich('[J] or [Esc] Close', x + w - 24, y + h - 40, 18, 400, 'right', '#c8c0b0');
  } else if (panel.kind === 'demoEnd') { drawDemoEnd();
  } else if (panel.kind === 'choice') {
    ctx.fillStyle = 'rgba(0,0,0,0.45)'; ctx.fillRect(0, 0, CW, CH);
    const bw = 760, bh = 420, bx = (CW - bw) / 2, by = (CH - bh) / 2; box(bx, by, bw, bh);
    drawRich(`[#ffd84a]${panel.title}[/]`, CW / 2, by + 28, 22, 700, 'center');
    panel.options.forEach((o, i) => { const oy = by + 100 + i * 68; box(bx + 50, oy, bw - 100, 56, 'rgba(32,24,18,0.95)', '#735638');
      drawRich(`[#ffd84a][${i + 1}][/]  ${o}`, bx + 72, oy + 16, 20, bw - 140); hit(bx + 50, oy, bw - 100, 56, () => { const pk = panel.pick; panel = null; pk(i); }); });
  } else if (panel.kind === 'view') {
    ctx.fillStyle = 'rgba(10,8,12,0.94)'; ctx.fillRect(0, 0, CW, CH);
    const vw = 1024, vh = 576, vx = (CW - vw) / 2, vy = 14;
    ctx.fillStyle = '#c89552'; ctx.fillRect(vx - 4, vy - 4, vw + 8, vh + 8);
    ctx.imageSmoothingEnabled = false;
    for (const key of panel.layers) { const im = IMG['World/' + key]; if (im) ctx.drawImage(im, vx, vy, vw, vh); }
    drawRich(panel.caption, CW / 2, vy + vh + 18, 18, 1100, 'center');
    drawRich('[E] or [Esc] Close', CW / 2, vy + vh + 44, 15, 600, 'center', '#c8c0b0');
  } else if (panel.kind === 'map') {
    ctx.fillStyle = 'rgba(10,8,12,0.94)'; ctx.fillRect(0, 0, CW, CH);
    const vw = 1024, vh = 576, vx = (CW - vw) / 2, vy = 14, k = vw / VMAP.width;
    ctx.fillStyle = '#c89552'; ctx.fillRect(vx - 4, vy - 4, vw + 8, vh + 8);
    ctx.imageSmoothingEnabled = false;
    const fogged = VMAP_IDS.filter(id => !vmapOpen(id));
    for (const key of ['vmap', ...fogged.map(id => 'vmap_fog_' + id)]) { const im = IMG['World/' + key]; if (im) ctx.drawImage(im, vx, vy, vw, vh); }
    for (const r of VMAP.regions) {
      const open = vmapOpen(r.id), who = open ? vmapWho(r.id) : '';
      const col = open ? '#3d291a' : '#605c62';
      ctx.font = '17px ' + FONT; const tw = Math.max(ctx.measureText(VMAP_NAME[r.id]).width, (ctx.font = '13px ' + FONT, ctx.measureText(vmapStatus(r.id)).width), who ? (ctx.font = '12px ' + FONT, ctx.measureText(who).width) : 0);
      ctx.fillStyle = 'rgba(242,232,204,0.82)'; ctx.fillRect(vx + r.labelX * k - tw / 2 - 8, vy + r.labelY * k - 3, tw + 16, who ? 54 : 40);
      drawRich(VMAP_NAME[r.id], vx + r.labelX * k, vy + r.labelY * k, 17, 260, 'center', col);
      drawRich(vmapStatus(r.id), vx + r.labelX * k, vy + r.labelY * k + 20, 13, 260, 'center', col);
      if (who) drawRich(who, vx + r.labelX * k, vy + r.labelY * k + 37, 12, 260, 'center', col);
    }
    const here = VMAP.regions.find(r => r.id === vmapHere()), pin = IMG['World/vmap_pin'];
    if (here && pin) { const bob = Math.round(Math.abs(Math.sin(performance.now() / 333)) * 3) * 2; ctx.drawImage(pin, vx + here.pinX * k - pin.width, vy + here.pinY * k - pin.height * 2 - bob, pin.width * 2, pin.height * 2); }
    const fogText = fogged.length ? `Fog still hangs over ${fogged.length} place(s). Take care of yourself and it will lift.` : 'The fog is gone. The whole valley is yours to walk.';
    drawRich(`[#ffd84a]Village Map[/]   ${fogText}`, CW / 2, vy + vh + 18, 18, 1100, 'center');
    drawRich('[M] or [Esc] Close', CW / 2, vy + vh + 44, 15, 600, 'center', '#c8c0b0');
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
  if (jwibul) { jwibul.t += dt; if (jwibul.t >= 2.6) { const d = jwibul.done; jwibul = null; d(); } }   // K-4: jwibul ring
  frame++;
  if (screen) { updateScreen(dt); clearPressed(); return; }
  updateBroadcast(dt, pressed.Space || pressed.Enter);
  { const st = guideStep(); if (st === 3) { unlock('guide_done'); toast('Your first day went well. From here, live as you like.  [M] map  [J] journal', 7); } }
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
  if (panel && panel.kind === 'demoEnd') {
    if (panel.page < 3 && (pressed.KeyE || pressed.Space || pressed.Enter)) panel.page++;
    else if (panel.page >= 3 && pressed.Digit1) window.open(STORE.url, '_blank');
    else if (panel.page >= 3 && pressed.Digit2) { panel = null; screen = { kind: 'title', sel: 0, confirm: false }; }
    clearPressed(); return;
  }
  if (!panel && demoEndPending && !announce && !dialogue) { demoEndPending = false; panel = { kind: 'demoEnd', page: 0 }; }
  if (panel && panel.kind === 'choice') {
    for (let i = 0; i < panel.options.length; i++) if (pressed['Digit' + (i + 1)]) { const pk = panel.pick; panel = null; pk(i); break; }
    clearPressed(); return;
  }
  if (panel && panel.kind === 'view' && panel.onClose && (pressed.Escape || pressed.KeyE || pressed.Space)) { const cb = panel.onClose; panel = null; clearPressed(); cb(); return; }
  if (panel) {
    if (pressed.Escape || (panel.kind === 'view' && (pressed.KeyE || pressed.Space)) || (panel.kind === 'shop' && pressed.KeyE) || (panel.kind === 'craft' && pressed.KeyC) || (panel.kind === 'journal' && pressed.KeyJ) || (panel.kind === 'album' && pressed.KeyP) || (panel.kind === 'map' && pressed.KeyM)) panel = null;
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
        if (d <= 0.1 * 0.35) { bonus = 0.5; text = 'PERFECT! Today\'s sell prices +50%'; } else if (d <= 0.1) { bonus = 0.25; text = 'Deal! Today\'s sell prices +25%'; } else text = 'Haggle failed... Try again next market day.';
        G.market.haggled = true; G.market.haggle = Math.max(G.market.haggle, bonus); if (bonus) { changeHeart(1, 'A friendly haggle with the old farmer.'); report('haggle'); }
        haggle = { done: true, text, t: 2.2 };
      }
    } else { haggle.t -= dt; if (haggle.t <= 0) go('market', HAGGLER[0], HAGGLER[1] - 1.1); }
    clearPressed(); return;
  }

  // time + needs
  const hours = dt * 3 * speed / 60;   // K-5: 3 game minutes per second (a day ~6 min)
  G.minute += dt * 3 * speed; while (G.minute >= 60) { G.minute -= 60; G.hour++; }
  if (G.hour >= 24 && !G.warnedMid) { G.warnedMid = true; toast("It's midnight. You should go to bed soon."); }
  if (G.hour >= 26) { endDay(true); go('home', -2.9, 0); clearPressed(); return; }   // H-1: wake beside the bedding
  G.hunger = Math.max(0, G.hunger - 5 * hours); G.thirst = Math.max(0, G.thirst - 5 * hours);   // W-1: 7 → 5
  if (G.hunger <= 0 || G.thirst <= 0) { G.log.starved = true; G.health -= 15 * hours; if (G.health <= 0) { G.health = 0; G.dead = true; showAnnounce('[#ff8080]You collapsed...[/]\n\nTake better care of yourself.\n\nPress "새로 시작" below to try again.', 9999); } }
  else if (G.hunger > 50 && G.thirst > 50) G.health = Math.min(100, G.health + 2 * hours);
  if (G.hunger < 20 && !G.warnedH) { G.warnedH = true; toast("You're getting hungry. (Right-click / F to eat)"); } if (G.hunger > 30) G.warnedH = false;
  if (G.thirst < 20 && !G.warnedT) { G.warnedT = true; toast(has('Homemade Barley Tea') || has('Barley Tea') ? "You're thirsty. Drink your barley tea. (Right-click / F)" : G.scene === 'market' ? "You're thirsty. Drink at the tap beside the village hall (E)." : "You're thirsty. Drink at the well (E)."); } if (G.thirst > 30) G.warnedT = false;

  // movement
  let mx = (keys.KeyD || keys.ArrowRight ? 1 : 0) - (keys.KeyA || keys.ArrowLeft ? 1 : 0);
  let my = (keys.KeyW || keys.ArrowUp ? 1 : 0) - (keys.KeyS || keys.ArrowDown ? 1 : 0);
  if (act) { act.t -= dt; if (act.t <= 0) act = null; }
  for (const f of fx) { f.t += dt; if (!f.pop) { f.vy -= f.g * dt; f.x += f.vx * dt; f.y += f.vy * dt; } } fx = fx.filter(f => f.t < f.life);
  if (wavePending && !announce && !dialogue && !panel && !screen) {
    const big = wavePending === 'big'; wavePending = null;
    const grow = big ? 2.0 : 1.3, hold = big ? 0.6 : 0.3, fade = big ? 1.0 : 0.7;
    const s = S().season, cols = s === 2 ? ['#5aa648', '#93d66a'] : s === 3 ? ['#ec8a2c', '#dc3a3e', '#f0cf48'] : ['#ffb6d0', '#ffe0ea'];
    wave = { t: 0, big, grow, hold, fade, dur: grow + hold + fade, sparks: Array.from({ length: big ? 14 : 9 }, (_, i) => [i / (big ? 14 : 9) * Math.PI * 2, Math.random() * 6]),
             leaves: big ? Array.from({ length: 16 }, (_, i) => { const a = Math.random() * Math.PI * 2, sp = 70 + Math.random() * 100; return { a, d: 25 + Math.random() * 40, vx: Math.cos(a) * sp, vy: (-Math.sin(a) + 0.3) * sp, col: cols[i % cols.length] }; }) : [] };
  }
  if (wave) { wave.t += dt; if (wave.t >= wave.dur) wave = null; }
  const moving = (mx || my) && !act;
  if (moving) {
    const l = Math.hypot(mx, my); let sp = 5 * (G.hunger <= 0 || G.thirst <= 0 ? 0.6 : 1);
    const ox = G.px, oy = G.py;
    const tiles = G.scene === 'forest' || G.scene === 'lake' || G.scene === 'sunset' || G.scene === 'farm';
    G.px += mx / l * sp * dt; if (tiles && feetBlocked()) G.px = ox;   // trees, water, the old tree, logs, reeds
    G.py += my / l * sp * dt; if (tiles && feetBlocked()) G.py = oy;
    if (Math.abs(mx) > Math.abs(my)) { G.dir = 'side'; G.facingLeft = mx < 0; } else G.dir = my > 0 ? 'up' : 'down';
    G.stepT += dt; if (G.stepT > 0.125) { G.stepT = 0; G.step = (G.step + 1) % 4; if (G.step % 2 === 0) sfx(G.scene === 'home' || roomAt(G.px, G.py) ? 'step_floor' : S().season === 0 ? 'step_snow' : 'step_dirt', 0.5, 0.15); }
  } else G.step = 0;
  clampPlayer();

  // portals
  if (G.scene === 'farm') {
    const open = MAP.exits.find(e => !e.locked && inRect(exitRect(e), G.px, G.py));
    if (near(DOOR[0], DOOR[1], 0.7)) go('home', 2.77, -1.4);
    else if (open) go(open.to.toLowerCase(), open.spawnX, open.spawnY);          // the end of the road is the way to the market
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
  } else if (G.scene === 'home') { if (G.py < -1.95 && G.px > 2.4 && G.px < 3.15) go('farm', ...arrival('Home')); }   // H-1: out the sliding door
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
  if (pressed.KeyM) panel = { kind: 'map' };
  for (let i = 1; i <= 8; i++) if (pressed['Digit' + i]) { const it = hotbarItems(); if (hotOffset + i - 1 < it.length) selectIndex(hotOffset + i - 1); }
  clearPressed();
}
function feetBlocked() {
  const blocked = G.scene === 'lake' ? lakeBlocked : G.scene === 'sunset' ? hillBlocked : G.scene === 'farm' ? farmBlocked : forestBlocked;
  for (const [dx, dy] of [[-0.2, -0.5], [0.2, -0.5], [-0.2, -0.28], [0.2, -0.28]]) if (blocked(Math.floor(G.px + dx), Math.floor(G.py + dy))) return true;
  return false;
}
function clampPlayer() {
  const s = sc(); let minX = s.cx - s.ww / 2 + 0.4, maxX = s.cx + s.ww / 2 - 0.4, minY = s.cy - s.wh / 2 + 0.6, maxY = s.cy + s.wh / 2 - 0.9;
  if (G.scene === 'farm') { minX = MAPB.x0 + 0.3; maxX = MAPB.x1 - 0.3; minY = MAPB.y0 + 0.5; maxY = Math.abs(G.px + 0.5) > 1.2 ? MAPB.y1 - 0.8 : MAPB.y1 - 0.35; }
  if (G.scene === 'forest') { minX = FORB.x0 + 0.3; maxX = FORB.x1 - 0.3; minY = FORB.y0 + 0.5; maxY = FORB.y1 - 0.8; }
  if (G.scene === 'lake') { minX = LAKB.x0 + 0.3; maxX = LAKB.x1 - 0.3; minY = LAKB.y0 + 0.5; maxY = LAKB.y1 - 0.8; }
  if (G.scene === 'sunset') { minX = HILB.x0 + 0.3; maxX = HILB.x1 - 0.3; minY = HILB.y0 + 0.5; maxY = HILB.y1 - 0.8; }
  if (G.scene === 'market') maxY = 2.4;
  if (G.scene === 'home') { minX = -4.1; maxX = 4.1; maxY = 1.7; minY = G.px > 2.45 && G.px < 3.1 ? -2.1 : -1.55; }   // H-1: the ondol room, the door in the front wall
  if (G.scene === 'hill') { minX = -8.4; maxX = 8.4; minY = -4.6; maxY = -1.2; }
  const room = roomAt(G.px, G.py);
  if (room) {   // H-2: inside: the room's walls, the door in the front wall
    const [lx, back] = roomW(room, 6, room.wallH), [rx, front] = roomW(room, room.w - 6, room.h - room.frontH);
    const d0 = roomW(room, room.doorX0, 0)[0], d1 = roomW(room, room.doorX1 + 1, 0)[0], inDoor = G.px > d0 + 0.15 && G.px < d1 - 0.15;
    minX = lx + 0.3; maxX = rx - 0.3; maxY = back + 0.4; minY = inDoor ? front - 0.1 : front + 0.45;
    if (inDoor && G.py - 0.4 < front - 0.3) { G.px = room.exitX; G.py = room.exitY; return; }   // out the door, back to the lane
  }
  G.px = Math.max(minX, Math.min(maxX, G.px)); G.py = Math.max(minY, Math.min(maxY, G.py));
  if (room) for (const f of room.furniture) {   // furniture: push the feet out of each rect
    const [x0, y1] = roomW(room, f.x0, f.y0), [x1, y0] = roomW(room, f.x1, f.y1), fy = G.py - 0.4;
    if (G.px > x0 - 0.15 && G.px < x1 + 0.15 && fy > y0 - 0.1 && fy < y1 + 0.1) {
      const d = [[G.px - (x0 - 0.15), 'l'], [(x1 + 0.15) - G.px, 'r'], [fy - (y0 - 0.1), 'd'], [(y1 + 0.1) - fy, 'u']].sort((a, b) => a[0] - b[0])[0];
      if (d[1] === 'l') G.px = x0 - 0.15; else if (d[1] === 'r') G.px = x1 + 0.15; else if (d[1] === 'd') G.py = y0 - 0.1 + 0.4; else G.py = y1 + 0.1 + 0.4;
    }
  }
  const solids = [];
  if (G.scene === 'home') { for (const x of [-4.0, -3.6]) solids.push([x, 1.25, 0.32]); for (const x of [3.6, 4.0]) solids.push([x, 1.3, 0.32]); for (const x of [2.1, 2.6]) solids.push([x, 1.12, 0.3]); solids.push([0.2, -0.4, 0.3]); }
  if (G.scene === 'farm') {
    solids.push([WELL[0], WELL[1] - 0.45, 0.55]);
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
  if (G.scene === 'market' && isMarketDay(G.day)) { FAIR_STALLS.forEach(([x, y, r]) => { for (let dx = -r + 0.25; dx <= r - 0.25; dx += 0.5) solids.push([x + dx, y + 0.3, 0.3]); }); solids.push([HAGGLER[0], HAGGLER[1] - 0.4, 0.3]); }
  if (G.scene === 'market') { solids.push([-4.2, -2.35, 0.35]); for (const dx of [-0.45, 0, 0.45]) solids.push([TAP[0] + 0.05 + dx, TAP[1] + 0.2, 0.3]);
    for (let x = 5.6; x <= 8.2; x += 0.5) for (const y of [-0.9, -1.5, -2.1, -2.55]) solids.push([x, y, 0.45]);   // the village hall (K-3)
    for (const ch of CHORES) if (ch.scene === 'market') solids.push([ch.x, ch.y + 0.15, 0.4]); }
  if (!G.sitting) for (const v of VILLAGERS) if (v.scene === G.scene && !sittingOut(v)) solids.push([v.x, v.y - 0.4, 0.25]);
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
// ------------------------------------------------------------------ S-1/S-2: sound (same rules as SoundRules.cs)
const SND = { ctx: null, buf: {}, on: true, music: null, musicId: undefined, amb: {}, next: 20, roosterDay: -1, test: null };
function sndInit() {
  if (SND.ctx) return;
  try {
    const C = window.AudioContext || window.webkitAudioContext; SND.ctx = new C();
    SND.master = SND.ctx.createGain(); SND.master.connect(SND.ctx.destination);
    SND.mus = SND.ctx.createGain(); SND.mus.gain.value = 0.7; SND.mus.connect(SND.master);
    SND.fx = SND.ctx.createGain(); SND.fx.gain.value = 0.8; SND.fx.connect(SND.master);
    for (const a of ['amb_wind_winter', 'amb_night', 'amb_fire']) sndLoop(a).then(n => { SND.amb[a] = n; });
  } catch (e) { SND.ctx = null; }
}
function sndBuf(name) {
  if (!SND.ctx || !AUDIO[name]) return Promise.resolve(null);
  if (!SND.buf[name]) {
    const bin = atob(AUDIO[name].split(',')[1]), arr = new Uint8Array(bin.length);
    for (let i = 0; i < bin.length; i++) arr[i] = bin.charCodeAt(i);
    SND.buf[name] = new Promise(res => SND.ctx.decodeAudioData(arr.buffer, res, () => res(null)));
  }
  return SND.buf[name];
}
function sfx(name, vol = 1, jitter = 0.06) {
  if (!SND.ctx || !SND.on) return;
  sndBuf(name).then(b => { if (!b) return; const s = SND.ctx.createBufferSource(), g = SND.ctx.createGain(); s.buffer = b; s.playbackRate.value = 1 + (Math.random() - 0.5) * 2 * jitter; g.gain.value = vol; s.connect(g).connect(SND.fx); s.start(); });
}
async function sndLoop(name, out = null, when = 0) {
  const b = await sndBuf(name); if (!b) return null;
  const s = SND.ctx.createBufferSource(), g = SND.ctx.createGain(); s.buffer = b; s.loop = true; g.gain.value = 0;
  s.connect(g).connect(out || SND.fx); s.start(when || SND.ctx.currentTime); return { s, g };
}
const smoothstep = x => { x = Math.max(0, Math.min(1, x)); return x * x * (3 - 2 * x); };
function musicPiece(hour) {
  if (panel && panel.kind === 'demoEnd') return 'spring';
  if (screen && (screen.kind === 'title' || screen.kind === 'opening')) return 'title';
  if (G.scene === 'farm' && S().season === 0 && gathering() && !Rt().boreumDone) return 'boreum';
  if (hour >= 19 || hour < 6) return null;
  if (G.scene === 'market') return 'market';
  if (G.scene === 'farm' || G.scene === 'home') return 'farm';
  return null;
}
const LAYERS = { farm: 3, market: 2 };
function musicLevel(id, hour, indoors) {
  if (!id) return 0; if (id === 'title' || id === 'spring' || id === 'boreum') return 1;
  const h = hour % 24; let k = h >= 18 && h < 19 ? 1 - (h - 18) : h >= 6 && h < 7 ? h - 6 : 1; return indoors ? k * 0.55 : k;
}
function layerVol(id, i, color, mday) {
  if (i === 0) return 1;
  if (id === 'farm') return i === 1 ? smoothstep((color - 0.18) / 0.14) : smoothstep((color - 0.36) / 0.12);
  if (id === 'market') return mday ? 1 : 0;
  return 0;
}
async function startPiece(id) {
  const n = LAYERS[id] || 1, names = n > 1 ? [...Array(n).keys()].map(i => `mus_${id}_${i}`) : [`mus_${id}`];
  const bufs = await Promise.all(names.map(sndBuf)); if (SND.musicId !== id) return;
  const at = SND.ctx.currentTime + 0.05, layers = [];
  for (const b of bufs) { if (!b) { layers.push(null); continue; } const s = SND.ctx.createBufferSource(), g = SND.ctx.createGain(); s.buffer = b; s.loop = true; g.gain.value = 0; s.connect(g).connect(SND.mus); s.start(at); layers.push({ s, g }); }
  SND.music = { id, layers };
}
function stopPiece(m) { if (!m) return; const t0 = SND.ctx.currentTime; for (const l of m.layers) if (l) { l.g.gain.setTargetAtTime(0, t0, 0.6); l.s.stop(t0 + 3); } }
function soundTick(dt) {
  if (!SND.ctx) return;
  const hour = G.hour + G.minute / 60, indoors = G.scene === 'home' || !!roomAt(G.px, G.py), outdoors = !!sc().outdoor && !indoors;
  const want = SND.test ? SND.test : musicPiece(hour);
  if (want !== SND.musicId) { SND.musicId = want; stopPiece(SND.music); SND.music = null; if (want) startPiece(want); }
  const t0 = SND.ctx.currentTime, color = color01(G.heart), mday = isMarketDay(G.day);
  if (SND.music) SND.music.layers.forEach((l, i) => { if (!l) return; const v = SND.on ? (SND.test ? 1 : musicLevel(SND.music.id, hour, indoors) * layerVol(SND.music.id, i, color, mday)) : 0; l.g.gain.setTargetAtTime(v, t0, 0.8); });
  // outdoor sounds
  const quiet = !SND.on || !!screen || (panel && panel.kind === 'demoEnd') || G.scene === 'hill' || !!SND.test, winter = S().season === 0, h = hour % 24, night = h >= 19 || h < 5;
  const wind = quiet || !winter || night ? 0 : outdoors ? (G.snowing ? 0.55 : 0.32) : indoors ? 0.08 : 0;
  const nightV = quiet || !night ? 0 : outdoors ? 0.45 : indoors ? 0.1 : 0;
  let fire = 0;
  if (!quiet && outdoors && G.scene === 'farm') {
    for (const l of nightLights()) if (l.flicker) { const r = l.r * 2.2, d = Math.hypot(G.px - l.x, G.py - l.y); fire = Math.max(fire, Math.max(0, 1 - d / r) * 0.7); }
  }
  for (const [k, v] of [['amb_wind_winter', wind], ['amb_night', nightV], ['amb_fire', fire]]) if (SND.amb[k]) SND.amb[k].g.gain.setTargetAtTime(v, t0, 1.2);
  if (quiet) return;
  if (G.day !== SND.roosterDay && hour >= 6.05 && hour < 7) { SND.roosterDay = G.day; sfx('rooster', indoors ? 0.5 : 0.85, 0.03); }
  SND.next -= dt;
  if (SND.next <= 0) { SND.next = 25 + Math.random() * 35; const r = Math.random();
    if (outdoors && h >= 6 && h < 10 && r < 0.6) sfx('magpie', 0.8, 0.05); else if (outdoors && night && r < 0.5) sfx('dog_far', 0.8, 0.05); }
}
// ------------------------------------------------------------------ N-1: night (same as NightCurve.cs + NightLight.cs)
const NIGHT_A = 0.58;
const NIGHT_KEYS = [
  [0, .06, .08, .20, NIGHT_A], [4, .06, .08, .20, NIGHT_A], [5, .20, .24, .46, .38], [6, .95, .72, .58, .10], [6.75, 1, .85, .70, 0],
  [16.5, 1, .62, .30, 0], [17.5, 1, .55, .25, .11], [18.5, .86, .38, .30, .19], [19.25, .34, .28, .50, .31], [20.25, .12, .14, .34, .46],
  [21, .06, .08, .20, NIGHT_A], [24, .06, .08, .20, NIGHT_A]];
const smooth01 = t => { t = Math.max(0, Math.min(1, t)); return t * t * (3 - 2 * t); };
function nightness(hour) { const h = ((hour % 24) + 24) % 24; if (h >= 20.5 || h < 4.5) return 1; if (h >= 18) return smooth01((h - 18) / 2.5); if (h >= 6.5) return 0; return 1 - smooth01((h - 4.5) / 2); }
function nightSample(hour, weather, moon) {
  const h = ((hour % 24) + 24) % 24; let i = 0; while (i < NIGHT_KEYS.length - 2 && h >= NIGHT_KEYS[i + 1][0]) i++;
  const A = NIGHT_KEYS[i], B = NIGHT_KEYS[i + 1], t = smooth01((h - A[0]) / (B[0] - A[0]));
  let [r, g, b, a] = [1, 2, 3, 4].map(k => A[k] + (B[k] - A[k]) * t); const n = nightness(hour);
  if (weather) a *= 1 + 0.12 * n;
  if (moon) { a *= 1 - 0.08 * n; r += (0.10 - r) * n; g += (0.15 - g) * n; b += (0.34 - b) * n; }
  return [r, g, b, Math.min(0.68, a)];
}
const nightBand = l => l <= 0 ? 0 : l >= 1 ? 1 : Math.round(l * 6) / 6;
const WINDOW_C = [1, .74, .40], LAMP_C = [1, .90, .62], FIRE_C = [1, .56, .22];
function poleLamps(add, head, ground) {
  add(head[0], head[1], 0.6, 0.95, LAMP_C);
  add(head[0] + (ground[0] - head[0]) * 0.62, head[1] + (ground[1] - head[1]) * 0.62, 2.0, 0.45, LAMP_C);
  add(ground[0], ground[1] + 0.3, 1.5, 0.32, LAMP_C);
}
function nightLights() {
  const L = [], add = (x, y, r, i, col, flicker = 0, seed = 0) => L.push({ x, y, r, i, col, flicker, seed });
  if (G.scene === 'farm') {
    add(DOOR[0] + 0.03, DOOR[1] + 1.30, 1.25, 0.85, WINDOW_C); add(DOOR[0] - 1.32, DOOR[1] + 1.36, 0.8, 0.7, WINDOW_C);
    add(DOOR[0] + 1.36, DOOR[1] + 1.36, 0.8, 0.7, WINDOW_C); add(DOOR[0], DOOR[1] - 0.1, 2.0, 0.32, WINDOW_C);
    MAP.rows.forEach((row, r) => [...row].forEach((ch, c) => { if (ch === '|') poleLamps(add,
      [MAP.originX + (c * 32 + 10) / 32, MAP.originY - (r * 32 - 38) / 32], [MAP.originX + (c * 32 + 10) / 32, MAP.originY - (r * 32 + 30) / 32]); }));
    if (Jg().fireToday) add(AGUNGI[0], AGUNGI[1] + 0.3, 1.7, 0.95, FIRE_C, 0.12, 1);
    const r = Rt(), burning = r.boreumDone ? (S().season === 0 && G.hour >= 19 && G.day <= r.boreumDay + 1) : r.lit;
    if (burning) add(DALJIP[0], DALJIP[1] + 1.1, 4.2, 1.25, FIRE_C, 0.10, 2);
  }
  if (G.scene === 'market') {
    add(-6.07, 2.70, 1.3, 0.72, WINDOW_C); add(-1.39, 2.90, 1.0, 0.66, WINDOW_C); add(7.51, 2.90, 1.15, 0.74, WINDOW_C); add(7.20, -2.27, 1.15, 0.74, WINDOW_C);
    add(-6.07, 1.55, 1.5, 0.28, WINDOW_C); add(-1.39, 1.75, 1.3, 0.26, WINDOW_C); add(7.51, 1.75, 1.4, 0.28, WINDOW_C); add(7.20, -3.35, 1.4, 0.28, WINDOW_C);
    poleLamps(add, [-4.13, 3.83], [-4.13, 1.70]);
  }
  if (jwibul) { const a = jwibul.t * 9; add(G.px + Math.cos(a) * 0.75, G.py + 0.1 + Math.sin(a) * 0.75, 1.3, Math.min(1, (2.6 - jwibul.t) * 2), FIRE_C, 0.15, 3); }
  return L;
}
const NIGHTCV = document.createElement('canvas');
let NIGHT_DIM = 0;   // how dark the outdoors is right now (snow and rain dim with it)
function drawNight(c) {
  NIGHT_DIM = 0;
  const hour = G.hour + G.minute / 60, night = nightness(hour), s = sc();
  const indoors = G.scene === 'home' || !!roomAt(G.px, G.py);
  if (indoors) { if (night > 0) { c.fillStyle = `rgba(255,184,107,${(0.10 * night).toFixed(3)})`; c.fillRect(0, 0, CW, CH); } return; }
  if (!s.outdoor) return;
  const r0 = Rt(), moon = r0.boreumDay > 0 && G.day === r0.boreumDay && G.hour >= 17;
  const [nr, ng, nb, a] = nightSample(hour, G.raining || G.snowing, moon);
  NIGHT_DIM = a;
  if (a <= 0.003) return;
  const T = 16, step = 1 / T, W = Math.ceil(s.ww * T) + 2, H = Math.ceil(s.wh * T) + 2;
  const ox = Math.floor((s.cx - s.ww / 2) / step) * step - step, oy = Math.floor((s.cy - s.wh / 2) / step) * step - step;
  const light = new Float32Array(W * H), tw = new Float32Array(W * H), wr = new Float32Array(W * H), wg = new Float32Array(W * H), wb = new Float32Array(W * H);
  const splat = (x, y, rad, inten, col, tint) => {
    if (inten <= 0) return;
    const x0 = Math.max(0, Math.floor((x - rad - ox) / step)), x1 = Math.min(W - 1, Math.ceil((x + rad - ox) / step));
    const y0 = Math.max(0, Math.floor((y - rad - oy) / step)), y1 = Math.min(H - 1, Math.ceil((y + rad - oy) / step));
    for (let j = y0; j <= y1; j++) { const dy = (oy + (j + 0.5) * step - y) / rad;
      for (let i = x0; i <= x1; i++) { const dx = (ox + (i + 0.5) * step - x) / rad, t = dx * dx + dy * dy; if (t >= 1) continue;
        const v = (1 - t) * (1 - t) * inten, k = (H - 1 - j) * W + i; light[k] += v;   // image rows run top-down
        const q = v * tint; tw[k] += q; wr[k] += q * col[0]; wg[k] += q * col[1]; wb[k] += q * col[2]; } }
  };
  splat(G.px, G.py + 0.35, 1.9, 0.34, [1, 1, 1], 0);   // a faint light around the player
  const now = performance.now() / 1000;
  for (const l of nightLights()) {
    const f = l.flicker ? 1 + l.flicker * (Math.sin(now * 7.3 + l.seed * 5) * 0.6 + Math.sin(now * 12.9 + l.seed) * 0.4) : 1;
    splat(l.x, l.y, l.r * f, l.i * night, l.col, 1);
  }
  NIGHTCV.width = W; NIGHTCV.height = H; const nc = NIGHTCV.getContext('2d'), img = nc.createImageData(W, H), d = img.data;
  for (let k = 0; k < W * H; k++) {
    const l = nightBand(Math.min(1, light[k])); let warm = 0, r = 0, g = 0, b = 0;
    if (tw[k] > 0) { r = wr[k] / tw[k]; g = wg[k] / tw[k]; b = wb[k] / tw[k]; warm = nightBand(Math.min(1, tw[k])) * 0.48 * night; }
    const dark = a * (1 - l), oa = dark + warm; if (oa <= 0.0001) continue;
    d[k * 4] = (nr * dark + r * warm) / oa * 255; d[k * 4 + 1] = (ng * dark + g * warm) / oa * 255; d[k * 4 + 2] = (nb * dark + b * warm) / oa * 255; d[k * 4 + 3] = Math.min(1, oa) * 255;
  }
  nc.putImageData(img, 0, 0);
  const [sx, sy] = toScreen(ox, oy + H * step);
  c.imageSmoothingEnabled = false; c.drawImage(NIGHTCV, sx, sy, W * step * ux(), H * step * uy());
}
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
  } else {
    const bg = IMG[sc().bg()] || IMG['World/bg_farm']; if (bg) c.drawImage(bg, 0, 0, CW, CH);
    if (G.scene === 'market' && !roomAt(G.px, G.py) && isMarketDay(G.day) && IMG['World/market_fair']) c.drawImage(IMG['World/market_fair'], 0, 0, CW, CH);   // market-day stalls
  }
  const draws = [];  // [sortY, fn]
  if (G.scene === 'farm') {
    for (const k in G.tiles) { const [x, y] = k.split(',').map(Number); const key = G.tiles[k].wet ? 'Tiles/tilled_wet' : 'Tiles/tilled';
      const [sx, sy] = toScreen(x, y + 1); if (sx < -64 || sy < -64 || sx > CW || sy > CH) continue; c.drawImage(IMG[key], Math.round(sx), Math.round(sy), 64, 64); }
    for (const p of PLOTS) {                     // hardened plots (gray crust), resting plots (straw on empty ridges)
      const open = plotOpen(p), rest = open && plotRested(p); if (open && !rest) continue;
      for (let x = p.x0; x <= p.x1; x++) for (let y = p.y0; y <= p.y1; y++) {
        if (rest && cropAt(x, y)) continue;
        const key = open ? 'World/plot_straw_' + (((x * 5 + y * 11) % 3 + 3) % 3) : 'World/plot_hard_' + (((x * 7 + y * 13) % 3 + 3) % 3) + (G.story.season === 0 ? '_winter' : '');
        const [sx, sy] = toScreen(x, y + 1); if (sx < -64 || sy < -64 || sx > CW || sy > CH) continue; c.drawImage(IMG[key], Math.round(sx), Math.round(sy), 64, 64);
      }
    }
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
      // gray fog past a lane that isn't open yet; once it opens, the fog lifts the first time you come near
      if (!REGIONS[e.to]) continue;
      const key = 'fog_lifted_' + e.to.toLowerCase(), open = areaOpen(e.to);
      if (open && unlocked(key) && !G.fogLift?.[e.to]) continue;
      const cx = (x0 + x1) / 2, cy = (y0 + y1) / 2, mx = (MAPB.x0 + MAPB.x1) / 2, my = (MAPB.y0 + MAPB.y1) / 2;
      const d = Math.hypot(cx - mx, cy - my) || 1, ox = cx + (cx - mx) / d * 0.8, oy = cy + (cy - my) / d * 0.8;
      let lift = 0;
      if (open) {
        G.fogLift = G.fogLift || {};
        if (G.fogLift[e.to] === undefined && Math.hypot(G.px - ox, G.py - oy) < 9) { G.fogLift[e.to] = 0; unlock(key); }
        if (G.fogLift[e.to] !== undefined) { G.fogLift[e.to] += 1 / 60; lift = G.fogLift[e.to] / 3.5; if (lift >= 1) { delete G.fogLift[e.to]; continue; } }
      }
      const alpha = Math.round((1 - lift) * 4) / 4, t = performance.now() / 1000;
      [[-1.3, 0.4], [1.0, -0.3], [0.1, 1.1]].forEach(([fx, fy], i) => {
        const px = Math.max(MAPB.x0 + 1.2, Math.min(MAPB.x1 - 1.2, ox + fx)) + Math.round(Math.sin(t * 0.25 + i * 1.9) * 0.35 * 32) / 32;
        const py = Math.max(MAPB.y0 + 0.6, Math.min(MAPB.y1 - 0.6, oy + fy)) + lift * (1.2 + i * 0.4);
        draws.push([-999, () => sprite(c, i % 2 ? 'World/fog_bank_2' : 'World/fog_bank', px, py, 1, 'center', false, alpha)]);
      });
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
    draws.push([WELL[1] - 0.6, () => sprite(c, G.story.season === 0 ? 'World/well_winter' : 'World/well', WELL[0], WELL[1])]);
    draws.push([CHEST[1] - 0.4, () => sprite(c, G.story.season === 0 ? 'World/storage_box_winter' : 'World/storage_box', CHEST[0], CHEST[1])]);
    draws.push([COMPOST[1] - 0.5, () => sprite(c, 'World/compost_heap', COMPOST[0], COMPOST[1] - 0.5, 1, 'bottom')]);
    { const j = Jg(), w = G.story.season === 0 ? '_winter' : '';
      draws.push([AGUNGI[1], () => sprite(c, j.fireToday ? 'World/agungi_fire' : 'World/agungi', AGUNGI[0], AGUNGI[1], 1, 'bottom')]);
      const rk = !j.hung.length ? 'meju_rack' : j.hung.some(d => G.day >= d) ? 'meju_rack_dried' : 'meju_rack_fresh';
      draws.push([MEJURACK[1], () => sprite(c, 'World/' + rk + w, MEJURACK[0], MEJURACK[1], 1, 'bottom')]);
      if (j.lidsOpen) draws.push([-998, () => sprite(c, 'World/jangdok_open', JANGDOK[0], JANGDOK[1], 1, 'bottom')]); }
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
  if (G.scene === 'farm' && unlocked('ending_seen')) draws.push([COTTAGE[1], () => sprite(c, seasonal('cottage_color'), COTTAGE[0], COTTAGE[1], 1, 'bottom')]);
  if (G.scene === 'farm') { const tg = guideTarget(guideStep()); if (tg) { const bob = Math.round(Math.abs(Math.sin(performance.now() / 333)) * 4) / 32; draws.push([-1000, () => sprite(c, 'World/guide_arrow', tg[0], tg[1] + bob)]); } }
  // H-1: the bedding, the wardrobe and the table are part of the room picture
  if (G.scene === 'market') {
    draws.push([STORE_KEEPER[1], () => sprite(c, 'World/merchant', STORE_KEEPER[0], STORE_KEEPER[1])]);   // H-2: the auntie behind her counter, inside
    if (isMarketDay(G.day)) draws.push([HAGGLER[1], () => sprite(c, 'World/haggler', HAGGLER[0], HAGGLER[1])]);
    draws.push([-2.5, () => sprite(c, 'World/donation_box', -4.2, -2.0)]);
    draws.push([0, () => sprite(c, G.story.season === 0 ? 'World/exit_sign_winter' : 'World/exit_sign', -8.27, -0.05)]);
  }
  if (G.scene === 'hill') for (const s of G.stars) draws.push([s.y, () => sprite(c, 'Items/star_fragment', s.x, s.y, 0.5 * (1 + Math.sin(performance.now() / 160 + s.x) * 0.1))]);
  if (G.scene === 'market' && dongjiWindow() && !Rt().dongjiDone) draws.push([POT_AT[1], () => sprite(c, 'World/rite_pot', POT_AT[0], POT_AT[1], 1, 'bottom')]);
  if (G.scene === 'farm') {
    const r = Rt();
    if (r.dongjiDelivered.includes('gate') && S().season === 0) draws.push([-997, () => sprite(c, 'World/rite_redbeans', GATE_AT[0], GATE_AT[1], 1, 'bottom')]);
    let dk = null; const fire = Math.floor(performance.now() / 160) % 2 ? 'World/daljip_fire_1' : 'World/daljip_fire_0';
    if (r.boreumDone) dk = S().season === 0 ? (G.hour >= 19 && G.day <= r.boreumDay + 1 ? fire : 'World/daljip_ash') : null;
    else if (r.lit) dk = fire;
    else if (boreumSched() && G.day >= r.boreumDay - RITE.notice) dk = daljipBuilt() ? 'World/daljip_built' : 'World/daljip_frame';
    if (dk) draws.push([DALJIP[1], () => sprite(c, dk, DALJIP[0], DALJIP[1], 1, 'bottom')]);
    if (gathering() && !r.boreumDone) [['rosa', -1.5, -0.2], ['theo', 1.5, -0.1], ['mayor', -0.9, -0.95], ['mina', 0.95, -0.95]].forEach(([id, dx, dy]) =>
      draws.push([DALJIP[1] + dy, () => sprite(c, vFace(id), DALJIP[0] + dx, DALJIP[1] + dy + 0.45)]));
  }
  for (const v of VILLAGERS) if (v.scene === G.scene && !(v.id === 'mina' && gathering())) {
    const [vx, vy] = vPos(v);
    draws.push([vy - 0.45, () => {
      const k = 1 + Math.sin((v.pulse || 0) * Math.PI) * 0.15; sprite(c, villagerSprite(v), vx, vy, k);
      if (hasBubble(v) && !dialogue) sprite(c, 'World/bubble', vx + 0.19, vy + 0.62 + Math.sin(performance.now() / 250) * 0.03);
    }]);
  }
  for (const ch of CHORES) if (ch.scene === G.scene) {
    const w = VILLAGERS.find(x => x.id === ch.who), nc = vNext(w), working = villagerProgress(w.id).chore > 0 && nc?.spot === ch.id;
    if (ch.id === 'sled' && G.story.season !== 0 && !working) continue;
    draws.push([ch.y, () => sprite(c, `World/chore_${ch.id}${working ? '_on' : ''}`, ch.x, ch.y, 1, 'bottom')]);
  }
  if (G.scene !== 'haggle') {
    const frameIdx = G.step === 1 ? 1 : G.step === 3 ? 2 : 0;
    if (jwibul) for (let i = 0; i < 10; i++) { const a = jwibul.t * 9 - i * 0.22; draws.push([-900, () => sprite(c, 'World/jwibul_spark', G.px + Math.cos(a) * 0.75, G.py + 0.1 + Math.sin(a) * 0.75, 1, 'center', false, Math.max(0, 1 - i / 10) * Math.min(1, (2.6 - jwibul.t) * 2))]); }
    const pkey = G.sitting ? 'Player/player_sit' : act ? `Player/player_${act.kind}_${G.dir}_${act.t > 0.2 ? 0 : 1}` : `Player/player_${G.dir}_${frameIdx}`;
    draws.push([G.py - 0.45, () => sprite(c, pkey, G.px, G.py, 1, 'center', G.dir === 'side' && G.facingLeft)]);
    for (const f of fx) draws.push([-900, () => {
      const a = Math.round(Math.max(0, 1 - f.t / f.life) * 4) / 4;
      if (f.pop) { const h = 0.9 * (1 - Math.pow(1 - Math.min(1, f.t / f.life * 1.6), 2)); sprite(c, f.pop, f.x, f.y + Math.round(h * 32) / 32, 0.75, 'center', false, f.t / f.life < 0.6 ? 1 : a); return; }
      const [sx, sy] = toScreen(f.x, f.y), k = Math.max(1, Math.round(uy() / 32)) * 2;
      c.globalAlpha = a; c.fillStyle = f.col; c.fillRect(Math.round(sx), Math.round(sy), k, k); c.globalAlpha = 1;
    }]);
    if (G.spirit) draws.push([G.spirit.y - 0.3, () => sprite(c, 'Spirit/' + spiritForm(S().spirit), G.spirit.x, G.spirit.y, 1 + Math.sin(G.spirit.pulse * Math.PI) * 0.25, 'center', G.spirit.vx < -0.05)]);
  }
  draws.sort((a, b) => b[0] - a[0]).forEach(d => d[1]());
  // night
  if (G.scene !== 'haggle' && G.scene !== 'hill') drawNight(c);   // N-1: evening and night light
  if (G.scene === 'lake') drawFireflies(c);

  // seasons of the heart: the world's saturation follows Heart (UI layer is not affected)
  let sat = 1 + (-80 + 95 * color01(G.heart)) / 100;   // F-0: (heart - 10) / 90

  worldCanvas.style.filter = `saturate(${sat.toFixed(2)})`;
}

let particles = [];
function renderUI() {
  hits = [];
  ctx.clearRect(0, 0, CW, CH); ctx.textBaseline = 'top';
  if (screen) { drawScreen(); return; }
  if (wave) drawWave();
  // weather
  if (sc().outdoor && (G.raining || G.snowing)) {
    if (particles.length !== 80 || particles.snow !== G.snowing) { particles = Array.from({ length: 80 }, () => ({ x: Math.random() * CW, y: Math.random() * CH, s: Math.random() })); particles.snow = G.snowing; }
    for (const p of particles) {
      const sh = 1 - NIGHT_DIM * 0.85, cs = (r, g, b, al) => `rgba(${Math.round(r * sh)},${Math.round(g * sh)},${Math.round(b * (0.15 + 0.85 * sh))},${al})`;
      if (G.snowing) { p.y += 0.8 + p.s; p.x += Math.sin(performance.now() / 600 + p.s * 9) * 0.6; ctx.fillStyle = cs(255, 255, 255, 0.85); ctx.fillRect(p.x, p.y, p.s > 0.7 ? 6 : 4, p.s > 0.7 ? 6 : 4); }
      else { p.y += 12 + p.s * 4; p.x -= 1.5; ctx.fillStyle = cs(190, 215, 255, 0.55); ctx.fillRect(p.x, p.y, 2, 16); }
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
  { const st = guideStep(); if (st >= 0 && st < 3) {                       // first-day guide card
    // stack from the top so a wrapped task line never runs into the hint (the card grows to fit)
    const tl = richLines(GUIDE[st][0], 18, 450, '#f6efe2').length, hl = richLines(GUIDE[st][1], 13, 450, '#c8c0b0').length;
    const taskY = 118, hintY = taskY + Math.round(tl * 18 * 1.3) + 4, cardH = hintY - 90 + Math.round(hl * 13 * 1.3) + 10;
    box(12, 90, 470, cardH, 'rgba(20,14,11,0.86)', '#c89552');
    drawRich(`[#ffd84a]Today[/]  [#c8c0b0]${st + 1}/3[/]`, 24, 98, 15, 440);
    drawRich(GUIDE[st][0], 24, taskY, 18, 450);
    drawRich(GUIDE[st][1], 24, hintY, 13, 450, 'left', '#c8c0b0');
  } }
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
    ctx.globalAlpha = Math.min(1, t.t / 0.5); const y = 96 + i * 42 + (broadcast && !announce && !dialogue && !panel && (G.scene === 'farm' || G.scene === 'market') ? 40 : 0);
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
  drawBroadcast();
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
    { const rp = ritePrompt(); if (rp && !VILLAGERS.some(v => v.scene === G.scene && near(...vPos(v), 1.0))) return rp;
      const v = VILLAGERS.find(v => v.scene === G.scene && near(...vPos(v), 1.0)); if (v) return sittingOut(v) && S().masilDay !== G.day ? `[E] Sit with ${v.name} (masil)` : `[E] Talk to ${v.name}`;
      const ch = choreNear(); if (ch) { const w = VILLAGERS.find(x => x.id === ch.who), c = vNext(w); return c?.kind === 'Pumasi' && c.spot === ch.id && villagerProgress(w.id).invited && vReady(w) ? `[E] Help out (${villagerProgress(w.id).chore}/${c.steps.length})` : ({ steamer: 'Rice-cake steamer', forge: 'Forge and bellows', briquettes: 'Coal briquettes', sled: 'Ice sled' })[ch.id]; } }
    if (near(3.68, -0.4, 1.2)) return '[E] Drink and refill your watering can';
    if (starVineOpen() && near(LADDER[0], LADDER[1] - 1.0, 1.2)) return '[E] Climb the glowing vine';
    if (near(POND[0], POND[1], 1.9)) return fishing ? (fishing.state === 'bite' ? '[#ffd84a][E] NOW![/]' : 'Waiting for a bite...') : (has('Fishing Rod') ? '[E] Fish' : 'The stream is full of fish');
    if (near(DOOR[0], DOOR[1], 1.6)) return 'Home';
    const jp = jangPrompt(); if (jp) return jp;
    if (near(COMPOST[0], COMPOST[1], 1.4)) return G.compostReady ? `[E] Take ${G.compostReady} bag(s) of compost` : ORGANIC.has(selectedItem()) ? `[E] Put ${selectedItem()} in the heap  (Shift+E: all)` : `Compost heap (${G.compostPoints} inside)`;
    if (G.px < MAPB.x0 + 2.2 && Math.abs(G.py + 9.1) < 1.4) return F().open ? '← To the forest' : 'Gray thorns block the forest path';
    if (G.px > MAPB.x1 - 2.4 && Math.abs(G.py + 10.6) < 1.4) return Lk().open ? 'To the lake →' : 'Tall gray reeds block the path to the lake';
    if (G.px > MAPB.x1 - 2.2 && Math.abs(G.py + 4.5) < 1.3) return 'To the market →';
    if (debrisAt(Math.floor(G.px), Math.floor(G.py - 0.4)) === 'g' || ['k', 'o'].some(ch => [[1, 0], [-1, 0], [0, 1], [0, -1]].some(([dx, dy]) => debrisAt(Math.floor(G.px) + dx, Math.floor(G.py - 0.4) + dy) === ch))) {
      const it = ITEMS[selectedItem()] || {}; return it.tool === 'hoe' || it.tool === 'scythe' ? 'Click to clear the old field' : 'The old field: clear it with a hoe or scythe';
    }
  } else if (G.scene === 'home') { if (onBedding()) return '[E] Lie down on the bedding and end the day'; if (G.py < -1.0 && G.px > 2.1 && G.px < 3.4) return 'Out to the porch';
  } else if (G.scene === 'market') {
    { const rp = ritePrompt(); if (rp && !VILLAGERS.some(v => v.scene === G.scene && near(...vPos(v), 1.0))) return rp;
      const v = VILLAGERS.find(v => v.scene === G.scene && near(...vPos(v), 1.0)); if (v) return sittingOut(v) && S().masilDay !== G.day ? `[E] Sit with ${v.name} (masil)` : `[E] Talk to ${v.name}`;
      const ch = choreNear(); if (ch) { const w = VILLAGERS.find(x => x.id === ch.who), c = vNext(w); return c?.kind === 'Pumasi' && c.spot === ch.id && villagerProgress(w.id).invited && vReady(w) ? `[E] Help out (${villagerProgress(w.id).chore}/${c.steps.length})` : ({ steamer: 'Rice-cake steamer', forge: 'Forge and bellows', briquettes: 'Coal briquettes', sled: 'Ice sled' })[ch.id]; } }
    if (near(-4.2, -2.0, 1.1)) return '[E] Share the food you hold (Shift+E: more)';
    if (!roomAt(G.px, G.py) && near(TAP[0], TAP[1] + 0.3, 1.15)) return '[E] Drink at the village tap and fill your can';   // W-1
    if (isMarketDay(G.day) && near(HAGGLER[0], HAGGLER[1], 1.3)) return G.market.haggled ? 'The old farmer is done haggling for today' : '[E] Haggle with the old farmer';
    if (near(STORE_KEEPER[0], STORE_KEEPER[1] - 0.9, 1.4)) return '[E] Buy and sell';
    { const r = !roomAt(G.px, G.py) && ROOMS.find(r => near(r.entranceX, r.entranceY, 1.0)); if (r) return `[E] Go into ${r.nameEn}`; }
    { const r = roomAt(G.px, G.py); if (r) { const d0 = roomW(r, r.doorX0, 0)[0], d1 = roomW(r, r.doorX1, 0)[0], front = roomW(r, 0, r.h - r.frontH)[1]; if (G.px > d0 - 0.4 && G.px < d1 + 0.4 && G.py < front + 1.0) return 'Out to the lane'; } }
    if (G.px < -7) return 'Back to the farm';
  } else if (G.scene === 'sunset') {
    if (near(HRACK[0], HRACK[1], 1.5)) { const h = Hl(); return h.drying.some(d => G.day >= d) ? '[E] Take down the dried persimmons' : has('Persimmon') ? '[E] Hang your persimmons' : h.drying.length ? 'The persimmons are drying' : 'A drying rack for persimmons'; }
    if (near(HBENCH[0], HBENCH[1], 1.6)) return '[E] Sit in the pavilion and look out over the valley';
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
    if (near(FTREE[0], FTREE[1] + 0.6, 2.2)) return '[E] Look at the old zelkova';
    const i = FSPOTS.findIndex(([x, y]) => near(x + 0.5, y + 0.5, 1.6));
    if (i >= 0) { const st = F().sapling[i]; return st < 0 ? (has('Acorn') ? 'Click the pebble ring with the Acorn to plant it' : 'An empty spot. Acorns turn up on the forest floor.') : st >= 3 ? 'This clearing is full of color' : F().watered[i] ? `Sapling ${st}/3: watered today` : `Sapling ${st}/3: water it today`; }
    if (G.px > FORB.x1 - 2) return 'Back to the farm →';
  } else if (G.scene === 'hill') { if (near(0, -4.4, 1.4)) return '[E] Climb back down'; if (near(5.1, -2.0, 1.5)) return '[E] Sit and watch the stars'; }
  return '';
}

// ------------------------------------------------------------------ save
const SAVE_KEY = 'mlf-preview-v2';
function save() { try { localStorage.setItem(SAVE_KEY, JSON.stringify(G)); } catch (e) { /* storage unavailable: preview still works */ } }
function load() { try { const s = localStorage.getItem(SAVE_KEY); if (s) { G = JSON.parse(s); migrateNames(); return true; } } catch (e) { /* ignore */ } return false; }
function migrateNames() {   // old saves: Bread -> Rice Cake and so on (same rule as ItemDB.Normalize)
  for (const [o, n] of Object.entries(ALIASES)) if (G.inv[o]) { G.inv[n] = (G.inv[n] || 0) + G.inv[o]; delete G.inv[o]; }
  if (ALIASES[G.selected]) G.selected = ALIASES[G.selected];
}

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
  [['Carrot', 5], ['Strawberry', 4], ['Corn', 3], ['Apple', 4], ['Minnow', 2], ['Carp', 1], ['Sunshine Shard', 2], ["Earth's Breath", 2], ['Star Fragment', 3], ['Wood', 12], ['Stone', 16], ['Campfire', 1], ['Rice Cake', 2], ['Potato Seed', 4], ['Strawberry Seed', 4], ['Corn Seed', 4]].forEach(([n, c]) => add(n, c));
  toast('Preview: items added to your bag.'); uiCanvas.focus();
};
$('t-market').onclick = () => {   // skip ahead to the next market day and walk into the market
  if (G.dead) return;
  let guard = 0; while (!isMarketDay(G.day) && guard++ < 6) endDay(false);
  announce = null; go('market', -7.6, -3.6); uiCanvas.focus();
};
$('t-hill').onclick = () => { S().kindness = Math.max(S().kindness, 60); unlock('starhill'); G.raining = G.snowing = false; if (G.hour < 20) { G.hour = 20; G.minute = 30; } toast('Preview: Star Hill is open. The vine glows at the meadow edge at night.'); uiCanvas.focus(); };
$('t-forest').onclick = () => {
  if (G.heart < 30) changeHeart(30 - G.heart, 'Preview tool');
  if (S().season < 1) { S().season = 1; S().highest = Math.max(S().highest, 1); G.story.seasonStart = G.day; }
  forestCheckOpen(); add('Acorn', 3); if (!has('Mattock')) add('Mattock');
  toast('Preview: spring, the forest is open. +3 Acorn, Mattock. The flower arch is at the west end of the farm lane.', 5); uiCanvas.focus();
};
$('t-hill2').onclick = () => {
  if (G.heart < 85) changeHeart(85 - G.heart, 'Preview tool');
  if (S().season < 3) { S().season = 3; S().highest = 3; unlock('season_autumn'); G.story.seasonStart = G.day; }
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
// the sound board (listen to every sound and piece of music)
{ const sb = $('soundboard'); if (sb) {
  const btn = (label, fn) => { const b = document.createElement('button'); b.type = 'button'; b.textContent = label; b.onclick = () => { sndInit(); fn(b); }; sb.appendChild(b); return b; };
  btn('소리 끄기', b => { SND.on = !SND.on; b.textContent = SND.on ? '소리 끄기' : '소리 켜기'; b.classList.toggle('on', !SND.on); uiCanvas.focus(); });
  const tracks = [['타이틀', 'title'], ['농장 (마음 따라 3겹)', 'farm'], ['장터 (장날 장구)', 'market'], ['대보름 풍물', 'boreum'], ['봄 (데모 끝)', 'spring']];
  for (const [label, id] of tracks) btn('♪ ' + label, b => { SND.test = SND.test === id ? null : id; document.querySelectorAll('#soundboard button.on').forEach(x => { if (x !== b && x.textContent.startsWith('♪')) x.classList.remove('on'); }); b.classList.toggle('on', SND.test === id); });
  const fxs = $('sfxboard');
  if (fxs) for (const k of Object.keys(AUDIO).filter(k => !k.startsWith('mus_') && !k.startsWith('amb_') && k !== 'step')) { const b = document.createElement('button'); b.type = 'button'; b.textContent = k; b.onclick = () => { sndInit(); sfx(k, 1, 0); }; fxs.appendChild(b); }
} }
$('t-night').onclick = () => { G.hour = Math.max(G.hour, 20); G.minute = 30; toast('Preview: it is night now.'); uiCanvas.focus(); };
$('t-speed').onclick = e => { speed = speed === 1 ? 4 : 1; e.currentTarget.classList.toggle('on', speed === 4); e.currentTarget.textContent = speed === 4 ? '시간 1배속' : '시간 4배속'; uiCanvas.focus(); };
$('t-new').onclick = () => { newGame(); toasts = []; dialogue = null; panel = null; announce = null; G.spirit = null; save(); startOpening(); uiCanvas.focus(); };

function introAnnounce() {
  showAnnounce('[#ff8fb1]A new life begins[/]\n\n"Day after day in the gray city felt endless. So I left it all behind and set out for a quiet village. I wonder... can I build my happiness again?"\n\nThe world is gray. It is winter in your heart.\nEat, drink, rest and go to bed on time: spring will come.\n\n[#c8c0b0]Till the dirt (Hoe) → plant (Carrot Seed) → water → sleep. Walk into trees and rocks to gather.\nEast of the yard, the vegetable plots wait. Only one is soft enough to farm; the others have gone gray and hard. A compost heap can bring them back.[/]', 14);
}

// ------------------------------------------------------------------ title screen + opening + first-day guide (D-1)
let screen = null;   // { kind: 'title', sel, confirm } | { kind: 'opening', i, shown, fade }
const OPENING = [
  ['bg_city', 'Overtime, then more overtime. One day the whole world started to look gray.'],
  ['op_bus', 'I quit, packed one suitcase, and took the last bus out to the countryside.'],
  ['op_arrive', 'An old house no one had lived in for years. Gray, just like me.'],
  ['op_arrive_color', 'I decided to take care of it, and of myself, a little at a time.'],
];
function titleItems() {
  if (screen.confirm) return [['Cancel', () => { screen.confirm = false; screen.sel = 0; }], ['Start over', () => { newGame(); toasts = []; dialogue = null; panel = null; announce = null; G.spirit = null; save(); startOpening(); }]];
  const list = [];
  if (resumed) list.push([`Continue  [#c8c0b0]${SEASONS[G.story.season]}, day ${G.day}[/]`, () => { screen = null; toast(`Welcome back. Day ${G.day}.`); }]);
  list.push(resumed ? ['New Game', () => { screen.confirm = true; screen.sel = 0; }] : ['Start', () => startOpening()]);
  return list;
}
function startOpening() { screen = { kind: 'opening', i: 0, shown: 0, fade: 0.4 }; }
function updateScreen(dt) {
  if (screen.kind === 'title') {
    const items = titleItems();
    if (pressed.ArrowUp || pressed.KeyW) screen.sel = (screen.sel + items.length - 1) % items.length;
    if (pressed.ArrowDown || pressed.KeyS) screen.sel = (screen.sel + 1) % items.length;
    if (pressed.Enter || pressed.Space || pressed.KeyE) items[screen.sel][1]();
    else if (screen.confirm && pressed.Escape) { screen.confirm = false; screen.sel = 0; }
  } else {
    const line = OPENING[screen.i][1];
    if (screen.fade > 0) screen.fade -= dt; else screen.shown += dt;
    if (pressed.Escape) { screen = null; return; }
    if (pressed.Enter || pressed.Space || pressed.KeyE) {
      if (screen.shown * 28 < line.length) screen.shown = line.length / 28 + 0.01;
      else if (++screen.i >= OPENING.length) screen = null; else { screen.shown = 0; screen.fade = 0.4; }
    }
  }
}
function drawScreen() {
  ctx.fillStyle = '#000'; ctx.fillRect(0, 0, CW, CH); ctx.imageSmoothingEnabled = false;
  hit(0, 0, CW, CH, () => {});
  if (screen.kind === 'title') {
    const bg = IMG['World/title_bg']; if (bg) ctx.drawImage(bg, 0, 0, CW, CH);
    drawRich('My Little Farmstead', CW / 2 + 3, 62 + 3, 58, 1000, 'center', '#1a100a');
    drawRich('[#ffd84a]My Little Farmstead[/]', CW / 2, 62, 58, 1000, 'center');
    ctx.fillStyle = 'rgba(20,12,8,0.55)'; ctx.fillRect(CW / 2 - 300, 140, 600, 30);
    drawRich('A tired heart, a quiet village, and the colors slowly coming back.', CW / 2, 145, 17, 700, 'center');
    if (screen.confirm) drawRich('[#ffd84a]Start over? Your current save will be erased.[/]', CW / 2, CH - 250, 18, 800, 'center');
    titleItems().forEach(([text, act], i) => {
      const w = 312, h = 40, x = CW / 2 - w / 2, y = CH - 210 + i * 50, on = i === screen.sel;
      box(x, y, w, h, 'rgba(20,14,11,0.96)', on ? '#ffd84a' : '#c89552');
      drawRich(on ? `[#ffd84a]${text.replace(/\[#c8c0b0\]|\[\/\]/g, '')}[/]` : text, CW / 2, y + 10, 20, 300, 'center');
      hit(x, y, w, h, () => { screen.sel = i; act(); });
    });
    drawRich('Up/Down choose   Enter select   (mouse works too)', CW / 2, CH - 26, 13, 800, 'center', '#c8c0b0');
  } else {
    const [key, line] = OPENING[screen.i], im = IMG['World/' + key];
    ctx.fillStyle = '#c89552'; ctx.fillRect(CW / 2 - 434, 20, 868, 494);
    if (im) ctx.drawImage(im, CW / 2 - 430, 24, 860, 486);
    box(CW / 2 - 434, 530, 868, 70, 'rgba(20,14,11,0.96)', '#c89552');
    drawRich(line.slice(0, Math.floor(screen.shown * 28)), CW / 2, 555, 21, 840, 'center');
    drawRich('[Space] next   [Esc] skip', CW - 170, CH - 34, 13, 300, 'center', '#c8c0b0');
    if (screen.fade > 0) { ctx.fillStyle = `rgba(0,0,0,${Math.round(Math.min(1, screen.fade / 0.4) * 4) / 4})`; ctx.fillRect(0, 0, CW, CH); }
    hit(0, 0, CW, CH, () => { pressed.Space = true; });
  }
}
// the world's real colour spreading out from you: the world canvas is drawn again without its saturation filter,
// clipped to a growing circle (same as ColorWave.cs, which uses a second camera without post-processing)
function drawWave() {
  const [px, py] = toScreen(G.px, G.py);
  const maxR = Math.max(...[[0, 0], [CW, 0], [0, CH], [CW, CH]].map(([x, y]) => Math.hypot(x - px, y - py))) * 1.08;
  const g = Math.min(1, wave.t / wave.grow), r = maxR * (1 - (1 - g) * (1 - g));
  const after = wave.t - wave.grow - wave.hold, a = after > 0 ? Math.round((1 - Math.min(1, after / wave.fade)) * 4) / 4 : 1;
  ctx.save(); ctx.globalAlpha = a; ctx.imageSmoothingEnabled = false;
  ctx.beginPath(); ctx.arc(px, py, Math.max(1, r - 10), 0, Math.PI * 2); ctx.clip();
  ctx.drawImage(worldCanvas, 0, 0, CW, CH);
  ctx.restore();
  // dithered edge: a band of 4px blocks, fewer towards the outside
  if (g < 1 && r > 12) {
    ctx.save(); ctx.globalAlpha = a; const B = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]];
    for (let ring = 0; ring < 3; ring++) {
      const rr = r - 10 + ring * 4 + 2, n = Math.max(8, Math.floor(2 * Math.PI * rr / 4)), keep = 12 - ring * 4;
      for (let i = 0; i < n; i++) { const an = i / n * Math.PI * 2, x = Math.round((px + Math.cos(an) * rr) / 4) * 4, y = Math.round((py + Math.sin(an) * rr) / 4) * 4;
        if (B[(y / 4) & 3][(x / 4) & 3] < keep) ctx.drawImage(worldCanvas, x * worldCanvas.width / CW, y * worldCanvas.height / CH, 4 * worldCanvas.width / CW, 4 * worldCanvas.height / CH, x, y, 4, 4); }
    }
    ctx.restore();
  }
  // a few warm sparkles riding the edge, and (new season) a handful of petals or leaves
  const edgeA = g < 1 ? 1 : Math.max(0, 1 - (wave.t - wave.grow) / 0.4);
  wave.sparks.forEach(([ang, ph]) => {
    const an = ang + wave.t * 0.25, x = Math.round((px + Math.cos(an) * r * 0.97) / 2) * 2, y = Math.round((py - Math.sin(an) * r * 0.97) / 2) * 2;
    ctx.globalAlpha = Math.round(edgeA * (Math.sin(wave.t * 10 + ph) > 0.2 ? 1 : 0.5) * 4) / 4; ctx.fillStyle = '#fff6d8';
    ctx.fillRect(x - 6, y - 1, 12, 2); ctx.fillRect(x - 1, y - 6, 2, 12);
  });
  wave.leaves.forEach(l => { ctx.globalAlpha = a; ctx.fillStyle = l.col; const x = px + Math.cos(l.a) * l.d + l.vx * wave.t, y = py - Math.sin(l.a) * l.d + l.vy * wave.t; ctx.fillRect(Math.round(x / 2) * 2, Math.round(y / 2) * 2, 8, 5); });
  ctx.globalAlpha = 1;
}
function guideStep() {
  if (!unlocked('guide_on') || unlocked('guide_done')) return -1;
  if (G.day >= 2) return 3;
  const crops = Object.values(G.crops);
  if (!crops.length) return 0;
  if (!crops.some(c => c.watered)) return 1;
  return 2;
}
const GUIDE = [
  ['Till the plot right of the house and plant carrot seeds', '[1] Hoe, left click on the plot  ·  then [4] Carrot Seed, left click'],
  ['Water the seeds you planted', '[2] Watering Can, left click the seeds  ·  out of water? [E] at the well'],
  ['When it gets dark, go inside and sleep', 'Through the house door, [E] at the bed (from 18:00)'],
];
function guideTarget(step) {
  if (step === 0 || step === 1) { const p = (MAP.plots || []).find(p => p.startOpen); return p ? [(p.x0 + p.x1 + 1) / 2, p.y1 + 1.5] : null; }
  if (step === 2) return [DOOR[0], DOOR[1] + 1.6];
  return null;
}

// ------------------------------------------------------------------ K-1: diary, village broadcast, Village Head Park
function pickLine(day, opts) { return opts[((day % opts.length) + opts.length) % opts.length]; }
function diaryLines(f, day) {
  const lines = [];
  if (f.passedOut) lines.push(pickLine(day, ['I passed out in the field. Someone carried me home.', 'Everything went dark. I woke up at home, not knowing how.']));
  else if (f.delta >= 6) lines.push(pickLine(day, ['Today was a pretty good day.', 'A good day. I want to remember it.', 'The day went by gently.']));
  else if (f.delta >= 0) lines.push(pickLine(day, ["An ordinary day. That's fine too.", 'A quiet day.', "Nothing special happened. I'm still here."]));
  else lines.push(pickLine(day, ['A hard day.', 'Today felt heavy.', "I didn't take care of myself today."]));
  const notes = [];
  if (f.riteNote) notes.push(f.riteNote);
  if (f.wishLine) notes.push(f.wishLine);
  if (f.starved) notes.push(pickLine(day, ['I skipped meals again.', 'My stomach growled all afternoon.']));
  if (f.exhausted && !f.passedOut) notes.push(pickLine(day, ['I worked until I had nothing left. Like back in the city.', 'I pushed too hard again.']));
  if (f.lateNight) notes.push(pickLine(day, ['I stayed up far too late.', 'Midnight came and went.']));
  if (f.masil) notes.push(pickLine(day, ["I sat out with a neighbor in the evening. We didn't need to say much.", 'I heard someone\'s story tonight. I think they needed to tell it.']));
  if (f.pumasi) notes.push(pickLine(day, ["I helped a neighbor with their work. Tired body, light heart.", 'Working side by side, the time flew.']));
  if (f.saecham) notes.push(pickLine(day, ['We shared a meal in the middle of the day.', 'Food tastes better eaten together.']));
  if (f.talked > 0 && !f.masil && !f.saecham) notes.push(pickLine(day, ['I talked with the neighbors for a while.', 'Someone smiled at me today.']));
  if (f.rested) notes.push(pickLine(day, ['I sat down for a while and just watched. The wind was nice.', 'I let myself rest for a bit.']));
  if (f.warm) notes.push(pickLine(day, ['I lit the agungi. The floor was warm under me all night.', 'The warm floor melted something in me.']));
  if (f.harvested > 0) notes.push(pickLine(day, ['I picked something I grew with my own hands.', 'The harvest was small, but it was mine.']));
  if (f.meals >= 2) notes.push(pickLine(day, ['I ate properly, twice.', 'Warm food, eaten slowly.'])); else if (f.meals === 1) notes.push(pickLine(day, ['I ate a proper meal.', 'I remembered to eat.']));
  if (f.onTime && !f.passedOut) notes.push(pickLine(day, ['Going to bed early tonight.', 'Bed before midnight, for once.']));
  if (f.drank && notes.length < 2) notes.push(pickLine(day, ['The well water was cold and sweet.', 'I drank plenty of water.']));
  for (const n of notes) if (lines.length < 3) lines.push(n);
  return lines;
}
function diaryMood(d) { return d >= 6 ? 'My heart feels much lighter.' : d > 0 ? 'My heart feels a little lighter.' : d === 0 ? 'My heart is the same as yesterday.' : d > -6 ? 'My heart feels a little heavier.' : 'My heart feels heavy.'; }
function broadcastLines(n) {
  const body = [];
  if (n.day === 2) body.push('Someone has moved into the old empty house down the lane. Do say hello, everyone.');
  if (n.after > n.before) body.push(n.after === 1 ? 'The air has softened a lot. Looks like spring is on its way.' : n.after === 2 ? "It's getting properly warm. Drink plenty of water, everyone." : 'The mornings are crisp now. Autumn has come.');
  else if (n.after < n.before) body.push("A cold snap has come back. Dress warmly and don't push yourselves.");
  if (n.forestOpened) body.push('I hear the path up the back hill is open again.');
  if (n.lakeOpened) body.push('The reeds by the reservoir have cleared. You can walk down there now.');
  if (n.hillOpened) body.push('The lane up to Sunset Hill is open.');
  if (isMarketDay(n.day)) body.push('Today is market day. The stalls are up in the lane, so drop by.'); else if (daysUntilMarket(n.day) === 1) body.push('Tomorrow is market day. Get your things ready to sell.');
  if (n.rite) body.push(n.rite);
  if (n.snowing) body.push('Snow tonight made the roads slippery. Careful out there, especially our elders.'); else if (n.raining) body.push("It's raining, so keep your sauce jar lids closed.");
  if ((n.raining || n.snowing) && n.lidsOpen) body.push('And whoever left their jar lids open, close them quick!');
  if (n.mejuSeason && n.day >= 3 && n.day % 4 === 3) body.push('Winter is meju season. Soybeans and salt are at the corner store in the market lane.');
  if (n.helpWanted && n.day % 2 === 0) body.push({ rosa: 'Grandma Boksun at the mill could use a hand with her rice-cake steamer.', theo: "Dongsu's forge needs someone to work the bellows. Lend him a hand, eh?",
    mina: "Little Mina's looking for someone to push her sled on the stream.", mayor: "The village hall needs its coal briquettes carried in. ...Yes, it's me asking." }[n.helpWanted]);
  if (!body.length && n.day > 2 && n.day % 3 === 0) body.push('Nothing special today. Just make sure you all eat well.');
  return body.length ? ['Ahem, ahem. Attention, everyone in the village.', ...body, "That's all from your village head."] : [];
}
let broadcast = null;   // { lines, i, t }
function queueBroadcast(lines) { broadcast = lines.length ? { lines, i: 0, t: 0 } : null; if (broadcast) sfx('chime_broadcast', 0.9, 0); }
function updateBroadcast(dt, skip) {
  if (!broadcast || announce || dialogue || panel || screen || (G.scene !== 'farm' && G.scene !== 'market')) return;
  const line = broadcast.lines[broadcast.i]; broadcast.t += dt;
  const done = broadcast.t * 22 >= line.length;
  if (skip && !done) broadcast.t = line.length / 22 + 0.01;
  else if ((done && broadcast.t > line.length / 22 + 1.8 + line.length / 14) || (skip && done)) { broadcast.i++; broadcast.t = 0; if (broadcast.i >= broadcast.lines.length) broadcast = null; }
}
function drawBroadcast() {
  if (!broadcast || announce || dialogue || panel || screen || (G.scene !== 'farm' && G.scene !== 'market')) return;
  const line = broadcast.lines[broadcast.i], w = 560, x = CW / 2 - w / 2, y = 52;
  box(x, y, w, 72, 'rgba(20,14,11,0.96)', '#c89552');
  const ic = IMG['World/loudspeaker']; if (ic) ctx.drawImage(ic, x + 12, y + 12, 48, 48);
  drawRich('[#ffd84a]Village broadcast  ·  Village Head Park[/]', x + 72, y + 10, 13, w - 84);
  drawRich(line.slice(0, Math.floor(broadcast.t * 22)), x + 72, y + 30, 16, w - 84);
}
// ------------------------------------------------------------------ boot
function loop(t) {
  const dt = Math.min(0.05, (t - last) / 1000 || 0); last = t;
  update(dt); soundTick(dt); renderWorld(); renderUI();
  requestAnimationFrame(loop);
}
newGame();
const resumed = load();
if (G.scene === 'haggle' || G.scene === 'hill') { G.scene = 'farm'; G.px = 0.9; G.py = -2.0; }
G.clearedDebris = G.clearedDebris || []; G.debrisHits = G.debrisHits || {};   // saves from before the bigger farm
G.openPlots = G.openPlots || []; G.restedPlots = G.restedPlots || []; G.compostPoints = G.compostPoints || 0; G.compostReady = G.compostReady || 0;   // before the homestead
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
    $('start').hidden = true; uiCanvas.focus(); sndInit();
    screen = { kind: 'title', sel: 0, confirm: false };
    requestAnimationFrame(t => { last = t; loop(t); });
  };
});
