# glc2d 보완 사항

수업 제공 glc2d 코드를 직접 사용하며 원저작자 주석을 유지했습니다. 수정은 아래의 실행 호환성과 입력/해제 문제를 해결하는 범위입니다.

- G2AppBase: 창 포커스 조회, RenderTarget.EndDraw 결과 확인, 실행 오류를 진입점으로 전달.
- G2D2DContext: 픽셀 좌표와 맞추기 위해 D2D DPI를 96으로 설정. 화면 배율이 높은 PC에서 이중 확대되어 화면이 잘리는 현상 수정.
- G2InputContext + G2PhysicalKeyboard: Num Lock에 관계없이 숫자패드0/.를 구분하는 물리 키 입력, 프레임 사이의 빠른 클릭을 보존하는 이벤트 버퍼, 포커스 초기화와 해제.
- G2AudioMp3/G2AudioSound/G2AudioContext: SourceVoice/ MasteringVoice를 DestroyVoice로 먼저 파괴한 후 버퍼와 관리 객체를 해제. 실제 종료 시 xaudio2_9.dll의 접근 위반이 발생한 문제를 수정.

음성 보이스 파괴가 끝나면 오디오 데이터 접근이 종료된다는 Microsoft 설명에 맞춰 해제 순서를 정리했습니다.
https://learn.microsoft.com/en-us/windows/win32/api/xaudio2/nf-xaudio2-ixaudio2voice-destroyvoice

게임 렌더링을 System.Drawing.Graphics나 오프스크린 GDI 이미지로 대체하지 않습니다. System.Drawing의 Color/RectangleF/Size는 색상·좌표 자료형으로만 사용합니다.
