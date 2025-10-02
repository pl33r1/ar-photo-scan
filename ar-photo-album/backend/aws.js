import { S3Client, GetObjectCommand } from "@aws-sdk/client-s3";
import { getSignedUrl } from "@aws-sdk/s3-request-presigner";
import dotenv from "dotenv";

dotenv.config();

const region = process.env.AWS_REGION || "eu-central-1";
const bucket = process.env.S3_BUCKET || "my-photo-album-bucket";

if (!bucket) {
  throw new Error("S3_BUCKET is not set");
}

export const s3 = new S3Client({
  region,
  credentials: process.env.AWS_ACCESS_KEY_ID
    ? {
        accessKeyId: process.env.AWS_ACCESS_KEY_ID,
        secretAccessKey: process.env.AWS_SECRET_ACCESS_KEY
      }
    : undefined
});

export async function getSignedUrlForKey(key, expiresInSeconds = 300) {
  const command = new GetObjectCommand({
    Bucket: bucket,
    Key: key
  });
  return await getSignedUrl(s3, command, { expiresIn: expiresInSeconds });
}

