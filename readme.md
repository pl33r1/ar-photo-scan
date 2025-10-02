## Руководство по использованию AR Photo Album

Этот документ — практическое руководство для разработчика/оператора по запуску и эксплуатации проекта.

### 1. Состав проекта

- `backend/` — Node.js сервер (Express) для:
  - выдачи `manifest` по альбому,
  - генерации presigned URL на видео в S3,
  - генерации QR-кода для ссылки на `manifest`.
- `unity-client/` — Unity проект с AR Foundation:
  - распознает печатные изображения (tracked images),
  - кэширует и воспроизводит видео поверх распознанной картинки,
  - умеет сканировать QR (ZXing) и подгружать `manifest` по URL.

### 2. Быстрый старт

1) Клонируйте/распакуйте проект в удобную директорию: `ar-photo-album/`.

2) Запустите backend:
```bash
cd ar-photo-album/backend
cp .env.example .env   # если есть шаблон; иначе создайте .env по примеру ниже
npm install
npm start
# Ожидайте: Server running on http://localhost:3000
```

Пример `.env`:
```
AWS_REGION=eu-central-1
S3_BUCKET=my-photo-album-bucket
AWS_ACCESS_KEY_ID=YOUR_KEY
AWS_SECRET_ACCESS_KEY=YOUR_SECRET
PORT=3000
APP_BASE_URL=https://app.example.com
QR_ALBUM_ID=ALB123
```

3) Подготовьте контент в S3:
- Загрузите файлы:
  - `videos/ALB123/p001.mp4`
  - `videos/ALB123/p002.mp4`
- Проверьте IAM: доступ `s3:GetObject` на соответствующие ключи в бакете.

4) Проверьте `manifest.json` в `backend/`:
- `album_id` должен совпадать с вашим (например, `ALB123`).
- `images[].id` должны совпадать с именами элементов в `Reference Image Library` в Unity (например, `p001`, `p002`).

5) Сгенерируйте QR (опционально):
```bash
npm run qr
# qr-ALB123.png будет вести на ${APP_BASE_URL}/manifest/ALB123
```

6) Откройте Unity проект:
- Путь: `ar-photo-album/unity-client/`
- Установите пакеты:
  - AR Foundation 4.x
  - ARKit XR Plugin (iOS), ARCore XR Plugin (Android)
  - ZXing.Net for Unity (из Unity Asset Store или UPM-портов)
- В сцене:
  - Добавьте `AR Session`, `AR Session Origin` (с `AR Camera`), `AR Tracked Image Manager`.
  - Подвяжите `Reference Image Library` (изображения, которые будут в печати).
  - Добавьте `ManifestManager` (вызовите `StartCoroutine(manifestManager.LoadManifest("http://<ip>:3000/manifest/ALB123"));`)
  - Добавьте `ARImageVideoController`, пропишите `backendBaseUrl` (например, `http://<ip>:3000`), и укажите `TrackedImageManager` и `VideoPrefab`.

7) Тест на устройстве:
- Соберите на iOS/Android.
- Убедитесь, что устройство видит backend по сети (`http://<ваш-ip>:3000`).
- Наведите камеру на печатное изображение (из `Reference Image Library`).
- Видео загрузится, закэшируется и начнет воспроизведение поверх изображения.

### 3. Частые вопросы (FAQ)

- Видео не воспроизводится:
  - Проверьте, что `GET /video/:albumId/:photoId` возвращает валидный `url`.
  - Убедитесь, что формат видео поддерживается устройством (H.264/AAC, mp4).
  - На iOS иногда требуется явный `Prepare()` и ожидание `isPrepared` — уже сделано в скрипте.
- Трекинг не срабатывает:
  - Имя `reference image` в библиотеке должно совпадать с `images[].id`.
  - Проверьте реальный физический размер изображения и освещение.
- Долгая загрузка:
  - В первый раз видео качается и сохраняется в кэш. Дальше играется из `Application.persistentDataPath`.
- Смена альбома:
  - Обновите `manifest.json` и перезапустите backend, либо укажите другой `albumId`.
  - Для публичной раздачи `manifest` используйте домен и HTTPS.

### 4. Обновление контента

1) Загрузите новое видео в S3 по ключу `videos/<ALBUM>/<PHOTO>.mp4`.
2) Обновите `backend/manifest.json` (названия `id`, `video_path`).
3) Перезапустите backend (если нужно) и протестируйте `GET /manifest/<ALBUM>` и `GET /video/<ALBUM>/<PHOTO>`.

### 5. Эксплуатация и прод

- Backend можно развернуть на любом Node-сервере (PM2, Docker, Nginx как reverse proxy).
- S3 + CloudFront для изображений `ref_image_url` (под печать) и статики сайта.
- Настройте HTTPS, CORS при необходимости (мобильные клиенты).
- Логи и мониторинг: CloudWatch, Sentry, и т.п.

### 6. Траблшутинг

- Проверяйте логи backend.
- Проверяйте сетевую доступность порта 3000 с устройства.
- На Android — пермишены Camera, Internet; на iOS — Camera Usage Description.
- Проверяйте кодеки видео (H.264 baseline/main + AAC).


