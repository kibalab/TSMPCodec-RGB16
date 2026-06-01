**한국어** | [English](README.en.md) | [日本語](README.md)

# TSMP Codec RGB16

RGB16은 RGB 채널을 사용해 TSMP 심볼을 더 높은 밀도로 기록하는 코덱입니다. Luma4보다 더 많은 payload를 담을 수 있지만, 캡처/송출 경로의 색상 보존 품질에 더 민감합니다.

## 특징

- RGB 기반 16-bit 계열 TSMP 심볼
- Luma4보다 높은 데이터 밀도
- 채널 비트 배분을 사용하는 RGB16/variable decode 경로 포함
- 선명한 색상 보존이 가능한 화면 송출 경로에 적합
- `TSMPSetup` Codec 탭에서 자동 검색

## 요구 사항

- TSMP Core: https://github.com/kibalab/TSMP-Core
- `com.kibalab.tsmp.core` 0.0.1 이상
- VRChat Worlds SDK 3.9.0 이상

## 설치

VRChat Creator Companion에서 VPM 저장소를 추가합니다.

```text
https://vpm.kiba.red/
```

그 다음 `TSMP Core`와 `TSMP Codec RGB16`을 설치합니다.

## 사용 방법

1. Core 패키지의 `Packages/com.kibalab.tsmp.core/Samples/TSMPController.prefab`을 씬에 배치합니다.
2. `TSMPSetup`의 Codec 탭에서 `Refresh Codecs`를 누릅니다.
3. `RGB16`을 선택합니다.
4. `Apply Setup`을 실행합니다.

## 배포 상태

현재 beta 단계이며 패키지 버전과 Git 태그는 `v0.0.x-beta.x` 형식을 사용합니다.

## 라이선스

MIT License. Copyright (c) 2026 KIBA_Labs.
