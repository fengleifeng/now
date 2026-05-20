# 卡牌默认图标

- `ids/{卡牌Id}.svg` — 每张卡的默认立绘（与 `cards.json` 的 `Id` 对应）
- `types/{类型}.svg` — 按 `CardType` 的兜底图（resource、tool、location 等）

解析顺序见 `scripts/ui/CardArtCatalog.cs`：自定义 `IconPath` → 按 Id → 按标签 → 按类型。

重新生成全部 SVG：

```powershell
.\tools\generate_card_art.ps1
```

在 Godot 中打开项目后会自动导入为纹理。
