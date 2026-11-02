# -*- coding: utf-8 -*-
"""
贴图视觉质检：把图片发给多模态模型（默认 gemini-3.8-flash-high），
按游戏美术总监的标准判定是否可用，输出结构化结论。
用法: python qa_assets.py <file1> [file2 ...]
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

RUBRIC = """你是资深游戏 UI 美术总监，正在验收一套暗黑哥特/绿火毁灭术风格的桌面 RPG 界面贴图。
逐张判定，标准：
1. 风格是否贴合：暗黑哥特、黑石+暗古铜金、亡灵魂火绿 (#7CFF1F)、魔兽世界级别质感；
2. 是否可工程化使用：该素材会被 CSS 直接使用（九宫格边框 / 平铺底纹 / 按钮底板 / 发光叠加），
   要求：主体居中、边缘干净、没有文字或水印、没有多余装饰溢出画面、四边可预测；
3. 明显缺陷：糊、低对比、噪点脏、主体不完整、被裁切、色偏。

只输出 JSON 数组，不要任何解释文字，格式：
[{"file":"文件名","pass":true/false,"score":0-10,"role_guess":"你判断它实际适合当什么","issues":["..."],"fix":"一句话改进建议"}]
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
    mime = {"png": "image/png", "webp": "image/webp", "jpg": "image/jpeg", "jpeg": "image/jpeg"}[ext]
    with open(path, "rb") as f:
        return "data:%s;base64,%s" % (mime, base64.b64encode(f.read()).decode())


def main():
    files = sys.argv[1:]
    if not files:
        print("usage: qa_assets.py <files...>")
        return 1
    content = [{"type": "text", "text": RUBRIC + "\n待检文件: " + ", ".join(os.path.basename(f) for f in files)}]
    for f in files:
        content.append({"type": "text", "text": "文件名: " + os.path.basename(f)})
        content.append({"type": "image_url", "image_url": {"url": to_data_url(f)}})

    body = json.dumps({
        "model": MODEL,
        "messages": [{"role": "user", "content": content}],
        "max_tokens": 2048,
        "temperature": 0.2,
    }).encode("utf-8")

    req = urllib.request.Request(
        GATEWAY, data=body,
        headers={"Authorization": "Bearer " + api_key(), "Content-Type": "application/json"},
        method="POST")
    with urllib.request.urlopen(req, timeout=180) as resp:
        data = json.loads(resp.read().decode("utf-8"))
    text = data["choices"][0]["message"]["content"] or ""
    m = re.search(r"\[.*\]", text, re.S)
    print(m.group(0) if m else text)
    return 0


if __name__ == "__main__":
    sys.exit(main())
