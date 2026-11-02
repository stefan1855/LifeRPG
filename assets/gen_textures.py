# -*- coding: utf-8 -*-
"""
LifeRPG 贴图流水线 · 第一段：生成「材质」与「装饰件」
- 材质：可平铺底纹（石头/金属/羊皮纸），程序做镜像平铺消除接缝
- 装饰件：黑底生成，亮度转 alpha，用于叠加发光
- 边框/按钮/进度条不在此生成，交给 build_ui_assets.py 程序化合成
"""
import base64
import io
import json
import os
import re
import sys
import time
import urllib.request

from PIL import Image

GATEWAY = "http://127.0.0.1:8317/v1/chat/completions"
CRED = os.path.join(os.environ["USERPROFILE"], ".dsh", ".credentials.yaml")
OUT_DIR = r"D:\Projects\LifeRPG\app\src\assets\textures"
RAW_DIR = r"D:\Projects\LifeRPG\assets\raw"

STYLE = (
    "dark fantasy gothic warlock, black stone and dark bronze gold, necromancer soul-fire green glow, "
    "ultra detailed, sharp, game asset, no text, no letters, no watermark, no logo"
)

# name, prompt, out_width, mode(material|ornament), max_kb
ASSETS = [
    ("mat_stone", "seamless tileable dark basalt stone surface, uniform lighting, fine grain, very dark near-black, no vignette, no border, flat top-down view, " + STYLE, 512, "material", 200),
    ("mat_stone_cracked", "seamless tileable dark cracked stone with faint green soul fire glowing inside the cracks, uniform lighting, no vignette, no border, flat top-down view, " + STYLE, 512, "material", 220),
    ("mat_metal_bronze", "seamless tileable aged dark bronze gold metal plate surface, brushed, scratches, uniform lighting, no vignette, no border, flat top-down view, " + STYLE, 512, "material", 200),
    ("mat_metal_iron", "seamless tileable black iron plate surface with rivet-free hammered texture, uniform lighting, no vignette, no border, flat top-down view, " + STYLE, 512, "material", 200),
    ("mat_parchment", "seamless tileable aged dark parchment paper texture, burnt brown black, subtle fibers, uniform lighting, no vignette, no border, flat top-down view, " + STYLE, 512, "material", 200),
    ("mat_bone", "seamless tileable ancient bone ivory surface with fine cracks, dark ivory tone, uniform lighting, no vignette, no border, flat top-down view, " + STYLE, 512, "material", 200),

    ("orn_skull", "single gothic demon skull ornament, bronze gold with black iron horns, glowing green eye sockets, isolated on pure black background, centered, facing forward, " + STYLE, 384, "ornament", 150),
    ("orn_divider", "thin horizontal gothic divider ornament, dark gold metal bar with a small skull in the exact center, symmetric, isolated on pure black background, " + STYLE, 768, "ornament", 120),
    ("orn_emblem", "circular gothic emblem, demon skull crowned with green soul fire inside a dark bronze gold ring, symmetric, isolated on pure black background, centered, " + STYLE, 512, "ornament", 200),
    ("orn_magic_circle", "arcane summoning magic circle, thin glowing green runes and geometric lines, perfectly circular, top-down view, isolated on pure black background, " + STYLE, 768, "ornament", 220),
    ("orn_rune_ring", "circular band of glowing green gothic runes, thin ring, top-down view, isolated on pure black background, " + STYLE, 768, "ornament", 180),
    ("orn_orb", "glowing green soul fire orb, spherical energy ball with white hot core and wispy flames, isolated on pure black background, centered, " + STYLE, 384, "ornament", 150),
    ("orn_wings", "pair of tattered demon wings spread wide, black bone and green glow edges, symmetric, isolated on pure black background, " + STYLE, 768, "ornament", 180),
    # ---- v1.5 细节件：包角 / 流体 / 技能盘 ----
    ("orn_corner", "single gothic cast iron corner bracket ornament for a frame corner, L-shaped decorative cast metal corner piece, dark iron with aged bronze gold beveled edges and a small rivet, pointing to the top-left, isolated on pure black background, " + STYLE, 128, "ornament", 90),
    ("tex_soulfluid", "seamless tileable flowing green soul fire liquid energy texture, vertical streaks of glowing emerald flame with bright white hot cores, uniform lighting, no vignette, no border, flat top-down view, " + STYLE, 512, "material", 130),
    ("plate_skill", "square gothic skill icon plate frame with hollow empty center, dark iron beveled border with aged bronze gold inner edge and four corner notches, isolated on pure black background, centered, " + STYLE, 160, "ornament", 60),

    ("fx_fog", "swirling toxic green mist and smoke, volumetric wisps, isolated on pure black background, " + STYLE, 768, "ornament", 200),
    ("fx_embers", "scattered glowing green embers and sparks, particles floating, isolated on pure black background, " + STYLE, 512, "ornament", 120),
]


