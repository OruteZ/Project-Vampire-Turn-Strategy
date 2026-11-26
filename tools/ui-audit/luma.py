"""Brightness of screenshots: one tab-separated line per PNG. Usage: py -3 -I tools/ui-audit/luma.py <png>... [--box x0,y0,x1,y1]"""
import argparse
import sys

from PIL import Image, ImageStat


def stats(path, box=None):
    img = Image.open(path).convert("RGB")
    if box:
        img = img.crop(box)
    hist = img.convert("L").histogram()
    n = sum(hist)

    def pct(q):
        seen = 0
        for v, count in enumerate(hist):
            seen += count
            if seen > q * n:
                return v
        return 255

    r, g, b = ImageStat.Stat(img).mean
    return sum(v * c for v, c in enumerate(hist)) / n, pct(0.5), pct(0.9), pct(0.99), r, g, b


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("png", nargs="+")
    ap.add_argument("--box", help="x0,y0,x1,y1 in image pixels, origin top-left")
    a = ap.parse_args()
    box = tuple(int(v) for v in a.box.split(",")) if a.box else None
    print("file\tmean\tp50\tp90\tp99\tR\tG\tB")
    for p in a.png:
        m, p50, p90, p99, r, g, b = stats(p, box)
        print(f"{p}\t{m:.1f}\t{p50}\t{p90}\t{p99}\t{r:.1f}\t{g:.1f}\t{b:.1f}")


if __name__ == "__main__":
    if len(sys.argv) > 1 and sys.argv[1] == "--selftest":
        import os, tempfile
        path = os.path.join(tempfile.mkdtemp(), "t.png")
        Image.new("RGB", (10, 10), (30, 60, 90)).save(path)
        m, p50, p90, p99, r, g, b = stats(path)
        assert (round(r), round(g), round(b)) == (30, 60, 90) and p50 == p90 == p99, (m, p50, r, g, b)
        assert stats(path, (0, 0, 5, 5))[4] == 30
        print("ok")
    else:
        main()
