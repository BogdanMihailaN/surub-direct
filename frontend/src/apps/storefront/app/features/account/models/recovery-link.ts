export interface RecoveryLink {
  userId: string;
  token: string;
}
export function parseRecoveryLink(fragment: string): RecoveryLink | null {
  // Angular can normalize encoded plus signs in the fragment; they are token data, not spaces.
  const params = new URLSearchParams(fragment.replace(/^#/, '').replace(/\+/g, '%2B'));
  const userId = params.get('userId'),
    token = params.get('token');
  if (
    !userId?.trim() ||
    userId.length > 450 ||
    !token?.trim() ||
    token.length > 4096 ||
    params.getAll('userId').length !== 1 ||
    params.getAll('token').length !== 1
  )
    return null;
  return { userId, token };
}
