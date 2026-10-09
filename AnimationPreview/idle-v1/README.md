# 霜誓者备战待机动画 · 第二版

打开 `待机动画演示.html` 即可播放，不需要联网。可以暂停、慢放、拖动时间、对比原站姿、显示骨骼/网格并导出 GIF。

本版使用原全身立绘的加权网格，制作六秒 `idle_loop`。第二版调整为重心降低、上身前倾、法杖前举的备战姿态，并加大呼吸、头发、衣摆和帽尖吊坠摆动。实际网格最大位移由第一版约 42 像素提升到约 84 像素；双脚固定、循环端点一致。闭眼素材由 GPT 图像编辑生成，动画仅采样眼部区域，其余部分使用原立绘。此次姿势调整在骨骼网格中完成，没有重新生图。

这是独立动画原型，尚未替换游戏模型。暂未制作 relaxed_loop、attack、cast、hurt、die，也未完成用于大幅动作的独立身体部件绑定。

## 工程

- `frostsworn-idle.json`：Spine 4.2 骨骼、网格和动画。
- `frostsworn-idle.atlas` 和 `assets/`：原立绘与闭眼素材。
- `build_idle.py`：生成动画数据及离线演示页；使用 Python 和 Pillow（仅读取图像尺寸）。
- `validate_idle.cjs`：使用实际 Spine runtime 检查循环衔接、脚底固定和眨眼切换。
- `preview-template.html`：演示界面源码。
- `image-edit-prompt.txt`：补图提示词。

Spine runtime 使用官方 npm `@esotericsoftware/spine-webgl@4.2.120`，附原许可证；GIF 编码使用 `gifenc@1.0.3`，附 MIT 许可证。Skeleton JSON 版本标记为 4.2.43，与教程所述游戏版本一致。

参考：[STS2 卡图和皮肤替换教程](https://tutorials.sts2modding.com/docs/05-card-art-and-skin-replacement/)、[Spine JSON 格式](https://esotericsoftware.com/spine-json-format)、[Spine Godot runtime](https://en.esotericsoftware.com/spine-godot)。游戏导入仍需对应引擎资源与场景接入。
