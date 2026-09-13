type TokenProvider = () => Promise<string | null>;

let provider: TokenProvider | undefined;

export function setClerkTokenProvider(nextProvider: TokenProvider | undefined) {
  provider = nextProvider;
}

export async function getClerkToken() {
  const token = await provider?.();
  if (!token) throw new Error('Sua sessão expirou. Entre novamente para continuar.');
  return token;
}
