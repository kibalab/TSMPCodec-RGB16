# TSMP Codec RGB16

TSMP 用の RGB16 codec パッケージです。RGB チャンネルを使い、Luma4 より高い payload 密度を提供します。色を鮮明に保持できるストリーム経路での使用に向いています。

## 要件

- TSMP Core: https://github.com/kibalab/TSMP-Core
- `com.kibalab.tsmp.core` 1.0.0 以降
- Unity 2022.3
- VRChat で使用する場合のみ Worlds SDK 3.9.0 以降が必要です。通常の Unity には不要です。

通常の Unity では Unity Package Manager で Core 1.0.0、基本 codec の Luma4、この codec をインストールします。ローカル checkout は各 package.json を Add package from disk で追加できます。VRCSDK は不要です。UPM は Core 1.0.0 を指定し、VPM は Core 1.0.0 以降を許可します。

## 使い方

TSMP Core と一緒にこのパッケージをインストールし、Core の `Samples/TSMPController.prefab` をシーンに配置します。その後、`TSMPSetup` の Codec タブで `RGB16` を選択します。設定は自動適用され、SDK の有無で操作手順は変わりません。

## リリース状態

RGB16 2.0.0 は前回の正式版 1.0.0 以降のベータ変更をすべて統合した正式リリースです。Core 1.0.0 を先にインストールしてください。VCC で試験版表示を有効にする必要はありません。

## 準備 API の互換性

このリリースには Core 1.0.0 とそのコーデック準備・出力 API が必要です。コーデックをインストールする前に Core を更新してください。準備マテリアルがない場合は従来のシェーダー経路を使用できますが、互換性のない Core API を補うことはできません。

UPM は Core 1.0.0、VPM は Core >=1.0.0 を指定します。ローカル/ディスクまたは Git インストールでは、プロジェクトの依存関係に Core も直接指定してください。パッケージのメタデータだけでは UPM は GitHub から Core を取得しません。VRChat Worlds SDK は VPM のみの依存関係です。
