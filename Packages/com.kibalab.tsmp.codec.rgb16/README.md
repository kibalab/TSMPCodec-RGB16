# TSMP Codec RGB16

TSMP 用の RGB16 codec パッケージです。RGB チャンネルを使い、Luma4 より高い payload 密度を提供します。色を鮮明に保持できるストリーム経路での使用に向いています。

## 要件

- TSMP Core: https://github.com/kibalab/TSMP-Core
- `com.kibalab.tsmp.core` 0.2.0 以降
- Unity 2022.3
- VRChat で使用する場合のみ Worlds SDK 3.9.0 以降が必要です。通常の Unity には不要です。

通常の Unity では Unity Package Manager で Core 0.2.0、その依存 codec Luma4、この codec をインストールします。ローカル checkout は各 package.json を Add package from disk で追加できます。VRCSDK は不要です。UPM は Core 0.2.0 を指定し、VPM は Core 0.2.0 以降を許可します。

## 使い方

TSMP Core と一緒にこのパッケージをインストールし、Core の `Samples/TSMPController.prefab` をシーンに配置します。その後、`TSMPSetup` の Codec タブで `RGB16` を選択します。設定は自動適用され、SDK の有無で操作手順は変わりません。

## リリース状態

このパッケージは beta 段階で、`v0.0.x-beta.x` 形式のタグを使用します。
