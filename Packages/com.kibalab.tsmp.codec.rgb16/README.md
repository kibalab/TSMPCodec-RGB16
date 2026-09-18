# TSMP Codec RGB16

TSMP 用の RGB16 codec パッケージです。RGB チャンネルを使い、Luma4 より高い payload 密度を提供します。色を鮮明に保持できるストリーム経路での使用に向いています。

## 要件

- TSMP Core: https://github.com/kibalab/TSMP-Core
- `com.kibalab.tsmp.core` 0.3.0-beta.2 以降
- Unity 2022.3
- VRChat で使用する場合のみ Worlds SDK 3.9.0 以降が必要です。通常の Unity には不要です。

通常の Unity では Unity Package Manager で Core 0.3.0-beta.2、基本 codec の Luma4、この codec をインストールします。ローカル checkout は各 package.json を Add package from disk で追加できます。VRCSDK は不要です。UPM は Core 0.3.0-beta.2 を指定し、VPM は Core 0.3.0-beta.2 以降を許可します。

## 使い方

TSMP Core と一緒にこのパッケージをインストールし、Core の `Samples/TSMPController.prefab` をシーンに配置します。その後、`TSMPSetup` の Codec タブで `RGB16` を選択します。設定は自動適用され、SDK の有無で操作手順は変わりません。

## リリース状態

このパッケージは beta 段階で、`v0.0.x-beta.x` 形式のタグを使用します。

## 準備 API の互換性

このリリースには Core 0.3.0-beta.2 で追加された準備 API が必要です。Core 0.2.0 と 0.3.0-beta.1 には `PrepareDecode` がなく、準備マテリアルを未設定にしてもコンパイルできません。このコーデックをインストールする前に Core を更新してください。準備マテリアル不足時の従来シェーダーへの fallback は、コンパイル後にのみ機能します。

UPM にはバージョン文字列、VPM には範囲を指定します。ローカル/ディスクまたは Git インストールでは、プロジェクトの依存関係に対応 Core も直接指定します。パッケージのメタデータだけでは UPM は GitHub から Core を取得しません。VPM ベータは公開後に試験版表示を有効にし、対応バージョンを選んでください。
