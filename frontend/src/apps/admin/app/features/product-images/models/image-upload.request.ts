import { ImageDetailsRequest } from './image-details.request';
export interface ImageUploadRequest extends ImageDetailsRequest {
  file: File;
}
