## AR Photo Album

Полноценный проект: backend (Node.js, Express, AWS S3 presign) + Unity клиент (AR Foundation).

### Требования

- Node.js 18+
- AWS аккаунт, S3 bucket, IAM ключи
- Unity 2021.3+ (или 2022 LTS), AR Foundation 4.x, ARKit XR Plugin (iOS), ARCore XR Plugin (Android)
- ZXing.Net for Unity (сканер QR)

---

## 1) Backend

Структура: `backend/`

Файлы:
- `package.json` — зависимости, скрипты
- `server.js` — Express сервер
- `aws.js` — генерация presigned URL
- `manifest.json` — описание альбома
- `generate_qr.js` — генерация QR-кода для манифеста

### Установка и запуск

```bash
cd ar-photo-album/backend
npm install
# Переменные окружения (создайте .env)
# AWS_REGION=eu-central-1
# S3_BUCKET=my-photo-album-bucket
# AWS_ACCESS_KEY_ID=...
# AWS_SECRET_ACCESS_KEY=...
# PORT=3000
# APP_BASE_URL=https://app.example.com
# QR_ALBUM_ID=ALB123

npm start
# Server running on http://localhost:3000
```

### Маршруты

- `GET /manifest/:albumId` — возвращает JSON манифест для альбома.
- `GET /video/:albumId/:photoId` — возвращает `{ "url": "<presigned s3 url>", "expires_in": 300, "key": "..." }`.

### Загрузка видео в S3

- Положите видео в бакет по ключам из `manifest.json`, например:
  - `videos/ALB123/p001.mp4`
  - `videos/ALB123/p002.mp4`
- Убедитесь, что у роли/пользователя есть `s3:GetObject` для бакета.
- CORS для S3 не требуется при presigned URL, но CloudFront может потребоваться для раздачи изображений.

### Генерация QR

```bash
npm run qr
# создаст qr-ALB123.png указывающий на ${APP_BASE_URL}/manifest/ALB123
```

---

## 2) Unity клиент

Структура: `unity-client/`

Файлы:
- `Assets/Scripts/ManifestManager.cs`
- `Assets/Scripts/ARImageVideoController.cs`
- `Assets/Scripts/QRCodeScanner.cs`
- `Assets/Prefabs/VideoPrefab.prefab`

### Настройка сцены

1. Создайте сцену.
2. Добавьте `AR Session`, `AR Session Origin` (с `AR Camera`).
3. На `AR Session Origin` добавьте `AR Tracked Image Manager` и назначьте `Reference Image Library` с изображениями страниц/фото альбома.
   - Имя каждой reference image должно совпадать с `id` в `manifest.json` (например, `p001`, `p002`).
   - Укажите физический размер, соответствующий печатному (можно использовать `physical_width_m`/`physical_height_m` из манифеста).
4. Добавьте пустой объект `Managers`, повесьте `ManifestManager` и скрипт, который вызовет:
   ```csharp
   StartCoroutine(manifestManager.LoadManifest("http://<backend-host>:3000/manifest/ALB123"));
   ```
5. На `AR Session Origin` добавьте `ARImageVideoController`, укажите `TrackedImageManager` и `VideoPrefab` (из `Assets/Prefabs/VideoPrefab.prefab`).
6. Импортируйте ZXing.Net for Unity и добавьте `QRCodeScanner` по необходимости (используйте `OnUrlDetected` чтобы получить URL манифеста и вызвать `LoadManifest`).

### Кэширование видео

Скрипт сохраняет видео в `Application.persistentDataPath/videos/<albumId>/<photoId>.mp4`. При последующих распознаваниях видео берётся из локального кэша.

### Сборка

- iOS: активируйте ARKit, добавьте камеру, запросите Camera Usage Description в `Player Settings`.
- Android: включите ARCore, добавьте камеру и микрофон, Internet permissions.
- Убедитесь, что устройство и сервер в одной сети или используйте публичный адрес/домены.

---

## 3) CloudFront/CDN для изображений

`ref_image_url` может раздаваться через CloudFront/сторонний CDN. Для AR Tracked Image Manager изображения должны быть в `Reference Image Library`. Поддержка runtime-добавления изображений требует `MutableRuntimeReferenceImageLibrary` и платформенных ограничений — в базовой версии используйте статическую библиотеку.

---

## 4) Проверка

- Запустите backend: `npm start`.
- В Unity в `ManifestManager.LoadManifest` передайте `http://<ip>:3000/manifest/ALB123`.
- Наведите камеру на печатное изображение, соответствующее `p001`/`p002`.
- Видео скачивается, кэшируется и проигрывается на плоскости `VideoPrefab`.


