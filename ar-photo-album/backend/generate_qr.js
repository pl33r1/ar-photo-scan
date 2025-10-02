import QRCode from "qrcode";
import dotenv from "dotenv";

dotenv.config();

const albumId = process.env.QR_ALBUM_ID || "ALB123";
const baseAppUrl = process.env.APP_BASE_URL || "https://app.example.com";

const manifestUrl = `${baseAppUrl}/manifest/${albumId}`;
const outFile = `./qr-${albumId}.png`;

QRCode.toFile(outFile, manifestUrl, { scale: 10, margin: 2 }, (err) => {
  if (err) throw err;
  console.log(`QR code saved to ${outFile} for URL: ${manifestUrl}`);
});

