"""Record fingerprints of the game's own files, so CI can refuse them.

The repository must never contain Drag'n Wash's files. This writes the SHA-256,
size and name of every file in a game install (not BepInEx, not mods) to
ci/game-fingerprints.json. Only hashes and names are stored, never content.
tools/check-game-files.py compares every file in the repository against it.

Run it on a machine with the game installed, once per game build and platform,
and commit the result. Each run adds to the file:

    python tools/game-fingerprints.py --game-dir "C:/Program Files (x86)/Steam/steamapps/common/Drag'n Wash" --label "Windows 9/12/2026_a93aa21a"
    python3 tools/game-fingerprints.py --game-dir ~/.local/share/Steam/steamapps/common/"Drag'n Wash" --label "Linux 9/13/2026_2a0da92f"
    python3 tools/game-fingerprints.py --game-dir ~/"Library/Application Support/Steam/steamapps/common/Drag'n Wash" --label "macOS 9/12/2026_a93aa21a"

With --stdout the result for that one install is printed instead (to merge a
scan made on another machine with --merge FILE).
"""
import argparse
import hashlib
import json
import os
import pathlib
import sys

OUT = pathlib.Path(__file__).resolve().parent.parent / "ci" / "game-fingerprints.json"

# Mod loader files and files made by Steam, Finder or crashes, not the game's.
SKIP_DIRS = {"BepInEx"}
SKIP_FILES = {
    ".doorstop_version", "doorstop_config.ini", "winhttp.dll", "libdoorstop.so",
    "libdoorstop.dylib", "run_bepinex.sh", "changelog.txt", "steam_appid.txt",
    ".DS_Store",
}
# preloader_*.log: what Doorstop writes into DragNWash.app/Contents/MacOS when BepInEx fails to start.
SKIP_PREFIXES = ("mono_crash.", "preloader_")
# Tiny files (an empty file, a one-line config) would match unrelated files.
MIN_SIZE = 256


def scan(game_dir):
    root = pathlib.Path(game_dir).expanduser()
    if not root.is_dir():
        sys.exit(f"Not a folder: {root}")
    files = {}
    for dirpath, dirnames, filenames in os.walk(root):
        rel_dir = pathlib.Path(dirpath).relative_to(root)
        if rel_dir == pathlib.Path("."):
            dirnames[:] = [d for d in dirnames if d not in SKIP_DIRS]
        for name in filenames:
            if name in SKIP_FILES or name.startswith(SKIP_PREFIXES):
                continue
            path = pathlib.Path(dirpath) / name
            size = path.stat().st_size
            if size < MIN_SIZE:
                continue
            digest = hashlib.sha256()
            with open(path, "rb") as f:
                for chunk in iter(lambda: f.read(1 << 20), b""):
                    digest.update(chunk)
            files[(rel_dir / name).as_posix()] = {"sha256": digest.hexdigest(), "size": size}
    return files


def merge(data, label, files):
    data.setdefault("builds", {})[label] = len(files)
    hashes = set(data.get("sha256", []))
    names = set(data.get("names", []))
    for rel, info in files.items():
        hashes.add(info["sha256"])
        names.add(pathlib.PurePosixPath(rel).name)
    data["sha256"] = sorted(hashes)
    data["names"] = sorted(names)
    return data


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--game-dir")
    parser.add_argument("--label", help='for example "Windows 9/12/2026_a93aa21a"')
    parser.add_argument("--stdout", action="store_true", help="print this scan instead of writing ci/game-fingerprints.json")
    parser.add_argument("--merge", help="a scan printed with --stdout on another machine")
    args = parser.parse_args()

    data = json.loads(OUT.read_text(encoding="utf-8")) if OUT.exists() else {
        "about": "SHA-256 and names of Drag'n Wash's own files, so CI can refuse them. No game content. See tools/game-fingerprints.py.",
    }
    if args.merge:
        other = json.loads(pathlib.Path(args.merge).read_text(encoding="utf-8"))
        data = merge(data, other["label"], other["files"])
    elif args.game_dir and args.label:
        files = scan(args.game_dir)
        if args.stdout:
            json.dump({"label": args.label, "files": files}, sys.stdout)
            return
        data = merge(data, args.label, files)
    else:
        parser.error("give --game-dir and --label, or --merge")

    OUT.parent.mkdir(exist_ok=True)
    OUT.write_text(json.dumps(data, indent=1, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"{OUT}: {len(data['sha256'])} hashes, {len(data['names'])} names, builds: {', '.join(data['builds'])}")


if __name__ == "__main__":
    main()
