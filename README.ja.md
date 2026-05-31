[한국어](README.md) | [English](README.en.md) | **日本語**

# TSMP Codec RGB16

RGB16 は RGB チャンネルを使い、Luma4 より高い密度で TSMP シンボルを記録する codec です。より多くの payload を扱えますが、キャプチャや配信経路の色再現性に強く依存します。

## 特徴

- RGB ベースの 16-bit TSMP シンボル
- Luma4 より高いデータ密度
- RGB16 と variable channel-bit のデコード経路
- 色を鮮明に保持できるストリーム経路に適合
- `TSMPSetup` の Codec タブで自動検出

## 要件

- TSMP Core: https://github.com/kibalab/TSMP-Core
- `com.kibalab.tsmp.core` 0.0.1 以降
- VRChat Worlds SDK 3.9.0 以降

## インストール

VRChat Creator Companion で VPM リポジトリを追加します。

```text
https://vpm.kiba.red/
```

その後、`TSMP Core` と `TSMP Codec RGB16` をインストールします。

## 使い方

1. Core パッケージの `Packages/com.kibalab.tsmp.core/Samples/TSMPController.prefab` をシーンに配置します。
2. `TSMPSetup` の Codec タブで `Refresh Codecs` を押します。
3. `RGB16` を選択します。
4. `Apply Setup` を実行します。

## リリース状態

このパッケージは beta 段階で、`v0.0.x-beta.x` 形式のタグを使用します。

## ライセンス

MIT License. Copyright (c) 2026 KIBA_Labs.
