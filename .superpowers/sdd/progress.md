# v1.6.0 subagent-driven progress (branch: feature/v1.6.0)

Done (controller, this session):
- swap-scripts tag helper (178a923)
- swap-frame tag helper (4466a0a)
- M5 fragment dependency DAG core (1b0922f)

Queue (subagent workflow — disjoint files, sequential):
- [ ] swap-upload tag helper (M6)
- [ ] StateMetadata reflection cache (M7)
- [ ] IRealtimePresence single-node (M6)
- [ ] analyzer CodeFixProvider for SWAP001 (M2 deferred)
- [ ] incremental AutoScanGenerator (M7)

Queue (controller — coupled/client-JS, hand-done + render-verified):
- [ ] WithFlash across redirect (M4)
- [ ] guarded swaps client-enforcement (M4)
- [ ] data-swap-seq version stamp + client guard (M3)
- [ ] fingerprint diff-skip (M5)
- [ ] optimistic UI + rollback (M6)
- [ ] streaming render + SSE framing (M7)
- [ ] idiomorph vendor + dogfood morph (M3)

Snapshot: implementers must NOT edit PublicApi *.txt; controller reconciles once at end.

Batch-1 complete (subagent-built + controller-fixed + reviewed):
- swap-upload (e86077f + fix) — review clean after strengthening attr-strip test
- IRealtimePresence (e6e0210 + fix) — review clean after fixing orphan race + consistency test
- StateMetadata cache (1a5e37d + fix) — review clean after read-only view + subclass-override test
- snapshots reconciled; 493 tests green

Batch-2 complete:
- SwapFlow step-machine (e91882c + fix) — review clean after empty-steps coverage
- incremental AutoScanGenerator (60899bc) — review APPROVED (output-preserving, cache-tracked)
- 501 lib + 30 gen tests green
Task WithFlash: complete
Task data-swap-seq: complete (client guard pending browser-verify)

=== v1.6.0 SHIPPED (2026-07-02) ===
- merged PR #75 (3558be4) + docs PR #76 (46cb94c) to main
- released v1.6.0 -> NuGet published (Swap.Htmx + Realtime + Realtime.Redis + Testing + Templates)
- website (swap.htmx) bumped 1.5.0->1.6.0, deployed to Railway, verified live (health/home 200, 0 console errors)
- wiki: 7 new pages pushed (swap.wiki 03d294e); llms.txt updated
- final whole-branch review: no Critical; 3 Important fixed pre-merge
- deferred -> v1.7.0 (task #22): fingerprint, optimistic UI, guarded swaps, DI-wire presence/flow, OOB coalescing, streaming/SSE framing, CodeFixProvider + review minors
