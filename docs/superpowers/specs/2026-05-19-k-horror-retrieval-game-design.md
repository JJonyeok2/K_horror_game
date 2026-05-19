# K-Horror Retrieval Game Design

## Summary

This game is a K-horror reinterpretation of the Lethal Company-style retrieval survival loop. The player works for a suspicious cultural artifact recovery company, enters Korean-themed haunted locations at night, collects valuable artifacts to meet a quota, and escapes before folklore threats make the site too dangerous.

The core fantasy is not generic treasure hunting. It is the feeling of stepping into a forbidden Korean place, taking objects that should not be disturbed, and hearing the building react.

## One-Line Pitch

A suspicious artifact recovery contractor enters hanok estates, old academies, temples, and ritual sites at night to meet a company quota by stealing or recovering cultural objects while Korean ghosts, yokai-like beings, and folklore anomalies awaken around them.

## Design Goals

- Preserve the Lethal Company-like loop: accept quota, enter dangerous site, collect items, decide whether to push deeper, escape, and settle value.
- Give the game a clear K-theme through locations, artifacts, taboos, sound, and monster design.
- Make greed the main decision pressure: valuable artifacts are deeper, heavier, and more spiritually dangerous.
- Use sound as a gameplay signal, not just atmosphere.
- Start with a single-player MVP that can later expand into cooperative multiplayer.

## Player Role

The player is a low-level contractor for a suspicious cultural artifact recovery company. The company claims it is preserving or recovering displaced artifacts, but its methods are ambiguous and often illegal. It sends the player into abandoned or sealed Korean sites at night with a required recovery quota.

This role supports:

- A quota system.
- Company messages and mission briefings.
- Repeated visits to different themed maps.
- Moral unease around disturbing cultural objects.
- A future multiplayer crew structure.

## Core Loop

1. The company assigns a quota and a target site.
2. The player arrives outside the site with limited equipment.
3. The player enters, explores, and identifies recoverable artifacts.
4. Picking up artifacts raises value and risk.
5. Breaking site taboos raises danger faster.
6. Folklore threats escalate through sound, sightings, route disruption, and pursuit.
7. The player chooses whether to leave safely or push deeper for higher-value artifacts.
8. Escaped artifacts are sold or submitted toward the quota.
9. Failure creates debt, penalties, or pressure for the next run.

## MVP Scope

The first MVP should prove the retrieval loop, greed pressure, Korean horror identity, and sound-driven threat escalation.

Included in MVP:

- One map: Jongga Estate.
- One main threat: Mourning-Clothes Apparition.
- One secondary anomaly: Will-o'-the-wisp style spirit fire.
- A small artifact set with value, weight, and resentment impact.
- A resentment meter that escalates the haunting.
- Basic quota and extraction flow.
- Sound events tied to resentment stages.

Deferred:

- Online multiplayer.
- Large procedural generation.
- Full economy and equipment shop.
- Multiple playable maps.
- Large monster roster.
- Complex narrative branching.

## Map 1: Jongga Estate

### Theme

The first map is an old clan estate. It should feel like a place where family rites, inheritance, and ancestral rules still matter. The horror comes from entering a house that is not empty, even though no living person should be there.

### Scale

The map should feel comparable to a normal Lethal Company indoor facility in traversal commitment: large enough that reaching the deepest valuable area and returning is stressful, but small enough to learn after several runs.

Target feel:

- Entry to deepest area should take long enough that retreat is a meaningful decision.
- Returning with heavy items should be slower and riskier.
- Some routes should become less reliable after the first artifact is taken.
- The player should feel safe near the entrance, uncertain in the middle, and exposed in the deepest rooms.

### Areas

- Front Gate: extraction-adjacent area, initial safety, vehicle return point.
- Outer Yard: open space where distant figures and sound cues can be staged.
- Sarangchae: early exploration zone with low-value objects and signs of prior human presence.
- Anchae: deeper residential zone with medium-value artifacts and stronger haunting.
- Storage Building: cramped item-dense area with noise hazards.
- Rear Yard: transition space where navigation can become unreliable.
- Shrine Room: highest-value and highest-risk zone, tied to ancestral rites and taboos.

