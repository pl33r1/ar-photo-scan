import express from "express";
import cors from "cors";
import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";
import dotenv from "dotenv";
import { getSignedUrlForKey } from "./aws.js";

dotenv.config();

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const app = express();
const port = process.env.PORT || 3000;

app.use(cors({ origin: true }));
app.use(express.json());

const manifestPath = path.join(__dirname, "manifest.json");

function loadManifest() {
  const raw = fs.readFileSync(manifestPath, "utf8");
  return JSON.parse(raw);
}

app.get("/manifest/:albumId", (req, res) => {
  try {
    const { albumId } = req.params;
    const manifest = loadManifest();

    if (!manifest || manifest.album_id !== albumId) {
      return res.status(404).json({ error: "Album not found" });
    }

    return res.json(manifest);
  } catch (err) {
    console.error("Failed to read manifest:", err);
    return res.status(500).json({ error: "Failed to load manifest" });
  }
});

app.get("/video/:albumId/:photoId", async (req, res) => {
  const { albumId, photoId } = req.params;

  try {
    const manifest = loadManifest();
    if (!manifest || manifest.album_id !== albumId) {
      return res.status(404).json({ error: "Album not found" });
    }

    const imageEntry =
      manifest.images?.find((img) => img.id === photoId) || null;
    if (!imageEntry) {
      return res.status(404).json({ error: "Photo not found" });
    }

    const key = imageEntry.video_path || `videos/${albumId}/${photoId}.mp4`;
    const url = await getSignedUrlForKey(key, 300);
    return res.json({ url, expires_in: 300, key });
  } catch (err) {
    console.error("Failed to generate presigned URL:", err);
    return res.status(500).json({ error: "Failed to generate URL" });
  }
});

app.listen(port, () => {
  console.log(`Server running on http://localhost:${port}`);
});

