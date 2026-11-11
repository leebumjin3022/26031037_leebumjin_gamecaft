# 더 다이스 파이트 — glc2d 적용 수정본

## 실행

`src/dicefight/dicefight.sln`을 Visual Studio에서 열고 Release 구성으로 실행합니다. .NET 9 SDK 및 Windows 데스크톱 개발 구성 요소가 필요합니다. 기존 NuGet 패키지를 복원해서 사용합니다.

## 평가 지적 사항 수정

기존 자체 GameForm/OnPaint/GDI+ 렌더링과 WinForms Timer 루프를 제거했습니다. 실행은 `AppMain → GameMain.Run → G2AppBase.Initialize/Update/Render`를 사용합니다.

| 기능 | 실제 사용 경로 |
|---|---|
| 게임 루프 | `GameMain : G2AppBase` |
| 이미지 | `GameAssets → G2Texture → G2TextureLoader → Direct2D` |
| 문자 | `GlcCanvas.Text → G2Font → DirectWrite` |
| 키보드/마우스 | `GameMain.Update → G2InputContext`, 스캔 코드 보완 포함 |
| 음악 | `GameAssets.StartMusic → G2Audio → G2AudioMp3 → XAudio2` |
| 도형/반전/배율 | glc2d가 생성한 `RenderTarget`에 직접 그리기 |

`UseWindowsForms`는 glc2d 자체의 `RenderForm`과 입력 창을 위해 필요합니다. 게임이 별도 WinForms Form이나 GDI+ 화면을 사용하는 것은 아닙니다.

## 리소스 정리

기존 707개, 219.93MiB에서 실제 사용하는 437개, 92.37MiB로 정리했습니다. 436개 PNG와 음악 1개입니다. 6프레임 구버전, 백업, sheet.png/미리보기/프롬프트, 중복 효과, 사용하지 않는 좌측 이동 이미지, 미사용 시작 로고와 웅크림 후반 프레임은 제출 대상에서 제외했습니다. 좌측 캐릭터는 오른쪽 원본을 반전합니다. 기존 파일은 별도 외부 백업에 보관했습니다.

`src/dicefight/assets.txt`가 파일 목록이며 `GameAssets.props`는 이 파일들만 빌드/배포 폴더로 복사합니다. `AssetAudit.Verify`로 누락·불필요 파일을 확인합니다. 새 리소스를 연결하면 이 두 목록도 함께 갱신합니다.

## 조작

- 1P: A/D 이동, Space 점프, S 웅크림, J 공격, W+J 상단, S+J 하단.
- 2P: 좌우 화살표 이동, 숫자패드0 점프, 아래 화살표 웅크림, 숫자패드. 공격, 위+. 상단, 아래+. 하단.
- 강공격: 상대 반대 방향+공격키를 0.5초 이상 누르고 공격키를 놓습니다.
- 점프 공격: 공중에서 공격키를 새로 누릅니다.
- 장풍: 지상에서 공격키를 0.2초 이내 두 번 누릅니다. 일반 단타에는 구분용 최대 0.2초 대기가 있습니다.
- Alt+Enter: glc2d 전체 화면 전환. Escape: 종료. 창 포커스를 잃으면 일시 정지합니다.

## 검증

`dicefight.exe --self-test`: 전투/입력/진행/리소스 61개 자동 검사.

`dicefight.exe --smoke`: 실제 glc2d 창에서 17개 장면을 렌더링하고 436개 텍스처를 로드하며 음악을 재생한 뒤 정상 종료합니다. 결과는 실행 폴더의 `glc2d-smoke.json`에 기록됩니다.

교수님이 코드를 확인할 때는 `AppMain.cs`, `GameMain.cs`, `GlcCanvas.cs`, `GameAssets.cs` 순서로 보면 실제 라이브러리 사용을 확인할 수 있습니다.
