# Community & Publicity Strategy — Conduit (Writing-Only, No Public Speaking)

> Scope note: this document is **separate** from `docs/architecture/` and does not modify any other `ReleaseProcess/` document. It answers **"how do we get the .NET community to know about Conduit"** under one hard constraint: **all outreach happens in writing**. No conference talks, no live streams, no podcasts, no unscripted video appearances by you personally. Every channel below is chosen or adapted specifically to work with that constraint.

## 1. Guiding Principle

- **Text-first, always.** Every announcement, every piece of content, every community reply starts as a written artifact (GitHub README/docs, a blog post, a written thread) that you compose asynchronously, at your own pace, with no time pressure and no live interaction required.
- **AI-assisted repurposing, not AI-invented claims.** AI tools are used to *reformat and repurpose content you already wrote* (turn a blog post into a Twitter/X thread, a LinkedIn post, an Instagram carousel script, a TikTok voiceover script) — never to invent unverified technical claims about Conduit. You review and approve everything before it's posted; the AI is a repurposing/production assistant, not an unsupervised spokesperson.
- **No obligation to appear on camera or speak.** Every video-capable channel (TikTok, Instagram Reels, YouTube Shorts) is designed around narrated/animated content (screen recordings, code diagrams, AI text-to-speech or AI-generated avatar narration reading a script you wrote) so your face and voice are never required.
- **Low synchronous-response pressure.** Prefer channels and formats where a delayed reply is completely normal (GitHub issues/discussions, forum posts, Reddit, dev.to comments) over channels that expect instant back-and-forth (live chat, X Spaces, Discord voice).

## 2. Channel Plan

| Channel | Purpose | Format (all written/asynchronous) | Effort level |
|---|---|---|---|
| **GitHub (README + Discussions + Releases)** | Source of truth; where technical credibility is earned | Well-written README, `CHANGELOG.md`, GitHub Discussions for Q&A, written release notes per version | Low-medium, ongoing |
| **X (Twitter) — `.NET` community** | Highest-density .NET developer audience; where MediatR-alternative discussions already happen | Short written threads (announcements, "why we built X this way" mini-essays), quote-replies to relevant .NET/OSS conversations | Medium |
| **LinkedIn** | Reaches engineering managers/decision-makers, not just individual devs — useful for future Pro/Enterprise leads | Longer-form written posts (same content as blog posts, reformatted), no video required | Low-medium |
| **dev.to / Medium / personal blog** | Long-form technical writing — the actual "proof" content that everything else links back to | Full articles: architecture deep-dives, "how the source generator works," benchmark results | Medium-high (main writing effort) |
| **Reddit (`r/dotnet`, `r/csharp`)** | High-trust technical audience, but sensitive to self-promotion — must add value, not just link-drop | Written technical posts/answers, only occasional (not every post) mention of Conduit when genuinely relevant | Low, careful/sparing use |
| **Instagram** | Broader/visual audience, useful for brand presence and B2B credibility signaling | Static graphics/carousels (architecture diagrams, quote cards from blog posts) + narrated Reels using AI voiceover reading your script over animated diagrams | Medium (needs a design template, reusable) |
| **TikTok** | Younger developer audience, algorithm-driven discovery independent of an existing following | Short narrated explainer videos (AI voice or AI avatar reading a script you wrote, over code/screen recordings) — never you on camera | Medium (needs a video template, reusable) |
| **Hacker News / lobste.rs** | One-shot high-leverage exposure moment for major milestones (v1.0.0 launch) | A single well-written "Show HN" post per major milestone, written text only, replies typed asynchronously | Very low frequency, high care per post |

## 3. Content Pipeline (write once, repurpose everywhere)

To keep this sustainable for one person who does not want to perform live, every channel is fed from a **single upstream long-form piece**, not written independently per channel:

