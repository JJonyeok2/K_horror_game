# Internal Playflow Proof

## Source

- Branch: `codex/unity-migration-phase1`
- Commit: `b444be9`
- Generated: `2026-05-30 08:50:31 UTC`

## Verification

- Tests: `120 total / 120 passed / 0 failed / 0 skipped`
- Test result: `Passed`
- Screenshot: `internal-playflow-proof.png`
- Screenshot bytes: `114895`

## Covered Flow

- Core loop smoke: `BongoHub -> JonggaEstate -> cargo load -> re-pickup -> return -> settlement`
- Threat loop smoke: `shrine theft -> grace -> ghost actor -> audio occlusion -> atmosphere cue`
- Cargo drop proof: `single-owner G input`, `inside van -> cargo hold`, `outside van -> world pickup`, `lowered-floor drop snap`
- Ghost AI proof: `Dormant -> Haunt -> Investigate -> Stalk -> Chase`, `front gate exit -> ReturnHome -> Despawn`, `ghost cannot cross into forest approach`
- Dokkaebi AI proof: `Lurk -> Misdirect -> BlockPath`, `estate entry -> Retreat -> Despawn`, `dokkaebi cannot cross into estate interior`
- Terminal UX proof: `terminal states -> depart -> traveling -> return -> settle`, `unavailable while traveling`
- Travel sequence proof: `travel fade -> generated bongo engine hum -> motion shake`
- Screenshot proof: bongo terminal, cargo hold, held cargo, shrine threat cue.
