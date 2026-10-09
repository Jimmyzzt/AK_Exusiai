"""Fetch CONFESS-47's original Spine source files from its PRTS asset index."""

import json
from pathlib import Path
from urllib.request import Request, urlopen


ROOT = Path(__file__).resolve().parents[1]
BASE = "https://torappu.prts.wiki/assets/char_spine/char_4188_confes/"
OUTPUT = ROOT / "references/official/spine/char_4188_confes"
HEADERS = {"User-Agent": "Mozilla/5.0", "Referer": "https://prts.wiki/"}


def fetch(relative: str) -> bytes:
    with urlopen(Request(BASE + relative, headers=HEADERS), timeout=30) as response:
        return response.read()


def save(relative: str, data: bytes) -> None:
    destination = OUTPUT / relative
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_bytes(data)
    print(f"Saved {destination.relative_to(ROOT)} ({len(data)} bytes)")


def main() -> None:
    metadata = fetch("meta.json")
    save("meta.json", metadata)
    seen = set()
    for choices in json.loads(metadata)["skin"].values():
        for variant in choices.values():
            stem = variant["file"]
            if stem in seen:
                continue
            seen.add(stem)
            atlas = fetch(stem + ".atlas")
            save(stem + ".atlas", atlas)
            save(stem + ".skel", fetch(stem + ".skel"))
            for line in atlas.decode("utf-8-sig").splitlines():
                page = line.strip()
                if line != page or not page.lower().endswith(".png"):
                    continue
                save(str(Path(stem).parent / page).replace("\\", "/"),
                     fetch(str(Path(stem).parent / page).replace("\\", "/")))


if __name__ == "__main__":
    main()