## First-Run Pacing

Before the first artifact is collected, the site is already dark and uneasy, but the threat appears human rather than supernatural. The player may see a person in mourning clothes standing in the yard, passing behind a paper door, or watching from the far side of a room. It should be unclear whether this is a caretaker, another intruder, or someone from a funeral.

After the first artifact is collected, the site reacts. Doors move, sound cues change, the mourning figure becomes impossible to explain, and the resentment system activates.

This creates a clean genre turn:

- Before first pickup: "Someone may be here."
- After first pickup: "Something was waiting for me to take that."

## Artifacts

Artifacts should feel culturally specific and physically grounded.

Examples:

- Brass ritual bowl.
- Old porcelain vessel.
- Ancestral tablet case.
- Folded funeral cloth.
- Calligraphy scroll.
- Clan genealogy book.
- Ritual candlestick.
- Lacquered chest.
- Old norigae or hair ornament.
- Shrine bell.

Each artifact should have:

- Value.
- Weight or carry burden.
- Resentment gain.
- Optional taboo tag, such as shrine item, funeral item, ancestor item, or document item.

## Taboos

Taboos are rules the site seems to enforce. They should be simple enough for players to learn through signs, company warnings, and consequences.

Initial Jongga Estate taboos:

- Do not disturb shrine objects.
- Do not move ritual vessels out of order.
- Do not step over marked thresholds.
- Do not extinguish shrine candles.
- Do not read certain names aloud or inspect forbidden records too long.

Taboos are not instant fail states. They increase resentment and may trigger local sound or visual events.

## Resentment System

The resentment system combines greed and ritual violation into one escalating danger value.

Suggested scoring:

- Cheap artifact: +1 resentment.
- Medium artifact: +2 resentment.
- Cultural treasure-class artifact: +3 resentment.
- Shrine artifact: +5 resentment.
- Minor taboo violation: +2 resentment.
- Major taboo violation: +4 resentment.

Stages:

- Stage 0: dormant unease before the first pickup.
- Stage 1: subtle sounds and distant movement.
- Stage 2: the mourning figure appears clearly but does not fully pursue.
- Stage 3: doors, routes, and lights become hostile.
- Stage 4: active pursuit begins.
- Stage 5: extraction becomes heavily contested through locked paths, false sounds, or lethal proximity.

The key decision is whether the next artifact is worth the next resentment stage.

## Threats

### Main Threat: Mourning-Clothes Apparition

The Mourning-Clothes Apparition is the first map's main monster. It initially reads as a human figure in mourning clothes. After the first artifact pickup, it becomes increasingly unnatural.

Behavior:

- Appears at long range before first pickup.
- Becomes clearer as resentment rises.
- Responds strongly to shrine items and taboo violations.
- Blocks paths before it fully chases.
- Begins active pursuit at high resentment.

Presentation:

- White or pale mourning clothes.
- Slow, dragging movement.
- Face partly hidden or unreadable.
- Sometimes framed behind doors, screens, or courtyard darkness.

### Secondary Anomaly: Spirit Fire

Spirit Fire is a smaller folklore anomaly used for misdirection and pressure.

Behavior:

- Appears in yards, corridors, or near valuable items.
- Can lure the player toward deeper rooms.
- Can mark unsafe routes or foreshadow apparition movement.
- Should not be the main killer in the first MVP.

### Object Anomalies

Some artifacts or ritual objects can become active hazards.

Examples:

- Ritual vessels clink when moved out of order.
- A scroll slowly unfurls after being disturbed.
- A shrine bell rings from a room the player just left.
- A wooden figure changes orientation when not watched.

These effects support the resentment system and sound design without requiring many full AI monsters in the first build.

## Broader Monster Pool

The full game should include ghosts, yokai-like beings, and Korean folklore anomalies rather than only ghosts.

Categories:

