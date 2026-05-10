"""Convert and resize an arbitrary image folder to mod-ready PNGs.

Reads every image in `--src` (any format Pillow can open: .webp, .png, .jpg, etc.),
converts to RGBA, resizes to fit a `--size`x`--size` square preserving aspect
ratio, centers on a transparent canvas, and writes lowercase `.png` files to
`--out`.

Defaults:
  --src  C:\\Users\\Brendan\\Downloads\\Bosses
  --out  same as --src
  --size 300

Examples:
  python scripts/resize_images.py
  python scripts/resize_images.py --out SlayTheMonolithMod/images --size 200
"""
from __future__ import annotations
import argparse
import os
import sys
from PIL import Image

EXTS = {'.webp', '.png', '.jpg', '.jpeg', '.bmp', '.gif', '.tiff'}


def process(src: str, out: str, size: int) -> int:
    if not os.path.isdir(src):
        print(f'ERROR: --src not a directory: {src}', file=sys.stderr)
        return 1
    os.makedirs(out, exist_ok=True)
    written = 0
    for fn in sorted(os.listdir(src)):
        stem, ext = os.path.splitext(fn)
        if ext.lower() not in EXTS:
            continue
        in_path = os.path.join(src, fn)
        out_path = os.path.join(out, stem.lower() + '.png')
        im = Image.open(in_path).convert('RGBA')
        # Fit into size x size preserving aspect ratio.
        im.thumbnail((size, size), Image.Resampling.LANCZOS)
        # Center on transparent canvas.
        canvas = Image.new('RGBA', (size, size), (0, 0, 0, 0))
        canvas.paste(im, ((size - im.width) // 2, (size - im.height) // 2), im)
        canvas.save(out_path)
        print(f'  {fn:30s} {im.width}x{im.height} (in source) -> {out_path}')
        written += 1
    print(f'\nWrote {written} PNG(s) to {out}')
    return 0


def main() -> int:
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument('--src', default=r'C:\Users\Brendan\Downloads\Bosses')
    p.add_argument('--out', default=None, help='defaults to --src')
    p.add_argument('--size', type=int, default=300)
    args = p.parse_args()
    return process(args.src, args.out or args.src, args.size)


if __name__ == '__main__':
    sys.exit(main())
