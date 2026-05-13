# Project Setup Guide

## 기준 환경

- Unity: `Unity 6 LTS`
- Template: `Universal 3D` 또는 `URP`
- Network: `Photon Fusion`
- Input: `Unity Input System`

## 1. Unity 프로젝트 생성

- Unity Hub에서 새 프로젝트 생성
- 템플릿은 URP 기반 3D 템플릿 선택
- 프로젝트 이름은 `BlackoutShift` 권장
- 저장 위치는 팀원이 합의한 작업 폴더로 통일

## 2. 첫 폴더 구조

프로젝트 생성 직후 아래 구조를 만든다.

- `Assets/Scenes`
- `Assets/Scripts`
- `Assets/Prefabs`
- `Assets/Materials`
- `Assets/Art`
- `Assets/Audio`
- `Assets/UI`
- `Assets/Network`
- `Assets/Plugins`

## 3. 기본 씬 구성

- `Bootstrap` 또는 `MainMenu` 씬
- `TestRoom` 씬

초기에는 `TestRoom` 하나만으로 멀티 접속을 검증해도 충분하다.

## 4. 패키지 및 기능 세팅

- Input System 활성화
- Photon Fusion 패키지 임포트
- 필요 시 TextMeshPro 기본 세팅 확인
- URP 기본 조명과 카메라 설정 점검

## 5. 첫날 구현 목표

- 플레이어 프리팹 생성
- 네트워크 스폰 연결
- 이동 구현
- 카메라 연결
- 2명 이상 접속 테스트

## 6. 첫날 완료 기준

- 호스트 생성 가능
- 클라이언트 접속 가능
- 서로의 플레이어가 보임
- 이동이 동기화됨
- 씬 재실행 시 큰 오류가 없음

## 7. 첫날 금지 사항

- 적 AI 구현
- 경보 시스템 구현
- 감옥 시스템 구현
- 고급 UI 제작
- 에셋 디테일 정리

## 8. 둘째 날 넘어갈 조건

- 멀티 접속이 재현 가능함
- 플레이어 이동과 스폰이 안정적임
- 테스트용 상호작용 오브젝트 1개를 네트워크 환경에서 다룰 수 있음