- Ghosts: mourning apparition, vengeful spirit, virgin ghost, ancestral spirit, grim reaper-like watcher.
- Yokai-like beings: dokkaebi, dueoksini, eoduksini, shapeshifting trickster spirits.
- Sound mimic beings: Jangsanbeom-inspired voice mimicry or mountain-call anomalies.
- Object spirits: cursed mask, moving wooden figure, haunted scroll, ringing ritual tools.
- Place anomalies: endless corridor, closing gate, changing yard, extinguishing shrine.

Map pairing examples:

- Jongga Estate: mourning apparition, spirit fire, ritual object anomalies.
- Old Seowon: book/name-based anomaly, shadow scholar, forbidden records.
- Mountain Hermitage: voice mimic, bell anomaly, temple-object spirit.
- Abandoned Hanok Village: dokkaebi misdirection, moving alleys, household spirits.
- Shaman Ritual House: bells, masks, ritual knives, spirit-possession anomalies.

## Sound Design

Sound should communicate state, location, and threat. The player should learn to read the site through audio.

Baseline ambience:

- Wind through wooden frames.
- Paper doors shaking.
- Old floorboards creaking.
- Distant insects or mountain night ambience.
- Occasional fabric movement.

Pre-pickup human-like cues:

- Distant footsteps.
- A low cough.
- A door sliding shut.
- Cloth brushing across wood.
- A faint funeral wail far outside.

Post-pickup haunting cues:

- Mourning cloth dragging nearby.
- Ritual vessels clinking.
- Shrine candles fluttering or going out.
- Bowing sounds from the shrine room.
- Breath behind the player.
- A funeral wail that moves closer as resentment rises.

Gameplay audio rules:

- Low resentment uses ambiguous sounds.
- Medium resentment uses directional warnings.
- High resentment uses misleading sounds and pursuit cues.
- Important sounds must remain readable even when atmospheric layers are active.

## Systems And Components

The design should be split into clear systems:

- Quota System: tracks required value, recovered value, debt or penalties.
- Artifact System: defines value, weight, category, and resentment impact.
- Inventory System: limits carrying capacity and slows the player under burden.
- Resentment System: tracks danger escalation from artifacts and taboos.
- Taboo System: detects ritual rule violations and triggers resentment changes.
- Threat Director: selects sightings, sounds, route interference, and pursuit states.
- Map System: defines rooms, routes, extraction, locked paths, and high-value zones.
- Audio System: plays ambience, positional cues, monster cues, and false cues.
- Extraction System: determines whether carried artifacts are successfully banked.

## Data Flow

1. The player interacts with an artifact or taboo-sensitive object.
2. The Artifact System or Taboo System emits a resentment change.
3. The Resentment System updates the current stage.
4. The Threat Director reads the stage and chooses events.
5. The Audio System and Map System present those events through sound, sightings, doors, lights, or route changes.
6. The player either extracts, drops items, hides, or pushes deeper.
7. Extracted artifacts update quota progress.

## Failure And Penalty

Failure should create pressure without ending the entire game too quickly.

Possible penalties:

- Lost carried artifacts.
- Increased debt.
- Reduced next-run equipment quality.
- More aggressive company messaging.
- Higher quota pressure after repeated failures.

For the MVP, use a simple debt or failed-run counter rather than a complex campaign economy.

## Testing Criteria

The design is working if:

- Players understand the objective within the first minute.
- The first artifact pickup clearly changes the site's behavior.
- Players can describe why taking one more item is risky.
- Sound cues help players make decisions instead of only startling them.
- The mourning apparition feels human at first and supernatural after activation.
- The player can escape with a small haul, but deeper greed often causes a close call or death.

## MVP Decisions

- Working title for planning: K-Horror Retrieval Game.
- Company framing: the company presents itself as a legitimate cultural artifact recovery contractor, but the missions feel legally and morally suspicious.
- First build target: first-person 3D single-player prototype.
- Quota penalty: a failed run loses carried artifacts and adds debt equal to the quota shortfall; three failed quota checks ends the prototype contract.
- First map threat names: Mourning-Clothes Apparition for the main threat, Spirit Fire for the secondary anomaly, and Ritual Object Anomalies for haunted artifacts.
