# -*- coding: utf-8 -*-
"""
UI 视觉验收：把界面截图发给多模态模型，按设计规格书与游戏 UI 标准打分并给出改进项。
用法: python qa_ui.py <screenshot.png> [更多截图...]
"""
import base64
import json
import os
import re
import sys
import urllib.request

GATEWAY = "http://127.0.0.1:8317/v1/chat/completions"
CRED = os.path.join(os.environ["USERPROFILE"], ".dsh", ".credentials.yaml")
MODEL = os.environ.get("QA_MODEL", "gemini-3.8-flash-high")

RUBRIC = """你是资深游戏 UI 美术总监，正在验收一款 Windows 桌面 RPG 应用（暗黑哥特 / 绿火毁灭术风格）的实际运行截图。
设计基准：
- 背景深渊黑 #050805，面板暗石板 #0B120B，金属边框暗古铜 #4A3C1B / 锈蚀金 #6D5B2A / 暗金 #8B7430 / 亮金 #C0A64C
- 核心发光亡灵魂火 #7CFF1F，强发光 #66F000，中发光 #A8FF4A
- 文字：主标题月光骨白 #CFEA78，面板标题 #D0E46A，大数值 #CFEA7B，正文 #B8AF87，时间 #8D855F
- 要求：魔兽世界级别的厚重金属质感、多层绿火微光、清晰的信息层级、呼吸感留白、不拥挤、不糊、不廉价

请严格评估这张截图，指出：
1. 整体观感打分（0-10）
2. 具体问题（布局、对齐、留白、层级、颜色、对比度、可读性、质感缺失）
3. 最值得优先修的 3 件事

只输出 JSON，不要解释文字：
{"file":"文件名","score":0-10,"verdict":"pass|needs_work|fail",
 "good":["..."],"issues":["..."],"top3":["..."]}
"""


def api_key():
    with open(CRED, "r", encoding="utf-8") as f:
        for line in f:
            m = re.match(r"\s*EASYCLIPROXYAPI_API_KEY:\s*(\S+)", line)
            if m:
                return m.group(1)
    raise RuntimeError("api key not found")


def to_data_url(path):
    ext = os.path.splitext(path)[1].lower().lstrip(".")
    mime = {"png": "image/png", "jpg": "image/jpeg", "jpeg": "image/jpeg", "webp": "image/webp"}[ext]
    with open(path, "rb") as f:
        return "data:%s;base64,%s" % (mime, base64.b64encode(f.read()).decode())


def main():
    files = sys.argv[1:]
    if not files:
        print("usage: qa_ui.py <png...>")
        return 1
    content = [{"type": "text", "text": RUBRIC}]
    for f in files:
        content.append({"type": "text", "text": "截图: " + os.path.basename(f)})
        content.append({"type": "image_url", "image_url": {"url": to_data_url(f)}})

    body = json.dumps({
        "model": MODEL,
        "messages": [{"role": "user", "content": content}],
        "max_tokens": 3000,
        "temperature": 0.2,
    }).encode("utf-8")

    req = urllib.request.Request(
        GATEWAY, data=body,
        headers={"Authorization": "Bearer " + api_key(), "Content-Type": "application/json"},
        method="POST")
    with urllib.request.urlopen(req, timeout=240) as resp:
        data = json.loads(resp.read().decode("utf-8"))
    text = data["choices"][0]["message"]["content"] or ""
    m = re.search(r"[\{\[].*[\}\]]", text, re.S)
    out = m.group(0) if m else text
    # 落盘避免控制台编码问题
    dst = os.path.join(os.path.dirname(os.path.abspath(files[0])), "qa_result.json")
    with open(dst, "w", encoding="utf-8") as f:
        f.write(out)
    print("RESULT ->", dst)
    return 0


if __name__ == "__main__":
    sys.exit(main())
