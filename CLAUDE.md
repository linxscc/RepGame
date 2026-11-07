# RepGame（Unity 客户端）

## UI 设计守则（强制）

新建或修改任何 UI 界面前，**必须先阅读并遵守 [Docs/UI设计守则.md](Docs/UI设计守则.md)**：

- 界面保持简洁，详情一律通过点击/悬停进一步查看（L0 常驻 → L1 摘要 → L2 详情弹窗 → L3 Tooltip）
- 动手写代码前，先输出该界面的“信息分层表”并完成自检
- 元素挂到 `ChessBattleScreen` 规定的层（Base / Drag / Popup / Fx / Tooltip），不要把详情和列表平铺在同一层级
- 美术风格见 `../UIConcept/STYLE.md`
