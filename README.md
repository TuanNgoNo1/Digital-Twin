# Digital Twin - Bai thuc hanh 2

Du an Unity mo phong bai thuc hanh dieu khien dong co bang PLC Mitsubishi FX3U. Sinh vien thuc hien dau noi, van hanh dong co, theo doi HMI va dong bo dong co ao voi du lieu telemetry RS-485.

## Chuc nang chinh

- Quy trinh thuc hanh gom 4 buoc trong scene chinh.
- Hieu ung lam noi bat day noi theo tung buoc.
- HMI hien thi trang thai dong co, RPM, chieu quay va encoder.
- Dong bo dong co ao theo telemetry thuc.
- Ho tro build WebGL va dong goi SCORM.

## Yeu cau

- Unity Hub.
- Unity Editor `6000.3.11f1`.
- Ket noi Internet trong lan mo dau de Unity Package Manager tai dependency.

Khong commit `Library`, `Temp`, `Logs` hay file build. Unity se tu tao lai cac thu muc nay.

## Mo project

1. Clone repository:

   ```powershell
   git clone https://github.com/TuanNgoNo1/Digital-Twin-BaiTH2.git
   ```

2. Mo Unity Hub, chon **Add > Add project from disk**.
3. Chon thu muc `Digital-Twin-BaiTH2`.
4. Mo bang Unity `6000.3.11f1` va cho Unity import asset/package.
5. Mo scene `Assets/Scenes/Sy_scene.unity`.
6. Bam **Play** de chay trong Editor.

Scene `Sy_scene` va `HMI_scene` da duoc khai bao trong Build Settings.

## Build WebGL

1. Vao **File > Build Profiles**.
2. Chon **Web** va **Switch Platform** neu can.
3. Kiem tra `Sy_scene` dung dau danh sach scene.
4. Chon **Build** va ghi ra mot thu muc nam ngoai project.

Project dang dung WebGL template `SCORMTemplate` trong `Assets/WebGLTemplates`.

## Ket noi he thong thuc

Project van mo va chay mo phong trong Unity khi khong co PLC. De nhan du lieu thuc, scene hien tai su dung cac endpoint:

- PLC gateway: `http://103.238.69.131:8080/plc`
- RS-485 telemetry: `http://103.238.69.131:8080/rs485/telemetry`
- Camera: duoc cau hinh tren component stream trong scene.

Gateway, driver PLC, GX Works2, camera worker va cac script van hanh server khong nam trong repository nay. Khi trien khai sang server khac, cap nhat URL trong Inspector cua `PLCController_v2` va component camera cho phu hop.

## Cau truc repository

```text
Assets/           Scene, script, prefab, model, texture va WebGL template
Packages/         Danh sach Unity package va lock file
ProjectSettings/  Cau hinh Unity, scene build va Player Settings
```

## Scene quan trong

- `Assets/Scenes/Sy_scene.unity`: scene thuc hanh chinh.
- `Assets/Scenes/HMI_scene.unity`: scene HMI.

## Luu y

- Khong commit cac thu muc do Unity sinh ra.
- Khong dua log, snapshot camera, file BAT/PowerShell hay thong tin dang nhap server vao repository.
- Neu doi URL gateway, kiem tra ca gia tri serialize trong scene va gia tri mac dinh trong `Assets/PLCController_v2.cs`.
