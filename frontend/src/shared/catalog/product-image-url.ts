export function productImageUrl(
  productId: string,
  imageId: string,
  thumbnail = false,
  admin = false,
) {
  return `/api/v1/${admin ? 'admin/' : ''}products/${encodeURIComponent(productId)}/images/${encodeURIComponent(imageId)}/${thumbnail ? 'thumbnail' : 'full'}`;
}