1. **Write one long-form piece** (blog post on dev.to or your own site) per milestone/topic — e.g., "Why Conduit has no reflection," "Conduit's source generator, explained," "Conduit v1.0.0 is out."
2. **Repurpose downstream, in this order**:
   - LinkedIn post: trim to ~3–5 paragraphs, same core argument, link back to the full post.
   - X thread: break into 6–10 short tweets, one idea per tweet, last tweet links to the full post.
   - Instagram carousel: pull 5–8 key sentences/diagrams into slide graphics; caption links to the full post.
   - TikTok/Reels script: extract the single most "hooky" idea from the post, write a 30–60 second narration script, generate narration via AI text-to-speech (or an AI avatar tool) over a screen recording or animated diagram, caption links to the full post.
   - Reddit/HN: only when the piece is genuinely milestone-worthy (not every blog post), post the link with a short written summary comment.
3. **Reuse an AI writing/repurposing assistant** (e.g., an LLM-based tool) to do the mechanical trimming/reformatting step for each downstream format from the source article — you supply the source text and review/edit the output before publishing; the assistant does not generate new technical claims on its own.

## 4. Publishing Cadence (sustainable, not exhausting)

| Cadence | Content |
|---|---|
| Per merged milestone (roughly per `ReleaseProcess/01-mvp-development-phases.md` / `03-full-development-phases-oss.md` phase boundary) | One long-form blog post + full repurposing pass across all channels |
| Weekly (only while there is real news) | One short written update on X/LinkedIn — a shipped feature, a fixed issue, a benchmark number — never manufactured filler content |
| As-needed | Reply (in writing, asynchronously, no pressure to respond immediately) to GitHub issues/Discussions and relevant Reddit/X conversations mentioning MediatR alternatives or .NET pipeline patterns |

- **No daily posting obligation.** A quiet week with no content is fine; consistency of *quality* matters more than frequency, and this avoids burnout for a one-person, non-performing content strategy.

## 5. Accounts to Create

- **GitHub organization** for Conduit (separate from your personal account) — hosts the repo, Discussions, and issue tracker; this is the primary channel and should be set up first.
- **X (Twitter) account**, dedicated to Conduit (not your personal account) — lets AI-repurposed content and technical announcements live in a focused feed without mixing with personal posts.
- **LinkedIn**: post from your personal profile (tagged/linked to the project) rather than a separate company page initially — personal profiles get more organic reach pre-launch than a brand-new company page with zero followers; revisit a company page once there is a small existing audience.
- **Instagram account**, dedicated to Conduit — needed for the visual/carousel and Reels content described above.
- **TikTok account**, dedicated to Conduit — needed for narrated short-form video; can be deferred until there are 2–3 blog posts already written to source scripts from (do not start this channel with nothing to repurpose).
- Reddit/dev.to/Medium: use existing personal accounts (or a consistent pseudonymous handle) — these communities are less about "brand accounts" and more about a consistent, trustworthy individual voice over time.

## 6. What This Strategy Deliberately Avoids

- No conference talks, meetups, or public speaking of any kind.
- No live streaming, no unscripted video, no live Q&A/AMA formats.
- No requirement to appear on camera or use your own recorded voice — all video narration can be AI-generated from scripts you write and approve.
- No obligation to be "always on" in real-time community chats (Discord voice channels, X Spaces) — written, asynchronous replies only.
- No content posted without your review — AI tools are used strictly for repurposing/formatting/narration of content you have already written and fact-checked, never as an unsupervised source of technical claims about Conduit.

## 7. Success Metrics

| Metric | Why it matters |
|---|---|
| NuGet.org download count trend | Direct measure of adoption; the ultimate goal of all publicity effort. |
| GitHub stars / Discussions activity | Signals organic technical interest and community self-sufficiency (fewer support burdens on you personally). |
| Blog post → repurposed content click-through rate | Confirms which downstream channel (X, LinkedIn, Instagram, TikTok) is actually driving readers back to the technical source, guiding where to spend future effort. |
| Ratio of inbound mentions/questions to outbound posts | A healthy community eventually starts talking about Conduit unprompted (in written form) — this is the leading indicator that the writing-only strategy is working without you needing to perform publicly. |

---

This document intentionally contains no engineering task list — for the module-by-module build order, see `03-full-development-phases-oss.md` and `04-full-development-phases-enterprise.md`; for pricing/packaging, see `05-monetization-strategy.md`. Revisit this strategy once the first 1–2 blog posts and their repurposed content are live, since channel effort allocation above is a starting hypothesis based on typical .NET developer-audience behavior, not measured data yet.
