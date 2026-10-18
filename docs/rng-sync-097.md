# 0.9.7 multiplayer RNG snapshot compatibility candidate

Status: local engine tests passed; two-machine reproduction is pending. Do not describe this as a confirmed universal multiplayer black-screen fix. Published to Workshop as a compatibility candidate at the user's request so both peers can update through Steam. Public 0.9.7 change note verified on 2026-10-19; two-PC confirmation remains pending.

Both reproduced client logs fail inside SyncRngMessage -> SerializableRelicGrabBag.Deserialize, with out-of-range model IDs (2386/2197 and 4020/2438). In the second session, the peers report equal initial model counts and gameplay hashes, while their initialization order differs. The exact third-party interaction remains unproven.

This candidate replaces only SyncRngMessage's body. RNG stream keys and rarity keys use fixed-width integers; relics use category/entry names rather than compressed local network numbers. Every RNG counter and all four state words, every rarity bucket, and exact relic queue order are preserved. The native message envelope, reliable delivery, buffering and state application remain in use. No packet is skipped and no unknown relic is substituted or dropped. Invalid bodies are rejected before assigning the decoded snapshot.

As corrected in 0.9.8, the actual stored model hash is finalized after Init with a fixed FSR1 protocol salt, so old Frostsworn peers fail the existing handshake instead of trying to decode this body. Every player must install the candidate and restart the game. Other message types still use the game's standard encoding: this targeted mitigation does not establish that every mod combination has identical tables or rule out unrelated desyncs. Save formats are unchanged.

Validation in the Godot/game/RitsuLib harness:
- Actual patched SyncRngMessage.Serialize/Deserialize and generic IPacketSerializable dispatch.
- 1983/2438/4190-entry simulated receiver tables, reversed local entries and 11/12/13-bit widths.
- All native RNG streams plus a high-valued custom stream; exact counters and 256-bit states.
- 324 relics, custom/empty rarity buckets, exact order, identical reserialized bytes (9868 bytes).
- Null seed and empty snapshots; invalid marker/count/unknown relic rejected without partial assignment.
- Native hash distinguishes the previous protocol.

Real-session acceptance: install on both computers, restart both games, keep the previously failing character-mod set, start a new run, verify both enter Neow, then enter multiple combat rooms and test save/reload. Collect both logs if a further fault occurs. The AncientWaifus missing animation API on the host is a separate confirmed compatibility error and is not changed here.

## 0.9.8 correction

The 0.9.7 getter-only Harmony patch was unsafe: callers may inline the original getter and disagree with callers passing through the patch. Replace it with a successful-Init finalizer that writes the actual hash storage; repeated application is idempotent and recomputed native hashes are salted once. Added a direct field reader JIT-compiled before finalization, ordinary getter comparisons, and real Init/repeated Init checks. All snapshot and new hash tests passed. Native Init itself can recompute a different hash on a repeated call due to cached-property hashing; tests observe each native result rather than incorrectly requiring an unchanged underlying hash. No change to snapshot wire FSR1 or save formats.
