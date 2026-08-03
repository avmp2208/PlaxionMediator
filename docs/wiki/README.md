# docs/wiki

This folder holds the source-of-truth markdown for the project's **GitHub Wiki**. GitHub wikis are their own git repository (`https://github.com/avmp2208/PlaxionMediator.wiki.git`) and are not automatically synced from `docs/wiki` — to publish updates:

1. Edit the relevant `.md` file(s) in this folder as part of a normal PR/commit to the main repo.
2. Clone the wiki repo separately (`git clone https://github.com/avmp2208/PlaxionMediator.wiki.git`) and copy the updated file(s) over (filenames map 1:1 to wiki page names, e.g. `Getting-Started.md` → the "Getting Started" wiki page), then commit/push to the wiki repo.

Pages:
- `Home.md` — wiki landing page / table of contents
- `Getting-Started.md`
- `Packages-Overview.md`
- `ASPNET-Core-and-Minimal-APIs.md`
- `Design-Overview.md`
- `Analyzers-Reference.md`
- `Testing-Guide.md`
- `Roadmap.md`
- `FAQ.md`
