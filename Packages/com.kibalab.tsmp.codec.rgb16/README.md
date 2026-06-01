# TSMP Codec RGB16

TSMP 用の RGB16 codec パッケージです。RGB チャンネルを使い、Luma4 より高い payload 密度を提供します。色を鮮明に保持できるストリーム経路での使用に向いています。

## 要件

- TSMP Core: https://github.com/kibalab/TSMP-Core
- `com.kibalab.tsmp.core` 0.0.1 以降
- VRChat Worlds SDK 3.9.0 以降

## 使い方

TSMP Core と一緒にこのパッケージをインストールし、Core の `Samples/TSMPController.prefab` をシーンに配置します。その後、`TSMPSetup` の Codec タブで `RGB16` を選択し、`Apply Setup` を実行します。

## リリース状態

このパッケージは beta 段階で、`v0.0.x-beta.x` 形式のタグを使用します。