def api_key():
    with open(CRED, "r", encoding="utf-8") as f:
        for line in f:
            m = re.match(r"\s*EASYCLIPROXYAPI_API_KEY:\s*(\S+)", line)
            if m:
                return m.group(1)
    raise RuntimeError("api key not found")


def generate(prompt, retries=3):
    key = api_key()
    body = json.dumps({
        "model": "gemini-3.1-flash-image",
        "messages": [{"role": "user", "content": prompt}],
        "max_tokens": 4096,
    }).encode("utf-8")
    last = None
    for attempt in range(retries):
        try:
            req = urllib.request.Request(
                GATEWAY, data=body,
                headers={"Authorization": "Bearer " + key, "Content-Type": "application/json"},
                method="POST")
            with urllib.request.urlopen(req, timeout=240) as resp:
                data = json.loads(resp.read().decode("utf-8"))
            imgs = data["choices"][0]["message"].get("images") or []
            if not imgs:
                raise RuntimeError("no image in response")
            b64 = imgs[0]["image_url"]["url"].split(",", 1)[1]
            return Image.open(io.BytesIO(base64.b64decode(b64))).convert("RGB")
        except Exception as e:  # noqa: BLE001
            last = e
            time.sleep(3 * (attempt + 1))
    raise RuntimeError("generate failed: %s" % last)


def make_seamless(img):
    """镜像平铺后取中心，得到左右上下边界严格相接的可平铺纹理"""
    w, h = img.size
    canvas = Image.new("RGB", (w * 2, h * 2))
    canvas.paste(img, (0, 0))
    canvas.paste(img.transpose(Image.FLIP_LEFT_RIGHT), (w, 0))
    canvas.paste(img.transpose(Image.FLIP_TOP_BOTTOM), (0, h))
    canvas.paste(img.transpose(Image.ROTATE_180), (w, h))
    return canvas.crop((w // 2, h // 2, w // 2 + w, h // 2 + h))


def black_to_alpha(img):
    img = img.convert("RGBA")
    alpha = img.convert("L").point(lambda v: min(255, int(v * 1.2)))
    img.putalpha(alpha)
    return img


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    os.makedirs(RAW_DIR, exist_ok=True)
    force = "--force" in sys.argv
    only = [a for a in sys.argv[1:] if not a.startswith("--")]

    manifest = []
    for name, prompt, width, mode, max_kb in ASSETS:
        if only and name not in only:
            continue
        out = os.path.join(OUT_DIR, name + ".webp")
        if os.path.exists(out) and not force:
            print("SKIP  %s" % name, flush=True)
            manifest.append({"name": name, "file": name + ".webp", "mode": mode})
            continue
        t0 = time.time()
        try:
            img = generate(prompt)
            img.save(os.path.join(RAW_DIR, name + ".png"))
            if mode == "material":
                img = make_seamless(img)
            else:
                img = black_to_alpha(img)
            w, h = img.size
            if w != width:
                img = img.resize((width, max(1, round(h * width / w))), Image.LANCZOS)
            img.save(out, "WEBP", quality=90, method=6)
            if os.path.getsize(out) > max_kb * 1024:
                img.save(out, "WEBP", quality=78, method=6)
            size = os.path.getsize(out)
            print("OK    %-18s %-9s %5.1fs %5d KB %s" % (name, mode, time.time() - t0, size // 1024, "%dx%d" % img.size), flush=True)
            manifest.append({"name": name, "file": name + ".webp", "mode": mode, "size": size})
        except Exception as e:  # noqa: BLE001
            print("FAIL  %-18s %s" % (name, e), flush=True)
    with open(os.path.join(OUT_DIR, "manifest.json"), "w", encoding="utf-8") as f:
        json.dump(manifest, f, ensure_ascii=False, indent=2)
    print("DONE %d assets" % len(manifest), flush=True)


if __name__ == "__main__":
    main()
