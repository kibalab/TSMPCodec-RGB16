[한국어](README.ko.md) | [English](README.en.md) | **日本語**

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
- `com.kibalab.tsmp.core` 1.0.0 以降
- Unity 2022.3
- VRChat で使用する場合のみ Worlds SDK 3.9.0 以降が必要です。通常の Unity には不要です。

## インストール

VRChat Creator Companion で VPM リポジトリを追加します。

```text
https://vpm.kiba.red/
```

その後、`TSMP Core` と `TSMP Codec RGB16` をインストールします。

通常の Unity では Unity Package Manager で Core 1.0.0、基本 codec の Luma4、この codec をインストールします。ローカル checkout は各 package.json を Add package from disk で追加できます。VRCSDK は不要です。UPM は Core 1.0.0 を指定し、VPM は Core 1.0.0 以降を許可します。

## 使い方

1. Core パッケージの `Packages/com.kibalab.tsmp.core/Samples/TSMPController.prefab` をシーンに配置します。
2. `TSMPSetup` の Codec タブで自動検出された `RGB16` を選択します。
3. 通常の Unity と VRChat の両方で codec と material が自動準備されます。変換メニューは不要です。

## リリース状態

RGB16 2.0.0 は前回の正式版 1.0.0 以降のベータ変更をすべて統合した正式リリースです。Core 1.0.0 を先にインストールしてください。VCC で試験版表示を有効にする必要はありません。

## ライセンス

MIT License. Copyright (c) 2026 KIBA_Labs.

## 準備 API の互換性

このリリースには Core 1.0.0 とそのコーデック準備・出力 API が必要です。コーデックをインストールする前に Core を更新してください。準備マテリアルがない場合は従来のシェーダー経路を使用できますが、互換性のない Core API を補うことはできません。

UPM は Core 1.0.0、VPM は Core >=1.0.0 を指定します。ローカル/ディスクまたは Git インストールでは、プロジェクトの依存関係に Core も直接指定してください。パッケージのメタデータだけでは UPM は GitHub から Core を取得しません。VRChat Worlds SDK は VPM のみの依存関係です。
